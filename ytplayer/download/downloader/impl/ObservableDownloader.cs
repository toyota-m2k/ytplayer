using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download.downloader.impl {
    public class ObservableDownloader : IDownloader {
        private IDownloader _innerDownloader;
        private Action<bool> _onCompleted;

        public ObservableDownloader(IDownloader inner, Action<bool> onCompleted) { 
            _innerDownloader = inner;
            _onCompleted = onCompleted;
        }

        public DLEntry Entry => _innerDownloader.Entry;

        public void Cancel() {
            _innerDownloader.Cancel();
        }

        public void Execute() {
            _innerDownloader.Execute();
            var status = Entry.Status;
            if (status == Status.COMPLETED) {
                _onCompleted?.Invoke(true);
            }
            else if (status == Status.FAILED || status == Status.CANCELLED) {
                _onCompleted?.Invoke(false);
            }

        }
    }

    public static class ObservableDownloaderExtensions {
        public static IDownloader WithCompletionNotification(this IDownloader downloader, Action<bool> onCompleted) {
            return new ObservableDownloader(downloader, onCompleted);
        }
    }
}
