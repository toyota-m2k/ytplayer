using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;

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
    public enum Mark {
        MARK_NONE = 0,  
        MARK_STAR = 1,      // "M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"
        MARK_FLAG = 2,      // "M14.4,6L14,4H5V21H7V14H12.6L13,16H20V6H14.4Z"
        MARK_HEART = 3,     // "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"
        //MARK_SPADE,     // "M12,2C9,7 4,9 4,14C4,16 6,18 8,18C9,18 10,18 11,17C11,17 11.32,19 9,22H15C13,19 13,17 13,17C14,18 15,18 16,18C18,18 20,16 20,14C20,9 15,7 12,2Z"
        //MARK_DIAMOND,   // "M19,12L12,22L5,12L12,2"
        //MARK_CLUB,      // ""M12,2C14.3,2 16.3,4 16.3,6.2C16.21,8.77 14.34,9.83 14.04,10C15.04,9.5 16.5,9.5 16.5,9.5C19,9.5 21,11.3 21,13.8C21,16.3 19,18 16.5,18C16.5,18 15,18 13,17C13,17 12.7,19 15,22H9C11.3,19 11,17 11,17C9,18 7.5,18 7.5,18C5,18 3,16.3 3,13.8C3,11.3 5,9.5 7.5,9.5C7.5,9.5 8.96,9.5 9.96,10C9.66,9.83 7.79,8.77 7.7,6.2C7.7,4 9.7,2 12,2Z""
        MARK_LAST,
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

    //[Table(Name = "t_download")]
    //public class DLEntryOld : MicPropertyChangeNotifier, IPlayable {
    //    [Column(Name = "url", IsPrimaryKey = true, CanBeNull = false)]
    //    public string KEY { get; private set; }
    //    public string Url => KEY;

    //    [Column(Name = "name", CanBeNull = true)]
    //    private string name;
    //    public string Name {
    //        get => name;
    //        set => setProp(callerName(), ref name, value, "NameToDisplay");
    //    }
    //    static readonly private Regex RegName = new Regex(@"(?<name>.*)(?:-[a-z0-9]+)$", RegexOptions.IgnoreCase);
    //    public string NameToDisplay {
    //        get {
    //            if (string.IsNullOrEmpty(name)) {
    //                return $"({Url})";
    //            } else {
    //                return name;
    //            }
    //        }
    //    }

    //    [Column(Name = "vpath", CanBeNull = true)]
    //    private string vpath;
    //    public string VPath {
    //        get => vpath;
    //        set => setProp(callerName(), ref vpath, value);
    //    }
    //    [Column(Name = "apath", CanBeNull = true)]
    //    private string apath;
    //    public string APath {
    //        get => apath;
    //        set => setProp(callerName(), ref apath, value);
    //    }

    //    [Column(Name = "status", CanBeNull = true)]
    //    private int status;
    //    public Status Status {
    //        get => (Status)status;
    //        set => setProp(callerName(), ref status, (int)value);
    //    }

    //    [Column(Name = "rating", CanBeNull = true)]
    //    private int rating;
    //    public Rating Rating {
    //        get => (Rating)rating;
    //        set => setProp(callerName(), ref rating, (int)value);
    //    }

    //    [Column(Name = "category", CanBeNull = true)]
    //    private string category;
    //    public Category Category {
    //        get => Settings.Instance.Categories.Get(category);
    //        set => setProp(callerName(), ref category, CategoryList.CategoryToDBLabel(value));
    //    }

    //    [Column(Name = "media", CanBeNull = true)]
    //    private int media;
    //    public MediaFlag Media {
    //        get => (MediaFlag)media;
    //        set => setProp(callerName(), ref media, (int)value);
    //    }
    //    [Column(Name = "date", CanBeNull = true)]
    //    private long date;
    //    public DateTime Date {
    //        get => AsTime(date);
    //        set => setProp(callerName(), ref date, value.ToFileTimeUtc());
    //    }

    //    [Column(Name = "volume", CanBeNull = true)]
    //    private long volume;
    //    const double VOL_RANGE = 200.0;
    //    const double VOL_TOLERANCE = 0.01;
    //    private long dbVolume(double v) {
    //        if(0.5-VOL_TOLERANCE<v && v < 0.5 + VOL_TOLERANCE) {
    //            return 0;
    //        }
    //        long r = (long)Math.Round((v - 0.5) * VOL_RANGE);
    //        return Math.Max(Math.Min(0, r), 100);   // 0<=r<=100
    //    }
    //    public double Volume {
    //        get => ((double)volume)/VOL_RANGE + 0.5;
    //        set => setProp(callerName(), ref volume, dbVolume(value));
    //    }

    //    [Column(Name = "desc", CanBeNull = true)]
    //    private string desc;
    //    public string Desc {
    //        get => desc ?? "";
    //        set => setProp(callerName(), ref desc, value);
    //    }

    //    [Column(Name = "duration", CanBeNull = true)]
    //    private ulong? duration;
    //    public ulong DurationInSec {
    //        get => duration ?? 0;
    //        set => setProp(callerName(), ref duration, value, "DurationText");
    //    }

    //    private string FormatDuration(ulong durationInSec) {
    //        var t = TimeSpan.FromSeconds(durationInSec);
    //        return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
    //    }

    //    public string DurationText => DurationInSec > 0 ? FormatDuration(DurationInSec) : "";

    //    [Column(Name = "mark", CanBeNull = true)]
    //    private int? mark;
    //    public Mark Mark {
    //        get => (Mark)(mark ?? 0);
    //        set => setProp(callerName(), ref mark, (int)value);
    //    }

    //    public string Path => string.IsNullOrEmpty(VPath) ? APath : VPath;
    //    public bool HasFile => (int)Media > 0;

    //    private int progress;
    //    public int Progress {
    //        get => progress;
    //        set => setProp(callerName(), ref progress, value);
    //    }

    //    public void Delete() {
    //        Status = Status.BLOCKED;
    //    }
        
    //    public DLEntryOld() {
    //        KEY = "";
    //        name = "";
    //        vpath = "";
    //        apath = "";
    //        status = (int)Status.INITIAL;
    //        rating = (int)Rating.NORMAL;
    //        category = "";
    //    }

    //    public static DLEntryOld Create(string url_) {
    //        return new DLEntryOld() { KEY = url_, Date=DateTime.UtcNow };
    //    }

    //    private static readonly DateTime EpochDate = new DateTime(1970, 1, 1, 0, 0, 0);
    //    public static DateTime AsTime(object obj) {
    //        try {
    //            if (obj != null && obj != DBNull.Value) {
    //                return DateTime.FromFileTimeUtc(Convert.ToInt64(obj));
    //            }
    //        } catch(Exception e) {
    //            Logger.error(e);
    //        }
    //        return EpochDate;
    //    }
    //}

    public class DLEntryTable : StorageTable<DLEntry> {
        public DLEntryTable(SQLiteConnection connection) : base(connection) { }
        public bool Contains(string key) {
            return Table.Any(c => c.KEY == key);
        }
        public override bool Contains(DLEntry entry) {
            return Contains(entry.KEY);
        }
        public DLEntry Find(string key) {
            //var f = from x in Table where x.KEY == key select x;
            //return f.FirstOrDefault();

            // return Table.Where((c) => c.KEY == key).FirstOrDefault();
            // 驚いたことに、FirstOrDefault()だとエラーになる。Primary Key (or Unique) の一致で検索するから、
            // 複数のエントリが見つかる可能性がないため、FirstOrDefault()は使えないようだ。
            return Table.SingleOrDefault(c => c.KEY == key);
        }
        // public long LastUpdated { get; }
    }


    [Table(Name = "t_download_ex")]
    public class DLEntry : PropertyChangeNotifier {
        [Column(Name = "id", IsPrimaryKey = true, CanBeNull = false)]
        public string KEY { get; private set; }
        public string Id => KEY;

        [Column(Name = "url", CanBeNull = false)]
        public string url { get; private set; }
        public string Url => url;

        [Column(Name = "name", CanBeNull = true)]
        private string name;
        public string Name {
            get => name;
            set => setProp(callerName(), ref name, value, "NameToDisplay");
        }
        // private static readonly Regex RegName = new Regex(@"(?<name>.*)(?:-[a-z0-9]+)$", RegexOptions.IgnoreCase);
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
            set {
                setProp(callerName(), ref status, (int)value);
                if (value == Status.COMPLETED) {
                    Storage.LastUpdated = DateTime.UtcNow.ToFileTimeUtc();
                }
            }
        }

        [Column(Name = "rating", CanBeNull = true)]
        private int rating;
        public Rating Rating {
            get => (Rating)rating;
            set => setProp(callerName(), ref rating, (int)value);
        }

        [Column(Name = "category", CanBeNull = true)]
        private string mCategory;
        public Category Category {
            get => Settings.Instance.Categories.Get(mCategory);
            set => setProp(callerName(), ref mCategory, CategoryList.CategoryToDbLabel(value));
        }

        [Column(Name = "media", CanBeNull = true)]
        private int mMedia;
        public MediaFlag Media {
            get => (MediaFlag)mMedia;
            set => setProp(callerName(), ref mMedia, (int)value);
        }
        [Column(Name = "date", CanBeNull = true)]
        private long mDate;
        public DateTime Date {
            get => AsTime(mDate);
            set => setProp(callerName(), ref mDate, value.ToFileTimeUtc());
        }
        public long LongDate => mDate;

        [Column(Name = "volume", CanBeNull = true)]
        private long volume;
        const double VOL_RANGE = 200.0;
        const double VOL_TOLERANCE = 0.01;
        private long dbVolume(double v) {
            if (0.5 - VOL_TOLERANCE < v && v < 0.5 + VOL_TOLERANCE) {
                return 0;
            }
            long r = (long)Math.Round((v - 0.5) * VOL_RANGE);
            return Math.Max(Math.Min(0, r), 100);   // 0<=r<=100
        }
        public double Volume {
            get => volume / VOL_RANGE + 0.5;
            set => setProp(callerName(), ref volume, dbVolume(value));
        }

        [Column(Name = "desc", CanBeNull = true)]
        private string desc;
        public string Desc {
            get => desc ?? "";
            set => setProp(callerName(), ref desc, value);
        }

        [Column(Name = "duration", CanBeNull = true)]
        private ulong? duration;
        public ulong DurationInSec {
            get => duration ?? 0;
            set => setProp(callerName(), ref duration, value, "DurationText");
        }

        private string FormatDuration(ulong durationInSec) {
            var t = TimeSpan.FromSeconds(durationInSec);
            return $"{t.Hours}:{t.Minutes:00}:{t.Seconds:00}";
        }

        public string DurationText => DurationInSec > 0 ? FormatDuration(DurationInSec) : "";

        [Column(Name = "mark", CanBeNull = true)]
        private int? mark;
        public Mark Mark {
            get => (Mark)(mark ?? 0);
            set => setProp(callerName(), ref mark, (int)value);
        }

        [Column(Name = "trim_start", CanBeNull = true)]
        private ulong? trim_start;
        public ulong TrimStart {
            get => trim_start ?? 0;
            set => setProp(callerName(), ref trim_start, value);
        }
        [Column(Name = "trim_end", CanBeNull = true)]
        private ulong? trim_end;
        public ulong TrimEnd {
            get => trim_end ?? 0;
            set => setProp(callerName(), ref trim_end, value);
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
            url = "";
            name = "";
            vpath = "";
            apath = "";
            status = (int)Status.INITIAL;
            rating = (int)Rating.NORMAL;
            mCategory = "";
        }

        public static DLEntry Create(string id, string url) {
            return new DLEntry() { KEY=id, url = url, Date = DateTime.UtcNow };
        }

        private static readonly DateTime epochDate = new DateTime(1970, 1, 1, 0, 0, 0);

        public static DateTime AsTime(object obj) {
            try {
                if (obj != null && obj != DBNull.Value) {
                    return DateTime.FromFileTimeUtc(Convert.ToInt64(obj));
                }
            }
            catch (Exception e) {
                Logger.error(e);
            }

            return epochDate;
        }
    }

    //public class DLEntryOldTable : StorageTable<DLEntryOld> {
    //    public DLEntryOldTable(SQLiteConnection connection) : base(connection) { }
    //    public override bool Contains(string key) {
    //        return Table.Where((c) => c.KEY == key).Any();
    //    }
    //    public override bool Contains(DLEntryOld entry) {
    //        return Contains(entry.KEY);
    //    }
    //    public DLEntryOld Find(string key) {
    //        //var f = from x in Table where x.KEY == key select x;
    //        //return f.FirstOrDefault();

    //        // return Table.Where((c) => c.KEY == key).FirstOrDefault();
    //        // 驚いたことに、FirstOrDefault()だとエラーになる。Primary Key (or Unique) の一致で検索するから、
    //        // 複数のエントリが見つかる可能性がないため、FirstOrDefault()は使えないようだ。
    //        return Table.Where((c) => c.KEY == key).SingleOrDefault();
    //    }
    //}
}
