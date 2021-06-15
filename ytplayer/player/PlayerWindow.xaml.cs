using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using ytplayer.data;
using ytplayer.download;

namespace ytplayer.player {
    /// <summary>
    /// PlayerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PlayerWindow : Window {
        private PlayerViewModel ViewModel {
            get => DataContext as PlayerViewModel;
            set {
                ViewModel?.Dispose();
                DataContext = value;
            }
        }


        public PlayerWindow(IStorageSupplier storageSupplier) {
            ViewModel = new PlayerViewModel(storageSupplier);
            InitializeComponent();
        }

        public event Action<DLEntry> PlayItemChanged;
        public event Action<PlayerWindow> PlayWindowClosing;
        public event Action<PlayerWindow> PlayWindowClosed;

        private TaskCompletionSource<bool> LoadCompletion = new TaskCompletionSource<bool>();

        //public IPlayList PlayList => Player.ControlPanel.PlayList;

        public (DLEntry entry, double position) CurrentPlayingInfo {
            get {
                var entry = ViewModel.PlayList.Current.Value;
                double position = 0;
                if(ViewModel.IsReady.Value) {
                    position = Player.SeekPosition;
                }
                return (entry, position);
            }
        }

        public void ResumePlay(IEnumerable<DLEntry> list, DLEntry entry/*, double pos*/) {
            if (entry != null) {
                ViewModel.PlayList.SetList(list, entry);
                //Player.ReserveSeekPosition(pos);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.PlayList.Current.Subscribe(OnCurrentItemChanged);
            ViewModel.StorageClosed.Subscribe((_) => Close());
            LoadCompletion.TrySetResult(true);
        }

        private void OnCurrentItemChanged(DLEntry item) {
            this.Title = item?.Name ?? "";
            PlayItemChanged?.Invoke(item);
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            ViewModel.SaveChapterListIfNeeds();
            LoadCompletion.TrySetResult(false);
            PlayWindowClosing?.Invoke(this);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            PlayWindowClosed?.Invoke(this);
            PlayWindowClosed = null;
            PlayItemChanged = null;
            ViewModel = null;
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            MainWindow.Instance?.RegisterUrl(e.Data.GetData(DataFormats.Text) as string, true);
        }

        public async void SetPlayList(IEnumerable<DLEntry> s, DLEntry initialItem = null) {
            await LoadCompletion.Task;
            ViewModel.PlayList.SetList(s, initialItem);
        }

        public async void AddToPlayList(DLEntry item) {
            await LoadCompletion.Task;
            ViewModel.PlayList.Add(item);
        }
    }
}
