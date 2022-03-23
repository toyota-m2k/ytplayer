using io.github.toyota32k.toolkit.view;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ytplayer.player;

namespace ytplayer.data {
    public class ChapterInfo : PropertyChangeNotifier {
        private bool mSkip;
        private string mLabel;

        public bool IsModified { get; private set; } = false;
        public void ResetModifiedFlag() { IsModified = false; }

        public ulong Position { get; private set; }

        public string Label {
            get => mLabel;
            set {
                if(setProp(callerName(), ref mLabel, value)) {
                    IsModified = true;
                }
            }
        }

        public bool Skip { 
            get => mSkip;
            set {
                if (setProp(callerName(), ref mSkip, value)) { 
                    IsModified = true; 
                }
            }
        }
        public ChapterInfo(ulong pos, bool skip = false, string label=null) {
            Position = pos;
            mSkip = skip;
            mLabel = label;
        }
        public string PositionText => PlayerViewModel.FormatDuration(Position);

        private ulong mLength = 0;
        public ulong Length {
            get => mLength;
            set => setProp(callerName(), ref mLength, value, "LengthText");
        }
        public string LengthText => PlayerViewModel.FormatDuration(mLength);

        private int mIndex = 0;
        public int Index {
            get => mIndex;
            set => setProp(callerName(), ref mIndex, value);
        }
    }

    //public struct ChapterSpan {
    //    public ulong Start;
    //    public ulong End;
    //    public ChapterSpan(ulong start, ulong end) {
    //        Start = start;
    //        End = end;
    //    }
    //}

    public class ChapterList {
        public ObservableCollection<ChapterInfo> Values { get; } = new ObservableCollection<ChapterInfo>();
        const ulong MIN_CHAPTER_SPAN = 1000;
        public string Owner { get; }

        private bool mIsModified = false;
        public bool IsModified {
            get { return mIsModified || Values.Any(c => c.IsModified); }
            private set => mIsModified = value;
        }
        public ChapterList(string owner, IEnumerable<ChapterInfo> src) {
            Owner = owner;
            foreach (var c in src) {
                AddChapter(c);
            }
        }

        public void ResetModifiedFlag() {
            mIsModified = false;
            foreach(var c in Values) {
                c.ResetModifiedFlag();
            }
        }

        public bool CanAddChapter(ulong pos) {
            if (GetNeighborChapterIndex(pos, out var prev, out var next)) {
                return false;
            }
            return CanAddChapter(pos, prev, next);
        }
        private bool CanAddChapter(ulong pos, int prev, int next) {
            ulong diff(ulong a, ulong b) {
                return a < b ? b - a : a - b;
            }
            if (prev >= 0 && diff(Values[prev].Position, pos) < MIN_CHAPTER_SPAN) {
                return false;
            }
            if (next >= 0 && diff(Values[next].Position, pos) < MIN_CHAPTER_SPAN) {
                return false;
            }
            return true;
        }

        public bool AddChapter(ChapterInfo chapter) {
            if(GetNeighborChapterIndex(chapter.Position, out _, out var next)) {
                return false;
            }
            if (!CanAddChapter(chapter.Position)) {
                return false;
            }
            if(next<0) {
                Values.Add(chapter);
            } else {
                Values.Insert(next, chapter);
            }
            IsModified = true;
            return true;
        }

        public bool RemoveChapter(ChapterInfo chapter) {
            if (!GetNeighborChapterIndex(chapter.Position, out var prev, out _)) {
                return false;
            }
            Values.RemoveAt(prev + 1);
            IsModified = true;
            return true;
        }

