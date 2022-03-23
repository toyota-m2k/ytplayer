using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;

namespace ytplayer.data {
    public delegate void NotificationProc<in T>(T entry) where T : class;

    public abstract class StorageTable<T> : IDisposable where T : class {
        public DataContext Context;
        public Table<T> Table { get; private set; }
        public event NotificationProc<T> AddEvent;
        public event NotificationProc<T> DelEvent;

        protected StorageTable(SQLiteConnection connection) {
            Context = new DataContext(connection);
            Table = Context.GetTable<T>();
        }

        // SQLite + Linq で、Primary Key を autoincrement にすると、なぜか、連続する複数回のinsertで、DuplicateKeyException が出て失敗する。
        // https://stackoverflow.com/questions/37159299/insert-with-autoincrement
        // では、３つの解決策が示されているが、１，２はダメで、３の、Insertするたびに、Contextを作り直す、という効率の悪そうな方法しかうまく行かなかった。
        public void FlashForce() {
            Update();
            var connection = Context.Connection;
            Context.Dispose();
            Context = new DataContext(connection);
            Table = Context.GetTable<T>();
        }

        //public abstract bool Contains(string key);
        public abstract bool Contains(T entry);

        public IEnumerable<T> List => Table;

        //public bool Contains(T entry) {
        //    return Table.Where((c) =>  entry.KEY == c.KEY).Any();
        //}
        //public bool Contains(string key) {
        //    return Table.Where((c) => key == c.KEY).Any();
        //}

        // 
        // Table.Where((c)=>c.KEY == key).Any()     は成功するが、
        // Table.Where((c)=>c.KEY == key).FirstOrDefault()  とか、
        // var list = Table.Where((c)=>c.KEY == key);
        // foreach(var e in list) {} のように、
        // エレメントを取得しようとすると例外(インターフェイスメンバー IEntry.KEY へのマッピングはサポートされていません)　がでる。
        // どうやらDBから取得したIEnumerableに対するWhereは SQLに変換されるので、その際、DLEntry.KEY は扱えても、IEntry.KEY は処理できない模様。
        // 
        //public T Find(string key) {
        //    var list = Table.Where((c) => c.KEY == key);
        //    if(list==null) {
        //        return null;
        //    }
        //    if(list.Any()) {
        //        foreach(var e in list) {
        //            return e;
        //        }
        //        //return list.First();
        //    } else {
        //        return null;
        //    }
        //}


        public bool Add(T add) {
            try {
                if (Contains(add)) {
                    // Already Registered.
                    return false;
                }
                Table.InsertOnSubmit(add);
                Table.Context.SubmitChanges();
                AddEvent?.Invoke(add);
                return true;
            }
            catch (Exception e) {
                Logger.error(e);
                return false;
            }
        }

        //public void Add(IEnumerable<T> adds) {
        //    Table.InsertAllOnSubmit(adds);
        //    Table.Context.SubmitChanges();
        //    if (AddEvent != null) {
        //        foreach (var a in adds) {
        //            AddEvent(a);
        //        }
        //    }
        //}

        public void Delete(T del, bool update = true) {
            Table.DeleteOnSubmit(del);
            if (update) {
                Update();
            }
            DelEvent?.Invoke(del);
        }
        public void Delete(T[] targets, bool update) {
            Table.DeleteAllOnSubmit(targets);
            if (update) {
                Update();
            }
            if (DelEvent != null) {
                foreach (var d in targets) {
                    DelEvent(d);
                }
            }
        }

        public void Update() {
            Table.Context.SubmitChanges();
        }

        public void Dispose() {
            AddEvent = null;
            DelEvent = null;
            Context?.Dispose();
            Context = null;
            Table = null;
        }
    }
}
