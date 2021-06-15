using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public struct ChapterInfo {
        public ulong Position;
        public bool Skip;
        public ChapterInfo(ulong pos, bool skip = false) {
            Position = pos;
            Skip = skip;
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

    public class ChapterList : SortedList<ulong, ChapterInfo> {
        const ulong MIN_CHAPTER_SPAN = 1000;
        public string Owner { get; }
        public bool IsModified { get; private set; } = false;
        public ChapterList(string owner, IEnumerable<ChapterInfo> src) {
            Owner = owner;
            foreach (var c in src) {
                Add(c.Position, c);
            }
        }

        public bool CanAddChapter(ulong pos) {
            if (GetNeighbourChapterIndex(pos, out var prev, out var next)) {
                return false;
            }
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
            if (!CanAddChapter(chapter.Position)) {
                return false;
            }
            Add(chapter.Position, chapter);
            IsModified = true;
            return true;
        }

        public bool RemoveChapter(ChapterInfo chapter) {
            if (Remove(chapter.Position)) {
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
            int count = Count, s = 0, e = count - 1, m;
            if (e < 0) {
                return false;
            }

            var markers = Keys;
            if (markers[e] < current) {
                prev = e;
                return false;
            }

            while (s < e) {
                m = (s + e) / 2;
                ulong v = markers[m];
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
            }
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
