using common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download.downloader {
    /**
     * プロセッサが返すダウンロードアイテム情報を保持するクラス
     */
    public class DownloadItemInfo {
        public string Name { get; }
        public string Id { get; }
        public bool AlreadyDownloaded { get; }
        public DownloadItemInfo(string name, string id, bool already) {
            Name = name;
            Id = id;
            AlreadyDownloaded = already;
        }
    }

    /**
     * Processor の共通実装
     */
    public abstract class DownloaderBase : IDownloader {
        /**
         * ダウンロード結果を保持するリスト
         */
        protected List<DownloadItemInfo> Results { get; } = new List<DownloadItemInfo>();

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
        public int Progress { get; protected set; } = 0;

        public DLEntry Entry { get; }
        protected IDownloadHost Host { get; }
        protected bool ExtractAudio { get; }

        protected string TargetUrlOrId => (!ExtractAudio) ? Entry.Url : GetIDStringFromURL(new Uri(Entry.Url));
        protected string OutputDir => !ExtractAudio ? Settings.Instance.EnsureVideoPath : Settings.Instance.EnsureAudioPath;
        protected string OutputExtension => !ExtractAudio ? "mp4" : "mp3";


        protected DownloaderBase(DLEntry entry, IDownloadHost host, bool extractAudio) {
            Entry = entry;
            Host = host;
            ExtractAudio = extractAudio;
        }

        protected ProcessStartInfo Prepare() {
            return new ProcessStartInfo() {
                FileName = "youtube-dl",
                Arguments = $"{BasicArguments} {SpecialArguments} {Entry.Url}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }


        public void Execute() {
            if (Entry.Status==Status.CANCELLED) {
                return;
            }
            Entry.Status = Status.DOWNLOADING;

            string orgPath = Environment.CurrentDirectory;
            Environment.CurrentDirectory = OutputDir;

            try {
                var psi = Prepare();
                var process = Process.Start(psi);
                while (true) {
                    var response = process.StandardOutput.ReadLine();
                    if (!ProcessResponse(response)) {
                        break;
                    }
                }
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error)) {
                    Host.ErrorOutput(error);
                    Entry.Status = Status.FAILED;
                    return;
                }

                if (Results.Count == 0) {
                    Host.ErrorOutput("no data");
                    Entry.Status = Status.FAILED;
                    return;
                }
                bool result = ValidateAndGetResult(Results[0], Entry);
                Host.Completed(Entry, result, ExtractAudio);

                if (Results.Count > 1) {
                    // リストだった場合
                    var baseUri = new Uri(Entry.Url);
                    for (int i = 1; i < Results.Count; i++) {
                        var subEntry = DLEntry.Create(NormalizeSubUrlForKey(baseUri, i, Results[i].Id));
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
            if(Entry.Status==Status.WAITING) {
                Entry.Status = Status.CANCELLED;
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
        private bool ValidateAndGetResult(DownloadItemInfo info, DLEntry entry) {
            var fname = GetSavedFilePath(info);
            if (!string.IsNullOrEmpty(fname)) { 
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
                Host.ErrorOutput($"not found: {fname}");
                entry.Status = Status.FAILED;
                return false;
            }
        }

        protected abstract string GetSavedFilePath(DownloadItemInfo info);
        protected abstract bool TryParseName(string res);
        protected abstract bool TryParseProgress(string res);

        public abstract string GetIDStringFromURL(Uri uri);
        public abstract string NormalizeUrlForKey(Uri uri);
        public abstract string NormalizeSubUrlForKey(Uri uri, int index, string id);
    }
}
