using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TjkYoutubeDL.Exceptions;
using TjkYoutubeDL.Utils;

namespace TjkYoutubeDL
{
    public class YoutubeDL
    {
        private const string endString = "[end]";

        private readonly string tempDirectory = Path.GetTempPath() + Path.GetRandomFileName() + "/";

        public enum LogType
        {
            Begin,
            Download,
            Convert,

            End,
            Abort,
            Wait,
        }

        private readonly Thread downloadThread;
        private readonly Thread getinfoThread;

        private readonly Queue<DownloadItem> downloadQueue;
        private readonly object queueDownloadLocker = new object();

        private readonly Queue<GetInfoItem> getInfoQueue;
        private readonly object queueGetInfoLocker = new object();

        private readonly List<Process> processes;
        private readonly object lockProcesslist = new object();

        public string YoutubeDLPath { get; private set; }

        public string FFMpegPath { get; private set; }

        public int ConnectTimeOut { get; set; } = 10;

        public string DownloadPath { get; set; }

        public bool EnableLog { get; set; }

        public YoutubeDL()
        {
            Directory.CreateDirectory(tempDirectory);

            downloadQueue = new Queue<DownloadItem>();
            getInfoQueue = new Queue<GetInfoItem>();

            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");

            downloadThread = new Thread(ThreadDownloadAction)
            {
                IsBackground = true,
                CurrentCulture = ci,
                CurrentUICulture = ci,
            };

            getinfoThread = new Thread(ThreadGetInfoAction)
            {
                IsBackground = true,
                CurrentCulture = ci,
                CurrentUICulture = ci,
            };

            EnableLog = false;

            processes = new List<Process>();

            getinfoThread.Start();
            downloadThread.Start();
        }

        public void SetYoutubeDLPath(string path)
        {
            if (!File.Exists(path))
            {
                throw new BadPathException(path);
            }
            YoutubeDLPath = Path.GetFullPath(path);
        }

        public void SetFFMpegPath(string path)
        {
            if (!File.Exists(path))
            {
                throw new BadPathException(path);
            }
            FFMpegPath = Path.GetFullPath(path);
        }

        public void CustomAsk(string fileName, string args, Func<string, bool> onDataAvailable, bool useErrorAsLog = false)
        {
            using (Process process = new Process())
            {
                lock (lockProcesslist)
                {
                    processes.Add(process);
                }

                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;

                PrintLog("start");
                process.Start();

                StreamReader reader = useErrorAsLog ? process.StandardError : process.StandardOutput;

                bool? resAnalysis = true;

                while (!reader.EndOfStream)
                {
                    string output = reader.ReadLine();
                    resAnalysis = onDataAvailable?.Invoke(output);
                    if (resAnalysis != true)
                    {
                        process.Kill();
                        break;
                    }
                }

                if (resAnalysis == true && !useErrorAsLog)
                {
                    StreamReader errorReader = process.StandardError;
                    while (!errorReader.EndOfStream)
                    {
                        string output = errorReader.ReadToEnd();
                        resAnalysis = onDataAvailable?.Invoke(output);
                    }
                }

                PrintLog("end");

                process.WaitForExit();
                onDataAvailable?.Invoke(endString);

                lock (lockProcesslist)
                {
                    processes.Remove(process);
                }
            }

        }

        public void GetInfo(IEnumerable<string> urls, Action<VideoInfo> onDataGet, Action<bool> onEnd)
        {
            lock (queueGetInfoLocker)
            {
                getInfoQueue.Enqueue(new GetInfoItem(urls, onDataGet, onEnd));
            }
        }

        public void GetInfo(string url, Action<VideoInfo> onDataGet, Action<bool> onEnd)
        {
            lock (queueGetInfoLocker)
            {
                GetInfo(new string[] { url }, onDataGet, onEnd);
            }
        }

