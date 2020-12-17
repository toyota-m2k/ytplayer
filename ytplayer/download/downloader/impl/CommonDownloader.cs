using System;
using System.Text.RegularExpressions;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {
    public class CommonDownloader : DownloaderBase {
        public CommonDownloader(DLEntry entry, IDownloadHost host) : base(entry,host) {

        }

        protected override string GetSavedFilePath(DownloadItemInfo info) {
            string dir = Settings.Instance.EnsureVideoPath;
            string ext = "mp4";
            var f = System.IO.Directory.GetFiles(dir, $"*-{info.Id}.{ext}", System.IO.SearchOption.TopDirectoryOnly);
            return (null != f && f.Length > 0) ? f[0] : null;
        }

        const string PtnSkipped = @"\[download\]\s+(?<name>.*-(?<id>.{11}))\.mp4\s+has already been downloaded";
        const string PtnVideoName = @"\[(?:(?:download)|(?:ffmpeg))\]\s+Destination:\s+(?<name>.*-(?<id>.{11}))\.mp4(?!\w)";
        const string PtnProgress = @"\[download\]\s+(?<progress>[0-9]+)\.[0-9]%";
        static Regex RegexSkipped = new Regex(PtnSkipped, RegexOptions.IgnoreCase);
        static Regex RegexName = new Regex(PtnVideoName, RegexOptions.IgnoreCase);
        static Regex RegexProgress = new Regex(PtnProgress, RegexOptions.IgnoreCase);

        protected override bool TryParseName(string res) {
            var matches = RegexName.Matches(res);
            if (matches.Count > 0) {
                var item = new DownloadItemInfo(matches[0].Groups["name"].Value, matches[0].Groups["id"].Value, false);
                Results.Add(item);
                return true;
            }
            matches = RegexSkipped.Matches(res);
            if (matches.Count > 0) {
                var item = new DownloadItemInfo(matches[0].Groups["name"].Value, matches[0].Groups["id"].Value, true);
                Results.Add(item);
                return true;
            }
            return false;
        }

        protected override bool TryParseProgress(string res) {
            var matches = RegexProgress.Matches(res);
            if (matches.Count > 0) {
                var g = matches[0].Groups["progress"];
                Progress = Convert.ToInt32(g.Value);
                return true;
            }
            return false;
        }

        public override string GetIDStringFromURL(Uri uri) {
            return uri.ToString();
        }

        public override string NormalizeUrlForKey(Uri uri) {
            return uri.ToString();
        }
        public override string NormalizeSubUrlForKey(Uri uri, int index, string id) {
            return $"{uri.ToString()} #{index}";
        }
    }

    public class CommonDownloaderFactory : IDownloaderFactory {
        public IDownloader Create(DLEntry entry, IDownloadHost host) {
            return new CommonDownloader(entry, host);
        }

        public bool IsAcceptableUrl(Uri uri) {
            return true;
        }
    }
}
