using System;
using System.Diagnostics;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download {
    public class Downloader {
        protected IDownloadHost Host { get; }
        protected DLEntry Entry { get; }
        protected Storage Storage { get; }

        public Downloader(DLEntry entry, IDownloadHost host, Storage storage) {
            Host = host;
            Entry = entry;
            Storage = storage;
        }

        protected virtual string Command => "youtube-dl";

        protected virtual string Arguments() {
            return $"--format mp4 {Entry.Url}";
        }

        private ProcessStartInfo Prepare() {
            return new ProcessStartInfo() {
                FileName = Command,
                Arguments = $"{Arguments()}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // RedirectStandardInput = true,
            };
        }
        private static bool AUDIO(MediaFlag flag) {
            return ((int)flag & (int)MediaFlag.AUDIO) != 0;
        }

        private static bool VIDEO(MediaFlag flag) {
            return ((int)flag & (int)MediaFlag.VIDEO) != 0;
        }

        private static bool ValidateResult(DownloadItemInfo res, DLEntry entry) {
            string dir = Settings.Instance.EnsureVideoPath;
            string ext = "mp4";

            var f = System.IO.Directory.GetFiles(dir, $"*{res.Id}.{ext}", System.IO.SearchOption.TopDirectoryOnly);
            if (f != null && f.Length > 0) {
                entry.Name = res.Name;
                entry.VPath = f[0];
                entry.Status = Status.DOWNLOADED;
                return true;
            } else {
                entry.Status = Status.FAILED;
                return false;
            }
        }

        private void DownloadSub() {
            var psi = Prepare();
            var processor = new Processor(Host);
            try {
                var process = Process.Start(psi);
                while(true) {
                    var o = process.StandardOutput.ReadLine();
                    if(!processor.ProcessResponse(o)) {
                        break;
                    }
                }
                //var o = process.StandardOutput.ReadToEnd();
                //Host.StandardOutput(o);
                var e = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(e)) {
                    Host.ErrorOutput(e);
                    Entry.Status = Status.FAILED;
                    Entry.Media |= MediaFlag.VIDEO;
                    return;
                }

                if(processor.Results.Count==0) {
                    Host.ErrorOutput("no data");
                    Entry.Status = Status.FAILED;
                    return;
                }
                bool result = ValidateResult(processor.Results[0], Entry);
                Host.Completed(Entry, result);

                for (int i = 1; i < processor.Results.Count; i++) {
                    var subEntry = DLEntry.Create($"{Entry.Url} #{i}");
                    subEntry.Media = MediaFlag.VIDEO;
                    if (ValidateResult(processor.Results[i], subEntry)) {
                        Host.FoundSubItem(subEntry);
                    }
                }
            }
            catch (Exception e) {
                Logger.error(e);
            }
        }

        public void Download() {
            string orgPath = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Settings.Instance.EnsureVideoPath;
            DownloadSub();
            Environment.CurrentDirectory = orgPath;
            Storage.DLTable.Update();
        }
    }
}
