using System;
using System.Text.RegularExpressions;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {

    public class YoutubeDownloader : CommonDownloader {
        public YoutubeDownloader(DLEntry entry, IDownloadHost host) : base(entry,host) {
        }

        public override string GetIDStringFromURL(Uri uri) {
            return YoutubeDownloaderFactory.GetIDStringFromURL(uri);
        }

        public override string NormalizeUrlForKey(Uri uri) {
            var id = GetIDStringFromURL(uri);
            if (id == null) return uri.ToString();
            return NormalizeSubUrlForKey(uri, 0, id);
        }

        public override string NormalizeSubUrlForKey(Uri uri, int index, string id) {
            if (id == null) {
                return base.NormalizeSubUrlForKey(uri, index, id);
            }
            return $"{uri.Scheme}://{uri.Host}{uri.LocalPath}?v={id}";
        }
    }

    public class YoutubeDownloaderFactory : IDownloaderFactory {
        public IDownloader Create(DLEntry entry, IDownloadHost host) {
            return new CommonDownloader(entry, host);
        }

        // 対応する書式
        // https://www.youtube.com/watch?v=QkBvmv8kt4U
        // https://www.youtube.com/watch?v=NhKEBTz2N28&list=RDNhKEBTz2N28&start_radio=1
        static Regex regexId = new Regex(@"[?&]v=(?<id>[^&=\r\n \t]+)");

        public static string GetIDStringFromURL(Uri uri) {
            var m = regexId.Match(uri.ToString());
            return m?.Groups?["id"].Value;
        }

        public bool IsAcceptableUrl(Uri uri) {
            return uri.Host.Contains("youtube.com") && GetIDStringFromURL(uri) != null;
        }
    }
}
