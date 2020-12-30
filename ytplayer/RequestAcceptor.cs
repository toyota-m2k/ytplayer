using common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ytplayer.common;

namespace ytplayer {
    public class RequestAcceptor : IDisposable {
        string RequestFile;
        WeakReference<MainWindow> owner;
        MainWindow Owner => owner?.GetValue();
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;
        TaskCompletionSource<object> completionSource;
        public bool Disposed { get; private set; } = false;

        
        public RequestAcceptor(MainWindow owner) {
            this.owner = new WeakReference<MainWindow>(owner);
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            completionSource = new TaskCompletionSource<object>();
            var local = Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("LOCALAPPDATA"));
            var uwpDir = Path.Combine(local, @"Packages\831bfe09-f728-4dba-964b-2678993c50e3_bax8kcjcv11ke\LocalState");
            var uwpFile = "request.txt";
            RequestFile = Path.Combine(uwpDir, uwpFile);
            if(!PathUtil.isFile(RequestFile)) {
                File.Create(RequestFile).Dispose();
            }
            Loop();
        }

        private void Loop() {
            Task.Run(async () => {
                using (var fs = new FileStream(RequestFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var stream = new StreamReader(fs)) {
                    while(!cancellationToken.IsCancellationRequested) {
                        var url = stream.ReadLine();
                        if (null != url) {
                            Owner.Dispatcher.Invoke(() => {
                                if (!cancellationToken.IsCancellationRequested) {
                                    Owner.RegisterUrl(url);
                                }
                            });
                        }
                        await Task.Delay(100);
                    }
                }
                PathUtil.safeDeleteFile(RequestFile);
                Disposed = true;
                completionSource.TrySetResult(null);
            });
        }

        public async void Dispose() {
            cancellationTokenSource.Cancel();
            await completionSource.Task;
        }

        public async Task WaitForClose() {
            await completionSource.Task;
        }
    }
}
