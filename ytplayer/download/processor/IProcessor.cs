using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download.processor {
    /**
     * 動画サイト固有の応答メッセージを処理する「プロセッサ」クラスのi/f定義
     */
    public interface IProcessor {
        void Download(DLEntry entry, IDownloadHost host);
        bool IsAcceptableUrl(Uri uri);
        string GetIDStringFromURL(Uri uri);
        string NormalizeUrlForKey(Uri uri);
        string NormalizeSubUrlForKey(Uri uri, int index, string id);
    }
}