using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ytplayer.browser;
using ytplayer.common;
using ytplayer.data;
using ytplayer.dialog;
using ytplayer.download;
using ytplayer.download.downloader;
using ytplayer.interop;
using ytplayer.player;
using ytplayer.server;
using static ytplayer.data.SyncManager;

namespace ytplayer {
    /**
     * アウトプットリストに出力する文字列エントリクラス
     */
    public class OutputMessage {
        private static readonly Brush ErrorColor = new SolidColorBrush(Colors.Red);
        private static readonly Brush StandardColor = new SolidColorBrush(Colors.Black);
        public string Message { get; }
        public bool Error { get; }
        public Brush Color => Error ? ErrorColor : StandardColor;

        public OutputMessage(string message, bool error) {
            Message = message;
            Error = error;
        }
    }

    /**
     * MainWindow上に表示する簡易ダイアログのID
     */
    public enum DialogTypeId {
        DELETE_COMFIRM,
        ACCEPT_DETERMINATION,
        EXTRACT_AUDIO,
        EDIT_DESCRIPTION,
        SYNC_FROM,
        MOVE_ITEMS,
        PROGRESS,
        CLOSING_MESSAGE,
    }

    /**
     * ビューモデル
     */
    public class MainViewModel : ViewModelBase {
        public ReactiveProperty<ObservableCollection<DLEntry>> MainList { get; } = new ReactiveProperty<ObservableCollection<DLEntry>>(new ObservableCollection<DLEntry>());
        public ReactivePropertySlim<bool> AutoDownload { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> AutoPlay { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> IsBusy { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ShowFilterEditor { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ShowSearchDialog { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ClipboardWatching { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> StatusString { get; } = new ReactivePropertySlim<string>();
        public ObservableCollection<OutputMessage> OutputList { get; } = new ObservableCollection<OutputMessage>();
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.FilterList);
        public ReactivePropertySlim<Category> CurrentCategory { get; } = new ReactivePropertySlim<Category>(Settings.Instance.Categories.All);
        public ReactivePropertySlim<bool> ShowBlocked { get; } = new ReactivePropertySlim<bool>(false);
        public RatingFilter RatingFilter { get; } = new RatingFilter();
        public ObservableCollection<string> SearchHistory => Settings.Instance.SearchHistories.History;
        public ReactivePropertySlim<string> FilterText { get; } = new ReactivePropertySlim<string>();

        public bool BusyWithModal = false;

        public ReactiveCommand CommandSettings { get; } = new ReactiveCommand();
        public ReactiveCommand CommandClearOutput { get; } = new ReactiveCommand();
        public ReactiveCommand CommandFoldOutput { get; } = new ReactiveCommand();
        public ReactiveCommand CommandPlay { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSearch { get; } = new ReactiveCommand();
        public ReactiveCommand CommandClearFilterText { get; } = new ReactiveCommand();
        public ReactiveCommand CommandExport { get; } = new ReactiveCommand();
        public ReactiveCommand CommandImport { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSync { get; } = new ReactiveCommand();
        public ReactiveCommand CommandMoveItems { get; } = new ReactiveCommand();
        public ReactiveCommand CommandBrowser { get; } = new ReactiveCommand();
        public ReactiveCommand CommandRepairDB { get; } = new ReactiveCommand();
        public ReactiveCommand CommandImportFiles { get; } = new ReactiveCommand();
        //public ReactiveCommand<string> CommandExportMediaFiles { get; } = new ReactiveCommand<string>();

        // Context Menu
        public ReactiveCommand OpenInWebBrowserCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PlayInWebBrowserCommand { get; } = new ReactiveCommand();
        public ReactiveCommand DeleteAndBlockCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetAndDownloadCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ExtractAudioCommand { get; } = new ReactiveCommand();
        public ReactiveCommand EditDescriptionCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CopyVideoPathCommand { get; } = new ReactiveCommand();

        // Dialog
        public abstract class DialogViewModel: ViewModelBase {
            public virtual bool CheckBeforeOk() { return true; }
            public abstract DialogTypeId Type { get; }
        }

        private DialogViewModel DialogCurrentViewModel { get; set; } = null;
        private TaskCompletionSource<bool> DialogTask { get; set; } = null;
        public ReactivePropertySlim<bool> DialogActivated { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> DialogTitle { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<DialogTypeId> DialogType { get; } = new ReactivePropertySlim<DialogTypeId>();
        public ReactiveCommand CommandCancel { get; } = new ReactiveCommand();
        public ReactiveCommand CommandOk { get; } = new ReactiveCommand();



        public Task<bool> ShowDialog(DialogViewModel model, string title) {
            DialogCurrentViewModel = model;
            BusyWithModal = true;
            DialogTask = new TaskCompletionSource<bool>();
            DialogType.Value = model.Type;
            DialogTitle.Value = title;
            DialogActivated.Value = true;
            return DialogTask.Task;
        }

        // Delete Item Dialog
        public class DeleteItemDialogViewModel : DialogViewModel {
            public override DialogTypeId Type => DialogTypeId.DELETE_COMFIRM;
            public ReactivePropertySlim<bool> BlockItem { get; } = new ReactivePropertySlim<bool>(false);
            public ReactivePropertySlim<bool> DeleteVideoFile { get; } = new ReactivePropertySlim<bool>(false);
            public ReactivePropertySlim<bool> DeleteAudioFile { get; } = new ReactivePropertySlim<bool>(false);
        }
        public DeleteItemDialogViewModel DeleteItemDialog { get; } = new DeleteItemDialogViewModel();
        public Task<bool> ShowDeleteItemDialog() {
            return ShowDialog(DeleteItemDialog, "Delete Items");
        }

        // Accept/Reject Determination Dialog
        public class DeterminationDialogViewModel : DialogViewModel {
            public override DialogTypeId Type => DialogTypeId.ACCEPT_DETERMINATION;
            public ReactivePropertySlim<string> Host { get; } = new ReactivePropertySlim<string>();
            public ReactiveCommand CommandOk { get; } = new ReactiveCommand();
        }
        public DeterminationDialogViewModel DeterminationDialog { get; } = new DeterminationDialogViewModel();
        public Task<bool> ShowDeterminationDialog(string host) {
            DeterminationDialog.Host.Value = host;
            return ShowDialog(DeterminationDialog, "Accept or Reject");
        }

        // Extract Audio Dialog
        public class ExtractAudoDialogViewModel : DialogViewModel {
            public override DialogTypeId Type => DialogTypeId.EXTRACT_AUDIO;
            public ReactiveProperty<bool> DeleteVideo { get; } = new ReactiveProperty<bool>();
            public ReactiveProperty<bool> DownloadAudio { get; } = new ReactiveProperty<bool>();
        }
        public ExtractAudoDialogViewModel ExtractAudoDialog { get; } = new ExtractAudoDialogViewModel();
        public Task<bool> ShowExtractAudioDialog() {
            return ShowDialog(ExtractAudoDialog, "Extract Audio");
        }

        // Description Dialog
        public class DescriptionDialogViewModel : DialogViewModel {
            public override DialogTypeId Type => DialogTypeId.EDIT_DESCRIPTION;
            public ReactiveProperty<string> Description { get; } = new ReactiveProperty<string>();
        }
        public DescriptionDialogViewModel DescriptionDialog { get; } = new DescriptionDialogViewModel();
        public Task<bool> ShowDescriptionDialog() {
            return ShowDialog(DescriptionDialog, "Description");
        }

        public class SyncDialogViewModel : DialogViewModel {
            public override DialogTypeId Type => DialogTypeId.SYNC_FROM;
            public ReactiveProperty<string> HostAddress { get; } = new ReactiveProperty<string>();
        }
        public SyncDialogViewModel SyncDialog { get; } = new SyncDialogViewModel();
        public async Task<bool> ShowSyncDialog() {
            if (string.IsNullOrEmpty(SyncDialog.HostAddress.Value)) {
                SyncDialog.HostAddress.Value = Settings.Instance.SyncPeer;
            }

            if (await ShowDialog(SyncDialog, "Synchronization")) {
                Settings.Instance.SyncPeer = SyncDialog.HostAddress.Value;
                return true;
            }
            return false;
        }

        public class MoveItemsViewModel : DialogViewModel {
            public override DialogTypeId Type => DialogTypeId.MOVE_ITEMS;
            public ReactiveProperty<bool> Video { get; } = new ReactiveProperty<bool>();
            public ReactiveProperty<bool> Audio { get; } = new ReactiveProperty<bool>();
            public ReactiveProperty<string> VideoTo { get; } = new ReactiveProperty<string>();
            public ReactiveProperty<string> AudioTo { get; } = new ReactiveProperty<string>();
            public ReactiveCommand SelectVideoPath { get; } = new ReactiveCommand();
            public ReactiveCommand SelectAudioPath { get; } = new ReactiveCommand();

            public void SelectFolder(string title, ReactiveProperty<string> initialPath, Window owner) {
                var r = FolderDialogBuilder.Create()
                    .title(title)
                    .initialDirectory(initialPath.Value)
                    .GetFilePath(owner);
                if(r!=null) {
                    initialPath.Value = r;
                }
            }

            public override bool CheckBeforeOk() {
                if(Video.Value) {
                    if(!PathUtil.isDirectory(VideoTo.Value)) {
                        return false;
                    }
                }
                if(Audio.Value) {
                    if(!PathUtil.isDirectory(AudioTo.Value)) {
                        return false;
                    }
                }
                return true;
            }
        }
        public MoveItemsViewModel MoveItemsDialog { get; } = new MoveItemsViewModel();
        public async Task<bool> ShowMoveItemsDialog(Window owner) {
            using (MoveItemsDialog.SelectVideoPath.Subscribe(() => MoveItemsDialog.SelectFolder("Select Video Folder", MoveItemsDialog.VideoTo, owner)))
            using (MoveItemsDialog.SelectAudioPath.Subscribe(() => MoveItemsDialog.SelectFolder("Select Audio Folder", MoveItemsDialog.AudioTo, owner))) {
                return await ShowDialog(MoveItemsDialog, "Move Files");
            }
        }

        public class ProgressViewModel : ViewModelBase, ISyncProgress {
            public ReactiveProperty<string> Message { get; } = new ReactiveProperty<string>();
            public ReactiveProperty<int> Total { get; } = new ReactiveProperty<int>();
            public ReactiveProperty<int> Current { get; } = new ReactiveProperty<int>();
            public ReactiveCommand OnCancel { get; } = new ReactiveCommand();

            private WeakReference<DispatcherObject> mOwner = null;
            public DispatcherObject Owner {
                get => mOwner?.GetValue();
                set { mOwner = new WeakReference<DispatcherObject>(value); }
            }

            public bool IsCancelled { get; set; }

            public void OnMessage(string msg) {
                Owner?.Dispatcher?.Invoke(() => {
                    Message.Value = msg;
                });
            }

            public void OnProgress(int current, int total) {
                Owner?.Dispatcher?.Invoke(() => {
                    Total.Value = total;
                    Current.Value = current;
                });
            }

            public ProgressViewModel() {
                OnCancel.Subscribe(() => {
                    IsCancelled = true;
                });
            }

        }
        public ProgressViewModel Progress { get; } = new ProgressViewModel();
        public void ShowProgress(string title, DispatcherObject owner) {
            Progress.IsCancelled = false;
            Progress.Owner = owner;
            BusyWithModal = true;
            DialogType.Value = DialogTypeId.PROGRESS;
            DialogTitle.Value = title;
            DialogActivated.Value = true;
        }
        public void HideProgress() {
            DialogActivated.Value = false;
            DialogTask = null;
            BusyWithModal = false;
        }

        private class ProgressDisposer : IDisposable {
            private MainViewModel viewModel;
            public ProgressDisposer(MainViewModel viewModel, string title, DispatcherObject owner) {
                this.viewModel = viewModel;
                viewModel.ShowProgress(title, owner);
            }

            public void Dispose() {
                viewModel?.HideProgress();
                viewModel = null;
            }
        }

        public IDisposable ActivateProgress(string title, DispatcherObject owner) {
            return new ProgressDisposer(this, title, owner);
        }


        /**
         * ビューモデルの構築
         */
        public MainViewModel() {
            CommandCancel.Subscribe(() => {
                DialogCurrentViewModel = null;
                DialogTask.TrySetResult(false);
                DialogActivated.Value = false;
                DialogTask = null;
                BusyWithModal = false;
            });
            CommandOk.Subscribe(() => {
                if (DialogCurrentViewModel?.CheckBeforeOk() ?? true) {
                    DialogCurrentViewModel = null;
                    DialogTask.TrySetResult(true);
                    DialogActivated.Value = false;
                    DialogTask = null;
                    BusyWithModal = false;
                }
            });
            CommandClearFilterText.Subscribe(() => FilterText.Value = "");
        }
    }

    /**
     * MainWindowクラス
     */
    public partial class MainWindow : Window, IDownloadHost, IYtListSource {
        #region Properties / Fields

        private RequestAcceptor mDownloadAcceptor = null;
        private DownloadManager mDownloadManager = null;
        private ClipboardMonitor mClipboardMonitor = null;
        private Storage Storage => mDownloadManager?.Storage;

        private static WeakReference<MainWindow> sInstance = null;
        public static MainWindow Instance {
            get => sInstance?.GetValue();
            private set => sInstance = new WeakReference<MainWindow>(value);
        }

        private MainViewModel viewModel {
            get => DataContext as MainViewModel;
            set {
                viewModel?.Dispose();
                DataContext = value;
            }
        }

        #endregion

        #region Initialization / Termination

        public MainWindow() {
            viewModel = new MainViewModel();
            viewModel.CommandSettings.Subscribe(() => {
                if (viewModel.IsBusy.Value) {
                    return;
                }
                viewModel.BusyWithModal = true;
                try {
                    ShowSettingDialog(Storage);
                } finally {
                    viewModel.BusyWithModal = false;
                }
            });

            viewModel.CommandClearOutput.Subscribe(() => {
                viewModel.OutputList.Clear();
            });
            viewModel.CommandFoldOutput.Subscribe(() => {
                RootGrid.RowDefinitions[3].Height = new GridLength(0, GridUnitType.Star);
            });
            viewModel.CommandPlay.Subscribe(Play);
            viewModel.CurrentCategory.Subscribe((c) => RefreshList());
            viewModel.RatingFilter.FilterChanged += RefreshList;
            viewModel.FilterText.Subscribe((s) => RefreshList());
            viewModel.ShowBlocked.Subscribe((s) => RefreshList());
            viewModel.AutoDownload.Subscribe((v) => {
                if (!v||Storage==null) return;
                var targets = Storage.DLTable.List.Where((e) => e.Status == Status.INITIAL || e.Status == Status.CANCELLED);
                mDownloadManager.Enqueue(targets);
            });

            viewModel.OpenInWebBrowserCommand.Subscribe(OpenInWebBrower);
            viewModel.PlayInWebBrowserCommand.Subscribe(PlayInWebBrower);
            viewModel.ExtractAudioCommand.Subscribe(ExtractAudio);
            viewModel.ResetAndDownloadCommand.Subscribe(ResetAndDownload);
            viewModel.DeleteAndBlockCommand.Subscribe(DeleteAndBlock);
            viewModel.EditDescriptionCommand.Subscribe(EditDescription);
            viewModel.CopyVideoPathCommand.Subscribe(CopyVideoPath);

            viewModel.ShowFilterEditor.Subscribe((v) => {
                if (v) {
                    ShowFilterEditorWindow();
                } else {
                    mFilterEditorWindow?.Close();
                }
            });

            viewModel.ShowSearchDialog.Subscribe((v) => {
                if (v) {
                    ShowTextSearchDialog();
                } else {
                    mTextSearchDialog?.Close();
                }
            });

            viewModel.CommandExport.Subscribe(ExportUrlList);
            viewModel.CommandImport.Subscribe(ImportUrlList);
            viewModel.CommandSync.Subscribe(SyncFrom);
            viewModel.CommandMoveItems.Subscribe(MoveItems);
            viewModel.CommandRepairDB.Subscribe(RepairDB);
            viewModel.CommandImportFiles.Subscribe(ImportVideoFiles);
            //viewModel.CommandExportMediaFiles.Subscribe(ExportMediaFiles);
            viewModel.CommandBrowser.Subscribe(ShowBrowser);
            //viewModel.ClipboardWatching.Subscribe((v) => {
            //    if (v) {
            //        Process.Start("btytbrs:");
            //    }
            //});
            InitializeComponent();
        }

        private void ShowBrowser(object obj) {
            Browser.ShowBrowser((url) => RegisterUrl(url));
        }

        YtServer mServer = null;

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            Settings.Instance.Placement.ApplyPlacementTo(this);
            Settings.Instance.SortInfo.SortUpdated += OnSortChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Instance = this;
            InitStorage();
            mClipboardMonitor = new ClipboardMonitor(this, true);
            mClipboardMonitor.ClipboardUpdate += OnClipboardUpdated;
            var lastUrl = Settings.Instance.LastPlayingUrl;
            if (!string.IsNullOrEmpty(lastUrl)) {
                var entry = viewModel.MainList.Value.Where((c) => c.KEY == lastUrl).FirstOrDefault();
                if (entry != null) {
                    MainListView.SelectedItem = entry;
                    MainListView.ScrollIntoView(entry);
                    if (Settings.Instance.RestartOnLoaded) {
                        var pos = Settings.Instance.LastPlayingPos;
                        var win = GetPlayer();
                        win.ResumePlay(viewModel.MainList.Value, entry/*, pos*/);
                    }
                }
            }
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //Debug.WriteLine(v.ToString());
#if DEBUG
            string dbg = "  <DBG>  ";
#else
            string dbg = "  ";
#endif
            this.Title = String.Format("{0}{5} - v{1}.{2}.{3}.{4}", version.ProductName, version.FileMajorPart, version.FileMinorPart, version.FileBuildPart, version.ProductPrivatePart, dbg);
            mDownloadAcceptor = new RequestAcceptor(this);

            StartServer();
        }

        private void StartServer() {
            StopServer();
            if (Settings.Instance.EnableServer && mServer==null) {
                mServer = new YtServer(this, Settings.Instance.ServerPort);
                mServer.Start();
            }
        }

        private void StopServer() {
            mServer?.Stop();
            mServer = null;
        }

        private bool ReadyToClose = false;
        private async Task CloseAndWait() {
            Storage.DLTable.Update();

            viewModel.DialogType.Value = DialogTypeId.CLOSING_MESSAGE;
            viewModel.DialogTitle.Value = "Bye...";
            viewModel.DialogActivated.Value = true;

            if (!mDownloadAcceptor.Disposed) {
                mDownloadAcceptor.Dispose();
                await mDownloadAcceptor.WaitForClose();
            }
            if (!mDownloadManager.Disposed) {
                mDownloadManager.Dispose();
                await mDownloadManager.WaitForClose();
            }
            ReadyToClose = true;
        }
        

        private async void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (mDownloadManager.IsBusy) {
                var r = MessageBox.Show("Are you sure to cancel downloading tasks？", "BooTube", MessageBoxButton.YesNo);
                if (r == MessageBoxResult.No) {
                    e.Cancel = true;
                    return;
                }
                mDownloadManager.Cancel();
            }
            Browser.CloseBrowser();
            if (!ReadyToClose) {
                e.Cancel = true;
                await CloseAndWait();
                Close();
                return;
            }
            //_ = CloseAndWait();
            //while(!ReadyToClose) {
            //    MessageBox.Show("Wait for finishing tasks...", "BooTube", MessageBoxButton.OK);
            //}

            StopServer();

            mClipboardMonitor.Dispose();
            if (mPlayerWindow != null) {
                Settings.Instance.RestartOnLoaded = true;
                mPlayerWindow.Close();
                mPlayerWindow = null;
            } else {
                Settings.Instance.RestartOnLoaded = false;
                if (Settings.Instance.LastPlayingUrl != (MainListView.SelectedItem as DLEntry)?.KEY) {
                    Settings.Instance.LastPlayingUrl = (MainListView.SelectedItem as DLEntry)?.KEY;
                    Settings.Instance.LastPlayingPos = 0;
                }
            }
            Settings.Instance.SortInfo.SortUpdated -= OnSortChanged;
            Instance = null;
            mFilterEditorWindow?.Close();

            Settings.Instance.Ratings = viewModel.RatingFilter.ToArray();
            Settings.Instance.Placement.GetPlacementFrom(this);
            Settings.Instance.Serialize();
            viewModel = null;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
        }

        #endregion

        #region Storage Management

        /**
         * @return true:DBがセットされた / false:DBはセットされなかった
         */
        private bool ShowSettingDialog(Storage currentStorage) {
            var dlg = new SettingDialog(currentStorage);
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.ShowDialog();
            bool ret = false;
            if (dlg.Result!=null && dlg.Result.Ok && dlg.Result.NewStorage != null) {
                SetStorage(dlg.Result.NewStorage);
                ret = true;
            }
            StartServer();
            return ret;
        }

        private void InitStorage(bool forceCreate = false) {
            if (forceCreate || !PathUtil.isFile(Settings.Instance.EnsureDBPath)) {
                while (true) {
                    viewModel.BusyWithModal = true;
                    try {
                        if (ShowSettingDialog(null)) {
                            return;
                        }
                    }
                    finally {
                        viewModel.BusyWithModal = false;
                    }
                }
            } else {
                var storage = Storage.OpenDB(Settings.Instance.EnsureDBPath);
                if(storage!=null) { 
                    SetStorage(storage);
                } else { 
                    InitStorage(true);
                }
            }
        }

        private void SetStorage(Storage newStorage) {
            if (mDownloadManager?.Storage != newStorage) {
                // ストレージが置き換わるときは、DownloadManagerを作り直す
                // DownloadManager.Dispose()の中で、ストレージもDisposeされる。
                mDownloadManager?.Dispose();
                mDownloadManager = new DownloadManager(this, newStorage);
                mDownloadManager.BusyChanged += OnBusyStateChanged;
                viewModel.IsBusy.Value = false;
                viewModel.AutoDownload.ForceNotify();
                newStorage.DLTable.AddEvent += OnDLEntryAdd;
                newStorage.DLTable.DelEvent += OnDLEntryDel;
                RefreshList();
                UpdateColumnHeaderOnSort();
            }
        }

        private void OnDLEntryDel(DLEntry entry) {
            Dispatcher.Invoke(() => {
                viewModel.MainList.Value.Remove(entry);
            });
        }

        private void OnDLEntryAdd(DLEntry entry) {
            Dispatcher.Invoke(() => {
                viewModel.MainList.Value.Add(entry);
                MainListView.ScrollIntoView(entry);
                // ToDo: Sort ... see DxxDBViewerWindow.xaml.cs SortInfo, etc...
            });
        }

        #endregion

        #region Targets List Management

        private void RefreshList() {
            if (Storage == null) return;
            var orgSelection = SelectedEntries.ToList();
            viewModel.MainList.Value = new ObservableCollection<DLEntry>(Storage.DLTable.List
                .FilterByRating(viewModel.RatingFilter)
                .FilterByCategory(viewModel.CurrentCategory.Value)
                .FilterByName(viewModel.FilterText.Value)
                .Where((c) => viewModel.ShowBlocked.Value || (c.Status != Status.BLOCKED && c.Status != Status.FAILED))
                .Sort());

            if(orgSelection.Count>0) {
                if(MainListView.SelectItems(orgSelection.Intersect(viewModel.MainList.Value))) {
                    DelayAndDo(100, () => MainListView.ScrollIntoView(MainListView.SelectedItem));
                }
            }
        }

        private async void DelayAndDo(int ms, Action fn) {
            await Task.Delay(ms);
            fn();
        }

        private void OnFilterBoxLostFocus(object sender, RoutedEventArgs e) {
            if (viewModel != null) {
                Settings.Instance.SearchHistories.Put(viewModel.FilterText.Value);
            }
        }

        private void OnDLEntryRatingClicked(object sender, RoutedEventArgs e) {
            var entry = ((FrameworkElement)sender).Tag as DLEntry;
            int rating = (int)entry.Rating + 1;
            if (rating > (int)Rating.EXCELLENT) {
                rating = 1;
            }
            entry.Rating = (Rating)rating;
        }

        private void OnDLEntryMarkClicked(object sender, RoutedEventArgs e) {
            var entry = ((FrameworkElement)sender).Tag as DLEntry;
            int mark = (int)entry.Mark + 1;
            if (mark >= (int)Mark.MARK_LAST) {
                mark = 0;
            }
            entry.Mark = (Mark)mark;
        }


        private void OnListItemDoubleClick(object sender, MouseButtonEventArgs e) {
            viewModel.CommandPlay.Execute();
        }

        #endregion

        #region Export / Import

        private void ExportUrlList(object obj) {
            var sel = MainListView.SelectedItems;
            IEnumerable<DLEntry> list;
            if(sel.Count>1) {
                list = sel.ToEnumerable<DLEntry>();
            } else {
                list = viewModel.MainList.Value.Where((e) => e.Status != Status.BLOCKED);
            }
            if (Utils.IsNullOrEmpty(list)) return;
            var path = SaveFileDialogBuilder.Create()
                .addFileType("Text", "*.txt")
                .defaultExtension("txt")
                .overwritePrompt(true)
                .defaultFilename("url-list")
                .GetFilePath(this);
            if (path == null) return;

            using (var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8)) {
                foreach (var e in list) {
                    writer.WriteLine(e.Url);
                }
            }
        }

        private void ImportUrlList(object obj) {
            var path = OpenFileDialogBuilder.Create()
                .addFileType("Text", "*.txt")
                .defaultExtension("txt")
                .defaultFilename("url-list")
                .GetFilePath(this);
            if (path == null) return;
            using (var reader = new StreamReader(path, System.Text.Encoding.UTF8)) {
                for(; ; ) {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    RegisterUrl(line);
                }
            }
        }
        private async void SyncFrom() {
            if (await viewModel.ShowSyncDialog()) {
                using (viewModel.ActivateProgress("Synchronizing Data", this)) {
                    await SyncManager.SyncFrom(viewModel.SyncDialog.HostAddress.Value, Storage, this, viewModel.Progress);
                }
            }
        }
        private async void MoveItems() {
            if (await viewModel.ShowMoveItemsDialog(GetWindow(this))) {
                using (viewModel.ActivateProgress("Moving Data", this)) {
                    var model = viewModel.MoveItemsDialog;
                    await SyncManager.MoveData(model.Video.Value ? model.VideoTo.Value : null, model.Audio.Value ? model.AudioTo.Value : null, null, Storage, this, viewModel.Progress, true);
                }
            }
        }
        private async void RepairDB() {
            using (WaitCursor.Start(this)) {
                int count = await ImportVideoFiles(Settings.Instance.VideoPath, ImportFileMode.LINK_TO);
                ((IReportOutput)this).StandardOutput($"Finished: {count} files repaired.");
            }
        }

        private async void ImportVideoFiles() {
            var targetPath = FolderDialogBuilder.Create()
                .title("Select Folder")
                .GetFilePath(GetWindow(this));
            if(targetPath == null) {
                return;
            }
            using (WaitCursor.Start(this)) {
                int count = await ImportVideoFiles(targetPath, ImportFileMode.MOVE_FILE);
                ((IReportOutput)this).StandardOutput($"Finished: {count} files imported.");
            }
        }

        //private async void ExportMediaFiles(string type) {
        //    var entries = SelectedEntries;
        //    if (Utils.IsNullOrEmpty(entries)) {
        //        return;
        //    }
        //    var targetPath = FolderDialogBuilder.Create()
        //        .title("Select Folder")
        //        .GetFilePath(GetWindow(this));
        //    if(targetPath == null) {
        //        return;
        //    }

        //    using (WaitCursor.Start(this)) {
        //        foreach(var entry in entries) {
        //            try {
        //                if (type == "V") {
        //                    if (!string.IsNullOrEmpty(entry.VPath) && File.Exists(entry.VPath)) {
        //                        var dstPath = Path.Combine(targetPath, Path.GetFileName(entry.VPath));
        //                        File.Copy(entry.VPath, dstPath);
        //                        ((IReportOutput)this).StandardOutput("OK: " + entry.Name);
        //                    }
        //                    else {
        //                        ((IReportOutput)this).StandardOutput("No Data: " + entry.Name);
        //                    }
        //                }
        //                else {
        //                    var srcPath = entry.APath;
        //                    if (!string.IsNullOrEmpty(entry.APath) && File.Exists(entry.APath)) {
        //                        var dstPath = Path.Combine(targetPath, Path.GetFileName(entry.APath));
        //                        File.Copy(entry.APath, dstPath);
        //                        ((IReportOutput)this).StandardOutput("OK: " + entry.Name);
        //                    }
        //                    else if(!string.IsNullOrEmpty(entry.VPath) && File.Exists(entry.VPath)) {
        //                        extractAudioFromVideo(entry, targetPath);
        //                    } else {
        //                        ((IReportOutput)this).StandardOutput("No Data: " + entry.Name);
        //                    }
        //                }
        //            }
        //            catch (Exception ex) {
        //                ((IReportOutput)this).ErrorOutput("NG: " + entry.Name);
        //            }
        //        }
        //    }
        //}

        private async Task<bool> extractAudioFromVideo(DLEntry entry, string targetPath) {
            return await Task.Run(() => {
                var dstName = Path.GetFileNameWithoutExtension(entry.VPath) + ".mp3";
                var dstPath = Path.Combine(targetPath, dstName);
                var pi = new ProcessStartInfo() {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{entry.VPath}\" -y -f mp3 -vn \"{dstPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    //StandardOutputEncoding = System.Text.Encoding.UTF8,
                    //StandardErrorEncoding = System.Text.Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var process = Process.Start(pi)) {
                    if (process == null) {
                        return false;
                    }

                    // ffmpeg は、stdoutには何も出力しない
                    //while(true) {
                    //    var msg = process.StandardOutput.ReadLine();
                    //    if (msg == null) break;
                    //    LoggerEx.info(msg);
                    //}

                    ((IReportOutput)this).StandardOutput("Extracting Audio from " + entry.Name);
                    while (true) {
                        var msg = process.StandardError.ReadLine();
                        if (msg == null) break;
                        ((IReportOutput)this).StandardOutput(msg);
                    }
                    process.WaitForExit();
                }
                return true;
            });
        }

        private enum ImportFileMode {
            MOVE_FILE,
            COPY_FILE,
            LINK_TO,
        };

        static Regex regFilePath = new Regex(@"(?<name>.*)(?:-(?<id>.{11})).mp4");
        private Task<int> ImportVideoFiles(string sourceDir, ImportFileMode mode) {
            bool safeCopyFile(string src, string dst) {
                try {
                    File.Copy(src, dst, false);
                    return true;
                } catch(Exception e) {
                    ((IReportOutput)this).ErrorOutput($"cannot copy to: {dst}");
                    LoggerEx.error(e);
                    return false;
                }
            }
            bool safeMoveFile(string src, string dst) {
                try {
                    File.Move(src, dst);
                    return true;
                }
                catch (Exception e) {
                    ((IReportOutput)this).ErrorOutput($"cannot move to: {dst}");
                    LoggerEx.error(e);
                    return false;
                }
            }

            return Task.Run(() => {
                int count = 0;
                foreach (var path in Directory.EnumerateFiles(sourceDir, "*.mp4", SearchOption.TopDirectoryOnly)) {
                    var filename = Path.GetFileName(path);
                    var m = regFilePath.Match(filename);
                    if (m != null && m.Success) {
                        var id = m.Groups?["id"]?.Value;
                        var name = m.Groups?["name"]?.Value;
                        if (id != null && !Storage.DLTable.Contains(id)) {
                            // found it!,  to be registered.
                            var dstPath = path;
                            switch (mode) {
                                case ImportFileMode.COPY_FILE:
                                    dstPath = Path.Combine(Settings.Instance.VideoPath, filename);
                                    if (!safeCopyFile(path, dstPath)) {
                                        continue;
                                    }
                                    break;
                                case ImportFileMode.MOVE_FILE:
                                    dstPath = Path.Combine(Settings.Instance.VideoPath, filename);
                                    if (!safeMoveFile(path, dstPath)) {
                                        continue;
                                    }
                                    break;
                                case ImportFileMode.LINK_TO:
                                    break;
                            }

                            var fi = new FileInfo(dstPath);
                            var entry = DLEntry.Create(id, $"https://www.youtube.com/watch?v={id}");
                            entry.Status = Status.COMPLETED;
                            entry.VPath = dstPath;
                            entry.Date = fi.CreationTime;
                            entry.Media = MediaFlag.VIDEO;
                            entry.Name = name;
                            entry.UpdateSizeAndDuration();
                            Storage.DLTable.Add(entry);
                            ((IReportOutput)this).StandardOutput($"regisgered: {name}");
                            count++;
                        } else {
                            ((IReportOutput)this).ErrorOutput($"skipped: {filename}");
                        }
                    } else {
                        ((IReportOutput)this).ErrorOutput($"invalid name: {filename}");
                    }
                }
                Storage.DLTable.Update();
                return count;
            });
        }


        #endregion

        #region Items Selection / Context Menu

        private DLEntry SelectedEntry => MainListView.SelectedItem as DLEntry;
        
        public IEnumerable<DLEntry> SelectedEntries => MainListView.SelectedItems.ToEnumerable<DLEntry>();
        public IEnumerable<DLEntry> ListedEntries => MainListView.Items.ToEnumerable<DLEntry>();
        public IEnumerable<DLEntry> AllEntries => Storage.DLTable.List;

        IEnumerable<DLEntry> IYtListSource.SelectedEntries {
            get {
                return Dispatcher.Invoke(() => {
                    return this.SelectedEntries;
                });
            }
        }

        DLEntry IYtListSource.CurrentEntry {
            get {
                return Dispatcher.Invoke(() => {
                    return SelectedEntry;
                });
            }
        }

        

        DLEntry IYtListSource.GetPrevEntry(string current, bool moveCursor) {
            return Dispatcher.Invoke(() => {
                var entry = ((IYtListSource)this).GetEntry(current);
                if (entry == null) return null;
                int idx = viewModel.MainList.Value.IndexOf(entry);
                if (idx > 0) {
                    idx--;
                    entry = viewModel.MainList.Value[idx];
                    if (entry != null && moveCursor) {
                        MainListView.SelectedItem = entry;
                    }
                    return entry;
                }
                return null;
            });
        }
        DLEntry IYtListSource.GetNextEntry(string current, bool moveCursor) {
            return Dispatcher.Invoke(() => {
                var entry = ((IYtListSource)this).GetEntry(current);
                if (entry == null) return null;
                int idx = viewModel.MainList.Value.IndexOf(entry);
                if (idx >= 0 && idx + 1 < viewModel.MainList.Value.Count) {
                    idx++;
                    entry = viewModel.MainList.Value[idx];
                    if (entry != null && moveCursor) {
                        MainListView.SelectedItem = entry;
                    }
                    return entry;
                }
                return null;
            });
        }
        DLEntry IYtListSource.GetEntry(string id) {
            return Dispatcher.Invoke(() => {
                try {
                    var r = viewModel.MainList.Value.Where((entry) => entry.Id == id).SingleOrDefault();
                    return r;
                } catch(Exception e) {
                    Logger.error(e);
                    return null;
                }
            });
        }

        IEnumerable<ChapterEntry> IYtListSource.GetChaptersOf(string id) {
            return Storage.ChapterTable.Table.Where((c) => c.Owner == id);
        }
        IEnumerable<IGrouping<String,ChapterEntry>> IYtListSource.GetChapters() {
            return Storage.ChapterTable.Table.GroupBy((c) => c.Owner);
        }

        string IYtListSource.CurrentId {
            get => ((IYtListSource)this).CurrentEntry?.KEY;
            set {
                Dispatcher.Invoke(() => {
                    var entries = viewModel.MainList.Value.Where((c) => c.KEY == value);
                    var entry = entries.SingleOrDefault();
                    if (entry != null) {
                        MainListView.SelectedItem = entry;
                        MainListView.ScrollIntoView(entry);
                    }
                });
            }
        }



        private void ProcessSelectedEntries(Action<IEnumerable<DLEntry>> action) {
            var entries = SelectedEntries;
            if (Utils.IsNullOrEmpty(entries)) {
                return;
            }
            action(entries);
        }

        private void DeleteAndBlock(object obj) {
            ProcessSelectedEntries(async (entries) => {
                if (await viewModel.ShowDeleteItemDialog()) {
                    var delVideo = viewModel.DeleteItemDialog.DeleteVideoFile.Value;
                    var delAudio = viewModel.DeleteItemDialog.DeleteAudioFile.Value;
                    var block = viewModel.DeleteItemDialog.BlockItem.Value;
                    foreach (var e in entries) {
                        if (delVideo && PathUtil.safeDeleteFile(e.VPath)) {
                            e.VPath = null;
                            e.Media = e.Media.MinusVideo();
                        }
                        if (delAudio && PathUtil.safeDeleteFile(e.APath)) {
                            e.APath = null;
                            e.Media = e.Media.MinusAudio();
                        }
                        e.UpdateSizeAndDuration();
                        if (block) {
                            e.Delete();
                        }
                    }
                    Storage.DLTable.Update();
                }
            });
        }

        private void ResetAndDownload(object obj) {
            ProcessSelectedEntries((entries) => {
                mDownloadManager.Enqueue(entries);
            });
        }

        private void ExtractAudio(object obj) {
            ProcessSelectedEntries(async (entries) => {
                if (await viewModel.ShowExtractAudioDialog()) {
                    mDownloadManager.EnqueueExtractAudio(viewModel.ExtractAudoDialog.DeleteVideo.Value, viewModel.ExtractAudoDialog.DownloadAudio.Value, entries);
                }
            });
        }

        private void OpenInWebBrower() {
            var url = SelectedEntry?.Url;
            if (url != null) {
                // Process.Start(url);
                Process.Start($"btytbrs:{url}");
            }
        }
        private void PlayInWebBrower() {
            var id = SelectedEntry?.Id;
            if (id != null) {
                Process.Start($"http://localhost:{Settings.Instance.ServerPort}/ytplayer/video?id={id}");
            }
        }

        private void EditDescription() {
            ProcessSelectedEntries(async (entries) => {
                var org = entries.First().Desc;
                if (!string.IsNullOrEmpty(org)) {
                    viewModel.DescriptionDialog.Description.Value = entries.First().Desc;
                }
                if (await viewModel.ShowDescriptionDialog()) {
                    foreach (var e in entries) {
                        e.Desc = viewModel.DescriptionDialog.Description.Value ?? "";
                    }
                    Storage.DLTable.Update();
                }
            });
        }

        private void CopyVideoPath() {
            var path = SelectedEntry?.VPath;
            if (path != null) {
                Clipboard.SetDataObject(path);
            }
        }


        #endregion

        #region Category & Rating Setting Panel

        private CategoryRatingDialog mFilterEditorWindow = null;
        private CategoryRatingDialog ShowFilterEditorWindow() {
            if(mFilterEditorWindow==null) {
                mFilterEditorWindow = new CategoryRatingDialog();
                mFilterEditorWindow.EditorWindowClosed += OnEditorWindowClosed;
                mFilterEditorWindow.CategoryRatingSelected += OnCategoryRatingChanged;
                mFilterEditorWindow.ShowActivated = false;
                mFilterEditorWindow.ShowInTaskbar = false;
                mFilterEditorWindow.Owner = this;
                if(CategoryRatingDialog.StartPosition.HasValue) {
                    mFilterEditorWindow.Left = CategoryRatingDialog.StartPosition.Value.X;
                    mFilterEditorWindow.Top = CategoryRatingDialog.StartPosition.Value.Y;
                } else {
                    mFilterEditorWindow.Left = this.Left + this.ActualWidth - 200;
                    mFilterEditorWindow.Top = this.Top + this.ActualHeight - 500;
                }
                mFilterEditorWindow.Show();
            }
            return mFilterEditorWindow;
        }

        private void OnEditorWindowClosed(CategoryRatingDialog obj) {
            if (obj == mFilterEditorWindow) {
                mFilterEditorWindow.EditorWindowClosed -= OnEditorWindowClosed;
                mFilterEditorWindow.CategoryRatingSelected -= OnCategoryRatingChanged;
                mFilterEditorWindow = null;
                viewModel.ShowFilterEditor.Value = false;
            }
        }

        private void OnCategoryRatingChanged(Rating? rating, Category category) {
            if (rating.HasValue) {
                foreach(DLEntry e in MainListView.SelectedItems) {
                    e.Rating = rating.Value;
                }
            }
            if (category != null) {
                foreach (DLEntry e in MainListView.SelectedItems) {
                    e.Category = category;
                }
            }
            Storage.DLTable.Update();
        }

        #endregion

        #region Search Text

        private TextSearchDialog mTextSearchDialog = null;
        private TextSearchDialog ShowTextSearchDialog() {
            if (mTextSearchDialog == null) {
                mTextSearchDialog = new TextSearchDialog();
                mTextSearchDialog.WindowClosed += OnTextSearchDialogClosed;
                mTextSearchDialog.SearchText += OnSearchText;
                mTextSearchDialog.ShowActivated = false;
                mTextSearchDialog.ShowInTaskbar = false;
                mTextSearchDialog.Owner = this;
                if (TextSearchDialog.StartPosition.HasValue) {
                    mTextSearchDialog.Left = TextSearchDialog.StartPosition.Value.X;
                    mTextSearchDialog.Top = TextSearchDialog.StartPosition.Value.Y;
                }
                else {
                    mTextSearchDialog.Left = this.Left + this.ActualWidth - 200;
                    mTextSearchDialog.Top = this.Top + this.ActualHeight - 300;
                }

                mTextSearchDialog.Show();
            }
            return mTextSearchDialog;
        }

        private void OnSearchText(string text, bool next) {
            //var xx = next ? "next" : "prev";
            //LoggerEx.debug($"{text} ({xx})");
            int index = MainListView.SelectedIndex;
            Func<DLEntry, bool> filter = (e) => (e.Name?.ContainsIgnoreCase(text) ?? false) || (e.Desc?.ContainsIgnoreCase(text) ?? false);

            DLEntry entry;
            if (next) {
                entry = viewModel.MainList.Value.Skip(index + 1).Where(filter).FirstOrDefault();
            } else {
                entry= viewModel.MainList.Value.Take(index).Where(filter).LastOrDefault();
            }
            if(entry!=null) {
                MainListView.SelectedItem = entry;
                MainListView.ScrollIntoView(entry);
            }
        }

        private void OnTextSearchDialogClosed(TextSearchDialog obj) {
            if (obj == mTextSearchDialog) {
                mTextSearchDialog = null;
                viewModel.ShowSearchDialog.Value = false;
            }
        }

        #endregion

        #region Player Window

        private PlayerWindow mPlayerWindow = null;
        private PlayerWindow GetPlayer() {
            if (mPlayerWindow == null) {
                mPlayerWindow = new PlayerWindow(mDownloadManager);
                mPlayerWindow.PlayItemChanged += OnPlayItemChanged;
                mPlayerWindow.PlayWindowClosing += OnPlayerWindowClosing;
                mPlayerWindow.PlayWindowClosed += OnPlayerWindowClosed;
                mPlayerWindow.Show();
            }
            return mPlayerWindow;
        }

        private void OnPlayerWindowClosing(PlayerWindow obj) {
            var (cur, pos) = obj.CurrentPlayingInfo;
            Settings.Instance.LastPlayingUrl = cur.KEY;
            Settings.Instance.LastPlayingPos = pos;
        }

        private void OnPlayerWindowClosed(PlayerWindow obj) {
            if (obj == mPlayerWindow) {
                mPlayerWindow.PlayItemChanged -= OnPlayItemChanged;
                mPlayerWindow.PlayWindowClosing -= OnPlayerWindowClosing;
                mPlayerWindow.PlayWindowClosed -= OnPlayerWindowClosed;
                mPlayerWindow = null;
            }
        }

        private void OnPlayItemChanged(DLEntry obj) {
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
                    MainListView.ScrollIntoView(obj);   
                }
            }
        }

        private void Play() {
            var win = GetPlayer();
            var selected = MainListView.SelectedItems;
            if(selected.Count>1) {
                win.SetPlayList(selected.ToEnumerable<DLEntry>());
            } else {
                win.SetPlayList(viewModel.MainList.Value, MainListView.SelectedItem as DLEntry);
            }
        }

        #endregion

        #region Register Items ( D&D / Clipboard )

        bool IYtListSource.RegisterUrl(string url) {
            return Dispatcher.Invoke(() => {
                return RegisterUrl(url, false);
            });
        }

        public bool RegisterUrl(string url, bool silent=false) {
            url = url.Trim();
            if(!url.StartsWith("https://")&&!url.StartsWith("http://")) {
                return false;
            }
            url = url.Split(Utils.Array("\r", "\n", " ", "\t"), StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
            if(string.IsNullOrEmpty(url)) {
                return false;
            }

            var uri = new Uri(url);
            var dlr = DownloaderSelector.Select(uri);
            if (dlr == null) {
                return false;
            }
            if(!Settings.Instance.AcceptList && dlr.IsList(uri)) {
                url = dlr.StripListIdFromUrl(uri);
                if(null==url) {
                    return false;
                }
                uri = new Uri(url);
            }

            //var det = Settings.Instance.Determinations.Query(uri.Host);
            //if(det==DeterminationList.Determination.REJECT) {
            //    return;
            //}
            //if(det==DeterminationList.Determination.UNKNOWN) {
            //    if(silent) {
            //        return;
            //    }
            //    var r = await viewModel.ShowDeterminationDialog(uri.Host);
            //    Settings.Instance.Determinations.Determine(uri.Host, r);
            //    if(!r) {
            //        return;
            //    }
            //}
            
            var target = DLEntry.Create(dlr.IdFromUri(uri), url);
            if (Storage.DLTable.Add(target)) {
                if (viewModel.AutoDownload.Value) {
                    mDownloadManager.Enqueue(target);
                }
            }
            return true;
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            RegisterUrl(e.Data.GetData(DataFormats.Text) as string);
        }

        private void OnClipboardUpdated(object sender, EventArgs e) {
            if(viewModel.BusyWithModal || !viewModel.ClipboardWatching.Value) {
                return; // ダイアログ表示中は何もしない
            }
            Dispatcher.InvokeAsync(() => {
                try {
                    RegisterUrl(Clipboard.GetText());
                }
                catch (Exception ex) {
                    Logger.error(ex);
                    ((IDownloadHost)this).ErrorOutput(ex.Message);
                }
            });
        }

        #endregion

        #region Sort

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject {
            if (depObj != null) {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T) {
                        yield return (T)child;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child)) {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void UpdateColumnHeaderOnSort() {
            var sorter = Settings.Instance.SortInfo;
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(MainListView)) {
                Debug.WriteLine(header.ToString());
                var textBox = FindVisualChildren<TextBlock>(header).FirstOrDefault();
                if (null != textBox) {
                    var key = Sorter.SortKeyFromString(textBox.Text);
                    if (key == sorter.Key) {
                        header.Tag = sorter.Order == SortOrder.ASCENDING ? "asc" : "desc";
                    } else {
                        header.Tag = null;
                    }
                }
            }
        }

        private void OnSortChanged() {
            UpdateColumnHeaderOnSort();
            RefreshList();
        }

        private void OnHeaderClick(object sender, RoutedEventArgs e) {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (null == header) {
                return;
            }
            Settings.Instance.SortInfo.SetSortKey(header.Content.ToString());
        }

        #endregion

        #region Download Status / Event Handling

        private bool Output(string msg, bool error) {
            if (null == msg) return false;
            msg = msg.Trim();
            if (msg.Length != 0) {
                Dispatcher.Invoke(() => {
                    viewModel?.OutputList?.Add(new OutputMessage(msg, error));
                    OutputListView.ScrollToTail();
                });
            }
            return true;
        }

        bool IReportOutput.StandardOutput(string msg) {
            return Output(msg, error: false);
        }

        bool IReportOutput.ErrorOutput(string msg) {
            return Output(msg, error: true);
        }

        void IDownloadHost.Completed(DLEntry target, bool succeeded, bool extractAudio) {
            if (succeeded && !extractAudio) {
                Dispatcher.Invoke(() => {
                    if (viewModel.AutoPlay.Value) {
                        GetPlayer().AddToPlayList(target);
                    }
                });
            }
        }

        void IDownloadHost.FoundSubItem(DLEntry target) {
            Dispatcher.Invoke(() => {
                Storage.DLTable.Add(target);
                if (viewModel.AutoPlay.Value) {
                    GetPlayer().AddToPlayList(target);
                }
            });
        }

        private void OnBusyStateChanged(bool busy) {
            Dispatcher.Invoke(() => {
                viewModel.IsBusy.Value = busy;
            });
        }

        #endregion
    }

    public static class FilterExt {
        public static IEnumerable<DLEntry> FilterByRating(this IEnumerable<DLEntry> s, RatingFilter rf) {
            return rf.Filter(s);
        }
        public static IEnumerable<DLEntry> FilterByCategory(this IEnumerable<DLEntry> s, Category c) {
            return c.Filter(s);
        }
        public static IEnumerable<DLEntry> FilterByName(this IEnumerable<DLEntry> s, string search) {
            search = search?.Trim();
            return string.IsNullOrEmpty(search) ? s : s.Where((e) => (e.Name?.ContainsIgnoreCase(search) ?? false) || (e.Desc?.ContainsIgnoreCase(search)??false));
        }
        public static IOrderedEnumerable<DLEntry> Sort(this IEnumerable<DLEntry> s) {
            return Settings.Instance.SortInfo.Sort(s);
        }
    }
}
