using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;

namespace ytplayer.export {
    public class SplitExporter : CopyExporter {
        IEnumerable<ExtractExporter> Exporters;
        public override string DstFile => throw new NotSupportedException("User DstFiles instead.");
        public List<string> DstFiles { get; private set; } = null;

        private IEnumerable<ExtractExporter> createExporters(ExportOption rootOption, IEnumerable<NamedPlayRange> ranges) {
            int index = 1;
            var ext = Path.GetExtension(rootOption.DstFile);
            var baseName = Path.GetFileNameWithoutExtension(rootOption.DstFile);
            var dstDir = Path.GetDirectoryName(rootOption.DstFile);

            foreach(NamedPlayRange r in ranges) {
                var dstName = string.IsNullOrEmpty(r.Name) ? $"{baseName}-({index++})" : $"{r.Name}-{baseName}";
                var dstFile = Path.Combine(dstDir, $"{dstName}{ext}");
                var option = ExportOption.DeriveFrom(rootOption, dstFile);
                yield return new ExtractExporter(option, r.Start, r.End);
            }
        }


        public SplitExporter(ExportOption option, IEnumerable<NamedPlayRange> ranges):base(option) {
            Exporters = createExporters(option, ranges);
        }

        public override async Task<bool> Export() {
            var stdOutProc = Option.StdOutProc;
            if (stdOutProc == null) {
                stdOutProc($"Splitting from {Path.GetFileName(Option.SrcFile)}");
            }

            foreach (var item in Exporters) {
                if(!await item.Export()) {
                    DeleteResult();
                    return false;
                }
            }
            DstFiles = Exporters.Select(e => e.DstFile).ToList();
            return true;
        }


        public override void DeleteResult() {
            foreach (var item in Exporters) {
                item.DeleteResult();
            }
        }
        public override void Cancel() {
            foreach(var item in Exporters) {
                item.Cancel();
            }
        }
    }
}
