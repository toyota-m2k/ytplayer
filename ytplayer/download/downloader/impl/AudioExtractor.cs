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
        DLEntry Entry { get; }
        IDownloadHost Host { get; }
        bool DeleteVideo;
        bool Overwrite;


        protected AudioExtractor(DLEntry entry, IDownloadHost host, bool deleteVideo, bool overwrite) {
            Entry = entry;
            Host = host;
            DeleteVideo = deleteVideo;
            Overwrite = overwrite;
        }

        public void Cancel() {
            if (Entry.Status == Status.WAITING) {
                Entry.Status = Status.CANCELLED;
            }
        }

        protected ProcessStartInfo Prepare(string outFile) {
            return new ProcessStartInfo() {
                FileName = "ffmpeg",
                Arguments = $"-y -i \"{Entry.VPath}\" -ab 192k \"{outFile}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }

        public void Execute() {
            if (Entry.Status == Status.CANCELLED) {
                return;
            }
            string outFile = System.IO.Path.Combine(Settings.Instance.AudioPath, System.IO.Path.GetFileNameWithoutExtension(Entry.VPath) + ".mp3");
            if (PathUtil.isFile(outFile)&&!Overwrite) {
                Entry.APath = outFile;
                Entry.Status = Status.DOWNLOADED;
                Entry.Media |= MediaFlag.AUDIO;
                return;
            }
            Entry.Status = Status.DOWNLOADING;
            try {
                var psi = Prepare(outFile);
                var process = Process.Start(psi);
                while (true) {
                    var response = process.StandardOutput.ReadLine();
                    if (!ProcessResponse(response)) {
                        break;
                    }
                }
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error)) {
                    Host.ErrorOutput(error);
                    Entry.Status = Status.FAILED;
                    return;
                }
            }
            catch (Exception ex) {
                Logger.error(ex);
            }
        }
        bool ProcessResponse(string res) {
            return false;
        }
    }
}