        public bool ClearAllChapters() {
            if (Values.Count > 0) {
                Values.Clear();
                IsModified = true;
                return true;
            }
            return false;
        }
        /**
            * 指定位置(current)近傍のChapterを取得
            * 
            * @param prev (out) currentの前のChapter（なければ-1）
            * @param next (out) currentの次のChapter（なければ-1）
            * @return true: currentがマーカー位置にヒット（prevとnextにひとつ前/後のindexがセットされる）
            *         false: ヒットしていない
            */
        public bool GetNeighborChapterIndex(ulong current, out int prev, out int next) {
            int count = Values.Count;
            int clipIndex(int index) {
                return (0 <= index && index < count) ? index : -1;
            }
            for (int i=0; i<count; i++) {
                if(current == Values[i].Position) {
                    prev = i - 1;
                    next = clipIndex(i + 1);
                    return true;
                }
                if(current<Values[i].Position) {
                    prev = i - 1;
                    next = i;
                    return false;
                }
            }
            prev = count - 1;
            next = -1;
            return false;
        }

        // private IEnumerable<PlayRange> GetDisabledChapterRanges() {
        //     bool skip = false;
        //     ulong skipStart = 0;
        //
        //     foreach (var c in Values) {
        //         if (c.Skip) {
        //             if (!skip) {
        //                 skip = true;
        //                 skipStart = c.Position;
        //             }
        //         } else {
        //             if (skip) {
        //                 skip = false;
        //                 yield return new PlayRange(skipStart, c.Position);
        //             }
        //         }
        //     }
        //     if(skip) {
        //         yield return new PlayRange(skipStart);
        //     }
        // }

        public IEnumerable<(ulong,bool)> GetChapterPositionAwareOfTrimming(PlayRange trimming) {
            var trimStart = trimming.Start;
            var trimEnd = trimming.End;
            bool skip = false;

            if (trimming.Start>0) {
                skip = true;
                yield return (0, true);
            }


            foreach (var c in Values) {
                if (trimStart > 0) {
                    if (c.Position <= trimStart) {
                        continue;
                    }
                    else {
                        skip = false;
                        yield return (trimStart, false);
                        trimStart = 0;
                    }
                }
                if(trimEnd!=0 && c.Position>=trimEnd) {
                    break;
                }
                if (c.Skip!=skip) {
                    if (c.Skip) {
                        skip = true;
                        yield return (c.Position, true);
                    }
                    else  { // !c.Skip
                        skip = false;
                        yield return (c.Position, false);
                    }
                }
            }
            if(trimStart>0) {
                skip = false;
                yield return (trimStart, false);
            }
            if (!skip && trimEnd>0) {
                yield return (trimEnd, true);
            }
        }

        public IEnumerable<PlayRange> GetDisabledRanges(PlayRange trimming, ulong duration=0) {
            var chap = GetChapterPositionAwareOfTrimming(trimming);
            ulong prev = 0;
            foreach(var (pos,skip) in chap) {
                if(skip) {
                    prev = pos;
                } else {
                    yield return new PlayRange(prev, pos);
                    prev = 0;
                }
            }
            if(prev>0) {
                yield return new PlayRange(prev, duration);
            }
            //var trimStart = trimming.Start;
            //var trimEnd = trimming.End;

            //var chapters = Values.Where(c => trimStart < c.Position && c.Position < trimEnd);


            //foreach(var r in GetDisabledChapterRanges()) {
            //    r.AdjustTrueEnd(duration);
            //    if(r.End < trimStart) {
            //        // ignore
            //        continue;
            //    } else if(trimStart>0) {
            //        if(r.Start<trimStart) {
            //            yield return new PlayRange(0, r.End);
            //            continue;
            //        } else {
            //            yield return new PlayRange(0, trimStart);
            //        }
            //        trimStart = 0;
            //    }

            //    if(trimEnd>0) {
            //        if (trimEnd < r.Start) {
            //            break;
            //        } else if(trimEnd < r.End) {
            //            trimEnd = 0;
            //            yield return new PlayRange(r.Start, 0);
            //            break;
            //        }
            //    }
            //    yield return r;
            //}
            //if(trimStart>0) {
            //    yield return new PlayRange(0, trimStart);
            //}
            //if(trimEnd>0) {
            //    yield return new PlayRange(trimEnd, 0);
            //}
        }

    }
}
