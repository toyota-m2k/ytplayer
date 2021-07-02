using io.github.toyota32k.toolkit.utils;
using System;
using System.Data.SQLite;
using System.Linq;
using ytplayer.common;

namespace ytplayer.data {
    //public interface IEntry {
    //    string KEY { get; }
    //}

    public class Storage : IDisposable {
        private const string APP_NAME = "YTPlayer";
        private const int DB_VERSION = 5;

        private SQLiteConnection Connection { get; set; }
        public static long LastUpdated { get; set; } = 0;

        public DLEntryTable DLTable { get; }
        public KVEntryTable KVTable { get; }
        public ChapterTable ChapterTable { get; }

        public string DBPath { get; }

        private Storage(string path) {
            bool creation = false;
            if (path == ":memory" || !PathUtil.isExists(path)) {
                // onMemoryDBまたは、存在しないときは新規作成
                creation = true;
            }
            DBPath = path;
            var builder = new SQLiteConnectionStringBuilder() { DataSource = path };
            Connection = new SQLiteConnection(builder.ConnectionString);
            Connection.Open();

            try {
                if (!creation) {
                    // 新規作成でなければ内容をチェック
                    var appName = getAppName();
                    if (appName != APP_NAME) {
                        throw new FormatException($"DB file is not for {APP_NAME}");
                    }
                    var version = getVersion();
                    if (version > DB_VERSION) {
                        throw new FormatException($"Newer DB version. ({version})");
                    }
                }
                InitTables();

                DLTable = new DLEntryTable(Connection);
                KVTable = new KVEntryTable(Connection);
                ChapterTable = new ChapterTable(Connection);

                // DLTable.Context.Log = Console.Out;
            } catch(Exception e) {
                LoggerEx.error(e);
                Dispose();
                throw e;
            }
        }

        public static bool CheckDB(string path) {
            try {
                using (new Storage(path)) {
                    return true;
                }
            }
            catch (Exception) {
                return false;
            }
        }

        public static Storage OpenDB(string path) {
            try {
                return new Storage(path);
            } catch(Exception e) {
                LoggerEx.error(e);
                return null;
            }
        }

        //public static Storage OpenDB(string path) {
        //    Storage storage = null;
        //    try {
        //        storage = new Storage(path, dontCreateTable: true);
        //        if (storage.getAppName() == APP_NAME) {
        //            return storage;
        //        }
        //        storage.Dispose();
        //        return null;
        //    }
        //    catch (Exception) {
        //        storage?.Dispose();
        //        return null;
        //    }
        //}


        //public static Storage OpenOrCreateDB(string path) {
        //    Storage storage = null;
        //    try {
        //        if (path==":memory" || !PathUtil.isExists(path)) {
        //            // onMemoryDBまたは、存在しないときは新規作成
        //            return new Storage(path, dontCreateTable:false);
        //        }
        //        // 存在するときは開く
        //        storage = new Storage(path, dontCreateTable: true);
        //        if (storage.getAppName() == APP_NAME) {
        //            return storage;
        //        }
        //        Logger.warn($"invalid db:{path}");
        //        storage.Dispose();
        //        return null;
        //    } catch(Exception e) {
        //        Logger.error(e);
        //        storage?.Dispose();
        //        return null;
        //    }

        //}



        //private void ConvertTable() {
        //    try {
        //        using (var DLTableOld = new DLEntryOldTable(Connection)) {
        //            //foreach (var e in DLTable.List) {
        //            //    Logger.debug(e.Name);
        //            //}

