using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.download {
    public interface IDownloadHost {
        bool StandardOutput(string msg);
        bool ErrorOutput(string msg);
        Storage Storage { get; }
    }
    public class DownloadManager {
        private Queue<DLEntry> Queue = new Queue<DLEntry>();
        private DLEntry Current = null;
        private AutoResetEvent QueueingEvent = new AutoResetEvent(false);
        private bool Alive = true;
        private TaskCompletionSource<object> TerminationSource = new TaskCompletionSource<object>();
        private Storage Storage => Host.Storage;
        private IDownloadHost Host;

        public DownloadManager(IDownloadHost host) {
            Host = host;
            Open();
        }

        public void Open() {
            Task.Run(() => {
                while(Alive) {
                    if(QueueingEvent.WaitOne(500)) {
                        Next();
                    }
                }
                TerminationSource.SetResult(null);
            });
        }

        public async Task Close() {
            Alive = false;
            await TerminationSource.Task;
        }

        public bool IsBusy {
            get {
                lock(this) {
                    return Current != null || Queue.Count > 0;
                }
            }
        }

        public void Enqueue(DLEntry target) {
            lock(this) {
                SetStatus(target, Status.WAITING);
                Queue.Enqueue(target);
                QueueingEvent.Set();
            }
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
            lock(this) {
                Current = null;
                while (Queue.Count > 0) {
                    var e = Queue.Dequeue();
                    if(e.Status!=Status.CANCELLED) {
                        SetStatus(e, Status.DOWNLOADING);
                        Current = e;
                        return e;
                    }
                }
                return null;
            }
        }

        public void Next() {
            var e = Dequeue();
            if(null==e) {
                return;
            }
            //var dl = new Downloader(e, Host);
        }

    }
}
