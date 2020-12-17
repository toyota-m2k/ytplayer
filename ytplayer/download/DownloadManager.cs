using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.data;
using ytplayer.download.processor;

namespace ytplayer.download {
    public interface IDownloadHost {
        bool StandardOutput(string msg);
        bool ErrorOutput(string msg);
        void Completed(DLEntry target, bool succeeded);
        void FoundSubItem(DLEntry foundEntry);
    }
    public class DownloadManager {
        public Storage Storage { get; private set; }

        private Queue<DLEntry> Queue = new Queue<DLEntry>();
        private DLEntry Current = null;
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

        public void Enqueue(DLEntry entry) {
            lock(this) {
                SetStatus(entry, Status.WAITING);
                Queue.Enqueue(entry);
                QueueingEvent.Set();
            }
            BusyChanged?.Invoke(IsBusy);
        }
        public void Enqueue(IEnumerable<DLEntry> entries) {
            lock (this) {
                foreach(var e in entries) {
                    SetStatus(e, Status.WAITING);
                    Queue.Enqueue(e);
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
                    if (e.Status != Status.DOWNLOADED) {
                        if(SetStatus(e, Status.CANCELLED, false)) {
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

        private DLEntry Dequeue() {
            bool update = false;
            lock (this) {
                Current = null;
                while (Queue.Count > 0) {
                    var e = Queue.Dequeue();
                    if(e.Status!=Status.CANCELLED) {
                        if (Alive) {
                            SetStatus(e, Status.DOWNLOADING);
                            Current = e;
                            return e;
                        }
                        SetStatus(e, Status.CANCELLED, update:false);
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
            Download(e);
            return true;
        }

        private void Download(DLEntry entry) {
            var uri = new Uri(entry.Url);
            var processor = ProcessorSelector.Select(uri);

            string orgPath = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Settings.Instance.EnsureVideoPath;
            processor.Download(entry, Host);
            Environment.CurrentDirectory = orgPath;
            Storage.DLTable.Update();
        }

    }
}
