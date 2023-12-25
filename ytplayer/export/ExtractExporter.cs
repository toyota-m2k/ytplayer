using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.export {
    /**
     * いちばんプリミティブな、メディアファイルから、指定区間を１つだけ抜き出すエクスポータークラス
     */
    public class ExtractExporter : StraightExporter {
        private ulong StartMs;
        private ulong EndMs;
        
        public ExtractExporter(ExportOption option, ulong startMs, ulong endMs) : base(option) {
            StartMs = startMs;
            EndMs = endMs;
        }

        private string TimeInSec(ulong ms) {
            return $"{ms / 1000}.{ms % 1000}";
        }

        public override async Task<bool> Export() {
            var stdOutProc = Option.StdOutProc;
            if (stdOutProc == null) {
                stdOutProc($"Extracting: {DLEntry.FormatDuration(StartMs / 1000)} - {DLEntry.FormatDuration(EndMs / 1000)} from {Path.GetFileName(Option.SrcFile)}");
            }

            var args = new List<string>();
            if (StartMs > 0) {
                args.Add("-ss");
                args.Add(TimeInSec(StartMs));
            }
            if (EndMs > 0) {
                args.Add("-to");
                args.Add(TimeInSec(EndMs));
            }
            args.Add("-i");
            args.Add($"\"{Option.SrcFile}\"");
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
            return await Convert(arguments);
        }
    }
}
