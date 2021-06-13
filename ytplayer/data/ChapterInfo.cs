using System;
using System.Collections.Generic;
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

    public struct ChapterSpan {
        public ulong Start;
        public ulong End;
        public ChapterSpan(ulong start, ulong end) {
            Start = start;
            End = end;
        }
    }

    public class ChapterList : SortedList<ulong, ChapterInfo> {
        const long MIN_CHAPTER_SPAN = 1000;
        public string Owner { get; }
        public bool IsModified { get; private set; } = false;
        public ChapterList(string owner, IEnumerable<ChapterInfo> src) {
            Owner = owner;
            foreach (var c in src) {
                Add(c.Position, c);
            }
        }

        public bool CanAddChapter(ulong pos) {
            long prev, next;
            if (GetNeighbourChapterIndex(pos, out prev, out next)) {
                return false;
            }
            if (prev >= 0 && Math.Abs(prev - (long)pos) < MIN_CHAPTER_SPAN) {
                return false;
            }
            if (next >= 0 && Math.Abs(next - (long)pos) < MIN_CHAPTER_SPAN) {
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
            * @param prev (out) currentの前のマーカー（なければ-1）
            * @param next (out) currentの次のマーカー（なければ-1）
            * @return true: currentがマーカー位置にヒット（prevとnextにひとつ前/後のindexがセットされる）
            *         false: ヒットしていない
            */
        private bool GetNeighbourChapterIndex(ulong current, out long prev, out long next) {
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

        public IEnumerable<ChapterSpan> GetDisabledSpans(ulong duration) {
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
                        yield return new ChapterSpan(skipStart, c.Position);
                    }
                }
            }
            if(skip) {
                yield return new ChapterSpan(skipStart, duration);
            }
        }
    }
}
