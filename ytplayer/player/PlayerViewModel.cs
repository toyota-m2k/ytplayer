using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using ytplayer.data;

namespace ytplayer.player {
    public enum PlayerState {
        UNAVAILABLE,
        LOADING,
        READY,
        PLAYING,
        ENDED,
        ERROR,
    }
    public class PlayerViewModel : ViewModelBase {
        // Item Entry
        public ReactivePropertySlim<ulong> Duration { get; } = new ReactivePropertySlim<ulong>(1000);
        public ReactivePropertySlim<ulong> Position { get; } = new ReactivePropertySlim<ulong>(0);
        public ReactivePropertySlim<PlayerState> State { get; } = new ReactivePropertySlim<PlayerState>(PlayerState.UNAVAILABLE);

        public ReadOnlyReactivePropertySlim<bool> IsReady { get; }
        public ReadOnlyReactivePropertySlim<bool> IsPlaying { get; }

        public ReactivePropertySlim<double> Speed { get; } = new ReactivePropertySlim<double>(0.5);
        public ReactivePropertySlim<double> Volume { get; } = new ReactivePropertySlim<double>(0.5);

        // Trimming
        public ReadOnlyReactivePropertySlim<ulong> TrimStart { get; }
        public ReadOnlyReactivePropertySlim<ulong> TrimEnd { get; }
        public ReadOnlyReactivePropertySlim<PlayRange> PlayRange { get; }

        public ReadOnlyReactivePropertySlim<string> TrimStartText { get; }
        public ReadOnlyReactivePropertySlim<string> TrimEndText { get; }
        public ReadOnlyReactivePropertySlim<string> DurationText { get; }
        public ReadOnlyReactivePropertySlim<string> PositionText { get; }


        public PlayList PlayList { get; } = new PlayList();
        public bool AutoPlay { get; } = true;
        //public Subject<bool> Ended { get; } = new Subject<bool>();
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.SelectList);

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
        //public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetSpeedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetVolumeCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AddChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand EditChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetTrimCommand { get; } = new ReactiveCommand();

        // Window
        public ReactiveProperty<bool> FitMode { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<bool> ShowPanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> ShowSizePanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> Fullscreen { get; } = new ReactiveProperty<bool>(false);
        public ReactiveCommand MaximizeCommand { get; } = new ReactiveCommand();

        // Player
        private WeakReference<Player> mPlayer = null;
        public Player Player {
            get => mPlayer?.GetValue();
            set { mPlayer = value == null ? null : new WeakReference<Player>(value); }
        }
        public ulong PlayerPosition {
            get => (ulong)(Player?.SeekPosition ?? 0);
            set { Player?.Apply((player)=>player.SeekPosition = value); }
        }


        //private IDisposable EndEventRegester { get; set; }
        //private IDisposable DurationRegester { get; set; }

        private string FormatDuration(ulong duration) {
            var t = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }


        public PlayerViewModel() {
            TrimStart = PlayList.Current.Select((c) => c?.TrimStart??0).ToReadOnlyReactivePropertySlim();
            TrimEnd = PlayList.Current.Select((c) => c?.TrimEnd ?? 0).ToReadOnlyReactivePropertySlim();
            PlayRange = PlayList.Current.Select((c) => new PlayRange(c?.TrimStart??0, c?.TrimEnd??0)).ToReadOnlyReactivePropertySlim();

            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            TrimStartText = TrimStart.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            TrimEndText = TrimEnd.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();

            IsPlaying = State.Select((v) => v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();
            IsReady = State.Select((v) => v == PlayerState.READY || v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();
        }
    }
}
