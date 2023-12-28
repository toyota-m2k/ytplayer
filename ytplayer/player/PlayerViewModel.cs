using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using ytplayer.data;
using ytplayer.download;
using ytplayer.wav;

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
        const double DEF_NORMAL_PANEL_WIDTH = 320;
        const double DEF_EDITING_PANEL_WIDTH = 440;

        #region Control Panel Position
        public ReactivePropertySlim<HorizontalAlignment> PanelHorzAlign { get; } = new ReactivePropertySlim<HorizontalAlignment>(HorizontalAlignment.Right);
        public ReactivePropertySlim<VerticalAlignment> PanelVertAlign { get; } = new ReactivePropertySlim<VerticalAlignment>(VerticalAlignment.Bottom);
        public ReactivePropertySlim<double> PanelWidth { get; } = new ReactivePropertySlim<double>(DEF_NORMAL_PANEL_WIDTH);
        #endregion

        #region Properties of Item Entry

        public ReactivePropertySlim<ulong> Duration { get; } = new ReactivePropertySlim<ulong>(1000);
        public ReactivePropertySlim<ulong> Position { get; } = new ReactivePropertySlim<ulong>(0);
        public ReactivePropertySlim<PlayerState> State { get; } = new ReactivePropertySlim<PlayerState>(PlayerState.UNAVAILABLE);

        public ReadOnlyReactivePropertySlim<bool> IsReady { get; }
        public ReadOnlyReactivePropertySlim<bool> IsPlaying { get; }

        public ReactivePropertySlim<double> Speed { get; } = new ReactivePropertySlim<double>(0.5);
        public ReactivePropertySlim<double> Volume { get; } = new ReactivePropertySlim<double>(0.5);

        #endregion

        #region Trimming/Chapters
        public ReactivePropertySlim<PlayRange> Trimming { get; } = new ReactivePropertySlim<PlayRange>(PlayRange.Empty);
        public ReadOnlyReactivePropertySlim<bool> HasTrimming { get; }
        public ReactivePropertySlim<ChapterList> Chapters { get; } = new ReactivePropertySlim<ChapterList>(null,ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReactivePropertySlim<List<PlayRange>> DisabledRanges { get; } = new ReactivePropertySlim<List<PlayRange>>(null);
        public ReadOnlyReactivePropertySlim<bool> HasDisabledRange { get; }
        public ReactivePropertySlim<bool> ChapterEditing { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<ObservableCollection<ChapterInfo>> EditingChapterList { get; } = new ReactivePropertySlim<ObservableCollection<ChapterInfo>>();
        public Subject<string> ReachRangeEnd { get; } = new Subject<string>();

        public ReactiveCommand<ulong> NotifyPosition { get; } = new ReactiveCommand<ulong>();
        public ReactiveCommand<PlayRange> NotifyRange { get; } = new ReactiveCommand<PlayRange>();
        public ReactivePropertySlim<PlayRange?> DraggingRange { get; } = new ReactivePropertySlim<PlayRange?>(null);
        /**
         * 現在再生中の動画のチャプター設定が変更されていればDBに保存する。
         */
        public void SaveChapterListIfNeeds() {
            WavFile?.Dispose();
            WavFile = null;
            var storage = StorageSupplier?.Storage;
            if (null == storage) return;
            var item = PlayList.Current.Value;
            if (item == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null || !chapterList.IsModified) return;
            storage.ChapterTable.UpdateByChapterList(chapterList);
        }

        /**
         * 再生する動画のチャプターリスト、トリミング情報、無効化範囲リストを準備する。
         */
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

        public void SetTrimmingStartAtCurrentPos() {
            SetTrimming(SetTrimmingStart);
        }
        public void SetTrimmingEndAtCurrentPos() {
            SetTrimming(SetTrimmingEnd);
        }

        public void ResetTrimmingStart() {
            ResetTrimming(SetTrimmingStart);
        }
        //public void ResetTrimmingStart(IPlayItem item) {
        //    SetTrimmingStart(item, 0);
        //}
        public void ResetTrimmingEnd() {
            ResetTrimming(SetTrimmingEnd);
        }

        private void SetTrimming(object obj) {
            switch (obj as String) {
                case "Start":
                    SetTrimmingStartAtCurrentPos(); 
                    break;
                case "End":
                    SetTrimmingEndAtCurrentPos();
                    break;
                default:
                    return;
            }
        }
        private void ResetTrimming(object obj) {
            switch (obj as String) {
                case "Start":
                    ResetTrimmingStart(); 
                    break;
                case "End":
                    ResetTrimmingEnd(); 
                    break;
                default: 
                    return;
            }
        }

        private void ResetTrimming(Func<DLEntry, ulong, PlayRange?> setFunc) {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var trimming = setFunc(item, 0);
            if (trimming == null) return;

            Trimming.Value = trimming.Value;
            DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming.Value).ToList();
        }

        private void SetTrimming(Func<DLEntry, ulong, PlayRange?> setFunc) {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var pos = PlayerPosition;
            var trimming = setFunc(item, pos);
            if (trimming == null) return;

            Trimming.Value = trimming.Value;
            DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming.Value).ToList();
        }
        private PlayRange? SetTrimmingStart(DLEntry item, ulong pos) {
            var trimming = Trimming.Value;
            if(trimming.TrySetStart(pos)) {
                item.TrimStart = pos;
                return trimming;
            }
            return null;
        }
        private PlayRange? SetTrimmingEnd(DLEntry item, ulong pos) {
            var trimming = Trimming.Value;
            if (trimming.TrySetEnd(pos)) {
                item.TrimEnd = pos;
                return trimming;
            }
            return null;
        }

        public void NotifyChapterUpdated() {
            Chapters.Value.Apply((chapterList) => {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            });
        }

        private void AddChapter() {
            AddChapter(PlayerPosition);
        }

        private void AddChapter(ulong pos) {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;
            if (pos > Duration.Value) return;
            if (chapterList.AddChapter(new ChapterInfo(pos))) {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            }
        }

        private void AddDisabledChapterRange(PlayRange range) {
            if (PlayList.Current.Value == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;

            range.AdjustTrueEnd(Duration.Value);
            var del = chapterList.Values.Where(c => range.Start <= c.Position && c.Position <= range.End).ToList();
            foreach (var e in del) {    // chapterList.Valuesは ObservableCollection なので、RemoveAll的なやつ使えない。
                chapterList.Values.Remove(e);
            }
            chapterList.AddChapter(new ChapterInfo(range.Start) { Skip = true });
            if (range.End != Duration.Value) {
                chapterList.AddChapter(new ChapterInfo(range.End));
            }
            Chapters.Value = chapterList;
            DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        }

        private void NextChapter() {
            var chapterList = Chapters.Value;
            chapterList.GetNeighborChapterIndex(PlayerPosition, out var prev, out var next);
            if(next>=0) {
                var c = chapterList.Values[next].Position;
                if(Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    return;
                }
            }
            GoForwardCommand.Execute();
        }

        private void PrevChapter() {
            var chapterList = Chapters.Value;
            var basePosition = PlayerPosition;
            if (basePosition > 1000)
                basePosition -= 1000;
            chapterList.GetNeighborChapterIndex(basePosition, out var prev, out var next);
            if (prev >= 0) {
                var c = chapterList.Values[prev].Position;
                if (Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    return;
                }
            }
            Position.Value = Trimming.Value.Start;
        }

        public void SeekRelative(long delta) {
            var pos = (ulong)Math.Min((long)Duration.Value, Math.Max(0, (long)Position.Value + delta));
            Position.Value = pos;
        }

        public void SetRating(Rating rating) {
            var item = PlayList.Current.Value;
            if (item != null) {
                item.Rating = rating;
            }
        }

        #endregion

        #region Auto Chapter

        public WavFile WavFile { get; set; } = null;
        public ReactiveCommand AutoChapterCommand { get; } = new ReactiveCommand();
        public ReactivePropertySlim<uint> AutoChapterThreshold { get; } = new ReactivePropertySlim<uint>(500);
        public ReactivePropertySlim<uint> AutoChapterSpan { get; } = new ReactivePropertySlim<uint>(1000);

        #endregion

        #region Linkage to Storage Source

        private WeakReference<IStorageSupplier> mStorageSupplier;
        private IStorageSupplier StorageSupplier => mStorageSupplier?.GetValue();

        public Subject<bool> StorageClosed { get; } = new Subject<bool>();

        void IStorageConsumer.OnClosingStorage(Storage storage) {
            SaveChapterListIfNeeds();
            mStorageSupplier = null;
            StorageClosed.OnNext(true);
        }

        #endregion

        #region Display Text

        public ReadOnlyReactivePropertySlim<string> TrimStartText { get; }
        public ReadOnlyReactivePropertySlim<string> TrimEndText { get; }
        public ReadOnlyReactivePropertySlim<string> DurationText { get; }
        public ReadOnlyReactivePropertySlim<string> PositionText { get; }

        static public string FormatDuration(ulong duration) {
            var t = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        #endregion

        #region Commands

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
        //public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SmallSeekBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SmallSeekForwardCommand { get; } = new ReactiveCommand();

        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetSpeedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetVolumeCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AddChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PrevChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand NextChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SyncChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ExportCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PanelPositionCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrimmingToChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClosePlayerCommand { get; } = new ReactiveCommand();
        public ReactiveCommand HelpCommand { get; } = new ReactiveCommand();
        #endregion

        #region Window Managements

        // Window
        public ReactivePropertySlim<bool> FitMode { get; } = new ReactivePropertySlim<bool>();
        public ReactivePropertySlim<bool> ShowPanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ShowSizePanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> Fullscreen { get; } = new ReactivePropertySlim<bool>(false);
        public ReactiveCommand MaximizeCommand { get; } = new ReactiveCommand();
        public PlayerCommand KeyCommands { get; }

        #endregion

        #region PlayList

        public PlayList PlayList { get; } = new PlayList();
        public bool AutoPlay { get; } = true;
        public ObservableCollection<Category> Categories => new ObservableCollection<Category>(Settings.Instance.Categories.SelectList);

        #endregion

        #region Reference to Player
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

        #endregion

        #region Construction/Destruction

        public PlayerViewModel(IStorageSupplier storageSupplier) {
            mStorageSupplier = new WeakReference<IStorageSupplier>(storageSupplier);
            storageSupplier.BindConsumer(this);

            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            TrimStartText = Trimming.Select((v) => FormatDuration(v.Start)).ToReadOnlyReactivePropertySlim();
            TrimEndText = Trimming.Select((v) => FormatDuration(v.End)).ToReadOnlyReactivePropertySlim();
            HasDisabledRange = DisabledRanges.Select((c) => c != null && c.Count > 0).ToReadOnlyReactivePropertySlim();
            HasTrimming = Trimming.Select(c => c.Start > 0 || c.End > 0).ToReadOnlyReactivePropertySlim();
            IsPlaying = State.Select((v) => v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();
            IsReady = State.Select((v) => v == PlayerState.READY || v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();

            GoForwardCommand.Subscribe(() => {
                if (!ChapterEditing.Value) {
                    PlayList.Next();
                }
            });
            GoBackCommand.Subscribe(() => {
                if (!ChapterEditing.Value) {
                    PlayList.Prev();
                }
            });
            SmallSeekBackCommand.Subscribe(() => {
                if (!ChapterEditing.Value) {
                    SeekRelative(-100);
                }
            });
            SmallSeekForwardCommand.Subscribe(() => {
                if (!ChapterEditing.Value) {
                    SeekRelative(100);
                }
            });
            TrashCommand.Subscribe(PlayList.DeleteCurrent);
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);

            SetTrimCommand.Subscribe(SetTrimming);
            ResetTrimCommand.Subscribe(ResetTrimming);

            AddChapterCommand.Subscribe(AddChapter);
            PrevChapterCommand.Subscribe(PrevChapter);
            NextChapterCommand.Subscribe(NextChapter);

            ChapterEditing.Subscribe((c) => {
                if(c) {
                    EditingChapterList.Value = Chapters.Value.Values;
                    PanelVertAlign.Value = VerticalAlignment.Stretch;
                    PanelHorzAlign.Value = HorizontalAlignment.Right;
                    PanelWidth.Value = DEF_NORMAL_PANEL_WIDTH;
                } else {
                    EditingChapterList.Value = null;
                    PanelWidth.Value = DEF_EDITING_PANEL_WIDTH;
                    PanelHorzAlign.Value = HorizontalAlignment.Right;
                    PanelVertAlign.Value = VerticalAlignment.Bottom;
                    SaveChapterListIfNeeds();
                }
            });

            NotifyRange.Subscribe(AddDisabledChapterRange);
            NotifyPosition.Subscribe(AddChapter);


            string prevId = null;
            ReachRangeEnd.Subscribe((prev) => {
                if(prevId == prev) {
                    LoggerEx.error("Next more than twice.");
                }
                if (PlayList.HasNext.Value) {
                    GoForwardCommand.Execute();
                } else {
                    PauseCommand.Execute();
                }
            });

            PanelPositionCommand.Subscribe(() => {
                switch(PanelHorzAlign.Value) {
                    default:
                    case HorizontalAlignment.Right:
                        PanelWidth.Value = double.NaN;
                        PanelHorzAlign.Value = HorizontalAlignment.Stretch;
                        PanelVertAlign.Value = VerticalAlignment.Bottom;
                        break;
                    case HorizontalAlignment.Stretch:
                        PanelWidth.Value = DEF_EDITING_PANEL_WIDTH;
                        PanelHorzAlign.Value = HorizontalAlignment.Left;
                        PanelVertAlign.Value = VerticalAlignment.Stretch;
                        break;
                    case HorizontalAlignment.Left:
                        PanelWidth.Value = DEF_EDITING_PANEL_WIDTH;
                        PanelHorzAlign.Value = HorizontalAlignment.Right;
                        PanelVertAlign.Value = VerticalAlignment.Stretch;
                        break;
                }
            });
            TrimmingToChapterCommand.Subscribe(() => {
                var item = PlayList.Current.Value;
                if (item == null) return;
                if (item.TrimStart > 0) {
                    AddDisabledChapterRange(new PlayRange(0, item.TrimStart));
                    ResetTrimming(SetTrimmingStart);
                }
                if (item.TrimEnd>0) {
                    AddDisabledChapterRange(new PlayRange(item.TrimEnd, 0));
                    ResetTrimming(SetTrimmingEnd);
                }
            });
            KeyCommands = new PlayerCommand(this);
        }

        public override void Dispose() {
            StorageSupplier?.UnbindConsumer(this);
            base.Dispose();
        }

        #endregion
    }
}
