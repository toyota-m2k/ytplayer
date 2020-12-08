using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public enum Status {
        INIT,
        WAITING,
        DOWNLOADING,
        DOWNLOADED,
        FAILED,
        CANCELLED,
        DELETED,
    }
    public enum Rating {
        DREADFUL,
        BAD,
        NORMAL,
        GOOD,
        EXCELLENT,
    }

    [Table(Name = "t_download")]
    public class DLEntry {
        [Column(Name = "id", IsPrimaryKey = true, IsDbGenerated =true)]
        int id;
        [Column(Name = "url", CanBeNull = false)]
        string url;
        [Column(Name = "name", CanBeNull = false)]
        string name;
        [Column(Name = "path", CanBeNull = false)]
        string path;
        [Column(Name = "status", CanBeNull = false)]
        int status;
        [Column(Name = "rating", CanBeNull = false)]
        int rating;
        [Column(Name = "category", CanBeNull = true)]
        string category;
        [Column(Name = "date", CanBeNull = false)]
        int date;
        [Column(Name = "desc", CanBeNull = true)]
        string desc;

        public Rating Rating {
            get => (Rating)rating;
            set => rating = (int)value;
        }
        public Status Status {
            get => (Status)status;
            set => status = (int)value;
        }

        public DLEntry(string url) {
            id = 0;
            this.url = url;
            name = "";
            path = "";
            Status = Status.INIT;
            Rating = Rating.NORMAL;
            category = "-";
        }
    }
}
