using common;
using Reactive.Bindings;
using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Windows;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.dialog {
    /**
     * 設定画面のビューモデル
     */
    public class SettingsViewModel : MicViewModelBase<SettingDialog> {
        public ReactivePropertySlim<string> DBPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> UseWSL { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> YoutubeDLPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> FFMpegPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> VideoPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> AudioPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> ErrorMessage { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> Cancellable { get; } = new ReactivePropertySlim<bool>(true);
        public ReadOnlyReactivePropertySlim<bool> CanUpdateYTD { get; }
        public ReactivePropertySlim<bool> Ready { get; } = new ReactivePropertySlim<bool>(true);

        public ReactiveCommand CommandDBPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandYTDLPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandFFMpegPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandVideoPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandAudioPath { get; } = new ReactiveCommand();

        public ReactiveCommand OKCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();
        public ReactiveCommand UpdateCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<bool> Completed { get; } = new ReactiveCommand<bool>();

        [Disposal(false)]
        public Storage NewStorage { get; private set; } = null;

        public SettingsViewModel(SettingDialog owner) : base(owner) {
            var src = Settings.Instance;
            DBPath.Value = src.DBPath;
            YoutubeDLPath.Value = src.YoutubeDLPath;
            FFMpegPath.Value = src.FFMpegPath;
            VideoPath.Value = src.VideoPath;
            AudioPath.Value = src.AudioPath;
            UseWSL.Value = src.UseWSL;

            CanUpdateYTD = YoutubeDLPath.Select((v) => PathUtil.isFile(System.IO.Path.Combine(v, "youtube-dl.exe"))).ToReadOnlyReactivePropertySlim();

            CommandDBPath.Subscribe(() => SelectDBFile(DBPath));
            CommandYTDLPath.Subscribe(() => SelectFolder("youtube-dl folder", YoutubeDLPath));
            CommandFFMpegPath.Subscribe(() => SelectFolder("ffmpeg folder", FFMpegPath));
            CommandVideoPath.Subscribe(() => SelectFolder("Video Folder", VideoPath));
            CommandAudioPath.Subscribe(() => SelectFolder("Audio Folder", AudioPath));

            OKCommand.Subscribe(() => {
                ErrorMessage.Value = Validate();
                if(!string.IsNullOrEmpty(ErrorMessage.Value)) {
                    return;
                }
                SaveSettings();
                Completed.Execute(true);
            });
            CancelCommand.Subscribe(() => Completed.Execute(false));
            UpdateCommand.Subscribe(UpdateYTD);
        }

        private void SelectFolder(string title, ReactivePropertySlim<string> path) {
            var r = FolderDialogBuilder.Create()
                .title(title)
                .initialDirectory(path.Value)
                .GetFilePath(Owner);
            if (null!=r) {
                path.Value = r;
            }
        }

        private void SelectDBFile(ReactivePropertySlim<string> path) {
            var r = SaveFileDialogBuilder.Create()
                .title("DB File")
                .initialDirectory(PathUtil.getDirectoryName(path.Value))
                .showFolders(true)
                .defaultExtension(YtpDef.DB_EXT)
                .defaultFilename(YtpDef.DEFAULT_DBNAME)
                .GetFilePath(Owner);
            if (null != r) {
                path.Value = r;
            }
        }

        private string Validate() {
            DBPath.Value = Settings.ComplementDBPath(DBPath.Value);

            //if (!string.IsNullOrEmpty(DBPath.Value)) {
            //    var dir = System.IO.Path.GetDirectoryName(DBPath.Value);
            //    if (dir == null) return "invalid db directory.";
            //    if (dir != string.Empty && !PathUtil.isDirectory(dir)) return $"invalid directory {dir}";
            //}
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
            if (!string.IsNullOrEmpty(AudioPath.Value)) {
                if (!PathUtil.isDirectory(AudioPath.Value)) {
                    return $"no such directory: {AudioPath.Value}";
                }
            }

            if (Owner.CurrentStorage == null || !PathUtil.isEqualDirectoryName(Owner.CurrentStorage.DBPath, DBPath.Value)) {
                // 現在と異なるDBファイルが指定された・・・開いてみる。
                try {
                    NewStorage = new Storage(DBPath.Value);
                } catch(Exception e) {
                    // 開けなかったらNG
                    Logger.error(e);
                    NewStorage = null;
                    return $"cannot create db.";
                }
            }

            return null;    // Succeeded.
        }
        private void SaveSettings() {
            var dst = Settings.Instance;
            dst.DBPath = DBPath.Value;
            dst.YoutubeDLPath = YoutubeDLPath.Value;
            dst.FFMpegPath = FFMpegPath.Value;
            dst.VideoPath = VideoPath.Value;
            dst.AudioPath = AudioPath.Value;
            dst.UseWSL = UseWSL.Value;
            dst.Serialize();
            dst.ApplyEnvironment();
        }

        private async void UpdateYTD(object obj) {
            Ready.Value = false;
            try {
                using (var client = new HttpClient())
                using (var stream = await client.GetStreamAsync("https://youtube-dl.org/downloads/latest/youtube-dl.exe"))
                using (var file = System.IO.File.Create(System.IO.Path.Combine(YoutubeDLPath.Value, "youtube-dl.exe"))) {
                    await stream.CopyToAsync(file);
                    await file.FlushAsync();
                }
            } finally {
                Ready.Value = true;
            }
        }
    }

    /// <summary>
    /// SettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingDialog : Window {
        public bool Result { get; private set; } = false;
        public Storage CurrentStorage { get; }
        public Storage NewStorage => viewModel.NewStorage;
        public SettingDialog(Storage currentStorage) {
            viewModel = new SettingsViewModel(this);
            viewModel.Cancellable.Value = currentStorage!=null;

            InitializeComponent();
            viewModel.Completed.Subscribe((res) => {
                Result = res;
                Close();
            });
        }
        private SettingsViewModel viewModel {
            get => DataContext as SettingsViewModel;
            set => DataContext = value;
        }

        private void OnClosed(object sender, EventArgs e) {
            viewModel?.Dispose();
        }
    }
}
