using common;
using System;
using System.Data.Linq.Mapping;
using System.Text.RegularExpressions;
using ytplayer.common;
using ytplayer.player;

namespace ytplayer.data {
    public enum Status {
        REGISTERED = 0,
        WAITING,
        DOWNLOADING,
        DOWNLOADED,
        FAILED,
        CANCELLED,
        LIST,
        DELETED,
    }
    public enum Rating {
        DREADFUL = 1,
        BAD = 2,
        NORMAL = 3,
        GOOD = 4,
    }
    public enum MediaFlag {
        NONE = 0,
        AUDIO = 1,
        VIDEO = 2,
        BOTH = 3,
    }

    [Table(Name = "t_download")]
    public class DLEntry : MicPropertyChangeNotifier, IPlayable {
        [Column(Name = "url", IsPrimaryKey = true, CanBeNull = false)]
        private string url;
        public string Url => url;

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
                    return "<Not Downloaded Yet>";
                } else {
                    var m = RegName.Matches(name);
                    if (m.Count > 0) {
                        var n = m[0].Groups["name"];
                        if (!string.IsNullOrEmpty(n.Value)) {
                            return n.Value;
                        }
                    }
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
        public string Category {
            get => category;
            set => setProp(callerName(), ref category, value);
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

        [Column(Name = "desc", CanBeNull = true)]
        private string desc;
        public string Desc {
            get => desc;
            set => setProp(callerName(), ref desc, value);
        }

        public string Path => string.IsNullOrEmpty(VPath) ? APath : VPath;
        
        public void Delete() {
            Status = Status.DELETED;
        }
        
        public DLEntry() {
            url = "";
            name = "";
            vpath = "";
            apath = "";
            status = (int)Status.REGISTERED;
            rating = (int)Rating.NORMAL;
            category = "-";
        }

        public static DLEntry Create(string url_) {
            return new DLEntry() { url = url_, Date=DateTime.UtcNow };
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
