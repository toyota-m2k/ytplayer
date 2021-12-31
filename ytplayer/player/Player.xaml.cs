using io.github.toyota32k.toolkit.utils;
using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ytplayer.common;
using ytplayer.data;

namespace ytplayer.player {
    /// <summary>
    /// Player.xaml の相互作用ロジック
    /// </summary>
    public partial class Player : UserControl {
        PlayerViewModel ViewModel => DataContext as PlayerViewModel;
        private CursorManager CursorManager;
        private double ReservePosition = 0;

        public Stretch Stretch {
            get => MediaPlayer.Stretch;
            set => MediaPlayer.Stretch = value;
        }

        public Player() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.Player = this;
            ViewModel.FitMode.Value = Stretch == Stretch.Uniform;
            ViewModel.FitMode.Subscribe(FitView);
            ViewModel.MaximizeCommand.Subscribe(ToggleFullscreen);
            ViewModel.Fullscreen.Value = Window.GetWindow(this).WindowStyle == WindowStyle.None;
            CursorManager = new CursorManager(Window.GetWindow(this));
            ViewModel.Speed.Subscribe((speed) => {
                double sr = (speed >= 0.5) ? 1 + (speed - 0.5) * 2 /* 1 ～ 2 */ : 0.2 + 0.8 * (speed * 2)/*0.2 ～ 1*/;
                MediaPlayer.SpeedRatio = sr;
            });
            ViewModel.Volume.Subscribe((volume) => {
                MediaPlayer.Volume = volume;
                var item = ViewModel.PlayList.Current.Value;
                if(item!=null) {
                    item.Volume = volume;
                }
            });
            ViewModel.PlayList.Current.Subscribe(OnCurrentItemChanged);

            ViewModel.PlayCommand.Subscribe(Play);
            ViewModel.PauseCommand.Subscribe(Pause);
            ViewModel.ChapterEditing.Subscribe(OnChapterEditing);
        }

        private void OnCurrentItemChanged(DLEntry item) {
            MediaPlayer.Stop();
            MediaPlayer.Source = null;
            ViewModel.SaveChapterListIfNeeds();
            ViewModel.State.Value = PlayerState.UNAVAILABLE;
            ViewModel.Trimming.Value = PlayRange.Empty;
            ViewModel.Chapters.Value = null;
            ViewModel.DisabledRanges.Value = null;

            ReservePosition = 0;
            Uri uri = null;
            if (item != null) {
                if (item.KEY == Settings.Instance.LastPlayingUrl && Settings.Instance.LastPlayingPos > 0) {
                    ReservePosition = Settings.Instance.LastPlayingPos;
                } else {
                    ReservePosition = item.TrimStart;
                }
                ViewModel.Volume.Value = item.Volume;

                string path = item.Path;
                if(!string.IsNullOrEmpty(path)) {
                    uri = new Uri(path);
                    ViewModel.State.Value = PlayerState.LOADING;
                }
                if (uri != null) {
                    MediaPlayer.Source = uri;
                    // Sourceをセットしただけでは OnMediaOpenedが呼ばれない。
                    // Play または、Stop を呼んでおく必要がある。
                    MediaPlayer.Stop();
                }
            }
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e) {
            if (!MediaPlayer.NaturalDuration.HasTimeSpan) return;
            ViewModel.State.Value = PlayerState.READY;
            ViewModel.Duration.Value = (ulong)MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            var current = ViewModel.PlayList.Current.Value;
            if (current != null) {
                current.DurationInSec = ViewModel.Duration.Value / 1000;
                ViewModel.PrepareChapterListForCurrentItem();
            }
            if (ViewModel.AutoPlay) {
                Play();
                double pos = 0;
                if (ReservePosition > 0 && ReservePosition < ViewModel.Duration.Value) {
                    pos = ReservePosition;
                }
                MediaPlayer.Position = TimeSpan.FromMilliseconds(pos);
                ReservePosition = 0;
            }
            if (!ViewModel.ShowPanel.Value && !ViewModel.ShowSizePanel.Value) {
                CursorManager?.Enable(true);
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e) {
            ViewModel.State.Value = PlayerState.READY;
            ControlPanel.Slider.OnMediaEnd();
            //if (mCurrentItemId != null) {
            //    ViewModel.ReachRangeEnd.OnNext(mCurrentItemId);
            //    mCurrentItemId = null;
            //}
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e) {
            ViewModel.State.Value = PlayerState.ERROR;
            LoggerEx.error(e.ErrorException);

            // ToDo: 
            // エラー表示と、Retry or Next 選択

            //if (mCurrentItemId != null) {
            //    ViewModel.ReachRangeEnd.OnNext(mCurrentItemId);
            //    mCurrentItemId = null;
            //}
        }

        public void Play() {
            if (ViewModel.IsReady.Value) {
                MediaPlayer.Play();
                ViewModel.State.Value = PlayerState.PLAYING;
            }
        }

        public void Pause() {
            if (ViewModel.IsPlaying.Value) {
                MediaPlayer.Pause();
                ViewModel.State.Value = PlayerState.READY;
            }
        }

        public void FitView(bool mode) {
            Stretch = mode ? Stretch.Uniform : Stretch.UniformToFill;
        }

        //public void Stop() {
        //    if (ViewModel.IsReady.Value) {
        //        MediaPlayer.Stop();
        //        ViewModel.State.Value = PlayerState.READY;
        //        ViewModel.Position.Value = 0;
        //    }
        //}

        public double SeekPosition {
            get => ViewModel.IsReady.Value ? MediaPlayer.Position.TotalMilliseconds : 0;
            set {
                if (ViewModel.IsReady.Value) {
                    MediaPlayer.Position = TimeSpan.FromMilliseconds(value);
                } else {
                    LoggerEx.error("cannot seek. (movie is not ready.)");
                }
            }
        }

        private bool ShowPanel(FrameworkElement panel, bool show) {
            if (ViewModel.ChapterEditing.Value) return true;
            switch (panel?.Tag as string) {
                case "ControlPanel":
                    ViewModel.ShowPanel.Value = show;
                    break;
                case "SizingPanel":
                    ViewModel.ShowSizePanel.Value = show;
                    break;
                default:
                    return false;
            }
            CursorManager?.Enable(!show);
            return true;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) {
            if(!ShowPanel(sender as FrameworkElement, true)) {
                CursorManager.Update(e.GetPosition(this));
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            CursorManager.Update(e.GetPosition(this));
        }

        private void OnMouseLeave(object sender, MouseEventArgs e) {
            if (!ShowPanel(sender as FrameworkElement, false)) {
                CursorManager.Reset();
            }
        }

        private void ToggleFullscreen() {
            var win = Window.GetWindow(this);
            if (win.WindowStyle == WindowStyle.None) {
                win.WindowStyle = WindowStyle.SingleBorderWindow;
                win.WindowState = WindowState.Normal;
                ViewModel.Fullscreen.Value = false;
            } else {
                win.WindowStyle = WindowStyle.None;         // タイトルバーと境界線を表示しない
                win.WindowState = WindowState.Maximized;    // 最大化表示
                ViewModel.Fullscreen.Value = true;
            }
        }

        private void OnPlayerClicked(object sender, MouseButtonEventArgs e) {
            if(ViewModel.IsPlaying.Value) {
                Pause();
            } else {
                Play();
            }
        }

        private void OnChapterEditing(bool edit) {
            if(edit) {
                ViewModel.ShowPanel.Value = true;
                ViewModel.ShowSizePanel.Value = false;
                CursorManager?.Enable(false);
            }
        }
    }
}
