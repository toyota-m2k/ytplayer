using common;
using Reactive.Bindings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ytplayer.interop;

namespace ytplayer {
    public class MainViewModel : MicViewModelBase {
        public ReactivePropertySlim<bool> AutoDownload { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> OnlySound { get; } = new ReactivePropertySlim<bool>(false);
        public ReactiveCommand CommandDownloadNow { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSettings { get; } = new ReactiveCommand();


        public MainViewModel() {
            //CommandAutoDownload.Subscribe(() => {
            //    AutoDownload.Value = !AutoDownload.Value;
            //});
        }
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            DataContext = new MainViewModel();
            InitializeComponent();
        }

        private MainViewModel viewModel => DataContext as MainViewModel;

        //class PathComparator : IEqualityComparer<string> {
        //    public new bool Equals(string x, string y) {
        //        return System.IO.Path.GetDirectoryName(x) == System.IO.Path.GetDirectoryName(y);
        //    }

        //    public int GetHashCode(string obj) {
        //        return System.IO.Path.GetDirectoryName(obj).GetHashCode();
        //    }
        //}

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            Settings.Instance.Placement.ApplyPlacementTo(this);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            Settings.Instance.Placement.GetPlacementFrom(this);
            Settings.Instance.Serialize();
        }

        private readonly string[] youtube_urls = {
            "https://www.youtube.com/",
            "https://i.ytimg.com/",
        };
        private bool isYoutubeUrl(string url) {
            if (url != null) {
                foreach (var y in youtube_urls) {
                    if (url.StartsWith(y)) {
                        return true;
                    }
                }
            }
            return false;
        }

        private void download(string url) {
            if(!isYoutubeUrl(url)) {
                return;
            }

            var psi = new ProcessStartInfo() {
                FileName = "youtube-dl",
                Arguments = $"--get-title {url}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // RedirectStandardInput = true,
            };
            //psi.EnvironmentVariables["Path"] = path;
            var process = Process.Start(psi);
            var s = process.StandardOutput.ReadToEnd();
            Debug.WriteLine(s);
            Output.Text += "\n";
            Output.Text += s;
            s = process.StandardError.ReadToEnd();
            Output.Text += s;
            Output.ScrollToEnd();
        }

        private async void Button_Click(object sender, RoutedEventArgs e) {

            var psi = new ProcessStartInfo() {
                FileName = "youtube-dl",
                Arguments = "--help",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                //RedirectStandardError = true,
                // RedirectStandardInput = true,
            };
            //psi.EnvironmentVariables["Path"] = path;
            var process = Process.Start(psi);
            var s = process.StandardOutput.ReadToEnd();
            Debug.WriteLine(s);
            Output.Text += s;

            if(!process.HasExited) {
                await Task.Run(async () => {
                    while (!process.HasExited) {
                        await Task.Delay(10);
                    }
                });
            }
            Output.Text += $"Done:{process.ExitCode}";
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            download(e.Data.GetData(DataFormats.Text) as string);
            //var fmts = e.Data.GetFormats();
            //foreach(var f in fmts) {
            //    try {
            //        var o = e.Data.GetData(f);
            //        Debug.WriteLine($"{f}: {o.ToString()}");
            //    } catch(Exception ex) {
            //        Debug.WriteLine($"{f}: error.");
            //    }
            //}
        }

        private ClipboardMonitor clipboardMonitor = null;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            clipboardMonitor = new ClipboardMonitor(this, true);
            clipboardMonitor.ClipboardUpdate += OnClipboardUpdated;
        }

        private void OnClipboardUpdated(object sender, EventArgs e) {
            download(Clipboard.GetText());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            clipboardMonitor.Dispose();
        }

    }
}
