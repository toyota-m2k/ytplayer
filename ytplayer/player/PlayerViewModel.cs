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
using ytplayer.download;

namespace ytplayer.player {
    public enum PlayerState {
        UNAVAILABLE,
        LOADING,
        READY,
        PLAYING,
        ENDED,
        ERROR,
    }
    public class PlayerViewModel : ViewModelBase, IStorageConsumer {
        // Item Entry
        public ReactivePropertySlim<ulong> Duration { get; } = new ReactivePropertySlim<ulong>(1000);
        public ReactivePropertySlim<ulong> Position { get; } = new ReactivePropertySlim<ulong>(0);
        public ReactivePropertySlim<PlayerState> State { get; } = new ReactivePropertySlim<PlayerState>(PlayerState.UNAVAILABLE);

        public ReadOnlyReactivePropertySlim<bool> IsReady { get; }
        public ReadOnlyReactivePropertySlim<bool> IsPlaying { get; }

        public ReactivePropertySlim<double> Speed { get; } = new ReactivePropertySlim<double>(0.5);
        public ReactivePropertySlim<double> Volume { get; } = new ReactivePropertySlim<double>(0.5);

        // Trimming/Chapters
        //public ReadOnlyReactivePropertySlim<ulong> TrimStart { get; }
        //public ReadOnlyReactivePropertySlim<ulong> TrimEnd { get; }
        public ReactivePropertySlim<PlayRange> Trimming { get; } = new ReactivePropertySlim<PlayRange>(null);
        public ReactivePropertySlim<ChapterList> Chapters { get; } = new ReactivePropertySlim<ChapterList>(null);
        public ReactivePropertySlim<List<PlayRange>> DisabledRanges { get; } = new ReactivePropertySlim<List<PlayRange>>(null);
        public void SaveChapterListIfNeeds() {
            var storage = StorageSupplier?.Storage;
            if (null == storage) return;
            var item = PlayList.Current.Value;
            if (item == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null || !chapterList.IsModified) return;
            storage.ChapterTable.UpdateByChapterList(chapterList);
        }
        public void PrepareChapterListForCurrentItem() {
            //Chapters.Value = null;
            //DisabledRanges.Value = null;
            //Trimming.Value = null;
            var storage = StorageSupplier?.Storage;
            if (null == storage) return;
            var item = PlayList.Current.Value;
            if (item == null) return;
            Trimming.Value = new PlayRange(item.TrimStart, item.TrimEnd);
            var chapterList = storage.ChapterTable.GetChapterList(item.KEY);
            if(chapterList!=null) {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            }
        }

        public Subject<bool> StorageClosed { get; } = new Subject<bool>();
        public void OnClosingStorage(Storage storage) {
            SaveChapterListIfNeeds();
            mStorageSupplier = null;
            StorageClosed.OnNext(true);
        }


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
        public ReactivePropertySlim<bool> FitMode { get; } = new ReactivePropertySlim<bool>();
        public ReactivePropertySlim<bool> ShowPanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ShowSizePanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> Fullscreen { get; } = new ReactivePropertySlim<bool>(false);
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

        private WeakReference<IStorageSupplier> mStorageSupplier;
        private IStorageSupplier StorageSupplier => mStorageSupplier?.GetValue();


        public PlayerViewModel(IStorageSupplier storageSupplier) {
            mStorageSupplier = new WeakReference<IStorageSupplier>(storageSupplier);
            storageSupplier.BindConsumer(this);

            //TrimStart = PlayList.Current.Select((c) => c?.TrimStart??0).ToReadOnlyReactivePropertySlim();
            //TrimEnd = PlayList.Current.Select((c) => c?.TrimEnd ?? 0).ToReadOnlyReactivePropertySlim();
            //Trimming = PlayList.Current.Select((c) => new PlayRange(c?.TrimStart??0, c?.TrimEnd??0)).ToReadOnlyReactivePropertySlim();

            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            TrimStartText = Trimming.Select((v) => FormatDuration(v?.Start??0)).ToReadOnlyReactivePropertySlim();
            TrimEndText = Trimming.Select((v) => FormatDuration(v?.End??0)).ToReadOnlyReactivePropertySlim();

            IsPlaying = State.Select((v) => v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();
            IsReady = State.Select((v) => v == PlayerState.READY || v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();

            GoForwardCommand.Subscribe(PlayList.Next);
            GoBackCommand.Subscribe(PlayList.Prev);
            TrashCommand.Subscribe(PlayList.DeleteCurrent);
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);
        }

        public override void Dispose() {
            StorageSupplier?.UnbindConsumer(this);
            base.Dispose();
        }
    }
}
