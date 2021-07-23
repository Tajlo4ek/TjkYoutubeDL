using System;
using System.Collections.Generic;
using System.Text;

namespace TjkYoutubeDL.Utils
{
    internal class GetInfoItem
    {
        public IReadOnlyList<string> Urls { get; private set; }

        private readonly Action<VideoInfo> onDataGet;
        private readonly Action<bool> onEnd;

        public GetInfoItem(IEnumerable<string> urls, Action<VideoInfo> onDataGet, Action<bool> onEnd)
        {
            this.onDataGet = onDataGet;
            this.onEnd = onEnd;
            this.Urls = new List<string>(urls).AsReadOnly();
        }

        public void OnDataGet(VideoInfo info)
        {
            onDataGet?.Invoke(info);
        }

        public void OnEnd(bool res)
        {
            onEnd?.Invoke(res);
        }

    }
}
