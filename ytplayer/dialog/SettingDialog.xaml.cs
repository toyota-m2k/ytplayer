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
using ytplayer.common;

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

        public string Validate() {
            if (!string.IsNullOrEmpty(DBPath.Value)) {
                var dir = System.IO.Path.GetDirectoryName(DBPath.Value);
                if (dir == null) return "invalid db directory.";
                if (dir != string.Empty && !PathUtil.isDirectory(dir)) return $"invalid directory {dir}";
            }
            if (!UseWSL.Value) {
                if (!string.IsNullOrEmpty(YoutubeDLPath.Value)) {
                    if (!PathUtil.isFile(System.IO.Path.Combine(YoutubeDLPath.Value, "youtube-dl.exe"))) return "youtube-dl is not found.";
                }
                if (!string.IsNullOrEmpty(FFMpegPath.Value)) {
                    if (!PathUtil.isFile(System.IO.Path.Combine(FFMpegPath.Value, "ffmpeg.exe")) ||
                        !PathUtil.isFile(System.IO.Path.Combine(FFMpegPath.Value, "ffprobe.exe"))) return "ffmpeg is not found.";
                }
            }
            if (!string.IsNullOrEmpty(VideoPath.Value)) {
                if(!PathUtil.isDirectory(VideoPath.Value)) {
                    return $"no such directory: {VideoPath.Value}";
                }
            }
            if (!string.IsNullOrEmpty(MusicPath.Value)) {
                if (!PathUtil.isDirectory(MusicPath.Value)) {
                    return $"no such directory: {MusicPath.Value}";
                }
            }
            return null;
        }
        public void SaveSettings() {
            if (Validate() != null) return;

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
