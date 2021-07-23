using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TjkYoutubeDL
{
    public class ThumbnailInfo
    {
        [JsonProperty("url")]
        public string Url { get; private set; }

        [JsonProperty("width")]
        public int Width { get; private set; }

        [JsonProperty("height")]
        public int Height { get; private set; }

        public override string ToString()
        {
            return string.Format("[url: {0}, size: {1}x{2}]", Url, Width, Height);
        }
    }

}
