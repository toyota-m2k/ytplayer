using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using ytplayer.data;
using ytplayer.download;
using ytplayer.intelop;
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
    public class PlayerViewModel : ViewModelBase, IStorageConsumer, IChapterEditorViewModelConnector {
        const double DEF_NORMAL_PANEL_WIDTH = 360;
        const double DEF_EDITING_PANEL_WIDTH = 440;
        const double DEF_EDITING_PANEL_HEIGHT = 350;

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

        #region Trimming/Chapters Reactive Properties

        public ReactiveProperty<PlayRange> Trimming { get; } = new ReactiveProperty<PlayRange>(PlayRange.Empty);
        public ReadOnlyReactivePropertySlim<bool> HasTrimming { get; }
        public ReactiveProperty<ChapterList> Chapters { get; } = new ReactiveProperty<ChapterList>((ChapterList)null, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReactiveProperty<List<PlayRange>> DisabledRanges { get; } = new ReactiveProperty<List<PlayRange>>((List<PlayRange>)null, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReadOnlyReactivePropertySlim<bool> HasDisabledRange { get; }
        public ReactiveProperty<bool> ChapterEditing { get; } = new ReactiveProperty<bool>(false);
        public ReadOnlyReactivePropertySlim<ObservableCollection<ChapterInfo>> EditingChapterList { get; }

        public ReactivePropertySlim<ChapterEditor> ChapterEditor { get; } = new ReactivePropertySlim<ChapterEditor>(null);
        //public ReadOnlyReactivePropertySlim<ObservableCollection<ChapterInfo>> EditingChapterCollection { get; }

        public Subject<string> ReachRangeEnd { get; } = new Subject<string>();

        public ReactiveCommand<ulong> NotifyPosition { get; } = new ReactiveCommand<ulong>();
        public ReactiveCommand<PlayRange> NotifyRange { get; } = new ReactiveCommand<PlayRange>();
        public ReactivePropertySlim<PlayRange?> DraggingRange { get; } = new ReactivePropertySlim<PlayRange?>(null);

        #endregion

        #region Editing Chapters
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
        #endregion

        #region Trimming

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

        #endregion

        #region Add Chapters

        private void AddChapter() {
            AddChapter(PlayerPosition);
        }

        private void AddChapter(ulong pos) {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var chapterEditor = ChapterEditor.Value;
            if (chapterEditor == null) return;
            if (pos > Duration.Value) return;
            chapterEditor.AddChapter(new ChapterInfo(pos));
        }

        private void AddDisabledChapterRange(PlayRange range) {
            if (PlayList.Current.Value == null) return;
            var chapterEditor = ChapterEditor.Value;
            if (chapterEditor == null) return;

            range.AdjustTrueEnd(Duration.Value);
            var del = chapterEditor.Chapters.Value.Values.Where(c => range.Start <= c.Position && c.Position <= range.End).ToList();
            chapterEditor.EditInGroup((gr) => {
                foreach (var e in del) {    // chapterList.Valuesは ObservableCollection なので、RemoveAll的なやつ使えない。
                    gr.RemoveChapter(e);
                }
                gr.AddChapter(new ChapterInfo(range.Start) { Skip = true });
                if (range.End != Duration.Value) {
                    gr.AddChapter(new ChapterInfo(range.End));
                }
            });
        }

        #endregion

        #region Edit Chapters by Expanding Chapter

        private void ExpandChapterToLeft() {
            ExpandChapter(false);
        }

        public void ExpandChapterToRight() {
            ExpandChapter(true);
        }


        public void ExpandChapter(bool toRight) {
            if (!ChapterEditing.Value) return;
            var chapterEditor = ChapterEditor.Value;
            if (chapterEditor == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;

            var hitIndex = chapterList.GetNeighborChapterIndexEx(PlayerPosition, out var prevIndex, out var nextIndex);
            ChapterInfo removingChapter = null, prevChapter = null;
            if (hitIndex >= 0) {
                // Hit
                removingChapter = chapterList[hitIndex];
                if (!toRight) {
                    prevChapter = chapterList[prevIndex];
                }
            }
            else {
                if (toRight) {
                    removingChapter = chapterList[nextIndex];
                }
                else {
                    removingChapter = chapterList[prevIndex];
                    prevChapter = chapterList[prevIndex - 1];
                }
            }
            chapterEditor.EditInGroup((gr) => {
                if (prevChapter != null) {
                    gr.SetSkip(prevChapter, removingChapter.Skip);
                }
                gr.RemoveChapter(removingChapter);
            });
        }

        #endregion

        #region Seek / Jump by Chapter

        public ReactiveCommand<int> SelectChapterEvent { get; } = new ReactiveCommand<int>();

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
                    if (ChapterEditing.Value) {
                        SelectChapterEvent.Execute(prev);
                    }
                    return;
                }
            }
            Position.Value = Trimming.Value.Start;
        }


        private void NextChapter() {
            var chapterList = Chapters.Value;
            chapterList.GetNeighborChapterIndex(PlayerPosition, out var prev, out var next);
            if(next>=0) {
                var c = chapterList.Values[next].Position;
                if(Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    if (ChapterEditing.Value) {
                        SelectChapterEvent.Execute(next);
                    }
                    return;
                }
            }
            GoForwardCommand.Execute();
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

        #region Undo/Redo

        private void Undo(bool redo) {
            var chapterEditor = ChapterEditor.Value;
            if (chapterEditor == null) return;
            if (redo) {
                chapterEditor.Do();
            }
            else {
                chapterEditor.Undo();
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

        public ReactiveCommand DisableCurrentChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ExpandLeftCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ExpandRightCommand { get; } = new ReactiveCommand();
        public ReactiveCommand UndoCommand { get; } = new ReactiveCommand();
        public ReactiveCommand RedoCommand { get; } = new ReactiveCommand();

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

        private common.DisposablePool mDisposablePool = new common.DisposablePool();


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
            EditingChapterList = Chapters.Select((c)=> c?.Values).ToReadOnlyReactivePropertySlim();
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
                SeekRelative(KeyState.IsKeyDown(KeyState.VK_CONTROL) ? -1000 : -100);
            });
            SmallSeekForwardCommand.Subscribe(() => {
                SeekRelative(KeyState.IsKeyDown(KeyState.VK_CONTROL) ? 1000 : 100);
            });
            DisableCurrentChapterCommand.Subscribe(() => {
                if (ChapterEditing.Value) {
                    var chapterEditor = ChapterEditor.Value;
                    if (chapterEditor == null) return;
                    chapterEditor.EditInGroup((gr) => {
                        var chapter = chapterEditor.Chapters.Value.GetChapterAtPosition(PlayerPosition);
                        if (chapter == null) {
                            var head = chapterEditor.Chapters.Value[0];
                            if (head == null || head.Position > 0) {
                                if (gr.AddChapter(new ChapterInfo(0))) {
                                    chapter = chapterEditor.Chapters.Value[0];
                                }
                                else {
                                    if (head == null) {
                                        return;
                                    }
                                    chapter = head;
                                }
                            }
                        }
                        gr.SetSkip(chapter, !chapter.Skip);
                    });
                }
            });

            UndoCommand.Subscribe(() => Undo(redo:false));
            RedoCommand.Subscribe(() => Undo(redo:true));
            ExpandLeftCommand.Subscribe(ExpandChapterToLeft);
            ExpandRightCommand.Subscribe(ExpandChapterToRight);

            TrashCommand.Subscribe(PlayList.DeleteCurrent);
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);

            SetTrimCommand.Subscribe(SetTrimming);
            ResetTrimCommand.Subscribe(ResetTrimming);

            AddChapterCommand.Subscribe(AddChapter);
            PrevChapterCommand.Subscribe(PrevChapter);
            NextChapterCommand.Subscribe(NextChapter);

            ChapterEditing.Subscribe((editing) => {
                if(editing) {
                    var item = PlayList.Current.Value;
                    if (item == null) return;
                    ChapterEditor.Value = new ChapterEditor(item, this);
                    PanelVertAlign.Value = VerticalAlignment.Stretch;
                    PanelHorzAlign.Value = HorizontalAlignment.Right;
                    PanelWidth.Value = DEF_EDITING_PANEL_WIDTH;
                    mDisposablePool.Add(EditingChapterList.Subscribe((_) => UpdateChapterLengthField()));
                    mDisposablePool.Add(Duration.Subscribe((_) => UpdateChapterLengthField()));
                    EditingChapterList.Value.CollectionChanged += OnChapterListChanged;
                }
                else {
                    mDisposablePool.Reset();
                    if (EditingChapterList.Value != null) { EditingChapterList.Value.CollectionChanged -= OnChapterListChanged; }
                    ChapterEditor.Value = null;
                    PanelWidth.Value = DEF_NORMAL_PANEL_WIDTH;
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
                    case HorizontalAlignment.Right: // --> Stretchに変更
                        PanelWidth.Value = Double.NaN;
                        PanelHorzAlign.Value = HorizontalAlignment.Stretch;
                        PanelVertAlign.Value = VerticalAlignment.Bottom;
                        break;
                    case HorizontalAlignment.Stretch:   // --> Leftに変更
                        PanelWidth.Value = DEF_EDITING_PANEL_WIDTH;
                        PanelHorzAlign.Value = HorizontalAlignment.Left;
                        PanelVertAlign.Value = VerticalAlignment.Stretch;
                        break;
                    case HorizontalAlignment.Left:  // --> Rightに戻る
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

        private void OnChapterListChanged(object sender, NotifyCollectionChangedEventArgs e) {
            UpdateChapterLengthField();
        }

        public override void Dispose() {
            StorageSupplier?.UnbindConsumer(this);
            mDisposablePool.Dispose();
            base.Dispose();
        }

        public void UpdateChapterLengthField() {
            if (!ChapterEditing.Value) return;
            var duration = Duration.Value;
            UpdateLengthField(duration);
        }

        private void UpdateLengthField(ulong duration) {
            if (duration == 0) return;
            var editingList = EditingChapterList.Value;
            if (editingList == null) return;

            if (editingList.Count == 0) return;
            for (var i = 0; i < editingList.Count - 1; i++) {
                var c0 = editingList[i];
                var c1 = editingList[i + 1];
                c0.Length = c1.Position - c0.Position;
                c0.Index = i + 1;
            }
            var c = editingList[editingList.Count - 1];
            c.Length = duration - c.Position;
            c.Index = editingList.Count;
        }


        #endregion
    }
}
