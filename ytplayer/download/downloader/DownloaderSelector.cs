using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;
using ytplayer.download.downloader.impl;

namespace ytplayer.download.downloader {
    public static class DownloaderSelector {

        private static List<IDownloaderFactory> Factories = new List<IDownloaderFactory>() {
            new YoutubeDownloaderFactory()
        };

        // fallback
        private static CommonDownloaderFactory CommonProcessorFactory = new CommonDownloaderFactory();

        public static IDownloaderFactory Select(Uri uri) {
            foreach (var f in Factories) {
                if (f.IsAcceptableUrl(uri)) {
                    return f;
                }
            }
            return null; //  CommonProcessorFactory;
        }
    }
}
