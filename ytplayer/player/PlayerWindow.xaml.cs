using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

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