        //            foreach (var e in DLTableOld.List) {
        //                var uri = new Uri(e.Url);
        //                var dlr = DownloaderSelector.Select(uri);
        //                if (dlr != null) {
        //                    var id = dlr.IdFromUri(uri);
        //                    DLEntry eo = DLTable.Find(id);
        //                    if (eo == null) {
        //                        var ex = DLEntry.Create(id, e.Url);
        //                        ex.Name = e.Name;
        //                        ex.Mark = e.Mark;
        //                        ex.Rating = e.Rating;
        //                        ex.Status = e.Status;
        //                        ex.DurationInSec = e.DurationInSec;
        //                        ex.Category = e.Category;
        //                        ex.Date = e.Date;
        //                        ex.Volume = e.Volume;
        //                        int media = 0;
        //                        if (!string.IsNullOrEmpty(e.VPath) && PathUtil.isFile(e.VPath)) {
        //                            ex.VPath = e.VPath;
        //                            media |= (int)MediaFlag.VIDEO;
        //                        }
        //                        if (!string.IsNullOrEmpty(e.APath) && PathUtil.isFile(e.APath)) {
        //                            ex.APath = e.APath;
        //                            media |= (int)MediaFlag.AUDIO;
        //                        }
        //                        ex.Media = (MediaFlag)media;
        //                        DLTable.Add(ex);
        //                    } else {
        //                        MediaFlag media = eo.Media;
        //                        if (!string.IsNullOrEmpty(e.VPath) && PathUtil.isFile(e.VPath)) {
        //                            if (!media.HasVideo()) {
        //                                eo.Media = media.PlusVideo();
        //                                eo.VPath = e.VPath;
        //                            }
        //                        }
        //                        if (!string.IsNullOrEmpty(e.APath) && PathUtil.isFile(e.APath)) {
        //                            if (!media.HasAudio()) {
        //                                eo.Media = media.PlusAudio();
        //                                eo.APath = e.APath;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            DLTable.Update();
        //        }
        //        executeSql(
        //            @"DROP INDEX IF EXISTS idx_category",
        //            @"DROP TABLE IF EXISTS t_download");
        //    } catch (Exception e) {
        //        Logger.error(e);
        //    }
        //}

        public void Dispose() {
            DLTable?.Dispose();
            KVTable?.Dispose();
            ChapterTable?.Dispose();

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
            try {
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
            catch (Exception) {
                return 0;
            }
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

        public string getAppName() {
            try {
                using (var cmd = Connection.CreateCommand()) {
                    cmd.CommandText = $"SELECT svalue FROM t_map WHERE name='appName'";
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            return Convert.ToString(reader["svalue"]);
                        }
                    }
                }
                return null;
            }
            catch (Exception) {
                return null;
            }
        }

        public bool setAppName() {
            using (var cmd = Connection.CreateCommand()) {
                try {
                    cmd.CommandText = $"UPDATE t_map SET svalue='{APP_NAME}' WHERE name='appName'";
                    if (1 == cmd.ExecuteNonQuery()) {
                        return true;
                    }
                    cmd.CommandText = $"INSERT INTO t_map (name,svalue) VALUES('appName','{APP_NAME}')";
                    return 1 == cmd.ExecuteNonQuery();
                }
                catch (SQLiteException) {
                    return false;
                }
            }
        }


        private void InitTables() {
            if(getVersion()==4) {
                executeSql(@"drop table t_chapter");
            }
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
                    trim_start INTEGER DEFAULT '0',
                    trim_end INTEGER DEFAULT '0',
                    desc TEXT
                )",
                @"CREATE INDEX IF NOT EXISTS idx_category ON t_download_ex(category)",
                @"CREATE TABLE IF NOT EXISTS t_map (
                    name TEXT NOT NULL PRIMARY KEY,
                    ivalue INTEGER DEFAULT '0',
                    svalue TEXT
                )",
                @"CREATE TABLE IF NOT EXISTS t_chapter (
	                id	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    owner TEXT NOT NULL,
                    position  INTEGER NOT NULL,
                    label TEXT,
                    skip INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(owner) REFERENCES t_download_ex(id),
                    UNIQUE(owner,position)
                )"
            );

            //if(getVersion()==0) {
            //    executeSql(@"ALTER TABLE t_download ADD COLUMN duration INTEGER DEFAULT '0'");
            //    setVersion(1);
            //}
            //if (getVersion() == 1) {
            //    executeSql(@"ALTER TABLE t_download ADD COLUMN mark INTEGER DEFAULT '0'");
            //    setVersion(2);
            //}
            var ver = getVersion();
            if(ver<3) {
                //executeSql(@"ALTER TABLE t_download_ex ADD COLUMN trim_start INTEGER DEFAULT '0' after mark");
                //executeSql(@"ALTER TABLE t_download_ex ADD COLUMN trim_end INTEGER DEFAULT '0' after trim_start");
                setAppName();
            }
            if(ver<DB_VERSION) {
                setVersion(DB_VERSION);
            }

        }
    }
}
