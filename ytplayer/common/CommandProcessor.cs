using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.common {
    public class CommandProcessor {
        public string Arguments { get; set; }
        public string Command;
        public bool ShowCommandPrompt = false;

        public Func<string,bool> HandleStandardOutput;
        public Func<string,bool> HandleStandardError;


        public CommandProcessor(string command, string arguments, Func<string, bool> stdoutProc, Func<string,bool> stderrProc, bool commandPrompt=false) {
            Command = command;
            Arguments = arguments;
            HandleStandardOutput = stdoutProc;
            HandleStandardError = stderrProc;
            ShowCommandPrompt = commandPrompt;
        }

        protected virtual bool OnPreExecute() { return true; }

        protected virtual ProcessStartInfo MakeParams() {
            if (ShowCommandPrompt) {
                return new ProcessStartInfo() {
                    FileName = Command,
                    Arguments = Arguments,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                };
            }
            else {
                return new ProcessStartInfo() {
                    FileName = Command,
                    Arguments = Arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                };
            };
        }

        public Task<bool> Execute() {
            return Task.Run(() => {
                if (!OnPreExecute()) return false;
                Process proc = null;
                try {
                    proc = Process.Start(MakeParams());
                    if (proc == null) {
                        LoggerEx.error("no process.");
                        return false;
                    }
                    string std;
                    if (!ShowCommandPrompt && HandleStandardOutput != null) {
                        while ((std = proc.StandardOutput.ReadLine()) != null) {
                            if(!HandleStandardOutput.Invoke(std)) {
                                throw new Exception("cancelled on handling stardard output");
                            }
                        }
                    }
                    if (!ShowCommandPrompt && HandleStandardError!= null) {
                        while ((std = proc.StandardError.ReadLine()) != null) {
                            if (!HandleStandardError.Invoke(std)) {
                                throw new Exception("cancelled on handling stardard error");
                            }
                        }
                    }
                    proc.WaitForExit();
                    LoggerEx.info($"Proc exit: {proc.ExitCode}");
                    return true;
                }
                catch (Exception e) {
                    LoggerEx.error(e);
                    return false;
                }
                finally {
                    proc?.Close();
                }
            });
        }
    }
}
