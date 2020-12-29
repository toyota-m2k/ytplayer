using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using common;
using ytplayer.common;

namespace ytplayer.ipc {
    public class PipeServer : IDisposable {
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        TaskCompletionSource<object> closed;
        WeakReference<MainWindow> owner;
        NamedPipeServerStream pipeStream;

        private MainWindow Owner => owner?.GetValue();

        private bool Alive => !cancellationToken.IsCancellationRequested;

        public PipeServer(MainWindow owner) {
            this.owner = new WeakReference<MainWindow>(owner);
            Loop();
        }

        public async Task Cancel() {
            cancellationTokenSource.Cancel();
            pipeStream.Close();
            await closed.Task;
        }

        private void Loop() {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            closed = new TaskCompletionSource<object>();

            Task.Run(async () => {
                var ps = new PipeSecurity();
                var sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
                var par = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
                ps.AddAccessRule(par);
                using (pipeStream = new NamedPipeServerStream("BooTube.BrowserPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 2048, 2048, ps)) {
                    while (Alive) {
                        try {
                            await pipeStream.WaitForConnectionAsync(cancellationToken);
                            using (var reader = new StreamReader(pipeStream)) {
                                while (pipeStream.IsConnected && Alive) {
                                    var str = await reader.ReadLineAsync();
                                    var owner = Owner;
                                    if (null == owner) {
                                        cancellationTokenSource.Cancel();
                                        break;
                                    }
                                    owner.Dispatcher.Invoke(() => {
                                        owner.RegisterUrl(str);
                                    });
                                }
                            }
                        } catch(Exception e) {
                            Debug.Assert(!Alive);
                            Logger.error(e);
                        }
                    }
                }
                closed.TrySetResult(null);
            });
        }

        public void Dispose() {
            _ = Cancel();
        }
    }
}
