using common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download {
    public class Processor {
        private IDownloadHost Host;
        private bool ForAudio;
        public MediaFlag Media => ForAudio ? MediaFlag.AUDIO : MediaFlag.VIDEO;
        public string Name { get; private set; } = null;
        public int Progress { get; private set; } = 0;
        public bool AlreadyDownloaded { get; private set; } = false;

        const string PtnSkipped = @"\[download\]\s+(?<name>.*)\.mp4\s+has already been downloaded";
        const string PtnAudioName = @"\[download\]\s+Destination:\s+(?<name>.*)\.mp3(?!\w)";
        const string PtnVideoName = @"\[download\]\s+Destination:\s+(?<name>.*)\.mp4(?!\w)";
        const string PtnProgress = @"\[download\]\s+(?<progress>[0-9]+)\.[0-9]%";
        static Regex RegexSkipped = new Regex(PtnSkipped, RegexOptions.IgnoreCase);
        static Regex RegexAudioName = new Regex(PtnAudioName, RegexOptions.IgnoreCase);
        static Regex RegexVideoName = new Regex(PtnVideoName, RegexOptions.IgnoreCase);
        static Regex RegexProgress = new Regex(PtnProgress, RegexOptions.IgnoreCase);

        Regex RegexName => ForAudio ? RegexAudioName : RegexVideoName;

        public Processor(IDownloadHost host, bool audio) {
            Host = host;
            ForAudio = audio;
        }

        public bool ProcessResponse(string res) {
            if(res==null) {
                return false;
            }
            res = res.Trim();
            if(!res.IsEmpty()) {
                Logger.debug(res);
                do {
                    if (TryParseName(res)) {
                        Logger.debug($"Name = {Name}");
                        Host.StandardOutput(res);
                        break;
                    }
                    if (TryParseProgress(res)) {
                        Logger.debug($"progress = {Progress}");
                        break;
                    }
                    Host.StandardOutput(res);
                } while (false);
            }
            return true;
        }

        private bool TryParseName(string res) {
            var matches = RegexName.Matches(res);
            if(matches.Count>1) {
                var g = matches[1].Groups["name"];
                Name = g.Value;
                return true;
            }
            matches = RegexSkipped.Matches(res);
            if(matches.Count>0) {
                AlreadyDownloaded = true;
                var g = matches[1].Groups["name"];
                Name = g.Value;
                return true;
            }
            return false;
        }
        private bool TryParseProgress(string res) {
            var matches = RegexProgress.Matches(res);
            if (matches.Count > 1) {
                var g = matches[1].Groups["progress"];
                Progress = Convert.ToInt32(g.Value);
                return true;
            }
            return false;
        }
    }
}
