using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.wav {
    public class CommandProcessor {
        public string Arguments { get; set; }
        public string Command;
        public bool ShowCommandPrompt = false;

        public Action<string> StandardOutput;
        public Action<string> StandardError;

        protected virtual bool Prepare() { return true; }

        protected virtual ProcessStartInfo MakeParams() {
            var info = new ProcessStartInfo() {
                FileName = Command,
                Arguments = Arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            if(!ShowCommandPrompt) {
                info.StandardOutputEncoding = System.Text.Encoding.UTF8;
                info.StandardErrorEncoding = System.Text.Encoding.UTF8;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
            }
            return info;
        }

        public Task<bool> Execute() {
            return Task.Run(() => {
                if(!Prepare()) return false;
                Process proc = null;
                try {
                    proc = Process.Start(MakeParams());
                    if (proc == null) {
                        LoggerEx.error("no process.");
                        return false;
                    }
                    string std;
                    if (!ShowCommandPrompt && StandardOutput != null) {
                        while ((std = proc.StandardOutput.ReadLine()) != null) {
                            StandardOutput.Invoke(std);
                        }
                    }
                    if (!ShowCommandPrompt && StandardError != null) {
                        while ((std = proc.StandardError.ReadLine()) != null) {
                            StandardError?.Invoke(std);
                        }
                    }
                    proc.WaitForExit();
                    LoggerEx.info($"Proc exit: {proc.ExitCode}");
                    return true;
                } catch(Exception e) {
                    LoggerEx.error(e);
                    return false;
                } finally {
                    proc?.Close();
                }
            });
        }
    }
}
