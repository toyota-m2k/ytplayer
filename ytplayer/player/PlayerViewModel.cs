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
    public class PlayerViewModel : ViewModelBase {

        // Item Entry
        public ReactiveProperty<ulong> Duration { get; } = new ReactiveProperty<ulong>(1000);
        public ReactiveProperty<ulong> Position { get; } = new ReactiveProperty<ulong>(0);

        public ReactiveProperty<bool> IsReady { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> IsPlaying { get; } = new ReactiveProperty<bool>(false);
        public ReadOnlyReactiveProperty<bool> IsReadyAndPlaying { get; }
        public ReactiveProperty<double> Speed { get; } = new ReactiveProperty<double>(0.5);
        public ReactiveProperty<double> Volume { get; } = new ReactiveProperty<double>(0.5);
        // Trimming
        public ReactiveProperty<ulong> TrimStart { get; } = new ReactiveProperty<ulong>();
        public ReactiveProperty<ulong> TrimEnd { get; } = new ReactiveProperty<ulong>();

        public ReadOnlyReactiveProperty<string> TrimStartText { get; }
        public ReadOnlyReactiveProperty<string> TrimEndText { get; }
        public ReadOnlyReactiveProperty<string> DurationText { get; }
        public ReadOnlyReactiveProperty<string> PositionText { get; }


        public PlayList PlayList { get; } = new PlayList();
        //public Subject<bool> Ended { get; } = new Subject<bool>();
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.SelectList);

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
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

        private WeakReference<Player> mPlayer = null;
        public Player Player {
            get => mPlayer?.GetValue();
            set { mPlayer = value==null ? null : new WeakReference<Player>(value); }
        }
        public ulong PlayerPosition {
            get => (ulong)Player.SeekPosition;
            set { Player.SeekPosition = value; }
        }

        //private IDisposable EndEventRegester { get; set; }
        //private IDisposable DurationRegester { get; set; }

        private string FormatDuration(ulong duration) {
            var t = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        public PlayerViewModel() {
            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            TrimStartText = TrimStart.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            TrimEndText = TrimEnd.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            IsReadyAndPlaying = IsReady.CombineLatest(IsPlaying, (r, p) => r && p).ToReadOnlyReactiveProperty();
        }
    }
}
