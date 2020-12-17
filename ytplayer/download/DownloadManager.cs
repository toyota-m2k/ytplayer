using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.data;
using ytplayer.download.downloader;

namespace ytplayer.download {
    public interface IDownloadHost {
        bool StandardOutput(string msg);
        bool ErrorOutput(string msg);
        void Completed(DLEntry target, bool succeeded);
        void FoundSubItem(DLEntry foundEntry);
    }
    public class DownloadManager {
        public Storage Storage { get; private set; }

        private Queue<IDownloader> Queue = new Queue<IDownloader>();
        private IDownloader Current = null;
        private AutoResetEvent QueueingEvent = new AutoResetEvent(false);
        private bool Alive = true;
        private TaskCompletionSource<object> TerminationSource = new TaskCompletionSource<object>();
        private IDownloadHost Host;

        public DownloadManager(IDownloadHost host, Storage strage) {
            Host = host;
            Storage = strage;
            Start();
        }

        private void Start() {
            Task.Run(() => {
                while(Alive) {
                    if(QueueingEvent.WaitOne(500)) {
                        while(Next()) { }
                    }
                }
                TerminationSource.SetResult(null);
            });
        }

        public async void Dispose() {
            await CloseAsync();
            Storage?.Dispose();
            Storage = null;
        }

        private async Task CloseAsync() {
            Alive = false;
            BusyChanged = null;
            await TerminationSource.Task;
        }

        public bool IsBusy {
            get {
                lock(this) {
                    return Current != null || Queue.Count > 0;
                }
            }
        }
        private IDownloader CreateDownloader(DLEntry entry) {
            var uri = new Uri(entry.Url);
            var factory = DownloaderSelector.Select(uri);
            return factory.Create(entry, Host);
        }

        public event Action<bool> BusyChanged;

        public void Enqueue(DLEntry entry) {
            lock(this) {
                SetStatus(entry, Status.WAITING);
                Queue.Enqueue(CreateDownloader(entry));
                QueueingEvent.Set();
            }
            BusyChanged?.Invoke(IsBusy);
        }
        public void Enqueue(IEnumerable<DLEntry> entries) {
            lock (this) {
                foreach(var entry in entries) {
                    SetStatus(entry, Status.WAITING);
                    Queue.Enqueue(CreateDownloader(entry));
                }
                QueueingEvent.Set();
            }
            BusyChanged?.Invoke(IsBusy);
        }

        public void Cancel() {
            lock (this) {
                while (Queue.Count > 0) {
                    var e = Queue.Dequeue();
                    e.Cancel();
                }
            }
            Storage.DLTable.Update();
            BusyChanged?.Invoke(IsBusy);
        }

        private bool SetStatus(DLEntry e, Status status, bool update=true) {
            if(e.Status!=status) {
                e.Status = status;
                if(update) {
                    Storage.DLTable.Update();
                }
                return true;
            }
            return false;
        }

        private IDownloader Dequeue() {
            lock (this) {
                Current = null;
                while (Queue.Count > 0) {
                    var e = Queue.Dequeue();
                    if(Alive) {
                        Current = e;
                        return e;
                    }
                    e.Cancel();
                }
            }
            Storage.DLTable.Update();
            BusyChanged?.Invoke(IsBusy);
            return null;
        }

        public bool Next() {
            if(!Alive) {
                return false;
            }
            var e = Dequeue();
            if(null==e) {
                return false;
            }
            Execute(e);
            return true;
        }

        private void Execute(IDownloader dl) {
            string orgPath = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Settings.Instance.EnsureVideoPath;
            dl.Execute();
            Environment.CurrentDirectory = orgPath;
            Storage.DLTable.Update();
        }

    }
}
