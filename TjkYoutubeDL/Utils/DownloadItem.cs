using System;
using static TjkYoutubeDL.YoutubeDL;

namespace TjkYoutubeDL.Utils
{
    internal class DownloadItem
    {
        public VideoInfo Info { get; private set; }
        public VideoFormat.Formats FileFormat { get; private set; }
        public VideoFormat.FileExt FileExt { get; private set; }

        private readonly Action<LogType, string[]> logger;

        public DownloadItem(VideoInfo info, VideoFormat.Formats format, VideoFormat.FileExt fileExt, Action<LogType, string[]> logger)
        {
            this.Info = info;
            this.FileFormat = format;
            this.FileExt = fileExt;
            this.logger = logger;
        }

        public void Log(LogType logType, string[] args)
        {
            logger?.Invoke(logType, args);
        }

    }
}
