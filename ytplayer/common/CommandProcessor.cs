using io.github.toyota32k.toolkit.utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ytplayer.common {
    public class CommandProcessor {
        public string Arguments { get; set; }
        public string Command;
        public bool ShowCommandPrompt = false;
        public int ExitCode = -1;

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
        protected virtual bool OnPostExecute(int exitCode) { return true; }

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
        private Process CommandProcess = null;

        public async Task<bool> Execute() {
            if (!OnPreExecute()) return false;

            Task stdoutTask = Task.CompletedTask;
            Task stderrTask = Task.CompletedTask;

            Process proc = null;
            try {
                lock (this) {
                    proc = Process.Start(MakeParams());
                    if (proc == null) {
                        LoggerEx.error("no process.");
                        return false;
                    }
                    CommandProcess = proc;
                }
                if (!ShowCommandPrompt && HandleStandardOutput != null) {
                    stdoutTask = Task.Run(() => {
                        while (!proc.StandardOutput.EndOfStream) {
                            var line = proc.StandardOutput.ReadLine();
                            if (!HandleStandardOutput.Invoke(line)) {
                                throw new Exception("cancelled on handling stardard output");
                            }
                        }
                    });
                }
                if (!ShowCommandPrompt && HandleStandardError != null) {
                    stderrTask = Task.Run(() => {
                        while (!proc.StandardError.EndOfStream) {
                            var line = proc.StandardError.ReadLine();
                            if (!HandleStandardError.Invoke(line)) {
                                throw new Exception("cancelled on handling stardard error");
                            }
                        }
                    });
                }
                await Task.WhenAll(stdoutTask, stderrTask);
                proc.WaitForExit();
                ExitCode = proc.ExitCode;
                LoggerEx.info($"Proc exit: {proc.ExitCode}");
                return OnPostExecute(ExitCode);
            }
            catch (Exception e) {
                LoggerEx.error(e);
                return false;
            }
            finally {
                proc?.Close();
            }
        }

        public void Cancel() {
            CommandProcess?.Kill();
            CommandProcess?.StandardOutput?.Close();
            CommandProcess?.StandardError?.Close();
            CommandProcess?.Close();
            CommandProcess = null;
        }
    }
}
