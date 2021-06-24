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
            get { return mIsModified || Values.Where((c) => c.IsModified).Any(); }
            private set { mIsModified = value; }
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
            if (GetNeighbourChapterIndex(pos, out var prev, out var next)) {
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
            int prev, next;
            if(GetNeighbourChapterIndex(chapter.Position, out prev, out next)) {
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
            int prev, next;
            if (!GetNeighbourChapterIndex(chapter.Position, out prev, out next)) {
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
        public bool GetNeighbourChapterIndex(ulong current, out int prev, out int next) {
            prev = next = -1;
            int count = Values.Count, s = 0, e = count - 1, m;
            if (e < 0) {
                return false;
            }

            if (Values[e].Position < current) {
                prev = e;
                return false;
            }

            do {
                m = (s + e) / 2;
                ulong v = Values[m].Position;
                if (v == current) {
                    prev = m - 1;
                    if (m < count - 1) {
                        next = m + 1;
                    }
                    return true;
                } else if (v < current) {
                    s = m + 1;
                } else { // current < markers[m]
                    e = m;
                }
            } while (s < e);
            next = s;
            prev = s - 1;
            return false;
        }

        private IEnumerable<PlayRange> GetDisabledChapterRanges() {
            bool skip = false;
            ulong skipStart = 0;

            foreach (var c in Values) {
                if (c.Skip) {
                    if (!skip) {
                        skip = true;
                        skipStart = c.Position;
                    }
                } else {
                    if (skip) {
                        skip = false;
                        yield return new PlayRange(skipStart, c.Position);
                    }
                }
            }
            if(skip) {
                yield return new PlayRange(skipStart, 0);
            }
        }

        public IEnumerable<PlayRange> GetDisabledRanges(PlayRange trimming) {
            var trimStart = trimming.Start;
            var trimEnd = trimming.End;
            foreach(var r in GetDisabledChapterRanges()) {
                if(r.End < trimming.Start) {
                    // ignore
                    continue;
                } else if(trimStart>0) {
                    if(r.Start<trimStart) {
                        yield return new PlayRange(0, r.End);
                        continue;
                    } else {
                        yield return new PlayRange(0, trimStart);
                    }
                    trimStart = 0;
                }

                if(trimEnd>0) {
                    if (trimEnd < r.Start) {
                        break;
                    } else if(trimEnd < r.End) {
                        trimEnd = 0;
                        yield return new PlayRange(r.Start, 0);
                        break;
                    }
                }
                yield return r;
            }
            if(trimStart>0) {
                yield return new PlayRange(0, trimStart);
            }
            if(trimEnd>0) {
                yield return new PlayRange(trimEnd, 0);
            }
        }

    }
}
