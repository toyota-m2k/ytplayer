﻿using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using ytplayer.data;

namespace ytplayer.dialog {
    /**
     * 設定画面のビューモデル
     */
    public class SettingsViewModel : ViewModelBase<SettingDialog> {
        public ReactivePropertySlim<string> DBPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> UseWSL { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<string> YoutubeDLPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> FFMpegPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> VideoPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> AudioPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<string> WorkPath { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> EnableServer { get; } = new ReactivePropertySlim<bool>();
        public ReactivePropertySlim<string> WebPageRoot { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<int> ServerPort { get; } = new ReactivePropertySlim<int>();
        public ReactivePropertySlim<bool> AcceptList { get; } = new ReactivePropertySlim<bool>();

        public ReactivePropertySlim<string> ErrorMessage { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<bool> Cancellable { get; } = new ReactivePropertySlim<bool>(true);
        public ReadOnlyReactivePropertySlim<bool> CanUpdateYTD { get; }
        public ReactivePropertySlim<bool> Ready { get; } = new ReactivePropertySlim<bool>(true);

        public ReactiveCommand CommandDBPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandYTDLPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandFFMpegPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandVideoPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandAudioPath { get; } = new ReactiveCommand();
        public ReactiveCommand CommandWorkPath { get; } = new ReactiveCommand();

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
            WorkPath.Value = src.WorkPath;
            EnableServer.Value = src.EnableServer;
            WebPageRoot.Value = src.WebPageRoot;
            ServerPort.Value = src.ServerPort;
            AcceptList.Value = src.AcceptList;

            CanUpdateYTD = YoutubeDLPath.Select((v) => PathUtil.isFile(System.IO.Path.Combine(v, YtpDef.YTDLP_EXE))).ToReadOnlyReactivePropertySlim();

            CommandDBPath.Subscribe(() => SelectDBFile(DBPath));
            CommandYTDLPath.Subscribe(() => SelectFolder($"{YtpDef.YTDLP_EXE} folder", YoutubeDLPath));
            CommandFFMpegPath.Subscribe(() => SelectFolder("ffmpeg folder", FFMpegPath));
            CommandVideoPath.Subscribe(() => SelectFolder("Video Folder", VideoPath));
            CommandAudioPath.Subscribe(() => SelectFolder("Audio Folder", AudioPath));
            CommandWorkPath.Subscribe(() => SelectFolder("Work Directory", WorkPath));

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
            var r = OpenFileDialogBuilder.Create()
                .title("DB File")
                .ensureFileExists(false)
                .initialDirectory(PathUtil.getDirectoryName(path.Value))
                .defaultExtension(YtpDef.DB_EXT)
                .defaultFilename(YtpDef.DEFAULT_DBNAME)
                .GetFilePath(Owner);
            if (null != r) {
                if (!CheckDB(r)) {
                    // ytplayer用のDBファイルではない
                    MessageBox.Show(Owner, "このファイルはいけません。", "DBファイル", MessageBoxButton.OK);
                    return;
                }
                path.Value = r;
            }
        }

        private bool CheckDB(string path) {
            try {
                if (!PathUtil.isExists(path)) {
                    // ファイルが存在しないときは新規作成可能なのでTrue
                    return true;
                }
                return Storage.CheckDB(path);
            }
            catch (Exception) {
                return false;
            }
        }

        //private Storage TryOpenStorage(string path) {
            //try {
            //    if(!CheckDB(path)) {
            //        return null;
            //    }
            //    return new Storage(path);
            //}
            //catch (Exception e) {
            //    // 開けなかったらNG
            //    Logger.error(e);
            //    return null;
            //}
        //}

        private string Validate() {
            DBPath.Value = Settings.ComplementDBPath(DBPath.Value);

            //if (!string.IsNullOrEmpty(DBPath.Value)) {
            //    var dir = System.IO.Path.GetDirectoryName(DBPath.Value);
            //    if (dir == null) return "invalid db directory.";
            //    if (dir != string.Empty && !PathUtil.isDirectory(dir)) return $"invalid directory {dir}";
            //}
            if (!UseWSL.Value) {
                if (!string.IsNullOrEmpty(YoutubeDLPath.Value)) {
                    if (!PathUtil.isFile(System.IO.Path.Combine(YoutubeDLPath.Value, YtpDef.YTDLP_EXE))) return "youtube-dl is not found.";
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
            if (!string.IsNullOrEmpty(WorkPath.Value)) {
                if (!PathUtil.isDirectory(WorkPath.Value)) {
                    return $"no such directory: {WorkPath.Value}";
                }
            }

            Console.WriteLine($"Current: {Owner.CurrentStorage?.DBPath}");
            Console.WriteLine($"New    : {DBPath.Value}");
            if (Owner.CurrentStorage == null || !PathUtil.isEqualDirectoryName(Owner.CurrentStorage.DBPath, DBPath.Value)) {
                // 現在と異なるDBファイルが指定された・・・開いてみる。
                NewStorage = Storage.OpenDB(DBPath.Value);
                if(null==NewStorage) {
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
            dst.WorkPath = WorkPath.Value;
            dst.EnableServer = EnableServer.Value;
            dst.ServerPort = ServerPort.Value;
            dst.AcceptList = AcceptList.Value;
            dst.WebPageRoot = WebPageRoot.Value;
            dst.Serialize();
            dst.ApplyEnvironment();
        }

        /**
         * ToDo: yt-dlp 自動アップデート
         * yt-dlp --update でいけそう。
         */
        private async void UpdateYTD(object obj) {
            //Ready.Value = false;
            //try {
            //    using (var client = new HttpClient())
            //    using (var stream = await client.GetStreamAsync("https://youtube-dl.org/downloads/latest/youtube-dl.exe"))
            //    using (var file = System.IO.File.Create(System.IO.Path.Combine(YoutubeDLPath.Value, "youtube-dl.exe"))) {
            //        await stream.CopyToAsync(file);
            //        await file.FlushAsync();
            //    }
            //} finally {
            //    Ready.Value = true;
            //}
            var psi = new ProcessStartInfo() {
                FileName = YtpDef.YTDLP_EXE,
                Arguments = $"--update",
                CreateNoWindow = true,
                UseShellExecute = false,
                //StandardOutputEncoding = System.Text.Encoding.UTF8,
                //StandardErrorEncoding = System.Text.Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            try {
                var sb = new StringBuilder();
                var error = false;
                using (WaitCursor.Start(Owner)) {
                    var process = Process.Start(psi);
                    if (process == null) {
                        throw new InvalidOperationException("start process error");
                    }
                    while (true) {
                        var s = await process.StandardOutput.ReadLineAsync();
                        if (s == null) break;
                        sb.AppendLine(s);
                    }
                    while (true) {
                        var s = await process.StandardError.ReadLineAsync();
                        if (s == null) break;
                        sb.AppendLine(s);
                        error = true;
                    }
                }

                MessageBox.Show(sb.ToString(), "Update yt-dlp", MessageBoxButton.OK
                    , error ? MessageBoxImage.Error : MessageBoxImage.Information);
            }
            catch (Exception ex) {
                LoggerEx.error(ex);
                MessageBox.Show("Something wrong...", "Update yt-dlp", MessageBoxButton.OK
                    , MessageBoxImage.Error);
            }



        }

    }

    /// <summary>
    /// SettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingDialog : Window {
        public class DResult {
            public bool Ok { get; }
            public Storage NewStorage { get; }

            public DResult(bool ok, Storage newStorage) {
                Ok = ok;
                NewStorage = newStorage;
            }
        }
        public DResult Result { get; private set; } = null;
        public Storage CurrentStorage { get; }

        public SettingDialog(Storage currentStorage) {
            CurrentStorage = currentStorage;
            Result = null;
            viewModel = new SettingsViewModel(this);
            viewModel.Cancellable.Value = currentStorage!=null;
            InitializeComponent();
            viewModel.Completed.Subscribe((res) => {
                Result = new DResult(res, viewModel.NewStorage);
                Close();
            });
        }
        private SettingsViewModel viewModel {
            get => DataContext as SettingsViewModel;
            set => DataContext = value;
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            viewModel?.Dispose();
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            if(!viewModel.Cancellable.Value && Result==null) {
                e.Cancel = true;
            }
        }
    }
}
