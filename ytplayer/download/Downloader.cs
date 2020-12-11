using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected virtual string Arguments(bool audio) {
            if(audio) {
                return $"-x --audio-format mp3 {Entry.Url}";
            } else {
                return $"--format mp4 {Entry.Url}";
            }
        }

        private ProcessStartInfo Prepare(bool audio) {
            return new ProcessStartInfo() {
                FileName = Command,
                Arguments = $"{Arguments(audio)}",
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

        private Status DownloadSub(bool audio) {
            var psi = Prepare(audio);
            var processor = new Processor(Host, audio);
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
                    return Status.FAILED;
                }

                if(!string.IsNullOrEmpty(processor.Name)) {
                    Entry.Name = processor.Name;
                    Entry.Media |= processor.Media;
                    return Status.DOWNLOADED;
                }
                //while (HandleResponse(process.StandardOutput.ReadLine(), audio)) { }
                //while (Host.ErrorOutput(process.StandardError.ReadLine())) { }
                return Status.FAILED;
            }
            catch (Exception e) {
                Logger.error(e);
                return Status.FAILED;
            }
        }

        public void Download(MediaFlag flag) {
            Status resa = Status.FAILED;
            Status resv = Status.FAILED;
            string orgPath = Environment.CurrentDirectory;
            if (AUDIO(flag)) {
                Environment.CurrentDirectory = Settings.Instance.EnsureAudioPath;
                resa = DownloadSub(true);
            }
            if (VIDEO(flag)) {
                Environment.CurrentDirectory = Settings.Instance.EnsureVideoPath;
                resv = DownloadSub(false);
            }
            Environment.CurrentDirectory = orgPath;
            Entry.Status = (resa == Status.DOWNLOADED || resv == Status.DOWNLOADED) ? Status.DOWNLOADED : Status.FAILED;
            Storage.DLTable.Update();
        }
    }
}