        private void GetInfo(GetInfoItem getInfoItem)
        {
            bool isOk = true;

            foreach (var url in getInfoItem.Urls)
            {
                var args = "-iqsj " + url + " --socket-timeout " + ConnectTimeOut;

                CustomAsk(YoutubeDLPath, args, (str) =>
                {
                    PrintLog("[debug] " + str);

                    if (str.Contains("ERROR:"))
                    {
                        isOk = false;
                        return false;
                    }
                    else if (str.Length > 10 && !str.Contains("WARNING"))
                    {
                        var vInfo = VideoInfo.Parse(str);
                        if (vInfo.IsLive != true)
                        {
                            getInfoItem.OnDataGet(vInfo);
                        }
                    }
                    return true;
                });
            }

            getInfoItem?.OnEnd(isOk);
        }

        public void Download(VideoInfo info, VideoFormat.Formats format, VideoFormat.FileExt fileExt, Action<LogType, string[]> callback)
        {
            if (format == VideoFormat.Formats.Unknow)
            {
                callback?.Invoke(LogType.Abort, new string[] { "error" });
                return;
            }

            callback?.Invoke(LogType.Wait, new string[] { "wait" });

            lock (queueDownloadLocker)
            {
                downloadQueue.Enqueue(new DownloadItem(info, format, fileExt, callback));
            }

        }

        private void Download(DownloadItem downloadItem)
        {
            downloadItem.Log(LogType.Begin, new string[] { "starting download" });

            var sbArgs = new StringBuilder();

            var info = downloadItem.Info;
            var fileExt = downloadItem.FileExt;
            var format = downloadItem.FileFormat;

            sbArgs.Append(info.Url);
            sbArgs.Append(" ");

            if (format.IsAudio())
            {
                sbArgs.Append("-x ");
            }
            else
            {
                sbArgs.Append("-f ");
                sbArgs.Append(info.GetFormatStr(format));
                sbArgs.Append(" ");
            }

            sbArgs.Append("--ffmpeg-location ");
            sbArgs.Append(FFMpegPath);
            sbArgs.Append(" ");

            var tempFileName = Path.GetRandomFileName();

            sbArgs.Append("-o ");
            sbArgs.Append(tempDirectory);
            sbArgs.Append("/");
            sbArgs.Append(tempFileName);
            sbArgs.Append(".%(ext)s ");

            PrintLog("[debug]" + sbArgs.ToString());

            bool haveError = false;

            CustomAsk(YoutubeDLPath, sbArgs.ToString(), (str) =>
            {
                PrintLog("[debug] " + str);

                if (str.Contains("ERROR"))
                {
                    downloadItem.Log(LogType.Abort, new string[] { "downloading error" });
                    haveError = true;
                    return false;
                }
                else if (str.Contains("[download]"))
                {
                    var progress = GetMatch(str, @"[0-9.]*%");
                    var speed = GetMatch(str, @"at.*s");
                    var time = GetMatch(str, @"ETA \S+");

                    if (progress.Length != 0 && speed.Length != 0)
                    {
                        progress = progress.Substring(0, progress.Length - 1);
                        speed = speed.Substring(3);
                        time = time.Substring(3);

                        downloadItem.Log(LogType.Download, new string[] { progress, speed, time });
                    }
                }
                return true;
            });
            sbArgs.Clear();

            var files = Directory.GetFiles(tempDirectory, tempFileName + ".*");
            if (files.Length != 1)
            {
                downloadItem.Log(LogType.Abort, new string[] { "downloading error" });
                return;
            }

            var downloadFullPath = files[0];
            var downloadExt = Path.GetExtension(downloadFullPath);
            var downloadName = Path.GetFileNameWithoutExtension(downloadFullPath);

            if (haveError)
            {
                DeleteFile(downloadFullPath);
                return;
            }

            string convertFilePath = tempDirectory + downloadName + "." + fileExt.ToString();

            if (fileExt == VideoFormat.FileExt.original)
            {
                convertFilePath = downloadFullPath;
            }
            else if (downloadExt != "." + fileExt.ToString())
            {
                sbArgs.Append("-i ");
                sbArgs.Append(downloadFullPath);
                sbArgs.Append(" ");
                sbArgs.Append(convertFilePath);
                sbArgs.Append(" -stats -v warning");

                CustomAsk(FFMpegPath, sbArgs.ToString(), (str) =>
                {
                    PrintLog("[debug] " + str);
                    var dataStr = GetMatch(str, @"size=.*x");

                    if (dataStr.Length == 0)
                    {
                        return false;
                    }

                    var timeStr = GetMatch(str, @"\d\d:\d\d:\d\d\.\d\d");
                    var time = TimeSpan.Parse(timeStr).TotalSeconds;

                    var speedStr = GetMatch(str, @"speed=.*x");
                    var secCount = (int)Math.Round((info.Duration - time) / float.Parse(speedStr.Substring(6, speedStr.Length - 7)));

                    var procent = (time / info.Duration * 100).ToString();
                    downloadItem.Log(LogType.Convert, new string[] { procent, ConvertTime(secCount) });

                    return true;
                }, true);

                downloadItem.Log(LogType.Convert, new string[] { "100", "0" });
            }


            if (!haveError)
            {
                haveError = !MoveFile(convertFilePath, this.DownloadPath + "/" + RemoveIllegalChars(info.Title) + Path.GetExtension(convertFilePath));
            }

            DeleteFile(downloadFullPath);
            DeleteFile(convertFilePath);


            downloadItem.Log(haveError ? LogType.Abort : LogType.End, new string[] { });

        }

