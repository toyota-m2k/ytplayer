using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download {
    public class Downloader {
        protected IDownloadHost Host { get; }
        protected DLEntry Entry { get; }

        public Downloader(DLEntry entry, IDownloadHost host) {
            Host = host;
            Entry = entry;
        }

        protected virtual string Command => "youtube_dl";

        protected virtual string Arguments(bool audio) {
            if(audio) {
                return $"-x --audio-fomat mp3 {Entry.Url}";
            } else {
                return $"--video-format mp4 {Entry.Url}";
            }
        }

        private ProcessStartInfo Prepare(bool music) {
            return new ProcessStartInfo() {
                FileName = Command,
                Arguments = $"{Arguments(music)}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // RedirectStandardInput = true,
            };
        }
        private Status Result = Status.FAILED;
        private bool HandleResponse(string resp) {
            if (resp == null) return false;
            // ToDo ...
            // 
            Result = Status.DOWNLOADED;
            return true;
        }

        public void Download(MediaFlag flag) {
            if (((int)flag & (int)MediaFlag.AUDIO) != 0) {
                var process = Process.Start(Prepare(true));
                while (HandleResponse(process.StandardOutput.ReadLine())) {}
                while (Host.ErrorOutput(process.StandardOutput.ReadLine())) {}
            }
            Entry.Status = Result;
            Host.Storage.DLTable.Update();
        }
    }
}
