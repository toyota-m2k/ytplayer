using io.github.toyota32k.toolkit.utils;
using System.Diagnostics;
using System.IO;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {
    public class FFMpegConverter : CommonDownloader {
        private bool DeleteVideoFile;
        private Status OrgStatus { get; }

        public FFMpegConverter(DLEntry entry, IDownloadHost host, bool deleteVideoFile) : base(entry, host, true) {
            DeleteVideoFile = deleteVideoFile;
            OrgStatus = entry.Status;
        }

        protected override ProcessStartInfo Prepare() {
            return new ProcessStartInfo() {
                FileName = "ffmpeg",
                Arguments = $"-i \"{Entry.VPath}\" -y -f mp3 -vn \"{GetSavedFilePath(null)}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                //StandardOutputEncoding = System.Text.Encoding.UTF8,
                //StandardErrorEncoding = System.Text.Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }

        protected override string GetSavedFilePath(DownloadResults.ItemInfo info) {
            if(!string.IsNullOrEmpty(Entry.APath)) {
                return Entry.APath;
            }
            if(!string.IsNullOrEmpty(Entry.VPath)) {
                var dir = System.IO.Path.GetDirectoryName(Entry.VPath);
                var name = System.IO.Path.GetFileNameWithoutExtension(Entry.VPath);
                return System.IO.Path.Combine(dir, $"{name}.mp3");
            }
            return null;
        }

        protected override bool ProcessStandardOutput(StreamReader standardOutput) {
            return true;
        }

        protected override bool ProcessStandardError(StreamReader standardError) {
            while (Alive) {
                var response = standardError.ReadLine();
                if(response==null) {
                    return true;
                }
                Host.StandardOutput(response);
                if(response.StartsWith("video:0kB")) {
                    Results[0].Completed = true;
                    Results[0].Name = Entry.Name;
                }
            }
            Entry.Status = Status.CANCELLED;
            Host.ErrorOutput("Cancelled");
            return false;
        }

        public override void Execute() {
            if (PathUtil.isFile(Entry.VPath)) {
                base.Execute();
            } else {
                Host.ErrorOutput("No video file.");
                Entry.Status = Status.FAILED;
            }
            if (Entry.Status == Status.COMPLETED && DeleteVideoFile && Entry.Media.HasAudio()) {
                if (PathUtil.safeDeleteFile(Entry.VPath)) {
                    Entry.Media = Entry.Media.MinusVideo();
                    Entry.VPath = null;
                }
            }
            if (OrgStatus == Status.COMPLETED) {
                Entry.Status = Status.COMPLETED;
            }
        }
    }
}
