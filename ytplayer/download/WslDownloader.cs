using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download {
    public class WslDownloader : Downloader {
        public WslDownloader(DLEntry entry, IDownloadHost host, Storage storage) : base(entry, host, storage) {
        }

        protected override string Command => "wsl";

        protected override string Arguments(bool audio) {
            return $"youtube-dl {base.Arguments(audio)}";
        }

    }
}
