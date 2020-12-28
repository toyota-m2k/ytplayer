using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;
using System.Linq;
using ytplayer.common;
using ytplayer.download.downloader;

namespace ytplayer.data {
    //public interface IEntry {
    //    string KEY { get; }
    //}

    public class Storage : IDisposable {
        private SQLiteConnection Connection { get; set; }



        public class KVEntryExTable : StorageTable<KVEntry> {
            public KVEntryExTable(SQLiteConnection connection) : base(connection) { }
            public override bool Contains(string key) {
                return Table.Where((c) => c.KEY == key).Any();
            }
            public override bool Contains(KVEntry entry) {
                return Contains(entry.KEY);
            }
            public KVEntry Find(string key) {
                return Table.Where((c) => c.KEY == key).SingleOrDefault();
            }
        }

        //public DataContext Context { get; set; }

        public DLEntryTable DLTable { get; }
        public KVEntryExTable KVTable { get; }

        public string DBPath { get; }

        public Storage(string path, bool dontCreateTable=false) {
            DBPath = path;
            var builder = new SQLiteConnectionStringBuilder() { DataSource = path };
            Connection = new SQLiteConnection(builder.ConnectionString);
            Connection.Open();
            if (!dontCreateTable) {
                InitTables();
            }

            DLTable = new DLEntryTable(Connection);
            KVTable = new KVEntryExTable(Connection);
            DLTable.Context.Log = Console.Out;

            ConvertTable();
        }

        private void ConvertTable() {
            try {
                using (var DLTableOld = new DLEntryOldTable(Connection)) {
                    //foreach (var e in DLTable.List) {
                    //    Logger.debug(e.Name);
                    //}

                    foreach (var e in DLTable.List) {
                        var uri = new Uri(e.Url);
                        var dlr = DownloaderSelector.Select(uri);
                        if (dlr != null) {
                            var id = dlr.IdFromUri(uri);
                            DLEntry eo = DLTable.Find(id);
                            if (eo == null) {
                                var ex = DLEntry.Create(id, e.Url);
                                ex.Name = e.Name;
                                ex.Mark = e.Mark;
                                ex.Rating = e.Rating;
                                ex.Status = e.Status;
                                ex.DurationInSec = e.DurationInSec;
                                ex.Category = e.Category;
                                ex.Date = e.Date;
                                ex.Volume = e.Volume;
                                int media = 0;
                                if (!string.IsNullOrEmpty(e.VPath) && PathUtil.isFile(e.VPath)) {
                                    ex.VPath = e.VPath;
                                    media |= (int)MediaFlag.VIDEO;
                                }
                                if (!string.IsNullOrEmpty(e.APath) && PathUtil.isFile(e.APath)) {
                                    ex.APath = e.APath;
                                    media |= (int)MediaFlag.AUDIO;
                                }
                                ex.Media = (MediaFlag)media;
                                DLTable.Add(ex);
                            } else {
                                MediaFlag media = eo.Media;
                                if (!string.IsNullOrEmpty(e.VPath) && PathUtil.isFile(e.VPath)) {
                                    if (!media.HasVideo()) {
                                        eo.Media = media.PlusVideo();
                                        eo.VPath = e.VPath;
                                    }
                                }
                                if (!string.IsNullOrEmpty(e.APath) && PathUtil.isFile(e.APath)) {
                                    if (!media.HasAudio()) {
                                        eo.Media = media.PlusAudio();
                                        eo.APath = e.APath;
                                    }
                                }
                            }
                        }
                    }
                    DLTable.Update();
                }
                executeSql(
                    @"DROP INDEX IF EXISTS idx_category",
                    @"DROP TABLE IF EXISTS t_download");
            } catch (Exception e) {
                Logger.error(e);
            }
        }

        public void Dispose() {
            DLTable?.Dispose();
            KVTable?.Dispose();

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

        public int getVersion() {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = $"SELECT ivalue FROM t_map WHERE name='version'";
                using (var reader = cmd.ExecuteReader()) {
                    if (reader.Read()) {
                        return Convert.ToInt32(reader["ivalue"]);
                    }
                }
            }
            return 0;
        }

        public bool setVersion(int version) {
            using (var cmd = Connection.CreateCommand()) {
                try {
                    cmd.CommandText = $"UPDATE t_map SET ivalue='{version}' WHERE name='version'";
                    if (1 == cmd.ExecuteNonQuery()) {
                        return true;
                    }
                    cmd.CommandText = $"INSERT INTO t_map (name,ivalue) VALUES('version','{version}')";
                    return 1 == cmd.ExecuteNonQuery();
                }
                catch (SQLiteException) {
                    return false;
                }
            }
        }


        private void InitTables() {
            executeSql(
                //@"CREATE TABLE IF NOT EXISTS t_download (
                //    url TEXT NOT NULL PRIMARY KEY,
                //    name TEXT,
                //    vpath TEXT,
                //    apath TEXT,
                //    date INTEGER DEFAULT '0',
                //    media INTEGER DEFAULT '0',
                //    status INTEGER DEFAULT '0',
                //    rating INTEGER DEFAULT '0',
                //    category TEXT,
                //    volume INTEGER DEFAULT '0',
                //    duration INTEGER DEFAULT '0',
                //    mark INTEGER DEFAULT '0',
                //    desc TEXT
                //)",
                @"CREATE TABLE IF NOT EXISTS t_download_ex (
                    id TEXT NOT NULL PRIMARY KEY,
                    url TEXT NOT NULL,
                    name TEXT,
                    vpath TEXT,
                    apath TEXT,
                    date INTEGER DEFAULT '0',
                    media INTEGER DEFAULT '0',
                    status INTEGER DEFAULT '0',
                    rating INTEGER DEFAULT '0',
                    category TEXT,
                    volume INTEGER DEFAULT '0',
                    duration INTEGER DEFAULT '0',
                    mark INTEGER DEFAULT '0',
                    desc TEXT
                )",
                @"CREATE INDEX IF NOT EXISTS idx_category ON t_download_ex(category)",
                @"CREATE TABLE IF NOT EXISTS t_map (
                    name TEXT NOT NULL PRIMARY KEY,
                    ivalue INTEGER DEFAULT '0',
                    svalue TEXT
                )"
            );

            if(getVersion()==0) {
                executeSql(@"ALTER TABLE t_download ADD COLUMN duration INTEGER DEFAULT '0'");
                setVersion(1);
            }
            if (getVersion() == 1) {
                executeSql(@"ALTER TABLE t_download ADD COLUMN mark INTEGER DEFAULT '0'");
                setVersion(2);
            }
        }
    }
}
