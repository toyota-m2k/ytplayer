using common;
using System;
using System.Data.Linq.Mapping;
using System.Text.RegularExpressions;
using ytplayer.common;
using ytplayer.player;

namespace ytplayer.data {
    public enum Status {
        INITIAL = 0,        // 初期状態（作成直後～DBに登録された状態）
        WAITING,            // ダウンロード処理待ち（キューに積まれた状態）
        DOWNLOADING,        // ダウンロード中
        COMPLETED,          // ダウンロード成功
        FAILED,             // ダウンロード失敗
        CANCELLED,          // ダウンロードがキャンセルされた
        BLOCKED,            // 削除済み：再びダウンロードされないようブロックされている
    }
    public enum Rating {
        DREADFUL = 1,
        BAD = 2,
        NORMAL = 3,
        GOOD = 4,
        EXCELLENT = 5,
    }
    [Flags]
    public enum MediaFlag {
        NONE = 0,
        AUDIO = 1,
        VIDEO = 2,
        BOTH = 3,
    }

    public static class FlagExt {
        public static bool HasAudio(this MediaFlag flag) {
            return (flag & MediaFlag.AUDIO) == MediaFlag.AUDIO;
        }
        public static bool HasVideo(this MediaFlag flag) {
            return (flag & MediaFlag.VIDEO) == MediaFlag.VIDEO;
        }
        public static MediaFlag PlusAudio(this MediaFlag flag) {
            return flag | MediaFlag.AUDIO;
        }
        public static MediaFlag MinusAudio(this MediaFlag flag) {
            return flag & ~MediaFlag.AUDIO;
        }
        public static MediaFlag PlusVideo(this MediaFlag flag) {
            return flag | MediaFlag.VIDEO;
        }
        public static MediaFlag MinusVideo(this MediaFlag flag) {
            return flag & ~MediaFlag.VIDEO;
        }
    }

    [Table(Name = "t_download")]
    public class DLEntry : MicPropertyChangeNotifier, IPlayable, IEntry  {
        [Column(Name = "url", IsPrimaryKey = true, CanBeNull = false)]
        public string KEY { get; private set; }
        public string Url => KEY;

        [Column(Name = "name", CanBeNull = true)]
        private string name;
        public string Name {
            get => name;
            set => setProp(callerName(), ref name, value, "NameToDisplay");
        }
        static readonly private Regex RegName = new Regex(@"(?<name>.*)(?:-[a-z0-9]+)$", RegexOptions.IgnoreCase);
        public string NameToDisplay {
            get {
                if (string.IsNullOrEmpty(name)) {
                    return $"({Url})";
                } else {
                    return name;
                }
            }
        }

        [Column(Name = "vpath", CanBeNull = true)]
        private string vpath;
        public string VPath {
            get => vpath;
            set => setProp(callerName(), ref vpath, value);
        }
        [Column(Name = "apath", CanBeNull = true)]
        private string apath;
        public string APath {
            get => apath;
            set => setProp(callerName(), ref apath, value);
        }

        [Column(Name = "status", CanBeNull = true)]
        private int status;
        public Status Status {
            get => (Status)status;
            set => setProp(callerName(), ref status, (int)value);
        }

        [Column(Name = "rating", CanBeNull = true)]
        private int rating;
        public Rating Rating {
            get => (Rating)rating;
            set => setProp(callerName(), ref rating, (int)value);
        }

        [Column(Name = "category", CanBeNull = true)]
        private string category;
        public Category Category {
            get => Settings.Instance.Categories.Get(category);
            set => setProp(callerName(), ref category, CategoryList.CategoryToDBLabel(value));
        }

        [Column(Name = "media", CanBeNull = true)]
        private int media;
        public MediaFlag Media {
            get => (MediaFlag)media;
            set => setProp(callerName(), ref media, (int)value);
        }
        [Column(Name = "date", CanBeNull = true)]
        private long date;
        public DateTime Date {
            get => AsTime(date);
            set => setProp(callerName(), ref date, value.ToFileTimeUtc());
        }

        [Column(Name = "volume", CanBeNull = true)]
        private long volume;
        const double VOL_RANGE = 200.0;
        const double VOL_TOLERANCE = 0.01;
        private long dbVolume(double v) {
            if(0.5-VOL_TOLERANCE<v && v < 0.5 + VOL_TOLERANCE) {
                return 0;
            }
            long r = (long)Math.Round((v - 0.5) * VOL_RANGE);
            return Math.Max(Math.Min(0, r), 100);   // 0<=r<=100
        }
        public double Volume {
            get => ((double)volume)/VOL_RANGE + 0.5;
            set => setProp(callerName(), ref volume, dbVolume(value));
        }

        [Column(Name = "desc", CanBeNull = true)]
        private string desc;
        public string Desc {
            get => desc;
            set => setProp(callerName(), ref desc, value);
        }

        public string Path => string.IsNullOrEmpty(VPath) ? APath : VPath;
        public bool HasFile => (int)Media > 0;

        private int progress;
        public int Progress {
            get => progress;
            set => setProp(callerName(), ref progress, value);
        }


        public void Delete() {
            Status = Status.BLOCKED;
        }
        
        public DLEntry() {
            KEY = "";
            name = "";
            vpath = "";
            apath = "";
            status = (int)Status.INITIAL;
            rating = (int)Rating.NORMAL;
            category = "";
        }

        public static DLEntry Create(string url_) {
            return new DLEntry() { KEY = url_, Date=DateTime.UtcNow };
        }

        private static readonly DateTime EpochDate = new DateTime(1970, 1, 1, 0, 0, 0);
        public static DateTime AsTime(object obj) {
            try {
                if (obj != null && obj != DBNull.Value) {
                    return DateTime.FromFileTimeUtc(Convert.ToInt64(obj));
                }
            } catch(Exception e) {
                Logger.error(e);
            }
            return EpochDate;
        }
    }
}
