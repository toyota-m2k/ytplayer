using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;

namespace ytplayer.data {
    [Table(Name = "t_map")]
    public class KVEntry {
        [Column(Name = "name", CanBeNull = false, IsPrimaryKey = true)]
        public string KEY { get;  private set; }

        [Column(Name = "ivalue", CanBeNull = true)]
        public int IntValue;

        [Column(Name = "svalue", CanBeNull = true)]
        public string StringValue;

        public KVEntry() {

        }
        public KVEntry(string name, int intValue, string stringValue=null) {
            KEY = name;
            this.IntValue = intValue;
            this.StringValue = stringValue;
        }
        public KVEntry(string name, string stringValue) {
            KEY = name;
            this.IntValue = 0;
            this.StringValue = stringValue;
        }
    }

    public class KVEntryTable : StorageTable<KVEntry> {
        public KVEntryTable(SQLiteConnection connection) : base(connection) { }
        public bool Contains(string key) {
            return Table.Any(c => c.KEY == key);
        }
        public override bool Contains(KVEntry entry) {
            return Contains(entry.KEY);
        }
        public KVEntry Find(string key) {
            return Table.SingleOrDefault(c => c.KEY == key);
        }
    }

}
