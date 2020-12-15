using common;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ytplayer.common;
using ytplayer.data;
using ytplayer.dialog;
using ytplayer.download;
using ytplayer.interop;
using ytplayer.player;

namespace ytplayer {
    public class OutputMessage {
        static Brush ErrorColor = new SolidColorBrush(Colors.Red);
        static Brush StandardColor = new SolidColorBrush(Colors.Black);
        public string Message { get; }
        public bool Error { get; }
        public Brush Color => Error ? ErrorColor : StandardColor;

        public OutputMessage(string message, bool error) {
            Message = message;
            Error = error;
        }
    }

    public enum DialogTypeId {
        DELETE_COMFIRM,
        ACCEPT_DETERMINATION,
    }

    public class MainViewModel : MicViewModelBase {
        public ReactiveProperty<ObservableCollection<DLEntry>> MainList { get; } = new ReactiveProperty<ObservableCollection<DLEntry>>(new ObservableCollection<DLEntry>());
        public ReactivePropertySlim<bool> AutoDownload { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> AutoPlay { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> OnlySound { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> IsBusy { get; } = new ReactivePropertySlim<bool>(false);
        //public ReactivePropertySlim<bool> IsSettingNow { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ClipboardWatching { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> StatusString { get; } = new ReactivePropertySlim<string>();
        public ObservableCollection<OutputMessage> OutputList { get; } = new ObservableCollection<OutputMessage>();
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.FilterList);
        public ReactivePropertySlim<Category> CurrentCategory { get; } = new ReactivePropertySlim<Category>(Settings.Instance.Categories.All);
        public RatingFilter RatingFilter { get; } = new RatingFilter();

        public bool BusyWithModal = false;

        public ReactiveCommand CommandDownloadNow { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSettings { get; } = new ReactiveCommand();
        public ReactiveCommand CommandClearOutput { get; } = new ReactiveCommand();
        public ReactiveCommand CommandFoldOutput { get; } = new ReactiveCommand();
        public ReactiveCommand CommandPlay { get; } = new ReactiveCommand();

        // Dialog
        private TaskCompletionSource<bool> DialogTask { get; set; } = null;
        public ReactivePropertySlim<bool> DialogActivated { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> DialogTitle { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<DialogTypeId> DialogType { get; } = new ReactivePropertySlim<DialogTypeId>();
        public ReactiveCommand CommandCancel { get; } = new ReactiveCommand();
        public ReactiveCommand CommandOk { get; } = new ReactiveCommand();
        public class DeleteItemDialogViewModel : MicViewModelBase {
            public ReactivePropertySlim<bool> DeleteItemsFromList { get; } = new ReactivePropertySlim<bool>(false);
            public ReactivePropertySlim<bool> DeleteDownloadedFiles { get; } = new ReactivePropertySlim<bool>(false);
        }
        public DeleteItemDialogViewModel DeleteItemDialog { get; } = new DeleteItemDialogViewModel();
        public Task<bool> ShowDialog(DialogTypeId type, string title) {
            BusyWithModal = true;
            DialogTask = new TaskCompletionSource<bool>();
            DialogType.Value = type;
            DialogTitle.Value = title;
            DialogActivated.Value = true;
            return DialogTask.Task;
        }
        public Task<bool> ShowDeleteItemDialog() {
            return ShowDialog(DialogTypeId.DELETE_COMFIRM, "Delete Items");
        }
        public class DeterminationDialogViewModel : MicViewModelBase {
            public ReactivePropertySlim<string> Host { get; } = new ReactivePropertySlim<string>();
            public ReactiveCommand CommandOk { get; } = new ReactiveCommand();
        }
        public DeterminationDialogViewModel DeterminationDialog { get; } = new DeterminationDialogViewModel();
        public Task<bool> ShowDeterminationDialog(string host) {
            DeterminationDialog.Host.Value = host;
            return ShowDialog(DialogTypeId.ACCEPT_DETERMINATION, "Accept or Reject");
        }

        public MainViewModel() {
            //CommandAutoDownload.Subscribe(() => {
            //    AutoDownload.Value = !AutoDownload.Value;
            //});
            CommandCancel.Subscribe(() => {
                DialogTask.TrySetResult(false);
                DialogActivated.Value = false;
                DialogTask = null;
                BusyWithModal = false;
            });
            CommandOk.Subscribe(() => {
                DialogTask.TrySetResult(true);
                DialogActivated.Value = false;
                DialogTask = null;
                BusyWithModal = false;
            });
        }
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, IDownloadHost {
        //private Storage mStorage = null;
        private DownloadManager mDownloadManager = null;
        private ClipboardMonitor mClipboardMonitor = null;
        private Storage Storage => mDownloadManager?.Storage;

        public MainWindow() {
            viewModel = new MainViewModel();
            viewModel.CommandSettings.Subscribe(() => {
                if (viewModel.IsBusy.Value) {
                    return;
                }
                viewModel.BusyWithModal = true;
                try {
                    var dlg = new SettingDialog(Storage);
                    dlg.ShowDialog();
                    if (dlg.Result) {
                        if (dlg.NewStorage != null) {
                            SetStorage(dlg.NewStorage);
                        }
                    }
                } finally {
                    viewModel.BusyWithModal = false;
                }
            });

            viewModel.CommandDownloadNow.Subscribe(() => {
                var targets = MainListView.SelectedItems.ToEnumerable<DLEntry>();
                mDownloadManager.Enqueue(targets, viewModel.OnlySound.Value ? MediaFlag.AUDIO : MediaFlag.VIDEO);
            });
            viewModel.CommandClearOutput.Subscribe(() => {
                viewModel.OutputList.Clear();
            });
            viewModel.CommandFoldOutput.Subscribe(() => {
                RootGrid.RowDefinitions[3].Height = new GridLength(0, GridUnitType.Star);
            });
            viewModel.CommandPlay.Subscribe(Play);
            viewModel.CurrentCategory.Subscribe((c) => {
                RefreshList();
            });
            viewModel.RatingFilter.FilterChanged += RefreshList;
            InitializeComponent();
        }

        private PlayerWindow mPlayerWindow = null;
        private PlayerWindow GetPlayer() {
            if (mPlayerWindow == null) {
                mPlayerWindow = new PlayerWindow();
                mPlayerWindow.PlayItemChanged += OnPlayItemChanged;
                mPlayerWindow.PlayWindowClosed += OnPlayerWindowClosed;
                mPlayerWindow.Show();
            }
            return mPlayerWindow;
        }

        private void OnPlayerWindowClosed(PlayerWindow obj) {
            if (obj == mPlayerWindow) {
                mPlayerWindow.PlayItemChanged -= OnPlayItemChanged;
                mPlayerWindow.PlayWindowClosed -= OnPlayerWindowClosed;
                mPlayerWindow = null;
            }
        }

        private void OnPlayItemChanged(IPlayable obj) {
            Storage.DLTable.Update();
            if (null == obj) return;
            if (obj != null) {
                if (!(obj is DLEntry)) {
                    obj = viewModel.MainList.Value.Where((v) => v.Url == obj.Url).FirstOrDefault();
                    if (obj == null) return;
                }
                int index = viewModel.MainList.Value.IndexOf((DLEntry)obj);
                if (index >= 0) {
                    MainListView.SelectedIndex = index;
                }
            }
        }

        private void Play() {
            var win = GetPlayer();
            var selected = MainListView.SelectedItems;
            if(selected.Count>1) {
                win.PlayList.SetList(selected.ToEnumerable<DLEntry>());
            } else {
                win.PlayList.SetList(viewModel.MainList.Value, MainListView.SelectedItem as IPlayable);
            }
        }

        private MainViewModel viewModel {
            get => DataContext as MainViewModel;
            set => DataContext = value;
        }

        private void RefreshList() {
            if (Storage == null) return;
            viewModel.MainList.Value = new ObservableCollection<DLEntry>(Storage.DLTable.List
                .FilterByRating(viewModel.RatingFilter).FilterByCategory(viewModel.CurrentCategory.Value));
        }

        private async void RegisterUrl(string url) {
            url = url.Trim();
            if(!url.StartsWith("https://")&&!url.StartsWith("http://")) {
                return;
            }
            url = url.Split(Utils.Array("\r", "\n", " ", "\t"), StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
            if(string.IsNullOrEmpty(url)) {
                return;
            }
            var uri = new Uri(url);
            var det = Settings.Instance.Determinations.Query(uri.Host);
            if(det==DeterminationList.Determination.REJECT) {
                return;
            }
            if(det==DeterminationList.Determination.UNKNOWN) {
                var r = await viewModel.ShowDeterminationDialog(uri.Host);
                Settings.Instance.Determinations.Determine(uri.Host, r);
                if(!r) {
                    return;
                }
            }

            var target = DLEntry.Create(url);
            if (Storage.DLTable.Add(target)) {
                if (viewModel.AutoDownload.Value) {
                    mDownloadManager.Enqueue(target, viewModel.OnlySound.Value ? MediaFlag.AUDIO : MediaFlag.VIDEO);
                }
            }
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
            Storage.DLTable.Update();
            mClipboardMonitor.Dispose();
            if (mPlayerWindow != null) {
                mPlayerWindow.Close();
                mPlayerWindow = null;
            }
            Settings.Instance.Ratings = viewModel.RatingFilter.ToArray();
            Settings.Instance.Placement.GetPlacementFrom(this);
            Settings.Instance.Serialize();
        }

        //private readonly string[] youtube_urls = {
        //    "https://www.youtube.com/",
        //    "https://i.ytimg.com/",
        //};
        //private bool isYoutubeUrl(string url) {
        //    if (url != null) {
        //        foreach (var y in youtube_urls) {
        //            if (url.StartsWith(y)) {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //private void download(string url) {
        //    if(!isYoutubeUrl(url)) {
        //        return;
        //    }

        //    var psi = new ProcessStartInfo() {
        //        FileName = "youtube-dl",
        //        Arguments = $"--get-title {url}",
        //        CreateNoWindow = true,
        //        UseShellExecute = false,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        // RedirectStandardInput = true,
        //    };
        //    //psi.EnvironmentVariables["Path"] = path;
        //    var process = Process.Start(psi);
        //    var s = process.StandardOutput.ReadToEnd();
        //    Debug.WriteLine(s);
        //    //Output.Text += "\n";
        //    //Output.Text += s;
        //    s = process.StandardError.ReadToEnd();
        //    //Output.Text += s;
        //    //Output.ScrollToEnd();
        //}

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
            //Output.Text += s;

            if(!process.HasExited) {
                await Task.Run(async () => {
                    while (!process.HasExited) {
                        await Task.Delay(10);
                    }
                });
            }
            //Output.Text += $"Done:{process.ExitCode}";
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
                    viewModel.BusyWithModal= true;
                    try {
                        var dlg = new SettingDialog(null);
                        dlg.ShowDialog();
                        if (dlg.Result && dlg.NewStorage != null) {
                            SetStorage(dlg.NewStorage);
                            return;
                        }
                    } finally {
                        viewModel.BusyWithModal = false;
                    }
                }
            } else {
                try {
                    var storage = new Storage(Settings.Instance.EnsureDBPath);
                    SetStorage(storage);
                } catch(Exception e) {
                    Logger.error(e);
                    InitStorage(true);
                }
            }
        }

        private void SetStorage(Storage newStorage) {
            if(mDownloadManager?.Storage != newStorage) {
                // ストレージが置き換わるときは、DownloadManagerを作り直す
                // DownloadManager.Dispose()の中で、ストレージもDisposeされる。
                mDownloadManager?.Dispose();
                mDownloadManager = new DownloadManager(this, newStorage);
                mDownloadManager.BusyChanged += OnBusyStateChanged;
                viewModel.IsBusy.Value = false;
                newStorage.DLTable.AddEvent += OnDLEntryAdd;
                newStorage.DLTable.DelEvent += OnDLEntryDel;
                RefreshList();
            }
        }

        private void OnBusyStateChanged(bool busy) {
            Dispatcher.Invoke(() => {
                viewModel.IsBusy.Value = busy;
            });
        }

        private void OnDLEntryDel(DLEntry entry) {
            Dispatcher.Invoke(() => {
                viewModel.MainList.Value.Remove(entry);
            });
        }

        private void OnDLEntryAdd(DLEntry entry) {
            Dispatcher.Invoke(() => {
                viewModel.MainList.Value.Add(entry);
                // ToDo: Sort ... see DxxDBViewerWindow.xaml.cs SortInfo, etc...
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            InitStorage();
            mClipboardMonitor = new ClipboardMonitor(this, true);
            mClipboardMonitor.ClipboardUpdate += OnClipboardUpdated;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
        }

        private void OnClipboardUpdated(object sender, EventArgs e) {
            if(viewModel.BusyWithModal || !viewModel.ClipboardWatching.Value) {
                return; // ダイアログ表示中は何もしない
            }
            RegisterUrl(Clipboard.GetText());
            //download(Clipboard.GetText());
        }

        private void OnHeaderClick(object sender, RoutedEventArgs e) {
            // Sort
        }

        private void OnListItemDoubleClick(object sender, MouseButtonEventArgs e) {

        }

        private bool Output(string msg, bool error) {
            if (null == msg) return false;
            msg = msg.Trim();
            if (msg.Length != 0) {
                Dispatcher.Invoke(() => {
                    viewModel.OutputList.Add(new OutputMessage(msg, error));
                });
            }
            return true;
        }

        bool IDownloadHost.StandardOutput(string msg) {
            return Output(msg, error: false);
        }

        bool IDownloadHost.ErrorOutput(string msg) {
            return Output(msg, error: true);
        }

        void IDownloadHost.Completed(DLEntry target, bool succeeded) {
            if (succeeded) {
                Dispatcher.Invoke(() => {
                    if (viewModel.AutoPlay.Value) {
                        GetPlayer().PlayList.Add(target);
                    }
                });
            }
        }

        void IDownloadHost.FoundSubItem(DLEntry target) {
            Dispatcher.Invoke(() => {
                Storage.DLTable.Add(target);
                if (viewModel.AutoPlay.Value) {
                    GetPlayer().PlayList.Add(target);
                }
            });
        }
    }

    static class FilterExt {
        public static IEnumerable<DLEntry> FilterByRating(this IEnumerable<DLEntry> s, RatingFilter rf) {
            return rf.Filter(s);
        }
        public static IEnumerable<DLEntry> FilterByCategory(this IEnumerable<DLEntry> s, Category c) {
            return c.Filter(s);
        }
    }
}
