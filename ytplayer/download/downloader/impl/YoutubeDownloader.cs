using System;
using System.Text.RegularExpressions;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {

    public class YoutubeDownloader : CommonDownloader {
        public YoutubeDownloader(DLEntry entry, IDownloadHost host, bool extractAudio) : base(entry, host, extractAudio) {
        }

        public override string GetIDStringFromURL(Uri uri) {
            return YoutubeDownloaderFactory.GetIDStringFromURL(uri.ToString());
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
        public IDownloader Create(DLEntry entry, IDownloadHost host, bool extractAudio) {
            return new CommonDownloader(entry, host, extractAudio);
        }

        // 対応する書式
        // https://www.youtube.com/watch?v=QkBvmv8kt4U
        // https://www.youtube.com/watch?v=NhKEBTz2N28&list=RDNhKEBTz2N28&start_radio=1
        static Regex regexId = new Regex(@"[?&]v=(?<id>[^&=\r\n \t]+)");

        public static (string id, string list) GetIdsStringFromURL(string url) {
            var m = regexId.Match(url);
            return (m?.Groups?["id"]?.Value, m?.Groups?["list"]?.Value);
        }

        public static string GetIDStringFromURL(string url) {
            return GetIdsStringFromURL(url).id;
        }

        public static string GetListIDStringFromURL(string url) {
            return GetIdsStringFromURL(url).list;
        }

        public bool IsAcceptableUrl(Uri uri) {
            return uri.Host.Contains("youtube.com") && GetIDStringFromURL(uri.ToString()) != null;
        }

        public string IdFromUri(Uri uri) {
            return GetIDStringFromURL(uri.ToString());
        }

        public string NormalizeUrl(Uri uri) {
            var id = GetIDStringFromURL(uri.ToString());
            return $"{uri.Scheme}://{uri.Host}{uri.LocalPath}?v={id}";
        }

        public bool IsList(Uri uri) {
            var list = GetListIDStringFromURL(uri.ToString());
            return !string.IsNullOrEmpty(list);
        }
    }
}
