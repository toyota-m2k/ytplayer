using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using ytplayer.common;

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

    public class RatingInfo {
        public Rating rating;
        public string label;
        public string svgPath;
        private RatingInfo(Rating rating, string label, string svgPath) {
            this.rating= rating;
            this.label = label;
            this.svgPath = svgPath;
        }

        public static RatingInfo[] List = new RatingInfo[] {
            new RatingInfo(Rating.DREADFUL, "Dreadful", "M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22A10,10 0 0,0 22,12C22,6.47 17.5,2 12,2M6.76,8.82L7.82,7.76L8.88,8.82L9.94,7.76L11,8.82L9.94,9.88L11,10.94L9.94,12L8.88,10.94L7.82,12L6.76,10.94L7.82,9.88L6.76,8.82M6.89,17.5C7.69,15.46 9.67,14 12,14C14.33,14 16.31,15.46 17.11,17.5H6.89M17.24,10.94L16.18,12L15.12,10.94L14.06,12L13,10.94L14.06,9.88L13,8.82L14.06,7.76L15.12,8.82L16.18,7.76L17.24,8.82L16.18,9.88L17.24,10.94Z"),
            new RatingInfo(Rating.BAD, "Bad", "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M7,9.5V8L10,9.5C10,10.3 9.3,11 8.5,11C7.7,11 7,10.3 7,9.5M14.77,17.23C14.32,16.5 13.25,16 12,16C10.75,16 9.68,16.5 9.23,17.23L7.81,15.81C8.71,14.72 10.25,14 12,14C13.75,14 15.29,14.72 16.19,15.81L14.77,17.23M17,9.5C17,10.3 16.3,11 15.5,11C14.7,11 14,10.3 14,9.5L17,8V9.5Z"),
            new RatingInfo(Rating.NORMAL, "Normal", "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M7,9.5C7,8.7 7.7,8 8.5,8C9.3,8 10,8.7 10,9.5C10,10.3 9.3,11 8.5,11C7.7,11 7,10.3 7,9.5M12,17.23C10.25,17.23 8.71,16.5 7.81,15.42L9.23,14C9.68,14.72 10.75,15.23 12,15.23C13.25,15.23 14.32,14.72 14.77,14L16.19,15.42C15.29,16.5 13.75,17.23 12,17.23M15.5,11C14.7,11 14,10.3 14,9.5C14,8.7 14.7,8 15.5,8C16.3,8 17,8.7 17,9.5C17,10.3 16.3,11 15.5,11Z"),
            new RatingInfo(Rating.GOOD, "Good", "M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22A10,10 0 0,0 22,12C22,6.47 17.5,2 12,2M8.88,7.82L11,9.94L9.94,11L8.88,9.94L7.82,11L6.76,9.94L8.88,7.82M12,17.5C9.67,17.5 7.69,16.04 6.89,14H17.11C16.31,16.04 14.33,17.5 12,17.5M16.18,11L15.12,9.94L14.06,11L13,9.94L15.12,7.82L17.24,9.94L16.18,11Z"),
            new RatingInfo(Rating.EXCELLENT, "Exellent", "M18.9,18.94L15.94,16C15.76,15.79 15.55,15.5 15.55,15.05A1.3,1.3 0 0,1 16.85,13.75C17.19,13.75 17.53,13.89 17.77,14.15L18.91,15.26L20.03,14.13C20.27,13.89 20.61,13.75 20.95,13.75A1.3,1.3 0 0,1 22.25,15.05C22.25,15.39 22.11,15.73 21.87,15.97L18.9,18.94M17.46,19.62C15.72,21.1 13.47,22 11,22A10,10 0 0,1 1,12A10,10 0 0,1 11,2A10,10 0 0,1 21,12C21,12.09 21,12.17 20.95,12.25C20.21,12.25 19.5,12.55 18.97,13.07L18.9,13.14L18.84,13.09C18.32,12.55 17.6,12.25 16.85,12.25A2.8,2.8 0 0,0 14.05,15.05C14.05,15.78 14.34,16.5 14.87,17.03L17.46,19.62M13,9.5C13,10.3 13.7,11 14.5,11C15.3,11 16,10.3 16,9.5C16,8.7 15.3,8 14.5,8C13.7,8 13,8.7 13,9.5M9,9.5C9,8.7 8.3,8 7.5,8C6.7,8 6,8.7 6,9.5C6,10.3 6.7,11 7.5,11C8.3,11 9,10.3 9,9.5M12.94,15.18L14,14.12L11.88,12L10.82,13.06L11.88,14.12L10.82,15.18L11.88,16.24L10.82,17.3L11.88,18.36L14,16.24L12.94,15.18Z"),
        };
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
    public class MarkInfo {
        public Mark mark;
        public string label;
        public string svgPath;
        private MarkInfo(Mark mark, string label, string svgPath) {
            this.mark = mark;
            this.label = label;
            this.svgPath = svgPath;
        }

        public static MarkInfo[] List = new MarkInfo[] {
            new MarkInfo(Mark.MARK_STAR, "Star", "M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z"),
            new MarkInfo(Mark.MARK_FLAG, "Flag", "M14.4,6L14,4H5V21H7V14H12.6L13,16H20V6H14.4Z"),
            new MarkInfo(Mark.MARK_HEART, "Heart", "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z"),
        };
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

        [Column(Name = "size", CanBeNull = true)]
        private ulong? size;

        public ulong Size {
            get => size ?? 0;
            set => setProp(callerName(), ref size, value );
        }


        public static string FormatDuration(ulong durationInSec) {
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
        public string ContentType => string.IsNullOrEmpty(VPath) ? "audio/mpeg": "video/mp4";
        public string BooType => string.IsNullOrEmpty(VPath) ? "mp3" : "mp4";

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

        private ulong getFileSize() {
            if (Path == null) return 0;
            try {
                var info = new FileInfo(Path);
                if (!info.Exists) {
                    return 0;
                }
                return (ulong)info.Length;
            }
            catch (Exception e) {
                LoggerEx.error(e);
                return 0;
            }

        }

        private ulong getMediaDuration() {
            if (Path == null) return 0;
            var d = MediaInfo.GetDuration(Path);
            return (ulong)(d?.TotalSeconds ?? 0);
        }

        public void ComplementSizeAndDuration() {
            if (Size == 0) {
                Size = getFileSize();
            }

            if (DurationInSec == 0) {
                DurationInSec = getMediaDuration();
            }
        }
        public void UpdateSizeAndDuration() {
            Size = getFileSize();
            DurationInSec = getMediaDuration();
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
