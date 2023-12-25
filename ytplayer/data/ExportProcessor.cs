using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.export {
    public class ExportProcessor {
        string OutputDir { get; }
        DLEntry Source { get; }
        ChapterList ChapterList { get; }

        [Flags]
        public enum Operation {
            COPY = 0,           // そのままコピー
            SPRIT = 1,          // 分割（チャプター毎に分割してファイルに出力）可能　＝有効なチャプターが２つ以上ある
            TRIMMING = 2,       // トリミング（スキップ部分をカットして１つのファイルに出力）可能　＝無効なチャプターが１つ以上ある
            EXTRACT_AUDIO = 4,  // 入力がビデオファイル
        }

        //public bool OnlyAudio { get; }
        //public bool NoTranscode { get; }
        public Func<string, bool> StdOutProc { get; }
        public Func<string, bool> StdErrProc { get; }

        private static readonly LoggerEx logger = new LoggerEx("EXPORT");

        public static Operation GetAvailableOperation(DLEntry entry, ChapterList chapterList) {
            var op = Operation.COPY;
            if (chapterList != null) {
                if (chapterList.Values.Where(c => !c.Skip).Count() > 1) {
                    // 有効なチャプターが２つ以上ある
                    op |= Operation.SPRIT;
                }
                if (entry.TrimStart>0 || entry.TrimEnd>0 || chapterList.Values.Where(c => c.Skip).Count() > 0) {
                    // トリミングされている、または、無効なチャプターが１つ以上ある
                    op |= Operation.TRIMMING;
                }
            }
            if(entry.Media.HasFlag(MediaFlag.VIDEO)) {
                op |= Operation.EXTRACT_AUDIO;
            }
            return op;
        }

        public Operation AvailableOperation { get; }

        //private IExporter Exporter { get; set; }

        public ExportProcessor(DLEntry entry, ChapterList chapterList, string outputDir, Func<string, bool> stdOutProc, Func<string, bool> stdErrProc) {
            OutputDir = outputDir;
            Source = entry;
            ChapterList = chapterList;
            StdOutProc = stdOutProc;
            StdErrProc = stdErrProc;
            AvailableOperation = GetAvailableOperation(entry, chapterList);
        }

        /**
         * 出力ファイル名を生成する
         */
        private string safeFileName(string name) {
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        private IExporter _exporter = null;
        private IExporter Exporter { 
            get { 
                lock(this) { 
                    return _exporter; 
                } 
            }
            set {
                lock(this) {
                    _exporter = value;
                }
            }
        }

        public async Task<bool> Export(Operation op, string fileName) { 
            bool checkFlag(Operation flag) {
                return op.HasFlag(flag) && AvailableOperation.HasFlag(flag);
            }
            bool extractAudio = checkFlag(Operation.EXTRACT_AUDIO);
            string srcFile = Source.Path;
            string ext = Path.GetExtension(srcFile).ToLower();
            if(extractAudio) {
                ext = ".mp3";
                if (Source.Media.HasAudio()) {
                    srcFile = Source.APath;
                    extractAudio = false;
                }
            }
            if(string.IsNullOrEmpty(fileName)) {
                fileName = Path.GetFileNameWithoutExtension(srcFile);
            } else {
                fileName = safeFileName(fileName);
            }
            string dstFile = Path.Combine(OutputDir, $"{fileName}{ext}");

            if (srcFile == dstFile) {
                throw new Exception("srcFile == dstFile");
            }
            var noTranscode = !extractAudio;
            var option = ExportOption.Create(srcFile, dstFile, extractAudio, noTranscode: noTranscode, overwrite: true, StdOutProc, StdErrProc, false);
            IExporter exporter;

            if (checkFlag(Operation.SPRIT)) {
                var ranges = ChapterList.GetEnabledChaptersAsNamedRanges(new PlayRange(Source.TrimStart, Source.TrimEnd)).ToList();
                exporter = new SplitExporter(option, ranges);
            } else if (checkFlag(Operation.TRIMMING)) {
                var ranges = ChapterList.GetEnabledRanges(new PlayRange(Source.TrimStart, Source.TrimEnd)).ToList();
                exporter = new TrimExporter(option, ranges);
            } else if(extractAudio) {
                exporter = new StraightExporter(option);
            } else { 
                exporter = new CopyExporter(option);
            }
            Exporter = exporter;
            try {
                return await exporter.Export();
            } finally {
                Exporter = null;
            }
        }

        public void Cancel() {
            Exporter?.Cancel();
            //Exporter = null;
        }
    }
}
