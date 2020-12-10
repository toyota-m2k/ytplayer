using common;
using Reactive.Bindings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ytplayer.common;
using ytplayer.data;
using ytplayer.dialog;
using ytplayer.interop;

namespace ytplayer {
    public class MainViewModel : MicViewModelBase {
        public ReactiveProperty<ObservableCollection<DLEntry>> MainList { get; } = new ReactiveProperty<ObservableCollection<DLEntry>>(new ObservableCollection<DLEntry>());

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
        private Storage mStorage = null;
        private ClipboardMonitor mClipboardMonitor = null;


        public MainWindow() {
            viewModel = new MainViewModel();
            viewModel.CommandSettings.Subscribe(() => {
                var dlg = new SettingDialog(mStorage);
                dlg.ShowDialog();
                if(dlg.Result) {
                    if(dlg.NewStorage!=null) {
                        mStorage.Dispose();
                        mStorage = dlg.NewStorage;
                        RefreshList();
                    }
                }
            });
            InitializeComponent();
        }

        private MainViewModel viewModel {
            get => DataContext as MainViewModel;
            set => DataContext = value;
        }

        private void RefreshList() {
            viewModel.MainList.Value = new ObservableCollection<DLEntry>(mStorage.DLTable.List);
        }

        private void RegisterUrl(string url) {
            url = url.Trim();
            if(!url.StartsWith("https://")&&!url.StartsWith("http://")) {
                return;
            }
            url = url.Split(Utils.Array("\r", "\n", " ", "\t"), StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
            if(string.IsNullOrEmpty(url)) {
                return;
            }
            var target = DLEntry.Create(url);
            mStorage.DLTable.Add(target, true);
        }

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
            RegisterUrl(e.Data.GetData(DataFormats.Text) as string);

            //download(e.Data.GetData(DataFormats.Text) as string);

            
            
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

        private void InitStorage(bool forceCreate=false) {
            if (forceCreate || !PathUtil.isFile(Settings.Instance.EnsureDBPath)) {
                while(true) {
                    var dlg = new SettingDialog(null);
                    dlg.ShowDialog();
                    if (dlg.Result && dlg.NewStorage!=null) {
                        SetStorage(dlg.NewStorage);
                        return;
                    }
                }
            } else {
                try {
                    var storage = new Storage(Settings.Instance.EnsureDBPath);
                    SetStorage(storage);
                } catch(Exception e) {
                    Logger.error(e);
                    mStorage = null;
                    InitStorage(true);
                }
            }
        }

        private void SetStorage(Storage newStorage) {
            if(mStorage!=null && mStorage!=newStorage) {
                mStorage.Dispose();
            }
            mStorage = newStorage;
            mStorage.DLTable.AddEvent += OnDLEntryAdd;
            mStorage.DLTable.DelEvent += OnDLEntryDel;
            RefreshList();
        }

        private void OnDLEntryDel(DLEntry entry) {
            viewModel.MainList.Value.Remove(entry);
        }

        private void OnDLEntryAdd(DLEntry entry) {
            viewModel.MainList.Value.Add(entry);
            // ToDo: Sort ... see DxxDBViewerWindow.xaml.cs SortInfo, etc...
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            InitStorage();
            mClipboardMonitor = new ClipboardMonitor(this, true);
            mClipboardMonitor.ClipboardUpdate += OnClipboardUpdated;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            mClipboardMonitor.Dispose();
        }

        private void OnClipboardUpdated(object sender, EventArgs e) {
            RegisterUrl(Clipboard.GetText());
            //download(Clipboard.GetText());
        }

        private void OnHeaderClick(object sender, RoutedEventArgs e) {
            // Sort
        }

        private void OnListItemDoubleClick(object sender, MouseButtonEventArgs e) {

        }
    }
}
