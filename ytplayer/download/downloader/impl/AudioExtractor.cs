using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {
    public class AudioExtractor : IDownloader {
        public DLEntry Entry { get; }
        private IDownloadHost Host;
        private IDownloader Downloader;
        private bool DeleteVideoFile;
        public AudioExtractor(DLEntry entry, IDownloadHost host, bool deleteVideoFile) {
            DeleteVideoFile = deleteVideoFile;
            Entry = entry;
            Host = host;
            Downloader = DownloaderSelector.Select(new Uri(entry.Url)).Create(entry,host,true);
        }

        public void Cancel() {
            Downloader.Cancel();
        }

        public void Execute() {
            var statusOrg = Entry.Status;
            Downloader.Execute();
            if(DeleteVideoFile && Entry.Media.HasAudio()) { 
                if (PathUtil.safeDeleteFile(Entry.VPath)) {
                    Entry.Media = Entry.Media.MinusVideo();
                    Entry.VPath = null;
                }
            }
            if (statusOrg == Status.COMPLETED) {
                Entry.Status = Status.COMPLETED;
            }
        }
    }
}
