﻿using common;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ytplayer.common;
using ytplayer.data;
using ytplayer.dialog;
using ytplayer.download;
using ytplayer.download.downloader;
using ytplayer.interop;
using ytplayer.player;
using ytplayer.server;

namespace ytplayer {
    /**
     * アウトプットリストに出力する文字列エントリクラス
     */
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

    /**
     * MainWindow上に表示する簡易ダイアログのID
     */
    public enum DialogTypeId {
        DELETE_COMFIRM,
        ACCEPT_DETERMINATION,
        EXTRACT_AUDIO,
        EDIT_DESCRIPTION,
        SYNC_FROM,
        CLOSING_MESSAGE,
    }

    /**
     * ビューモデル
     */
    public class MainViewModel : MicViewModelBase {
        public ReactiveProperty<ObservableCollection<DLEntry>> MainList { get; } = new ReactiveProperty<ObservableCollection<DLEntry>>(new ObservableCollection<DLEntry>());
        public ReactivePropertySlim<bool> AutoDownload { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> AutoPlay { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> IsBusy { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ShowFilterEditor { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ClipboardWatching { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> StatusString { get; } = new ReactivePropertySlim<string>();
        public ObservableCollection<OutputMessage> OutputList { get; } = new ObservableCollection<OutputMessage>();
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.FilterList);
        public ReactivePropertySlim<Category> CurrentCategory { get; } = new ReactivePropertySlim<Category>(Settings.Instance.Categories.All);
        public ReactivePropertySlim<bool> ShowBlocked { get; } = new ReactivePropertySlim<bool>(false);
        public RatingFilter RatingFilter { get; } = new RatingFilter();
        public ObservableCollection<string> SearchHistory => Settings.Instance.SearchHistories.History;
        public ReactivePropertySlim<string> SearchText { get; } = new ReactivePropertySlim<string>();

        public bool BusyWithModal = false;

        public ReactiveCommand CommandSettings { get; } = new ReactiveCommand();
        public ReactiveCommand CommandClearOutput { get; } = new ReactiveCommand();
        public ReactiveCommand CommandFoldOutput { get; } = new ReactiveCommand();
        public ReactiveCommand CommandPlay { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSearch { get; } = new ReactiveCommand();
        public ReactiveCommand CommandClearSearchText { get; } = new ReactiveCommand();
        public ReactiveCommand CommandExport { get; } = new ReactiveCommand();
        public ReactiveCommand CommandImport { get; } = new ReactiveCommand();
        public ReactiveCommand CommandSync { get; } = new ReactiveCommand();
        public ReactiveCommand CommandBrowser { get; } = new ReactiveCommand();

        // Context Menu
        public ReactiveCommand OpenInWebBrowserCommand { get; } = new ReactiveCommand();
        public ReactiveCommand DeleteAndBlockCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetAndDownloadCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ExtractAudioCommand { get; } = new ReactiveCommand();
        public ReactiveCommand EditDescriptionCommand { get; } = new ReactiveCommand();

        // Dialog
        private TaskCompletionSource<bool> DialogTask { get; set; } = null;
        public ReactivePropertySlim<bool> DialogActivated { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> DialogTitle { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<DialogTypeId> DialogType { get; } = new ReactivePropertySlim<DialogTypeId>();
        public ReactiveCommand CommandCancel { get; } = new ReactiveCommand();
        public ReactiveCommand CommandOk { get; } = new ReactiveCommand();

        public Task<bool> ShowDialog(DialogTypeId type, string title) {
            BusyWithModal = true;
            DialogTask = new TaskCompletionSource<bool>();
            DialogType.Value = type;
            DialogTitle.Value = title;
            DialogActivated.Value = true;
            return DialogTask.Task;
        }

        // Delete Item Dialog
        public class DeleteItemDialogViewModel : MicViewModelBase {
            public ReactivePropertySlim<bool> BlockItem { get; } = new ReactivePropertySlim<bool>(false);
            public ReactivePropertySlim<bool> DeleteVideoFile { get; } = new ReactivePropertySlim<bool>(false);
            public ReactivePropertySlim<bool> DeleteAudioFile { get; } = new ReactivePropertySlim<bool>(false);
        }
        public DeleteItemDialogViewModel DeleteItemDialog { get; } = new DeleteItemDialogViewModel();
        public Task<bool> ShowDeleteItemDialog() {
            return ShowDialog(DialogTypeId.DELETE_COMFIRM, "Delete Items");
        }

        // Accept/Reject Determination Dialog
        public class DeterminationDialogViewModel : MicViewModelBase {
            public ReactivePropertySlim<string> Host { get; } = new ReactivePropertySlim<string>();
            public ReactiveCommand CommandOk { get; } = new ReactiveCommand();
        }
        public DeterminationDialogViewModel DeterminationDialog { get; } = new DeterminationDialogViewModel();
        public Task<bool> ShowDeterminationDialog(string host) {
            DeterminationDialog.Host.Value = host;
            return ShowDialog(DialogTypeId.ACCEPT_DETERMINATION, "Accept or Reject");
        }

        // Extract Audio Dialog
        public class ExtractAudoDialogViewModel : MicViewModelBase {
            public ReactiveProperty<bool> DeleteVideo { get; } = new ReactiveProperty<bool>();
            public ReactiveProperty<bool> DownloadAudio { get; } = new ReactiveProperty<bool>();
        }
        public ExtractAudoDialogViewModel ExtractAudoDialog { get; } = new ExtractAudoDialogViewModel();
        public Task<bool> ShowExtractAudioDialog() {
            return ShowDialog(DialogTypeId.EXTRACT_AUDIO, "Extract Audio");
        }

        // Description Dialog
        public class DescriptionDialogViewModel : MicViewModelBase {
            public ReactiveProperty<string> Description { get; } = new ReactiveProperty<string>();
        }
        public DescriptionDialogViewModel DescriptionDialog { get; } = new DescriptionDialogViewModel();
        public Task<bool> ShowDescriptionDialog() {
            return ShowDialog(DialogTypeId.EDIT_DESCRIPTION, "Description");
        }

        public class SyncDialogViewModel:MicViewModelBase {
            public ReactiveProperty<string> HostAddress { get; } = new ReactiveProperty<string>();
        }
        public SyncDialogViewModel SyncDialog { get; } = new SyncDialogViewModel();
        public async Task<bool> ShowSyncDialog() {
            if (string.IsNullOrEmpty(SyncDialog.HostAddress.Value)) {
                SyncDialog.HostAddress.Value = Settings.Instance.SyncPeer;
            }

            if(await ShowDialog(DialogTypeId.SYNC_FROM, "Synchronization")) {
                Settings.Instance.SyncPeer = SyncDialog.HostAddress.Value;
                return true;
            }
            return false;
        }


        /**
         * ビューモデルの構築
         */
        public MainViewModel() {
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
            CommandClearSearchText.Subscribe(() => SearchText.Value = "");
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
            viewModel.SearchText.Subscribe((s) => RefreshList());
            viewModel.ShowBlocked.Subscribe((s) => RefreshList());
            viewModel.AutoDownload.Subscribe((v) => {
                if (!v||Storage==null) return;
                var targets = Storage.DLTable.List.Where((e) => e.Status == Status.INITIAL || e.Status == Status.CANCELLED);
                mDownloadManager.Enqueue(targets);
            });

            viewModel.OpenInWebBrowserCommand.Subscribe(OpenInWebBrower);
            viewModel.ExtractAudioCommand.Subscribe(ExtractAudio);
            viewModel.ResetAndDownloadCommand.Subscribe(ResetAndDownload);
            viewModel.DeleteAndBlockCommand.Subscribe(DeleteAndBlock);
            viewModel.EditDescriptionCommand.Subscribe(EditDescription);

            viewModel.ShowFilterEditor.Subscribe((v) => {
                if (v) {
                    GetFilterEditorWindow();
                } else {
                    mFilterEditorWindow?.Close();
                }
            });
            viewModel.CommandExport.Subscribe(ExportUrlList);
            viewModel.CommandImport.Subscribe(ImportUrlList);
            viewModel.CommandSync.Subscribe(SyncFrom);
            viewModel.CommandBrowser.Subscribe(() => Process.Start("btytbrs:"));
            //viewModel.ClipboardWatching.Subscribe((v) => {
            //    if (v) {
            //        Process.Start("btytbrs:");
            //    }
            //});
            InitializeComponent();
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
                    var pos = Settings.Instance.LastPlayingPos;
                    if (pos > 0) {
                        var win = GetPlayer();
                        win.ResumePlay(viewModel.MainList.Value, entry, pos);
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
            if (Settings.Instance.EnableServer && mServer==null) {
                mServer = new YtServer(this, Settings.Instance.ServerPort);
                mServer.Start();
            } else {
                StopServer();
            }
        }

        private void StopServer() {
            mServer?.Stop();
            mServer = null;
        }

        private bool CloseToBeWaited() {
            Storage.DLTable.Update();

            bool result = false;
            if(!mDownloadManager.Disposed) {
                mDownloadManager.Dispose();
                result = true;
            }
            if(!mDownloadAcceptor.Disposed) {
                mDownloadAcceptor.Dispose();
                result = true;
            }
            return result;
        }

        private void WaitAndClose() {
            viewModel.DialogType.Value = DialogTypeId.CLOSING_MESSAGE;
            viewModel.DialogTitle.Value = "Bye...";
            viewModel.DialogActivated.Value = true;
            Task.Run(async () => {
                await mDownloadAcceptor.WaitForClose();
                await mDownloadManager.WaitForClose();
            }).GetAwaiter().GetResult();
        }
        

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (mDownloadManager.IsBusy) {
                var r = MessageBox.Show("ダウンロード中のため終了できません。キャンセルしますか？", "ytplayer", MessageBoxButton.YesNo);
                if (r == MessageBoxResult.No) {
                    e.Cancel = true;
                    return;
                }
            }

            if (CloseToBeWaited()) {
                WaitAndClose();
            }

            StopServer();

            mClipboardMonitor.Dispose();
            if (mPlayerWindow != null) {
                var (cur, pos) = mPlayerWindow.CurrentPlayingInfo;
                Settings.Instance.LastPlayingUrl = cur.KEY;
                Settings.Instance.LastPlayingPos = pos;
                mPlayerWindow.Close();
                mPlayerWindow = null;
            } else {
                Settings.Instance.LastPlayingUrl = (MainListView.SelectedItem as DLEntry)?.KEY;
                Settings.Instance.LastPlayingPos = 0;
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
            viewModel.MainList.Value = new ObservableCollection<DLEntry>(Storage.DLTable.List
                .FilterByRating(viewModel.RatingFilter)
                .FilterByCategory(viewModel.CurrentCategory.Value)
                .FilterByName(viewModel.SearchText.Value)
                .Where((c) => viewModel.ShowBlocked.Value || (c.Status != Status.BLOCKED && c.Status != Status.FAILED))
                .Sort());
        }

        private void OnSearchBoxLostFocus(object sender, RoutedEventArgs e) {
            if (viewModel != null) {
                Settings.Instance.SearchHistories.Put(viewModel.SearchText.Value);
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
        private async void SyncFrom(object obj) {
            if (await viewModel.ShowSyncDialog()) {
                await SyncManager.SyncFrom(viewModel.SyncDialog.HostAddress.Value, Storage, this);
            }
        }

        #endregion

        #region Items Selection / Context Menu

        private DLEntry SelectedEntry => MainListView.SelectedItem as DLEntry;
        
        public IEnumerable<DLEntry> SelectedEntries => MainListView.SelectedItems.ToEnumerable<DLEntry>();
        public IEnumerable<DLEntry> ListedEntries => MainListView.Items.ToEnumerable<DLEntry>();
        public IEnumerable<DLEntry> AllEntries => Storage.DLTable.List;

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


        #endregion

        #region Category & Rating Setting Panel

        private CategoryRatingDialog mFilterEditorWindow = null;
        private CategoryRatingDialog GetFilterEditorWindow() {
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
                    mFilterEditorWindow.Top = this.Top + this.ActualHeight - 300;
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

        #region Player Window

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
            try {
                RegisterUrl(Clipboard.GetText());
            } catch(Exception ex) {
                Logger.error(ex);
                ((IDownloadHost)this).ErrorOutput(ex.Message);
            }
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
                    viewModel.OutputList.Add(new OutputMessage(msg, error));
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
