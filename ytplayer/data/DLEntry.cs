using common;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public enum Status {
        REGISTERED = 0,
        WAITING,
        DOWNLOADING,
        DOWNLOADED,
        FAILED,
        CANCELLED,
        DELETED,
    }
    public enum Rating {
        DREADFUL = 1,
        BAD = 2,
        NORMAL = 3,
        GOOD = 4,
        EXCELLENT = 5,
    }
    public enum MediaFlag {
        NONE = 0,
        AUDIO = 1,
        VIDEO = 2,
        BOTH = 3,
    }

    [Table(Name = "t_download")]
    public class DLEntry : MicPropertyChangeNotifier {
        [Column(Name = "url", IsPrimaryKey = true, CanBeNull = false)]
        private string url;
        public string Url => url;

        [Column(Name = "name", CanBeNull = true)]
        private string name;
        public string Name {
            get => name;
            set => setProp(callerName(), ref name, value);
        }

        [Column(Name = "path", CanBeNull = true)]
        private string path;
        public string Path {
            get => path;
            set => setProp(callerName(), ref path, value);
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

        public DLEntry() {
            this.url = "";
            name = "";
            path = "";
            status = (int)Status.REGISTERED;
            rating = (int)Rating.NORMAL;
            category = "-";
        }

        public static DLEntry Create(string url_) {
            return new DLEntry() { url = url_ };
        }

        private static readonly DateTime EpochDate = new DateTime(1970, 1, 1, 0, 0, 0);
        public static DateTime AsTime(object obj) {
            try {
                if (obj != null && obj != DBNull.Value) {
                    return DateTime.FromFileTimeUtc(Convert.ToInt64(obj));
                }
            } catch(Exception e) {
            }
            return EpochDate;
        }
    }
}
