using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    [Table(Name = "t_map")]
    public class KVEntry {
        [Column(Name = "name", CanBeNull = false, IsPrimaryKey = true)]
        public string KEY { get;  private set; }

        [Column(Name = "ivalue", CanBeNull = true)]
        public int iValue;

        [Column(Name = "svalue", CanBeNull = true)]
        public string sValue;

        public KVEntry() {

        }
        public KVEntry(string name, int iValue, string sValue=null) {
            KEY = name;
            this.iValue = iValue;
            this.sValue = sValue;
        }
        public KVEntry(string name, string sValue) {
            KEY = name;
            this.iValue = 0;
            this.sValue = sValue;
        }
    }

    public class KVEntryTable : StorageTable<KVEntry> {
        public KVEntryTable(SQLiteConnection connection) : base(connection) { }
        public bool Contains(string key) {
            return Table.Where((c) => c.KEY == key).Any();
        }
        public override bool Contains(KVEntry entry) {
            return Contains(entry.KEY);
        }
        public KVEntry Find(string key) {
            return Table.Where((c) => c.KEY == key).SingleOrDefault();
        }
    }

}
