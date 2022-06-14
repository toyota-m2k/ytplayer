using io.github.toyota32k.toolkit.utils;
using System;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {
    public class AudioExtractor : IDownloader {
        public DLEntry Entry { get; }
        private IDownloadHost Host;
        private IDownloader Downloader;
        private bool DeleteVideoFile;
        private Status OrgStatus { get; }
        public AudioExtractor(DLEntry entry, IDownloadHost host, bool deleteVideoFile) {
            DeleteVideoFile = deleteVideoFile;
            Entry = entry;
            Host = host;
            OrgStatus = entry.Status;
            Downloader = DownloaderSelector.Select(new Uri(entry.Url)).Create(entry,host,true);
        }

        public void Cancel() {
            Downloader.Cancel();
        }

        public void Execute() {
            Downloader.Execute();
            if(Entry.Status==Status.COMPLETED && DeleteVideoFile && Entry.Media.HasAudio()) { 
                if (PathUtil.safeDeleteFile(Entry.VPath)) {
                    Entry.Media = Entry.Media.MinusVideo();
                    Entry.VPath = null;
                    Entry.UpdateSizeAndDuration();
                }
            }
            if (OrgStatus == Status.COMPLETED) {
                Entry.Status = Status.COMPLETED;
            }
        }
    }
}
