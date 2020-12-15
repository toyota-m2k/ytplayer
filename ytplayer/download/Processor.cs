using common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download {

    public class DownloadItemInfo {
        public string Name { get; }
        public string Id { get; }
        public bool AlreadyDownloaded { get; }
        public DownloadItemInfo(string name, string id, bool already) {
            Name = name;
            Id = id;
            AlreadyDownloaded = already;
        }
    }

    public class Processor {
        private IDownloadHost Host;
        //private bool ForAudio;
        //public MediaFlag Media => ForAudio ? MediaFlag.AUDIO : MediaFlag.VIDEO;
        public int Progress { get; private set; } = 0;
        public List<DownloadItemInfo> Results = new List<DownloadItemInfo>();

        const string PtnSkipped = @"\[download\]\s+(?<name>.*(?<id>-.{11}))\.mp4\s+has already been downloaded";
        //const string PtnAudioName = @"\[(?:(?:download)|(?:ffmpeg))\]\s+Destination:\s+(?<name>.*(?<id>-\w+))\.mp3(?!\w)";
        const string PtnVideoName = @"\[(?:(?:download)|(?:ffmpeg))\]\s+Destination:\s+(?<name>.*(?<id>-.{11}))\.mp4(?!\w)";
        const string PtnProgress = @"\[download\]\s+(?<progress>[0-9]+)\.[0-9]%";
        static Regex RegexSkipped = new Regex(PtnSkipped, RegexOptions.IgnoreCase);
        //static Regex RegexAudioName = new Regex(PtnAudioName, RegexOptions.IgnoreCase);
        static Regex RegexName = new Regex(PtnVideoName, RegexOptions.IgnoreCase);
        static Regex RegexProgress = new Regex(PtnProgress, RegexOptions.IgnoreCase);

        //Regex RegexName => ForAudio ? RegexAudioName : RegexVideoName;

        public Processor(IDownloadHost host) {
            Host = host;
            //ForAudio = audio;
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
                        Host.StandardOutput(res);
                        break;
                    }
                    if (TryParseProgress(res)) {
                        break;
                    }
                    Host.StandardOutput(res);
                } while (false);
            }
            return true;
        }

        private bool TryParseName(string res) {
            var matches = RegexName.Matches(res);
            if(matches.Count>0) {
                var item = new DownloadItemInfo(matches[0].Groups["name"].Value, matches[0].Groups["id"].Value, false);
                Results.Add(item);
                return true;
            }
            matches = RegexSkipped.Matches(res);
            if(matches.Count>0) {
                var item = new DownloadItemInfo(matches[0].Groups["name"].Value, matches[0].Groups["id"].Value, true);
                Results.Add(item);
                return true;
            }
            return false;
        }
        private bool TryParseProgress(string res) {
            var matches = RegexProgress.Matches(res);
            if (matches.Count > 0) {
                var g = matches[0].Groups["progress"];
                Progress = Convert.ToInt32(g.Value);
                return true;
            }
            return false;
        }
    }
}
