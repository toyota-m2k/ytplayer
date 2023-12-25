using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.export {
    public class TrimExporter : CopyExporter {
        List<PlayRange> Ranges { get; }
        public TrimExporter(ExportOption option, IEnumerable<PlayRange> ranges):base(option) {
            Ranges = ranges.ToList();
        }
        private IExporter _currentExporter = null;
        private IExporter currentExporter {
            get {
                lock (this) {
                    return _currentExporter;
                }
            }
            set {
                lock(this) {
                    _currentExporter = value;   
                }
            }
        }
        public override async Task<bool> Export() {
            try {
                if (Ranges.Count == 1) {
                    // 1つの区間だけなら、ExtractExporterを使う
                    var exporter = new ExtractExporter(Option, Ranges[0].Start, Ranges[0].End);
                    currentExporter = exporter;
                    return await exporter.Export();
                }
                else {
                    // 複数の区間なら、SplitExporter + MergeExporter を使う
                    var workDir = Settings.Instance.EnsureWorkPath;
                    var uuid = Guid.NewGuid().ToString("N");
                    var ext = Path.GetExtension(Option.SrcFile);
                    var splitExporter = new SplitExporter(ExportOption.DeriveFrom(Option, Path.Combine(workDir, $"{uuid}{ext}")), Ranges.Select((it) => new NamedPlayRange(it.Start, it.End, null)));
                    currentExporter = splitExporter;
                    if (!await splitExporter.Export()) {
                        return false;
                    }
                    try {
                        var mergeExporter = new MergeExporter(Option, splitExporter.DstFiles);
                        currentExporter = mergeExporter;
                        return await mergeExporter.Export();
                    }
                    finally {
                        splitExporter.DeleteResult();
                    }
                }
            }
            finally {
                currentExporter = null;
            }
        }
        public override void Cancel() {
            currentExporter?.Cancel();
            //currentExporter = null;
        }
    }
}
