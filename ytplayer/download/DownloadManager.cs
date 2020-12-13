using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download {
    public interface IDownloadHost {
        bool StandardOutput(string msg);
        bool ErrorOutput(string msg);
        void Completed(DLEntry target, bool succeeded);
        void FoundSubItem(DLEntry foundEntry);
    }
    public class DownloadManager {
        public Storage Storage { get; private set; }

        private class DLTarget {
            public DLEntry Entry;
            //public MediaFlag Media;
            public DLTarget(DLEntry entry, MediaFlag media) {
                Entry = entry;
                //Media = media;
            }
        }
        private Queue<DLTarget> Queue = new Queue<DLTarget>();
        private DLTarget Current = null;
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
        public event Action<bool> BusyChanged;

        public void Enqueue(DLEntry target, MediaFlag media) {
            lock(this) {
                SetStatus(target, Status.WAITING);
                Queue.Enqueue(new DLTarget(target, media));
                QueueingEvent.Set();
            }
            BusyChanged?.Invoke(IsBusy);
        }
        public void Enqueue(IEnumerable<DLEntry> targets, MediaFlag media) {
            lock (this) {
                foreach(var e in targets) {
                    SetStatus(e, Status.WAITING);
                    Queue.Enqueue(new DLTarget(e, media));
                }
                QueueingEvent.Set();
            }
            BusyChanged?.Invoke(IsBusy);
        }

        public void Cancel() {
            bool update = false;
            lock (this) {
                while (Queue.Count > 0) {
                    var e = Queue.Dequeue();
                    if (e.Entry.Status != Status.DOWNLOADED) {
                        if(SetStatus(e.Entry, Status.CANCELLED, false)) {
                            update = true;
                        }
                    }
                }
            }
            if (update) {
                Storage.DLTable.Update();
            }
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

        private DLTarget Dequeue() {
            bool update = false;
            lock (this) {
                Current = null;
                while (Queue.Count > 0) {
                    var e = Queue.Dequeue();
                    if(e.Entry.Status!=Status.CANCELLED) {
                        if (Alive) {
                            SetStatus(e.Entry, Status.DOWNLOADING);
                            Current = e;
                            return e;
                        }
                        SetStatus(e.Entry, Status.CANCELLED, update:false);
                        update = true;
                    }
                }
            }
            if(update) {
                Storage.DLTable.Update();
            }
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
            var dl = CreateDownloader(e);
            dl.Download();
            return true;
        }

        private Downloader CreateDownloader(DLTarget e) {
            if(Settings.Instance.UseWSL) {
                return new WslDownloader(e.Entry, Host, Storage);
            } else {
                return new Downloader(e.Entry, Host, Storage);
            }
        }
    }
}
