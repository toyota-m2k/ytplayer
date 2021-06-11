using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ytplayer.data;

namespace ytplayer.download.downloader {

    public class DownloadResults {
        /**
         * プロセッサが返すダウンロードアイテム情報を保持するクラス
         */
        public class ItemInfo {
            public string Name { get; set; }
            public string Id { get; }
            public bool Completed { get; set; } = false;
            public bool AlreadyDownloaded { get; set; }
            public ItemInfo(string id) {
                Name = "";
                Id = id;
            }
        }

        private List<ItemInfo> ItemList = new List<ItemInfo>();

        public ItemInfo LastResult => ItemList.LastOrDefault();

        public IEnumerable<ItemInfo> List => ItemList;

        private ItemInfo ItemAt(string id) {
            return List.Where((c) => c.Id == id).LastOrDefault();
        }

        public void Add(string id) {
            ItemList.Add(new ItemInfo(id));
        }

        public void CompleteLast() {
            var last = ItemList.LastOrDefault();
            if(last!=null) {
                last.Completed = true;
            }
        }

        public void AddOrUpdate(string id, string name, bool already) {
            var e = ItemAt(id);
            if(e!=null) {
                e.Name = name;
                e.AlreadyDownloaded = already;
                e.Completed = true;
            } else {
                ItemList.Add(new ItemInfo(id) { Name = name, Completed = already, AlreadyDownloaded = already });
            }
        }

        public int Count => ItemList.Count;

        public ItemInfo this[int i] => ItemList[i];
    }

    /**
     * Processor の共通実装
     */
    public abstract class DownloaderBase : IDownloader {
        /**
         * ダウンロード結果を保持するリスト
         */
        protected DownloadResults Results { get; } = new DownloadResults();

        protected virtual string BasicArguments {
            get {
                if(!ExtractAudio) {
                    return "--format mp4";
                } else {
                    return "-x --audio-format mp3";
                }
            }
        }

        /**
         * youtube-dlに渡す、サイト固有の引数（不要なら空文字）
         */
        protected virtual string SpecialArguments => "";

        /**
         * ダウンロード進捗(%)を保持するプロパティ... とりあえず int型。未使用。
         * もしプログレスバーなどを表示するならその時考えるけど、わりとDLはすぐに終わるから、今のところ不要では？
         */
        public int Progress { 
            get => Entry.Progress; 
            protected set => Entry.Progress = value; 
        }

        public DLEntry Entry { get; }
        protected IDownloadHost Host { get; }
        protected bool ExtractAudio { get; }

        protected string TargetUrlOrId => (!ExtractAudio) ? Entry.Url : GetIDStringFromURL(new Uri(Entry.Url));
        protected string OutputDir => !ExtractAudio ? Settings.Instance.EnsureVideoPath : Settings.Instance.EnsureAudioPath;
        protected string OutputExtension => !ExtractAudio ? "mp4" : "mp3";

        private Process DownloadProcess { get; set; } = null;
        public bool Alive { get; private set; } = true;

        protected DownloaderBase(DLEntry entry, IDownloadHost host, bool extractAudio) {
            Entry = entry;
            Host = host;
            ExtractAudio = extractAudio;
            Progress = 0;
        }

        protected virtual ProcessStartInfo Prepare() {
            return new ProcessStartInfo() {
                FileName = "youtube-dl",
                Arguments = $"{BasicArguments} {SpecialArguments} {Entry.Url}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }

        private Process BeginProcess() {
            lock(this) {
                if(!Alive) {
                    return null;
                }
                DownloadProcess = Process.Start(Prepare());
                return DownloadProcess;
            }
        }

        protected virtual bool ProcessStandardOutput(StreamReader standardOutput) {
            while (Alive) {
                var response = standardOutput.ReadLine();
                if (!ProcessResponse(response)) {
                    return true;
                }
            }
            Entry.Status = Status.CANCELLED;
            Host.ErrorOutput("Cancelled");
            return false;
        }

        protected virtual bool ProcessStandardError(StreamReader standardError) {
            var error = standardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error)) {
                Host.ErrorOutput(error);
                Entry.Status = Status.FAILED;
                return false;
            }
            return true;
        }

        public virtual void Execute() {
            if (Entry.Status == Status.CANCELLED) {
                return;
            }
            Entry.Status = Status.DOWNLOADING;

            string orgPath = Environment.CurrentDirectory;
            Environment.CurrentDirectory = OutputDir;

            try {
                Results.Add(Entry.Id);
                var process = BeginProcess();
                if(process==null) {
                    Entry.Status = Status.CANCELLED;
                    return;
                }
                if(!ProcessStandardOutput(process.StandardOutput)) {
                    return;
                }
                if(!ProcessStandardError(process.StandardError)) {
                    return;
                }

                bool result = ValidateAndGetResult(Results[0], Entry);
                Host.Completed(Entry, result, ExtractAudio);

                if (Results.Count > 1) {
                    // リストだった場合
                    var baseUri = new Uri(Entry.Url);
                    for (int i = 1; i < Results.Count; i++) {
                        var subEntry = DLEntry.Create(Results[i].Id, NormalizeSubUrlForKey(baseUri, i, Results[i].Id));
                        if (ValidateAndGetResult(Results[i], subEntry)) {
                            Host.FoundSubItem(subEntry);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Environment.CurrentDirectory = orgPath;
                Entry.Status = Status.FAILED;
                Logger.error(ex);
            }
        }

        public void Cancel() {
            lock (this) {
                if (Entry.Status == Status.WAITING) {
                    Entry.Status = Status.CANCELLED;
                }
                Alive = false;
                if(null!=DownloadProcess) {
                    DownloadProcess.Kill();
                    DownloadProcess = null;
                }
            }
        }

        /**
         * @param res: youtube-dlからのレスポンス（１行分）
         * @return true: go ahead / false: reached to EOS
         */
        public bool ProcessResponse(string res) {
            if (res == null) {
                return false;
            }
            res = res.Trim();
            if (!res.IsEmpty()) {
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

        /**
         * 結果の検証と、DLEntryへの結果取り出し
         * 
         * ProcessResponse()がFalseを返した後（EOSに達した後）、
         * DLEntryに結果を取り出す。
         */
        private bool ValidateAndGetResult(DownloadResults.ItemInfo info, DLEntry entry) {
            var fname = GetSavedFilePath(info);
            if (!string.IsNullOrEmpty(fname)) {
                if (info.Completed) {
                    entry.Name = info.Name;
                    entry.Status = Status.COMPLETED;
                    if (!ExtractAudio) {
                        entry.VPath = fname;
                        entry.Media = entry.Media.PlusVideo();
                    } else {
                        entry.APath = fname;
                        entry.Media = entry.Media.PlusAudio();
                    }
                    return true;
                } else {
                    entry.Status = (Alive) ? Status.FAILED : Status.CANCELLED;
                    PathUtil.safeDeleteFile(fname);
                    return false;
                }
            } else {
                Host.ErrorOutput($"not found: {fname}");
                entry.Status = Status.FAILED;
                return false;
            }
        }

        protected abstract string GetSavedFilePath(DownloadResults.ItemInfo info);
        protected abstract bool TryParseName(string res);
        protected abstract bool TryParseProgress(string res);

        public abstract string GetIDStringFromURL(Uri uri);
        public abstract string NormalizeUrlForKey(Uri uri);
        public abstract string NormalizeSubUrlForKey(Uri uri, int index, string id);
    }
}
