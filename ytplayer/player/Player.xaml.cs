using common;
using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ytplayer.common;

namespace ytplayer.player {
    public interface IPlayerViewModel {
        IReadOnlyReactiveProperty<double> Duration { get; }
        IReadOnlyReactiveProperty<bool> IsReady { get; }
        IReadOnlyReactiveProperty<bool> IsPlaying { get; }
        IReactiveProperty<double> Speed { get; }
        IReactiveProperty<double> Volume { get; }
        Subject<bool> Ended { get; }
    }

    public class PlayerViewModel : MicViewModelBase, IPlayerViewModel {
        IReadOnlyReactiveProperty<double> IPlayerViewModel.Duration => Duration;
        IReadOnlyReactiveProperty<bool> IPlayerViewModel.IsReady => IsReady;
        IReadOnlyReactiveProperty<bool> IPlayerViewModel.IsPlaying => IsPlaying;
        IReactiveProperty<double> IPlayerViewModel.Speed => Speed;
        IReactiveProperty<double> IPlayerViewModel.Volume => Volume;

        public ReactiveProperty<double> Duration { get; } = new ReactiveProperty<double>(100);
        public ReactiveProperty<bool> IsReady { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> IsPlaying { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> ShowPanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> ShowSizePanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<double> Speed { get; } = new ReactiveProperty<double>(0.5);
        public ReactiveProperty<double> Volume { get; } = new ReactiveProperty<double>(0.5);
        public Subject<bool> Ended { get; } = new Subject<bool>();

        public ReactiveProperty<bool> Fullscreen { get; } = new ReactiveProperty<bool>(false);
        public ReactiveCommand MaximizeCommand { get; } = new ReactiveCommand();

    }

    public interface IPlayer {
        IPlayerViewModel ViewModel { get; }
        void SetSource(string path, bool start=true);
        void Play();
        void Pause();
        void Stop();
        void ReserveSeekPosition(double pos);
        double SeekPosition { get; set; }
        Stretch Stretch { get; set; }
    }

    /// <summary>
    /// Player.xaml の相互作用ロジック
    /// </summary>
    public partial class Player : UserControl, IPlayer {
        PlayerViewModel ViewModel {
            get => DataContext as PlayerViewModel;
            set {
                ViewModel?.Dispose();
                DataContext = value;
            }
        }
        IPlayerViewModel IPlayer.ViewModel => ViewModel;
        private bool starting = false;
        private CursorManager CursorManager;

        public Stretch Stretch {
            get => MediaPlayer.Stretch;
            set => MediaPlayer.Stretch = value;
        }

        public Player() {
            ViewModel = new PlayerViewModel();
            ViewModel.MaximizeCommand.Subscribe(ToggleFullscreen);
            InitializeComponent();
        }

        public void Initialize() {
            ControlPanel.Initialize(this);
            CursorManager = new CursorManager(Window.GetWindow(this));
            ViewModel.Speed.Subscribe((speed) => {
                double sr = (speed >= 0.5) ? 1 + (speed - 0.5) * 2 /* 1 ～ 2 */ : 0.2 + 0.8 * (speed * 2)/*0.2 ～ 1*/;
                MediaPlayer.SpeedRatio = sr;
            });
        }

        public void Terminate() {
            ControlPanel.Terminate();
            ViewModel = null;
        }

        public void SetSource(string path, bool start=true) {
            starting = start;
            ViewModel.IsReady.Value = false;
            ViewModel.IsPlaying.Value = false;
            ViewModel.Speed.Value = 0.5;
            MediaPlayer.Source = string.IsNullOrEmpty(path) ? null : new Uri(path);
            if(path!=null) {
                Debug.Assert(PathUtil.isFile(path));
                Play();
            }
        }

        public void Play() {
            ViewModel.IsPlaying.Value = true;
            MediaPlayer.Play();
        }

        public void Stop() {
            ViewModel.IsPlaying.Value = false;
            MediaPlayer.Stop();
        }

        public void Pause() {
            if(ViewModel.IsPlaying.Value) {
                ViewModel.IsPlaying.Value = false;
                MediaPlayer.Pause();
            }
        }

        public double SeekPosition {
            get => ViewModel.IsReady.Value ? MediaPlayer.Position.TotalMilliseconds : 0;
            set {
                if (ViewModel.IsReady.Value) {
                    MediaPlayer.Position = TimeSpan.FromMilliseconds(value);
                }
            }
        }

        private double ReservePosition = 0;
        public void ReserveSeekPosition(double pos) {
            if(ViewModel.IsReady.Value) {
                MediaPlayer.Position = TimeSpan.FromMilliseconds(pos);
            } else {
                ReservePosition = pos;
            }
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e) {
            ViewModel.IsReady.Value = true;
            ViewModel.Duration.Value = MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            if(starting) {
                Play();
                if(ReservePosition>0 && ReservePosition<ViewModel.Duration.Value) {
                    MediaPlayer.Position = TimeSpan.FromMilliseconds(ReservePosition);
                    ReservePosition = 0;
                }
            }
            if(!ViewModel.ShowPanel.Value && !ViewModel.ShowSizePanel.Value) {
                CursorManager?.Enable(true);
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e) {
            ViewModel.IsPlaying.Value = false;
            ViewModel.Ended.OnNext(true);
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e) {
            ViewModel.IsReady.Value = false;
            ViewModel.IsPlaying.Value = false;
            ViewModel.Ended.OnNext(false);
        }

        private bool ShowPanel(FrameworkElement panel, bool show) {
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

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.Fullscreen.Value = Window.GetWindow(this).WindowStyle == WindowStyle.None;
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

    }
}
