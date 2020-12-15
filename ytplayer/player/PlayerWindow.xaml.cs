using System;
using System.Windows;

namespace ytplayer.player {
    /// <summary>
    /// PlayerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PlayerWindow : Window {
        public PlayerWindow() {
            InitializeComponent();
        }

        public event Action<IPlayable> PlayItemChanged;
        public event Action<PlayerWindow> PlayWindowClosed;

        //private static PlayerWindow sInstance = null;
        //public static PlayerWindow Instance {
        //    get {
        //        if(sInstance==null) {
        //            sInstance = new PlayerWindow();
        //            sInstance.Show();
        //        }
        //        return sInstance;
        //    }
        //}

        public IPlayList PlayList => Player.ControlPanel.PlayList;

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
        }
    }
}
