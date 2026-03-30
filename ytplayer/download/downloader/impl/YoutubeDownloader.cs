using System;
using System.Text.RegularExpressions;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {

    public class YoutubeDownloader : CommonDownloader {
        public YoutubeDownloader(DLEntry entry, IDownloadHost host, bool extractAudio) : base(entry, host, extractAudio) {
        }

        public override string GetIDStringFromURL(Uri uri) {
            return YoutubeDownloaderFactory.GetIDStringFromUrl(uri.ToString());
        }

        public override string NormalizeUrlForKey(Uri uri) {
            var id = GetIDStringFromURL(uri);
            if (id == null) return uri.ToString();
            return NormalizeSubUrlForKey(uri, 0, id);
        }

        public override string NormalizeSubUrlForKey(Uri uri, int index, string id) {
            if (id == null) {
                return base.NormalizeSubUrlForKey(uri, index, null);
            }
            return $"{uri.Scheme}://{uri.Host}{uri.LocalPath}?v={id}";
        }
    }

    public class YoutubeDownloaderFactory : IDownloaderFactory {
        public IDownloader Create(DLEntry entry, IDownloadHost host, bool extractAudio) {
            return new YoutubeDownloader(entry, host, extractAudio);
        }

        // 対応する書式
        // https://www.youtube.com/watch?v=QkBvmv8kt4U
        // https://www.youtube.com/watch?v=NhKEBTz2N28&list=RDNhKEBTz2N28&start_radio=1
        // https://youtu.be/UF9PWHDJ-AE
        // https://www.youtube.com/embed/23GcaWtbVdQ?rel=0
        private static readonly Regex regexId = new Regex(@"(?:[?&]v=|youtu.be/|embed/)(?<id>[^?&=\r\n \t]+)(?:[?&]list=(?<list>[^&=\r\n \t]+))?");

        public static (string id, string list) GetIdsStringFromUrl(string url) {
            var m = regexId.Match(url);
            return (m.Groups["id"]?.Value, m.Groups["list"]?.Value);
        }

        public static string GetIDStringFromUrl(string url) {
            return GetIdsStringFromUrl(url).id;
        }

        public static string GetListIDStringFromUrl(string url) {
            return GetIdsStringFromUrl(url).list;
        }

        public bool IsAcceptableUrl(Uri uri) {
            return (uri.Host.Contains("youtube.com")||uri.Host.Contains("youtu.be")) && GetIDStringFromUrl(uri.ToString()) != null;
        }

        public string IdFromUri(Uri uri) {
            return GetIDStringFromUrl(uri.ToString());
        }

        public string StripListIdFromUrl(Uri uri) {
            var id = GetIDStringFromUrl(uri.ToString());
            return $"{uri.Scheme}://{uri.Host}{uri.LocalPath}?v={id}";
        }

        public bool IsList(Uri uri) {
            var list = GetListIDStringFromUrl(uri.ToString());
            return !string.IsNullOrEmpty(list);
        }
    }
}
