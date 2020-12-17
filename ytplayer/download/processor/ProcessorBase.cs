using common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.download.processor {
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
    public abstract class ProcessorBase : IProcessor {
        /**
         * ダウンロード結果を保持するリスト
         */
        protected List<DownloadItemInfo> Results { get; } = new List<DownloadItemInfo>();

        /**
         * youtube-dlに渡す、サイト固有の引数（不要なら空文字）
         */
        protected virtual string SpecialArguments => "--format mp4";

        /**
         * ダウンロード進捗(%)を保持するプロパティ... とりあえず int型。未使用。
         * もしプログレスバーなどを表示するならその時考えるけど、わりとDLはすぐに終わるから、今のところ不要では？
         */
        public int Progress { get; protected set; } = 0;

        protected ProcessorBase() {
        }

        protected ProcessStartInfo Prepare(DLEntry entry) {
            return new ProcessStartInfo() {
                FileName = "youtube-dl",
                Arguments = $"{SpecialArguments} { entry.Url }",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }


        public void Download(DLEntry entry, IDownloadHost host) {
            Progress = 0;
            Results.Clear();

            try {
                var psi = Prepare(entry);
                var process = Process.Start(psi);
                while (true) {
                    var response = process.StandardOutput.ReadLine();
                    if (!ProcessResponse(response, host)) {
                        break;
                    }
                }
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error)) {
                    host.ErrorOutput(error);
                    entry.Status = Status.FAILED;
                    return;
                }

                if (Results.Count == 0) {
                    host.ErrorOutput("no data");
                    entry.Status = Status.FAILED;
                    return;
                }
                bool result = ValidateAndGetResult(Results[0], entry);
                host.Completed(entry, result);

                if (Results.Count > 1) {
                    // リストだった場合
                    var baseUri = new Uri(entry.Url);
                    for (int i = 1; i < Results.Count; i++) {
                        var subEntry = DLEntry.Create(NormalizeSubUrlForKey(baseUri, i, Results[i].Id));
                        if (ValidateAndGetResult(Results[i], subEntry)) {
                            host.FoundSubItem(subEntry);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.error(ex);
            }
        }

        /**
         * @param res: youtube-dlからのレスポンス（１行分）
         * @return true: go ahead / false: reached to EOS
         */
        public bool ProcessResponse(string res, IDownloadHost host) {
            if (res == null) {
                return false;
            }
            res = res.Trim();
            if (!res.IsEmpty()) {
                Logger.debug(res);
                do {
                    if (TryParseName(res)) {
                        host.StandardOutput(res);
                        break;
                    }
                    if (TryParseProgress(res)) {
                        break;
                    }
                    host.StandardOutput(res);
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
        public bool ValidateAndGetResult(DownloadItemInfo info, DLEntry entry) {
            var fname = GetSavedFilePath(info);
            if (!string.IsNullOrEmpty(fname)) { 
                entry.Name = info.Name;
                entry.VPath = fname;
                entry.Media |= MediaFlag.VIDEO;
                entry.Status = Status.DOWNLOADED;
                return true;
            } else {
                entry.Status = Status.FAILED;
                return false;
            }
        }

        protected abstract string GetSavedFilePath(DownloadItemInfo info);
        protected abstract bool TryParseName(string res);
        protected abstract bool TryParseProgress(string res);

        public abstract bool IsAcceptableUrl(Uri uri);
        public abstract string GetIDStringFromURL(Uri uri);
        public abstract string NormalizeUrlForKey(Uri uri);
        public abstract string NormalizeSubUrlForKey(Uri uri, int index, string id);
    }
}
