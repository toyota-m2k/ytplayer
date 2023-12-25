using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.common;

namespace ytplayer.export {
    public class MergeExporter : StraightExporter {
        IEnumerable<string> SourceFiles;

        public MergeExporter(ExportOption option, IEnumerable<string> sourceFiles):base(option) {
            this.SourceFiles = sourceFiles;
        }

        public override async Task<bool> Export() {
            var stdOutProc = Option.StdOutProc;
            if (stdOutProc == null) {
                stdOutProc($"Merging: to {Path.GetFileName(Option.DstFile)}");
            }
            var workDir = Settings.Instance.EnsureWorkPath;
            var uuid = Guid.NewGuid().ToString("N");
            var listFile = Path.Combine(workDir, $"{uuid}.txt");
            using(var sw = new StreamWriter(listFile)) {
                foreach (var path in SourceFiles) {
                    sw.WriteLine($"file {path.Replace("\\","/")}");
                }
                sw.Flush();
            }

            var args = new List<string>();
            args.Add("-f");
            args.Add("concat");
            args.Add("-safe");      // -safe 0 相対パス可
            args.Add("0");
            args.Add("-i");
            args.Add($"\"{listFile}\"");
            if (Option.Overwrite) {
                args.Add("-y");
            }
            else {
                args.Add("-n");
            }
            if (Option.OnlyAudio) {
                args.Add("-vn");
                args.Add("-f");
                args.Add("mp3");
            }
            if (Option.NoTranscode) {
                args.Add("-c");
                args.Add("copy");
            }
            args.Add($"\"{Option.DstFile}\"");   
            var arguments = string.Join(" ", args);
            return await Convert(arguments, () => PathUtil.safeDeleteFile(listFile));
        }
    }
}