        private string ConvertTime(int secCount)
        {
            var sCount = secCount % 60;
            var mCount = (secCount / 60) % 60;
            var hCount = secCount / 3600;

            StringBuilder sb = new StringBuilder();

            if (hCount > 0)
            {
                if (hCount >= 10)
                {
                    sb.Append(hCount);
                }
                else
                {
                    sb.Append("0");
                    sb.Append(hCount);
                }
                sb.Append("h.");
            }

            if (mCount > 0 || hCount > 0)
            {
                if (mCount >= 10)
                {
                    sb.Append(mCount);
                }
                else
                {
                    sb.Append("0");
                    sb.Append(mCount);
                }
                sb.Append("m.");
            }

            if (sCount >= 10 || (hCount == 0 && mCount == 0))
            {
                sb.Append(sCount);
            }
            else
            {
                sb.Append("0");
                sb.Append(sCount);
            }
            sb.Append("s");

            return sb.ToString();
        }

        private void ThreadDownloadAction()
        {
            while (true)
            {
                DownloadItem current = null;

                lock (queueDownloadLocker)
                {
                    if (downloadQueue.Count > 0)
                    {
                        current = downloadQueue.Dequeue();
                    }
                }

                if (current != null)
                {
                    Download(current);
                }


                Thread.Sleep(50);
            }
        }

        private void ThreadGetInfoAction()
        {
            while (true)
            {
                GetInfoItem current = null;

                lock (getInfoQueue)
                {
                    if (getInfoQueue.Count > 0)
                    {
                        current = getInfoQueue.Dequeue();
                    }
                }

                if (current != null)
                {
                    GetInfo(current);
                }


                Thread.Sleep(50);
            }
        }

        private string GetMatch(string str, string reg)
        {
            Regex regex = new Regex(reg);
            MatchCollection matches = regex.Matches(str);
            if (matches.Count > 0)
            {
                return matches[0].Value;
            }
            return "";
        }

        private bool MoveFile(string fromPath, string toPath)
        {
            if (!File.Exists(fromPath))
            {
                return false;
            }

            if (File.Exists(toPath))
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(toPath);
                string extension = Path.GetExtension(toPath);
                string path = Path.GetDirectoryName(toPath);
                string newFullPath = toPath;
                int count = 1;

                while (File.Exists(newFullPath))
                {
                    string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                    newFullPath = Path.Combine(path, tempFileName + extension);
                }
                toPath = newFullPath;
            }

            File.Move(fromPath, toPath);
            return true;
        }

        private void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string RemoveIllegalChars(string path)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(path, "");
        }

        ~YoutubeDL()
        {
            KillAllProc();

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        private void KillAllProc()
        {
            lock (lockProcesslist)
            {
                foreach (var process in processes)
                {
                    try
                    {
                        var p = Process.GetProcessById(process.Id);
                        p.Kill();
                        p.WaitForExit();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        private void PrintLog(string log)
        {
            if (EnableLog)
            {
                Debug.Indent();
                Debug.WriteLine(log);
                Debug.Unindent();
            }
        }

    }

}
