using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ytplayer.data {
    /**
     * Undo/Redo可能な編集操作素片のi/f定義
     */
    public interface IChapterEditUnit {
        bool Do();
        bool Undo();
    }

    /**
     * ChapterEditorを弱参照で保持するための共通実装クラス
     */
    abstract class OwnerdUnitBase {
        private WeakReference<ChapterEditor> mOwner;
        protected virtual ChapterEditor Owner => mOwner?.GetValue();
        public OwnerdUnitBase(ChapterEditor owner) {
            mOwner = new WeakReference<ChapterEditor>(owner);
        }
    }

    /**
     * Undo/Redo用の履歴管理用のi/f定義
     */
    public interface IChapterEditHistory {
        bool AddChapter(ChapterInfo chapter, ulong seekOnUndo = AddRemoveRec.NO_SEEK_ON_UNDO);
        bool RemoveChapter(ChapterInfo chapter, ulong seekOnUndo = AddRemoveRec.NO_SEEK_ON_UNDO);
        //void OnLabelChanged(ChapterInfo chapter, string prev, string current);
        void OnSkipChanged(ChapterInfo chapter, bool current);
        bool SetSkip(ChapterInfo chapter, bool skip);
        bool SetTrimmingStart(ulong pos);
        bool SetTrimmingEnd(ulong pos);
        bool ResetTrimmingStart();
        bool ResetTrimmingEnd();
        bool EditInGroup(Action<IChapterEditHistory> fn);
    }

    /**
     * IChapterEditHistoryの共通実装（CompoundRec, ChapterEditorの共通基底クラス）
     */
    public abstract class AbsChapterEditHistory : ViewModelBase<ChapterEditor>, IChapterEditHistory {
        protected List<IChapterEditUnit> Records = new List<IChapterEditUnit>(100);
        public AbsChapterEditHistory(ChapterEditor owner) : base(owner) {

        }

        public int CountOfRecords => Records.Count;

        private bool Apply(ChapterInfo chapter, bool add, ulong seekOnUndo) {
            var owner = Owner;
            if (owner == null) return false;
            var rec = new AddRemoveRec(owner, chapter, add, seekOnUndo);
            if (rec.Do()) {
                Records.Add(rec);
                owner?.NotifyChapterUpdated();
                return true;
            }
            return false;
        }

        public virtual bool AddChapter(ChapterInfo chapter, ulong seekOnUndo) {
            return Apply(chapter, true, seekOnUndo);
        }

        public virtual bool RemoveChapter(ChapterInfo chapter, ulong seekOnUndo) {
            return Apply(chapter, false, seekOnUndo);
        }

        //public virtual void OnLabelChanged(ChapterInfo chapter, string prev, string current) {
        //    var owner = Owner;
        //    if (null == owner) return;
        //    var rec = new LabelRec(owner, chapter.Position, prev, current);
        //    Records.Add(rec);
        //}

        /**
         * Bindingにより、ChapterInfo.Skipが変更されたときに呼び出されて、編集履歴を更新するためのAPI
         */
        public virtual void OnSkipChanged(ChapterInfo chapter, bool current) {
            var owner = Owner;
            if (null == owner) return;
            var rec = new SkipRec(owner, chapter.Position, skip: current);
            Records.Add(rec);
        }

        /**
         * プログラム的にSkipをセットする場合のAPI
         */
        public virtual bool SetSkip(ChapterInfo chapter, bool skip) {
            var owner = Owner;
            if (null == owner) return false;
            var rec = new SkipRec(owner, chapter.Position, skip);
            if (rec.Do()) {
                Records.Add(rec);
                owner?.NotifyDisableRangeChanged();
                return true;
            }
            return false;
        }

        public virtual bool SetTrimmingStart(ulong pos) {
            var owner = Owner;
            if (null == owner) return false;
            var rec = new TrimmingRec(owner, pos, end: false);
            if (rec.Do()) {
                Records.Add(rec);
                return true;
            }
            return false;
        }
        public virtual bool SetTrimmingEnd(ulong pos) {
            var owner = Owner;
            if (null == owner) return false;
            var rec = new TrimmingRec(owner, pos, end: true);
            if (rec.Do()) {
                Records.Add(rec);
                return true;
            }
            return false;
        }
        public virtual bool ResetTrimmingStart() {
            return SetTrimmingStart(0);
        }
        public virtual bool ResetTrimmingEnd() {
            return SetTrimmingEnd(0);
        }

        public virtual bool EditInGroup(Action<IChapterEditHistory> fn) {
            var owner = Owner;
            if (null == owner) return false;
            var cr = new CompoundRec(Owner);
            fn(cr);
            if (cr.CountOfRecords > 0) {
                Records.Add(cr);
                return true;
            }
            return false;
        }
    }

    /**
     * Chapterの追加/削除操作用IChapterEditUnit実装クラス
     */
    class AddRemoveRec : OwnerdUnitBase, IChapterEditUnit {
        public const ulong NO_SEEK_ON_UNDO = ulong.MaxValue;
        bool Add;
        ChapterInfo Target;
        ChapterList ChapterList => Owner?.ChapterList;
        ulong SeekOnUndo = ulong.MaxValue;
        public AddRemoveRec(ChapterEditor owner, ChapterInfo chapter, bool add, ulong seekOnUndo):base(owner) {
            Add = add;
            Target = chapter;
            SeekOnUndo = seekOnUndo;
        }

        private bool Apply(bool add) {
            if (add) {
                if(!(ChapterList?.AddChapter(Target) ?? false)) {
                    return false;
                }
            }
            else {
                if (!(ChapterList?.RemoveChapter(Target) ?? false)) {
                    return false;
                }
            }
            Owner?.NotifyChapterUpdated();
            return true;
        }

        public bool Do() {
            return Apply(Add);
        }

        public bool Undo() {
            if(Apply(!Add)) {
                if(SeekOnUndo!=NO_SEEK_ON_UNDO) {
                    Owner?.RequestSeek?.Execute(SeekOnUndo);
                }
                return true;
            }
            return false;
        }
    }

    /**
     * TrimmingStart/Endの設定/設定解除操作用IChapterEditUnit実装クラス
     */
    class TrimmingRec   : OwnerdUnitBase, IChapterEditUnit {
        private bool End;
        private ulong Position;
        private ulong PreviousPosition;

        public TrimmingRec(ChapterEditor owner, ulong pos, bool end):base(owner) {
            Position = pos;
            PreviousPosition = end ? owner.Trimming.Value.End : owner.Trimming.Value.Start;
            End = end;
        }

        private bool Apply(bool undo) {
            var owner = Owner;
            if (owner == null) return false;
            var item = owner.CurrentItem;
            if (item == null) return false;

            var trimming = owner.Trimming.Value;
            ulong pos = undo ? PreviousPosition : Position;
            
            if (!End) {
                if (!trimming.TrySetStart(pos)) {
                    return false;
                }
                item.TrimStart = pos;
            }
            else {
                if (!trimming.TrySetEnd(pos)) {
                    return false;
                }
                item.TrimEnd = pos;
            }
            owner.Trimming.Value = trimming;
            owner.NotifyDisableRangeChanged();
            return true;
        }

        public bool Do() {
            return Apply(undo:false);
        }

        public bool Undo() {
            return Apply(undo:true);
        }
    }

    /**
     * Chapterのプロパティ(Skip/Label)設定操作共通のIChapterEditUnit実装クラス
     */
    abstract class PropRec<T> : OwnerdUnitBase, IChapterEditUnit {
        T Prev;
        T Next;

        ulong TargetPosition;
        public PropRec(ChapterEditor owner, ulong targetPosition, T prev, T next):base(owner) {
            TargetPosition = targetPosition;
            Prev = prev;
            Next = next;
        }
        protected abstract void SetProp(ChapterInfo chapter, T prop);

        protected ChapterInfo ChapterAt(ulong position) {
            return Owner?.ChapterList?.GetChapterAt(position);
        }

        public bool Do() {
            var chapter = ChapterAt(TargetPosition);
            if (null == chapter) return false;
            SetProp(chapter, Next);
            return true;
        }

        public bool Undo() {
            var chapter = ChapterAt(TargetPosition);
            if (null == chapter) return false;
            SetProp(chapter, Prev);
            return true;
        }
    }

    /**
     * Chapterのラベル設定操作用のIChapterEditUnit実装クラス
     */
    //class LabelRec : PropRec<string> {
    //    public LabelRec(ChapterEditor owner, ulong targetPosition, string prev, string next) : base(owner, targetPosition, prev, next) {
    //    }

    //    protected override void SetProp(ChapterInfo chapter, string prop) {
    //        chapter.SetLabel(prop, notifyToEditor: false);
    //    }
    //}

    /**
     * ChapterのSkip属性設定操作用のIChapterEditUnit実装クラス
     */
    class SkipRec : PropRec<bool> {
        public SkipRec(ChapterEditor owner, ulong targetPosition, bool skip) : base(owner, targetPosition, prev:!skip, next:skip) {
        }

        protected override void SetProp(ChapterInfo chapter, bool prop) {
            chapter.Skip = prop;
            //if (chapter.SetSkip(prop, notifyToEditor: false)) {
            //    Owner?.NotifyDisableRangeChanged();
            //}
        }
    }

    /**
     * 複数の操作をまとめて１つのUndo/Redoとして扱うためのIChapterEditUnit実装クラス
     */
    class CompoundRec : AbsChapterEditHistory, IChapterEditUnit {
        public CompoundRec(ChapterEditor owner):base(owner) { }

        public bool Do() {
            foreach (var rec in Records) {
                if (!rec.Do()) {
                    return false;
                }
            }
            return true;
        }
        public bool Undo() {
            foreach (var rec in ((IEnumerable<IChapterEditUnit>)Records).Reverse()) {
                if (!rec.Undo()) {
                    return false;
                }
            }
            return true;
        }
    }

    public interface IChapterEditorViewModelConnector {
        ReactiveProperty<PlayRange> Trimming { get; } 
        ReactiveProperty<ChapterList> Chapters { get; }
        ReactiveProperty<List<PlayRange>> DisabledRanges { get; }
        //ReactiveProperty<ObservableCollection<ChapterInfo>> EditingChapterList { get; }
    }

    /**
     * Chapter編集用公開APIクラス
     */
    public class ChapterEditor : AbsChapterEditHistory, IChapterEditUnit {
        #region Reactive Properties
        private IChapterEditorViewModelConnector Connector;

        public ReactiveProperty<PlayRange> Trimming => Connector.Trimming;
        public ReactiveProperty<ChapterList> Chapters => Connector.Chapters;
        public ReactiveProperty<List<PlayRange>> DisabledRanges => Connector.DisabledRanges;
        //public ReactiveProperty<ObservableCollection<ChapterInfo>> EditingChapterList => Connector.EditingChapterList;

        #endregion

        #region Non-Reactive Properties

        public ChapterList ChapterList => Chapters.Value;
        public DLEntry CurrentItem { get; private set; }

        #endregion

        #region Initialization / Media/Storage Connection

        /**
         * コンストラクタ
         */
        public ChapterEditor(DLEntry item, IChapterEditorViewModelConnector connector) : base(null) {
            Owner = this;
            SetTarget(item, connector);
        }

        /**
         * Playerに動画ファイルがロードされた時に呼び出す
         */
        private void SetTarget(DLEntry item, IChapterEditorViewModelConnector connector) {
            Connector = connector;
            CurrentItem = item;
            if (item == null) {
                Reset();
                return;
            }
            NotifyDisableRangeChanged();
        }

        ///**
        // * Chapterが変更されていれば保存する。
        // */
        //public void SaveChapterListIfNeeds() {
        //    var storage = App.Instance.DB;
        //    if (null == storage) return;
        //    var item = CurrentItem;
        //    if (item == null) return;
        //    var chapterList = Chapters.Value;
        //    if (chapterList == null || !chapterList.IsModified) return;
        //    storage.ChapterTable.UpdateByChapterList(this, chapterList);
        //}

        #endregion

        #region Notify updating chapters

        private bool SuppressNotification = false;
        private bool ChapterListChanged = false;
        private bool DisableRangeChanged = false;

        /**
         * Chapterリストが変更されたときにChaptersとDisabledRangesを更新する
         */
        public void NotifyChapterUpdated() {
            if(SuppressNotification) {
                ChapterListChanged = true;
                return;
            }
            var chapterList = Chapters.Value;
            Chapters.Value = chapterList;       //セットしなおすことで更新する
            DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        }
        /**
         * Chapterリストは変更されていないが、Skip/Trimmingなどの変更によりDisableRangeが変化したときに更新を行う。
         */
        public void NotifyDisableRangeChanged() {
            if (SuppressNotification) {
                DisableRangeChanged = true;
                return;
            }
            DisabledRanges.Value = Chapters.Value?.GetDisabledRanges(Trimming.Value)?.ToList();
        }

        public ReactiveCommand<ulong> RequestSeek = new ReactiveCommand<ulong>();

        #endregion

        #region Edit Operation

        public override bool AddChapter(ChapterInfo chapter, ulong seekOnUndo=AddRemoveRec.NO_SEEK_ON_UNDO) {
            Prepare();
            return base.AddChapter(chapter, seekOnUndo);
        }

        public override bool RemoveChapter(ChapterInfo chapter, ulong seekOnUndo = AddRemoveRec.NO_SEEK_ON_UNDO) {
            Prepare();
            return base.RemoveChapter(chapter, seekOnUndo);
        }

        //public override void OnLabelChanged(ChapterInfo chapter, string prev, string current) {
        //    Prepare();
        //    base.OnLabelChanged(chapter, prev, current);
        //}

        public override void OnSkipChanged(ChapterInfo chapter, bool current) {
            Prepare();
            base.OnSkipChanged(chapter, current);
        }

        public override bool SetSkip(ChapterInfo chapter, bool skip) {
            Prepare();
            return base.SetSkip(chapter, skip);
        }

        //--------------------------------------------
        public override bool SetTrimmingStart(ulong pos) {
            Prepare();
            return base.SetTrimmingStart(pos);
        }

        public override bool SetTrimmingEnd(ulong pos) {
            Prepare();
            return base.SetTrimmingEnd(pos);
        }

        //--------------------------------------------
        public override bool ResetTrimmingStart() {
            Prepare();
            return base.ResetTrimmingStart();
        }
        public override bool ResetTrimmingEnd() {
            Prepare();
            return base.ResetTrimmingEnd();
        }
        //--------------------------------------------

        /**
         * Chapterリスト、DisabledRangeリストの更新通知を遅延するためのブロックを提供する
         */
        public bool InSupressNotificationBlock(Func<bool> fn) {
            SuppressNotification = true;
            ChapterListChanged = false;
            DisableRangeChanged = false;

            try {
                return fn();
            } finally {
                SuppressNotification = false;
                if (ChapterListChanged) {
                    NotifyChapterUpdated();
                }
                else if (DisableRangeChanged) {
                    NotifyDisableRangeChanged();
                }
                ChapterListChanged = false;
                DisableRangeChanged = false;
            }
        }

        /**
         * 履歴のグループ化
         */
        public override bool EditInGroup(Action<IChapterEditHistory> fn) {
            return InSupressNotificationBlock(() => {
                var cr = new CompoundRec(Owner);
                fn(cr);
                if (cr.CountOfRecords > 0) {
                    Prepare();
                    Records.Add(cr);
                    return true;
                }
                return false;
            });
        }

        #endregion

        #region Compound Operation

        public bool AddDisabledChapterRange(ulong duration, PlayRange range, IChapterEditHistory parentGroup=null) {
            if (CurrentItem == null) return false;
            var chapterList = Chapters.Value;
            if (chapterList == null) return false;

            range.AdjustTrueEnd(duration);
            var del = chapterList.Values.Where(c => range.Start <= c.Position && c.Position <= range.End).ToList();
            var start = new ChapterInfo(range.Start);
            if(parentGroup==null) {
                parentGroup = this;
            }
            parentGroup.EditInGroup((gr) => {
                foreach (var e in del) {    // chapterList.Valuesは ObservableCollection なので、RemoveAll的なやつ使えない。
                    gr.RemoveChapter(e);
                }
                gr.AddChapter(start);
            });
            parentGroup.EditInGroup((gr) => {
                if (range.End != duration) {
                    ulong seekOnUndo = range.End > range.Start ? range.Start : AddRemoveRec.NO_SEEK_ON_UNDO;
                    gr.AddChapter(new ChapterInfo(range.End), seekOnUndo);
                }
                gr.SetSkip(start, true);
            });
            return true;
        }

        public bool ClearAllChapters() {
            var all = Chapters.Value?.Values?.ToList();
            if (Utils.IsNullOrEmpty(all)) return false;
            return EditInGroup((gr) => {
                foreach (var e in all) {
                    gr.RemoveChapter(e);
                }
            });
        }

        public bool TrimmingToChapter(ulong duration) {
            var item = CurrentItem;
            if (item == null) return false;

            return EditInGroup(gr => {
                if (item.TrimStart > 0) {
                    AddDisabledChapterRange(duration, new PlayRange(0, item.TrimStart), gr);
                    gr.ResetTrimmingStart();
                }
                if (item.TrimEnd > 0) {
                    AddDisabledChapterRange(duration, new PlayRange(item.TrimEnd, 0), gr);
                    gr.ResetTrimmingEnd();
                }
            });
        }

        #endregion

        #region Undo/Redo

        private int UndoPosition = -1;

        private void ClearUndoBuffer() {
            UndoPosition = -1;
            Records.Clear();
        }

        private void Prepare() {
            if (UndoPosition >= 0) {
                Records.RemoveRange(UndoPosition, Records.Count - UndoPosition);
                UndoPosition = -1;
            }
        }

        public void Reset() {
            ClearUndoBuffer();
            //SaveChapterListIfNeeds();
            //EditingChapterList.Value = null;
            Chapters.Value = null;
            DisabledRanges.Value = null;
            Trimming.Value = PlayRange.Empty;
            SuppressNotification = false;
        }

        public bool Do() {
            return InSupressNotificationBlock(() => {
                if (0 <= UndoPosition && UndoPosition < Records.Count) {
                    if (!Records[UndoPosition].Do()) {
                        ClearUndoBuffer();
                        return false;
                    }
                    UndoPosition++;
                    if (UndoPosition == Records.Count) {
                        UndoPosition = -1;
                    }
                    return true;
                }
                return false;
            });
        }

        public bool Undo() {
            return InSupressNotificationBlock(() => {
                if (UndoPosition < 0) {
                    if (Records.Count <= 0) return false;
                    UndoPosition = Records.Count;
                }
                if (UndoPosition == 0) {
                    return false;
                }
                UndoPosition--;
                if (!Records[UndoPosition].Undo()) {
                    ClearUndoBuffer();
                    return false;
                }
                return true;
            });
        }
        #endregion
    }
}
