using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ytplayer.common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace ytplayer.export {
    public class StraightExporter : CopyExporter {
        public StraightExporter(ExportOption option) : base(option) {
        }

        protected CommandProcessor commandProcessor = null;
        protected virtual async Task<bool> Convert(string arguments, Action completed = null) {
            try {
                CommandProcessor processor = null;
                lock (this) {
                    if (commandProcessor != null) {
                        throw new InvalidOperationException("CommandProcessor is already running.");
                    }
                    logger.debug($"arguments={arguments}");
                    processor = new CommandProcessor("ffmpeg", arguments, Option.StdOutProc, Option.StdErrProc, Option.ShowCommandPromptOnConverting);
                    commandProcessor = processor;
                }
                if (!await processor.Execute()||processor.ExitCode!=0) {
                    logger.error("cannot convert media file.");
                    Option.StdErrProc?.Invoke($"Convert Error (ExitCode={processor.ExitCode})");
                    DeleteResult();
                    return false;
                }
                logger.info($"exported: {DstFile}");
                return true;
            }
            finally {
                completed?.Invoke();
                lock (this) {
                    commandProcessor = null;
                }
            }
        }
        public override void Cancel() {
            lock (this) {
                commandProcessor?.Cancel();
                //commandProcessor = null;
            }
        }

        public override async Task<bool> Export() {
            var args = new List<string>();
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

            //var stdoutTask = Task.Run(() => {
            //    while (!proc.StandardOutput.EndOfStream) {
            //        var line = proc.StandardOutput.ReadLine();
            //        Option.StdOutProc?.Invoke(line);
            //    }
            //});
            //var stderrTask = Task.Run(() => {
            //    while (!proc.StandardError.EndOfStream) {
            //        var line = proc.StandardError.ReadLine();
            //        Option.StdErrProc?.Invoke(line);
            //    }
            //});
            //await Task.WhenAll(stdoutTask, stderrTask);
        }
    }
}
