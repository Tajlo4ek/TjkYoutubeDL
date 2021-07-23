using System;
using System.Collections.Generic;
using static TjkYoutubeDL.VideoFormat;

namespace TjkYoutubeDL.Utils
{
    public static class FormatsExtension
    {
        public static string GetStringValue(this Formats format)
        {
            switch (format)
            {
                case Formats.AudioOnly:
                    return "audio";
                case Formats.Video_144p:
                    return "144";
                case Formats.Video_240p:
                    return "240";
                case Formats.Video_360p:
                    return "360";
                case Formats.Video_480p:
                    return "480";
                case Formats.Video_720p:
                    return "720";
                case Formats.Video_1080p:
                    return "1080";
            }

            return "unknow";
        }

        public static Formats Parse(string str)
        {
            foreach (Formats format in Enum.GetValues(typeof(Formats)))
            {
                if (format.GetStringValue() == str)
                {
                    return format;
                }
            }

            return Formats.Unknow;
        }

        public static bool IsAudio(this Formats format)
        {
            return format == Formats.AudioOnly;
        }

        public static bool IsVideo(this Formats format)
        {
            return !format.IsAudio() && format != Formats.Unknow;
        }

        public static bool IsAudioExt(this FileExt fileExt)
        {
            return fileExt == FileExt.mp3 || fileExt == FileExt.wav;
        }

        public static bool IsVideoExt(this FileExt fileExt)
        {
            return !fileExt.IsAudioExt();
        }

        public static FileExt[] GetAudioExt()
        {
            var list = new List<FileExt>();

            foreach (FileExt ext in Enum.GetValues(typeof(FileExt)))
            {
                if (ext.IsAudioExt())
                {
                    list.Add(ext);
                }
            }

            return list.ToArray();
        }

        public static FileExt[] GetVideoExt()
        {
            var list = new List<FileExt>();

            foreach (FileExt ext in Enum.GetValues(typeof(FileExt)))
            {
                if (ext.IsVideoExt())
                {
                    list.Add(ext);
                }
            }

            return list.ToArray();
        }
    
    }

}
