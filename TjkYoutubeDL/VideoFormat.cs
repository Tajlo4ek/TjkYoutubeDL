using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TjkYoutubeDL
{
    public class VideoFormat
    {
        public enum Formats
        {
            Unknow,

            AudioOnly,

            Video_144p,
            Video_240p,
            Video_360p,
            Video_480p,
            Video_720p,
            Video_1080p,
        }

        public enum FileExt
        {
            mp3,
            wav,

            original,
            mkv,
            mp4,
        }



        [JsonProperty("format_id")]
        public string FomatId { get; private set; }


        [JsonProperty("height")]
        public int? Height { get; private set; }

        [JsonProperty("url")]
        public string Url { get; private set; }

        [JsonProperty("ext")]
        public string Ext { get; private set; }

        [JsonProperty("format_note")]
        private string formatNote;

        public string FormatNote
        {
            get
            {
                if (formatNote == null)
                {
                    formatNote = Height.ToString();
                }
                return formatNote;
            }

            private set
            {
                formatNote = value;
            }
        }


        public override string ToString()
        {
            return FomatId + "-" + Height;
        }

    }

}
