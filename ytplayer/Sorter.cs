using System;
using System.Collections.Generic;
using System.Linq;
using ytplayer.data;

namespace ytplayer {
    public enum SortKey { 
        STATUS,
        MEDIA,
        NAME,
        MARK,
        RATING,
        CATEGORY,
        DATE,
        DURATION,
        DESC,
    }

    public enum SortOrder {
        ASCENDING = 1,
        DESCENDING = -1,
    }

    public class Sorter : IComparer<DLEntry> {
        public event Action SortUpdated;
        private SortKey mPrevKey = SortKey.DATE;
        private SortKey mKey = SortKey.DATE;

        public Sorter() {
        }

        public SortKey Key {
            get => mKey;
            set {
                if (mKey != value) {
                    mPrevKey = mKey;
                    mKey = value;
                    SortUpdated?.Invoke();
                }
            }
        }

        private SortOrder mOrder = SortOrder.ASCENDING;
        public SortOrder Order { 
            get => mOrder;
            set {
                if(mOrder!=value) {
                    mOrder = value;
                    SortUpdated?.Invoke();
                }
            }
        }

        public int Compare(DLEntry x, DLEntry y) {
            int r = Compare(x, y, Key);
            if(r==0 && Key!=mPrevKey) {
                r = Compare(x, y, mPrevKey);
            }
            if(r==0) {
                r = Compare(x, y, SortKey.DATE);
            }
            return r * (int)Order;
        }


        private int Compare(DLEntry x, DLEntry y, SortKey key) {
            switch (Key) {
                case SortKey.CATEGORY:
                    return x.Category.SortIndex - y.Category.SortIndex;
                case SortKey.DESC:
                    return string.Compare(x.Desc, y.Desc);
                case SortKey.DURATION:
                    return (int)(x.DurationInSec - y.DurationInSec);
                case SortKey.MARK:
                    return (int)x.Mark - (int)y.Mark;
                case SortKey.MEDIA:
                    return (int)x.Media - (int)y.Media;
                case SortKey.NAME:
                    return string.Compare(x.Name, y.Name);
                case SortKey.RATING:
                    return (int)x.Rating - (int)y.Rating;
                case SortKey.STATUS:
                    return (int)x.Status - (int)y.Status;
                case SortKey.DATE:
                default:
                    return DateTime.Compare(x.Date, y.Date);
            }
        }

        public IOrderedEnumerable<DLEntry> Sort(IEnumerable<DLEntry> list) {
            return list.OrderBy((e) => e, this);
        }

        public void SetSortKey(string v) {
            var k = SortKeyFromString(v);
            if(k != Key) {
                Key = k;
            } else {
                Order = (SortOrder)((int)Order * -1);
            }
        }
        public static SortKey SortKeyFromString(string v) {
            return (SortKey)Enum.Parse(typeof(SortKey), v, true);
        }
    }
}
