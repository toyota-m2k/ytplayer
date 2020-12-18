using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download.downloader {
    /**
     * 動画サイト固有の応答メッセージを処理する「プロセッサ」クラスのi/f定義
     */
    public interface IDownloader {
        DLEntry Entry { get; }
        void Execute();
        void Cancel();
    }

    public interface IDownloaderFactory {
        IDownloader Create(DLEntry entry, IDownloadHost host, bool extractAudio);
        bool IsAcceptableUrl(Uri uri);
    }
}
