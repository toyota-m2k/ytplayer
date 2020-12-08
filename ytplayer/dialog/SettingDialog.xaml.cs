using common;
using Reactive.Bindings;
using System;
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
using System.Windows.Shapes;

namespace ytplayer.dialog {
    public class SettingsViewModel : MicViewModelBase {
        public ReactivePropertySlim<string> DBPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> YoutubeDLPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> FFMpegPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> VideoPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> MusicPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> UseWSL { get; } = new ReactivePropertySlim<bool>(false);

        public SettingsViewModel() {
            var src = Settings.Instance;
            DBPath.Value = src.DBPath;
            YoutubeDLPath.Value = src.YoutubeDLPath;
            FFMpegPath.Value = src.FFMpegPath;
            VideoPath.Value = src.VideoPath;
            MusicPath.Value = src.MusicPath;
            UseWSL.Value = src.UseWSL;
        }

        public void SaveSettings() {
            var dst = Settings.Instance;
            dst.DBPath = DBPath.Value;
            dst.YoutubeDLPath = YoutubeDLPath.Value;
            dst.FFMpegPath = FFMpegPath.Value;
            dst.VideoPath = VideoPath.Value;
            dst.MusicPath = MusicPath.Value;
            dst.UseWSL = UseWSL.Value;
            dst.Serialize();
            dst.ApplyEnvironment();
        }
    }

    /// <summary>
    /// SettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingDialog : Window {
        public SettingDialog() {
            DataContext = new SettingsViewModel();
            InitializeComponent();
        }
        private SettingsViewModel viewModel => DataContext as SettingsViewModel;
    }
}
