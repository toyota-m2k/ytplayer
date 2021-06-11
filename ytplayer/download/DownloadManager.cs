using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.data;
using ytplayer.download.downloader;
using ytplayer.download.downloader.impl;

namespace ytplayer.download {
    public interface IReportOutput {
        bool StandardOutput(string msg);
        bool ErrorOutput(string msg);
    }
    public interface IDownloadHost : IReportOutput {
        void Completed(DLEntry target, bool succeeded, bool extractAudio);
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

        public bool Disposed { get; private set; } = false;

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
                Disposed = true;
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

        public async Task WaitForClose() {
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
            return factory?.Create(entry, Host, false);
        }

        public event Action<bool> BusyChanged;

        private void InternalEnqueue(IEnumerable<IDownloader> dls) {
            if(Utils.IsNullOrEmpty(dls)) {
                return;
            }
            lock(this) {
                foreach(var d in dls) {
                    SetStatus(d.Entry, Status.WAITING);
                    Queue.Enqueue(d);
                }
                QueueingEvent.Set();
            }
            BusyChanged?.Invoke(IsBusy);
        }

        public void Enqueue(DLEntry entry) {
            Enqueue(entry.ToSingleEnumerable());
        }
        
        public void Enqueue(IEnumerable<DLEntry> entries) {
            InternalEnqueue(entries.Select((entry) => CreateDownloader(entry)).Where((dlr)=>dlr!=null));
        }
        
        public void EnqueueExtractAudio(bool deleteVideo, bool downloadAudioFile, DLEntry entry) {
            EnqueueExtractAudio(deleteVideo, downloadAudioFile, entry.ToSingleEnumerable());
        }
        
        public void EnqueueExtractAudio(bool deleteVideo, bool downloadAudioFile, IEnumerable<DLEntry> entries) {
            if (downloadAudioFile) {
                InternalEnqueue(entries.Select((entry) => new AudioExtractor(entry, Host, deleteVideo)));
            } else {
                InternalEnqueue(entries.Select((entry) => new FFMpegConverter(entry, Host, deleteVideo)));
            }
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
            dl.Execute();
            Storage.DLTable.Update();
        }

    }
}
