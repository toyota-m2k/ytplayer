using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public class Storage : IDisposable {
        private SQLiteConnection Connection { get; set; }

        public class StorageTable<T> where T: class {
            public Table<T> Table { get; private set; }
            public StorageTable(DataContext ctx) {
                Table = ctx.GetTable<T>();
            }

            public IEnumerable<T> List => Table;

            public void Add(T add, bool update=true) {
                Table.InsertOnSubmit(add);
                if(update) {
                    Table.Context.SubmitChanges();
                }
            }

            public void Add(IEnumerable<T> adds, bool update = true) {
                Table.InsertAllOnSubmit(adds);
                if (update) {
                    Table.Context.SubmitChanges();
                }
            }

            public void Delete(T del, bool update=true) {
                Table.DeleteOnSubmit(del);
                if(update) {
                    Update();
                }
            }
            public void Delete(IEnumerable<T> dels, bool update) {
                Table.DeleteAllOnSubmit(dels);
                if(update) {
                    Update();
                }
            }

            public void Update() {
                Table.Context.SubmitChanges();
            }
        }

        public DataContext Context { get; set; }

        public StorageTable<DLEntry> DLTable { get; }
        public StorageTable<KVEntry> KVTable { get; }

        public Storage(string path) {
            var builder = new SQLiteConnectionStringBuilder() { DataSource = path };
            Connection = new SQLiteConnection(builder.ConnectionString);
            Connection.Open();
            InitTables();

            Context = new DataContext(Connection);
            Context.Log = Console.Out;
            DLTable = new StorageTable<DLEntry>(Context);
            KVTable = new StorageTable<KVEntry>(Context);
        }

        public void Dispose() {
            if(Context!=null) {
                Context.Dispose();
                Context = null;
            }
            if (Connection != null) {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
        }

        // DB操作ヘルパー
        private void executeSql(params string[] sqls) {
            using (var cmd = Connection.CreateCommand()) {
                foreach (var sql in sqls) {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void InitTables() {
            executeSql(
                @"CREATE TABLE IF NOT EXISTS t_download (
                    url TEXT NOT NULL PRIMARY KEY,
                    name TEXT,
                    path TEXT,
                    category TEXT,
                    desc TEXT,
                    status INTEGER DEFAULT '0',
                    rating INTEGER DEFAULT '0',
                    date INTEGER DEFAULT '0',
                    media INTEGER DEFAULT '0'
                )",
                @"CREATE INDEX IF NOT EXISTS idx_path ON t_download(path)",
                @"CREATE INDEX IF NOT EXISTS idx_category ON t_download(category)",
                @"CREATE TABLE IF NOT EXISTS t_map (
                    name TEXT NOT NULL PRIMARY KEY,
                    ivalue INTEGER DEFAULT '0',
                    svalue TEXT
                )"
            );
        }
    }
}
