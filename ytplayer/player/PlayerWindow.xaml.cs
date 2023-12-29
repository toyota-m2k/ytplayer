using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ytplayer.data;
using ytplayer.download;
using ytplayer.wav;

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

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            Settings.Instance.PlayerPlacement.ApplyPlacementTo(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.PlayList.Current.Subscribe(OnCurrentItemChanged);
            ViewModel.StorageClosed.Subscribe((_) => Close());
            ViewModel.AutoChapterCommand.Subscribe(OnAutoChapter);
            ViewModel.ClosePlayerCommand.Subscribe(Close);
            ViewModel.ExportCommand.Subscribe(OnExportFile);
            LoadCompletion.TrySetResult(true);
            ViewModel.KeyCommands.Enable(GetWindow(this), true);
        }

        private void OnCurrentItemChanged(DLEntry item) {
            this.Title = item?.Name ?? "";
            PlayItemChanged?.Invoke(item);
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            ViewModel.KeyCommands.Enable(GetWindow(this), false);
            Settings.Instance.PlayerPlacement.GetPlacementFrom(this);
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

        //private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        //    switch (e.Key) {
        //        case Key.Escape:
        //            Close();
        //            break;
        //        default:
        //            return;
        //    }
        //    e.Handled = true;
        //}

        private void AddTextToRichEdit(string text, Brush fg) {
            var p = OutputView.Document.Blocks.FirstBlock as Paragraph;
            var run = new Run(text);
            if (fg != null) {
                run.Foreground = fg;
            }
            p.Inlines.Add(run);
            p.Inlines.Add(new LineBreak());
            OutputView.ScrollToEnd();
        }

        private void StandardOutput(string text) {
            LoggerEx.info(text);
            StandardError(text);
        }
        private void StandardError(string text) {
            Dispatcher.Invoke(() => {
                LoggerEx.error(text);
                Brush brush = null;
                if (text.StartsWith("size=")) {
                    brush = new SolidColorBrush(Colors.Green);
                } else if (text.ToLower().Contains("error")) {
                    brush = new SolidColorBrush(Colors.Red);
                }
                AddTextToRichEdit(text, brush);
            });
        }


        private async void OnAutoChapter() {
            if (!ViewModel.ChapterEditing.Value) return;
            var item = ViewModel.PlayList.Current.Value;
            if (item == null) return;
            var chapterEditor = ViewModel.ChapterEditor.Value;
            if (chapterEditor==null) {
                return;
            }
            if (chapterEditor.Chapters.Value.Values.Count > 0) {
                if (MessageBoxResult.OK != MessageBox.Show(GetWindow(this), "All chapters will be replaced with created chapters.", "Auto Chapter", MessageBoxButton.OKCancel)) {
                    return;
                }
            }

            OutputView.Visibility = Visibility.Visible;
            OutputView.Document = new FlowDocument();
            OutputView.Document.Blocks.Add(new Paragraph());

            try {

                if (ViewModel.WavFile == null) {
                    AddTextToRichEdit("Extracting sound track...", new SolidColorBrush(Colors.Blue));
                    try {
                        var outFile = Path.Combine(Settings.Instance.EnsureWorkPath, "x.wav");
                        PathUtil.safeDeleteFile(outFile);

                        using (WaitCursor.Start(this)) {
                            ViewModel.WavFile = await WavFile.CreateFromMP4(item.VPath, outFile, StandardOutput, StandardError);
                            if (ViewModel.WavFile == null) {
                                MessageBox.Show(GetWindow(this), "Cannot extract sound track from MP4 file.", "Auto Chapter", MessageBoxButton.OK);
                                return;
                            }
                        }
                    }
                    catch (Exception e) {
                        LoggerEx.error(e);
                    }
                }

                AddTextToRichEdit("Analyzing sound track...", new SolidColorBrush(Colors.Blue));

                T limit<IT, T>(IT v, T min, T max) {
                    if ((dynamic)v < (dynamic)min) return min;
                    if ((dynamic)max < (dynamic)v) return max;
                    return (T)(dynamic)v;
                }

                var wavFile = ViewModel.WavFile;
                short threshold = limit(ViewModel.AutoChapterThreshold.Value, (short)0, (short)5000);
                double span = limit((double)(ViewModel.AutoChapterSpan.Value) / 1000, (double)0.5, (double)5.0);
                var result = await Task<bool>.Run(() => {
                    var ranges = wavFile.ScanChapter(threshold, span);

                    if (!Utils.IsNullOrEmpty(ranges)) {
                        Dispatcher.Invoke(() => {
                            chapterEditor.ClearAllChapters();
                            chapterEditor.AddChapter(new ChapterInfo(0)); // 先頭にチャプターを設定しておく
                        });
                        chapterEditor.EditInGroup((gr) => {
                            foreach (var r in ranges) {
                                var d = r.Item2 - r.Item1;
                                var p = r.Item2 - Math.Min(d / 2, 1.0);
                                var pos = (ulong)Math.Round(p * 1000);
                                Dispatcher.Invoke(() => {
                                    gr.AddChapter(new ChapterInfo(pos));
                                    AddTextToRichEdit($"  chapter-{chapterEditor.Chapters.Value.Values.Count} : {pos} msec", new SolidColorBrush(Colors.Gray));
                                });
                            }
                        });
                        return true;
                    } else {
                        return false;
                    }
                });
                if (!result) {
                    AddTextToRichEdit($"     chapter was detected.", new SolidColorBrush(Colors.Red));
                    MessageBox.Show(GetWindow(this), "No chapter was detected.", "Auto Chapter", MessageBoxButton.OK);
                }
            }
            finally {
                await Task.Delay(1000);
                OutputView.Visibility = Visibility.Hidden;
                OutputView.Document.Blocks.Clear();
            }
        }

        private void OnExportFile() {
            var item = ViewModel.PlayList.Current.Value;
            if (item == null) return;
            ViewModel.SaveChapterListIfNeeds();
            var chapterList = ViewModel.Chapters.Value;
            var exportWindow = new ExportWindow(item, chapterList);
            exportWindow.Owner = this;
            exportWindow.Show();
        }
    }
}
