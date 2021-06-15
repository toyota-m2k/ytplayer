﻿using io.github.toyota32k.toolkit.utils;
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
        public ReactivePropertySlim<ChapterList> Chapters { get; } = new ReactivePropertySlim<ChapterList>(null,ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReactivePropertySlim<List<PlayRange>> DisabledRanges { get; } = new ReactivePropertySlim<List<PlayRange>>(null);
        public ReadOnlyReactivePropertySlim<bool> HasDisabledRange { get; }
        
        /**
         * 現在再生中の動画のチャプター設定が変更されていればDBに保存する。
         */
        public void SaveChapterListIfNeeds() {
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

        private void SetTrimming(object obj) {
            switch (obj as String) {
                case "Start":   SetTrimming(SetTrimmingStart); break;
                case "End": SetTrimming(SetTrimmingEnd); break;
                default: return;
            }
        }
        private void ResetTrimming(object obj) {
            switch (obj as String) {
                case "Start": ResetTrimming(SetTrimmingStart); break;
                case "End": ResetTrimming(SetTrimmingEnd); break;
                default: return;
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

        private void AddChapter() {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var pos = PlayerPosition;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;
            if(chapterList.AddChapter(new ChapterInfo(pos))) {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            }

        }

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

        private string FormatDuration(ulong duration) {
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
        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetSpeedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetVolumeCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AddChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand EditChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetTrimCommand { get; } = new ReactiveCommand();

        #endregion

        #region Window Managements

        // Window
        public ReactivePropertySlim<bool> FitMode { get; } = new ReactivePropertySlim<bool>();
        public ReactivePropertySlim<bool> ShowPanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ShowSizePanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> Fullscreen { get; } = new ReactivePropertySlim<bool>(false);
        public ReactiveCommand MaximizeCommand { get; } = new ReactiveCommand();

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

            //TrimStart = PlayList.Current.Select((c) => c?.TrimStart??0).ToReadOnlyReactivePropertySlim();
            //TrimEnd = PlayList.Current.Select((c) => c?.TrimEnd ?? 0).ToReadOnlyReactivePropertySlim();
            //Trimming = PlayList.Current.Select((c) => new PlayRange(c?.TrimStart??0, c?.TrimEnd??0)).ToReadOnlyReactivePropertySlim();

            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            TrimStartText = Trimming.Select((v) => FormatDuration(v.Start)).ToReadOnlyReactivePropertySlim();
            TrimEndText = Trimming.Select((v) => FormatDuration(v.End)).ToReadOnlyReactivePropertySlim();
            HasDisabledRange = DisabledRanges.Select((c) => c != null && c.Count > 0).ToReadOnlyReactivePropertySlim();

            IsPlaying = State.Select((v) => v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();
            IsReady = State.Select((v) => v == PlayerState.READY || v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();

            GoForwardCommand.Subscribe(PlayList.Next);
            GoBackCommand.Subscribe(PlayList.Prev);
            TrashCommand.Subscribe(PlayList.DeleteCurrent);
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);

            SetTrimCommand.Subscribe(SetTrimming);
            ResetTrimCommand.Subscribe(ResetTrimming);

            AddChapterCommand.Subscribe(AddChapter);
        }

        public override void Dispose() {
            StorageSupplier?.UnbindConsumer(this);
            base.Dispose();
        }

        #endregion
    }
}
