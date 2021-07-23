using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TjkYoutubeDL.Utils;

namespace TjkYoutubeDL
{
    public class VideoInfo
    {

        [JsonProperty("title")]
        public string Title { get; private set; }

        [JsonProperty("id")]
        public string Id { get; private set; }

        [JsonProperty("thumbnails")]
        public ThumbnailInfo[] Thumbnails { get; private set; }

        [JsonProperty("webpage_url")]
        public string Url { get; private set; }

        [JsonProperty("formats")]
        private readonly VideoFormat[] _availableVideoFormats = null;

        [JsonProperty("is_live")]
        public bool? IsLive { get; private set; }

        [JsonProperty("duration")]
        public float Duration { get; private set; }

        [JsonProperty("playlist_id")]
        public string Playlist { get; private set; }

        private List<VideoFormat.Formats> _availableFormats;

        public IReadOnlyCollection<VideoFormat.Formats> AvailableFormats { get { return _availableFormats.AsReadOnly(); } }

        public string GetFormatStr(VideoFormat.Formats format)
        {
            if (!format.IsAudio() && format != VideoFormat.Formats.Unknow)
            {
                var videoId = string.Empty;
                var audioId = string.Empty;

                int height = int.Parse(format.GetStringValue());
                foreach (var f in _availableVideoFormats)
                {
                    if (f.Height == height || f.FormatNote.Contains(height.ToString()))
                    {
                        videoId = f.FomatId;
                        break;
                    }
                }

                foreach (var f in _availableVideoFormats)
                {
                    if (f.Height == null)
                    {
                        audioId = f.FomatId;
                        break;
                    }
                }

                if (audioId != string.Empty)
                {
                    return string.Format("{0}+{1}", videoId, audioId);
                }
                else
                {
                    return videoId;
                }
            }

            return string.Empty;
        }

        public bool IsFormatAvailable(VideoFormat.Formats format)
        {
            return _availableFormats.Contains(format);
        }

        public static VideoInfo Parse(string json)
        {
            var obj = JsonConvert.DeserializeObject<VideoInfo>(json);

            obj._availableFormats = new List<VideoFormat.Formats>();

            if (obj._availableVideoFormats.Length > 0)
            {
                obj._availableFormats.Add(VideoFormat.Formats.AudioOnly);
            }

            foreach (var f in obj._availableVideoFormats)
            {
                if (f.Height != null)
                {
                    var format = Utils.FormatsExtension.Parse(f.Height.ToString());

                    if (format != VideoFormat.Formats.Unknow && !obj._availableFormats.Contains(format))
                    {
                        obj._availableFormats.Add(format);
                    }
                }

                if (f.FormatNote != null)
                {
                    var format = Utils.FormatsExtension.Parse(f.FormatNote.Substring(0, f.FormatNote.Length - 1));
                    if (format != VideoFormat.Formats.Unknow && !obj._availableFormats.Contains(format))
                    {
                        obj._availableFormats.Add(format);
                    }
                }

            }

            return obj;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(string.Format("[id: {0},  title: {1}, thumbnails:[", Id, Title));

            foreach (var thumbnail in Thumbnails)
            {
                sb.Append(thumbnail.ToString());
                sb.Append(',');
            }
            sb.Append("]]");

            return sb.ToString();
        }

    }

}
