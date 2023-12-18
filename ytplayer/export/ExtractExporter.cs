using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.common;

namespace ytplayer.export {
    internal class ExtractExporter {
        public IExportOption Option { get; set; }
        public bool IsExtracted => DstFile != null && System.IO.File.Exists(DstFile);
        
        public ExtractExporter(IExportOption option) {
            Option = option;
        }

        public ExtractTo(string dstFile, long startMs, long endMs) {
            var args = new List<string>();
            args.Add("-ss");
            args.Add(startMs.ToString());
            args.Add("-to");
            args.Add(endMs.ToString());
            args.Add("-i");
            args.Add(Option.SrcFile);
            if(Option.OnlyAudio) {
                args.Add("-vn");
                args.Add("-f");
                args.Add("mp3");
            }
            if(Option.NoTranscode) {
                args.Add("-c");
                args.Add("copy");
            }
            args.Add(dstFile);
            var arguments = string.Join(" ", args);

            var processor = new CommandProcessor("ffmpeg", arguments, Option.StdOutProc, Option.StdErrProc, Option.ShowCommandPromptOnConverting);
            if(await processor.Execute()) {
                DstFile
            }
        }
    }
}
