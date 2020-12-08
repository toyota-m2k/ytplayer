using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    public class Storage : IDisposable {
        private SQLiteConnection Connection { get; set; }


        public Storage() {
            var builder = new SQLiteConnectionStringBuilder() { DataSource = Settings.Instance.EnsureDBPath };
            Connection = new SQLiteConnection(builder.ConnectionString);
            Connection.Open();
            InitTables();
        }

        public void Dispose() {
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
                @"CREATE TABLE IF NOT EXISTS t_downloaded (
                    id INTEGER NOT NULL PRIMARY KEY,
                    url TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    path TEXT NOT NULL,
                    status INTEGER NOT NULL,
                    desc TEXT,
                    flags INTEGER DEFAULT '0',
                    date INTEGER DEFAULT '0'
                )",
                @"CREATE INDEX IF NOT EXISTS idx_date ON t_downloaded(path)"
            );
        }

    }
}
