using System;
using System.Collections.Generic;
using System.Windows;

namespace ytplayer.player {
    /// <summary>
    /// PlayerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PlayerWindow : Window {
        public PlayerWindow() {
            DataContext = this;
            InitializeComponent();
        }

        public event Action<IPlayable> PlayItemChanged;
        public event Action<PlayerWindow> PlayWindowClosed;

        public IPlayList PlayList => Player.ControlPanel.PlayList;

        public (IPlayable entry, double position) CurrentPlayingInfo {
            get {
                var entry = PlayList.Current.Value;
                double position = 0;
                var player = Player as IPlayer;
                if(player.ViewModel.IsPlaying.Value) {
                    position = player.SeekPosition;
                }
                return (entry, position);
            }
        }

        public void ResumePlay(IEnumerable<IPlayable> list, IPlayable entry, double pos) {
            if (entry != null) {
                PlayList.SetList(list, entry);
                Player.ReserveSeekPosition(pos);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            Player.Initialize();
            PlayList.Current.Subscribe(CurrentChanged);
        }

        private void CurrentChanged(IPlayable obj) {
            this.Title = obj?.Name ?? "";
            PlayList.Current.Subscribe((c) => {
                PlayItemChanged?.Invoke(c);
            });
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            PlayWindowClosed?.Invoke(this);
            PlayWindowClosed = null;
            PlayItemChanged = null;
            Player.Terminate();
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            MainWindow.Instance?.RegisterUrl(e.Data.GetData(DataFormats.Text) as string, true);
        }
    }
}
