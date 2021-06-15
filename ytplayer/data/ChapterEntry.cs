using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.data {
    [Table(Name = "t_chapter")]
    public class ChapterEntry {
        [Column(Name = "id", IsPrimaryKey = true, CanBeNull = false)]
        private int? id { get; set; } = null;

        [Column(Name = "owner", CanBeNull = false)]
        public string Owner { get; private set; }

        [Column(Name = "position", CanBeNull = false)]
        public ulong Position{ get; private set; }

        [Column(Name = "skip", CanBeNull = false)]
        private int skip { get; set; }
        public bool Skip {
            get => skip!=0;
            set { skip = value ? 1 : 0; }
        }

        public ChapterEntry() {
            Owner = "";
            Position = 0;
            skip = 0;
        }

        static public ChapterEntry Create(string owner, ulong pos, bool skip=false) {
            return new ChapterEntry() { Owner = owner, Position = pos, Skip = skip };
        }
        static public ChapterEntry Create(string owner, ChapterInfo info) {
            return new ChapterEntry() { Owner = owner, Position = info.Position, Skip = info.Skip };
        }

        public ChapterInfo ToChapterInfo() {
            return new ChapterInfo(Position, Skip);
        }
    }

    public class ChapterTable : StorageTable<ChapterEntry> {
        public ChapterTable(SQLiteConnection connection) : base(connection) { }
        public override bool Contains(ChapterEntry entry) {
            return Table.Where((c) => c.Owner == entry.Owner && c.Position == entry.Position).Any();
        }

        public ChapterList GetChapterList(string owner) {
            return new ChapterList(owner, Table.Where((c) => c.Owner == owner).Select((c)=>c.ToChapterInfo()));
        }
        
        public void UpdateByChapterList(ChapterList updated) {
            var current = GetChapterList(updated.Owner);

            var appended = updated.Values.Except(current.Values).Select((c)=>ChapterEntry.Create(updated.Owner, c)).ToList();
            var deleted = current.Values.Except(updated.Values).Select((c) => ChapterEntry.Create(updated.Owner, c)).ToList();

            Table.InsertAllOnSubmit(appended);
            Table.DeleteAllOnSubmit(deleted);
            Update();
        }
    }
}
