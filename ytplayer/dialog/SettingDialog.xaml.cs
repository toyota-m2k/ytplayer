using common;
using Reactive.Bindings;
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

namespace ytplayer.dialog {
    public class SettingsViewModel : MicViewModelBase {
        public ReactivePropertySlim<string> DBPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> YoutubeDLPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> FFMpegPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> VideoPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> MusicPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> UseWSL { get; } = new ReactivePropertySlim<bool>(false);

        public void CopyFrom(SettingsViewModel src) {
            DBPath.Value = src.DBPath.Value;
            YoutubeDLPath.Value = src.YoutubeDLPath.Value;
            FFMpegPath.Value = src.FFMpegPath.Value;
            VideoPath.Value = src.VideoPath.Value;
            MusicPath.Value = src.MusicPath.Value;
            UseWSL.Value = src.UseWSL.Value;
        }
    }

    /// <summary>
    /// SettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingDialog : Window {
        public SettingDialog() {
            InitializeComponent();
        }
    }
}
