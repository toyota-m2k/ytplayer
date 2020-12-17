using System;
using System.Text.RegularExpressions;

namespace ytplayer.download.processor.impl {

    public class YoutubeProcessor : CommonProcessor {
        public YoutubeProcessor() {
        }

        public override bool IsAcceptableUrl(Uri uri) {
            return uri.Host.Contains("youtube.com") && GetIDStringFromURL(uri) != null;
        }

        // 対応する書式
        // https://www.youtube.com/watch?v=QkBvmv8kt4U
        // https://www.youtube.com/watch?v=NhKEBTz2N28&list=RDNhKEBTz2N28&start_radio=1
        static Regex regexId = new Regex(@"[?&]v=(?<id>[^&=\r\n \t]+)");
        
        public override string GetIDStringFromURL(Uri uri) {
            var m = regexId.Match(uri.ToString());
            return m?.Groups?["id"].Value;
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
}
