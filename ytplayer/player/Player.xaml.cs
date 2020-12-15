using common;
using Reactive.Bindings;
using System;
using System.Diagnostics;
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
        Subject<bool> Ended { get; }
    }

    public class PlayerViewModel : MicViewModelBase, IPlayerViewModel {
        IReadOnlyReactiveProperty<double> IPlayerViewModel.Duration => Duration;
        IReadOnlyReactiveProperty<bool> IPlayerViewModel.IsReady => IsReady;
        IReadOnlyReactiveProperty<bool> IPlayerViewModel.IsPlaying => IsPlaying;
        public Subject<bool> Ended { get; } = new Subject<bool>();

        public ReactiveProperty<double> Duration { get; } = new ReactiveProperty<double>(100);
        public ReactiveProperty<bool> IsReady { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> IsPlaying { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> ShowPanel { get; } = new ReactiveProperty<bool>(true);
    }

    public interface IPlayer {
        IPlayerViewModel ViewModel { get; }
        void SetSource(string path, bool start=true);
        void Play();
        void Pause();
        void Stop();
        double SeekPosition { get; set; }
        Stretch Stretch { get; set; }
    }

    /// <summary>
    /// Player.xaml の相互作用ロジック
    /// </summary>
    public partial class Player : UserControl, IPlayer {
        PlayerViewModel ViewModel {
            get => DataContext as PlayerViewModel;
            set => DataContext = value;
        }
        IPlayerViewModel IPlayer.ViewModel => ViewModel;
        private bool starting = false;

        public Stretch Stretch {
            get => MediaPlayer.Stretch;
            set => MediaPlayer.Stretch = value;
        }

        public Player() {
            ViewModel = new PlayerViewModel();
            InitializeComponent();
        }

        public void Initialize() {
            ControlPanel.Initialize(this);
        }

        public void SetSource(string path, bool start=true) {
            starting = start;
            ViewModel.IsReady.Value = false;
            ViewModel.IsPlaying.Value = false;
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

        private void OnMediaOpened(object sender, RoutedEventArgs e) {
            ViewModel.IsReady.Value = true;
            ViewModel.Duration.Value = MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            if(starting) {
                Play();
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

        private void OnMouseEnter(object sender, MouseEventArgs e) {
            ViewModel.ShowPanel.Value = true;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e) {
            ViewModel.ShowPanel.Value = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ////ControlPanel.Initialize(this);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            ViewModel.Dispose();
            ViewModel = null;
        }
    }
}
