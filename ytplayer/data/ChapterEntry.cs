using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;

namespace ytplayer.data {
    [Table(Name = "t_chapter")]
    public class ChapterEntry {
        [Column(Name = "id", IsPrimaryKey = true)]
        private int? id { get; set; } = null;

        [Column(Name = "owner", CanBeNull = false)]
        public string Owner { get; private set; }

        [Column(Name = "position", CanBeNull = false)]
        public ulong Position{ get; private set; }

        [Column(Name = "label", CanBeNull = true)]
        public string Label { get; set; }

        [Column(Name = "skip", CanBeNull = false)]
        private int skip { get; set; }
        public bool Skip {
            get => skip!=0;
            set => skip = value ? 1 : 0;
        }

        public ChapterEntry() {
            Owner = "";
            Position = 0;
            Label = null;
            skip = 0;
        }

        public static ChapterEntry Create(string owner, ulong pos, bool skip=false, string label=null) {
            return new ChapterEntry() { Owner = owner, Position = pos, Skip = skip, Label = label };
        }
        public static ChapterEntry Create(string owner, ChapterInfo info) {
            return new ChapterEntry() { Owner = owner, Position = info.Position, Skip = info.Skip, Label = info.Label };
        }

        public ChapterInfo ToChapterInfo() {
            return new ChapterInfo(Position, Skip, Label);
        }
    }

    public class ChapterTable : StorageTable<ChapterEntry> {
        public ChapterTable(SQLiteConnection connection) : base(connection) { }
        public override bool Contains(ChapterEntry entry) {
            return Table.Any(c => c.Owner == entry.Owner && c.Position == entry.Position);
        }

        // private class ChapterGroup {
        //     public string Owner { get; }
        //     public List<ChapterEntry> Chapters { get; }
        //
        //     public ChapterGroup(string owner, List<ChapterEntry> list) {
        //         Owner = owner;
        //         Chapters = list;
        //     }
        // };

        public IEnumerable<ChapterEntry> GetChapterEntries(string owner) {
            return Table.Where((c) => c.Owner == owner);
        }

        public ChapterList GetChapterList(string owner) {
            return new ChapterList(owner, Table.Where((c) => c.Owner == owner).Select((c)=>c.ToChapterInfo()));
        }

        private class PositionComparator : IEqualityComparer<ChapterInfo> {
            public bool Equals(ChapterInfo x, ChapterInfo y) {
                return y != null && x != null && x.Position == y.Position;
            }

            public int GetHashCode(ChapterInfo obj) {
                return obj.Position.GetHashCode();
            }
        }

        private static PositionComparator PComp = new PositionComparator();

        // Autoincrementのprimary keyのせいで、DuplicateKeyExceptionが出る問題対策
        public void AddAll(IEnumerable<ChapterEntry> source) {
            foreach (var a in source) {
                Table.InsertOnSubmit(a);
                FlashForce();
            }
        }


        public void UpdateByChapterList(ChapterList updated) {
            var current = GetChapterList(updated.Owner);

            var appended = updated.Values.Except(current.Values, PComp).Select((c)=>ChapterEntry.Create(updated.Owner, c)).ToList();
            var deleted = current.Values.Except(updated.Values, PComp).Select((c) => ChapterEntry.Create(updated.Owner, c)).ToList();
            var modified = updated.Values.Where((c)=>c.IsModified).Intersect(current.Values, PComp);

            foreach(var m in modified) {
                var entry = Table.SingleOrDefault(c => c.Position == m.Position && c.Owner == current.Owner);
                if (entry != null) {
                    entry.Skip = m.Skip;
                    entry.Label = m.Label;
                }
            }

            foreach(var a in appended) {
                Table.InsertOnSubmit(a);
                FlashForce();
            }
            // 残念ながら、Autoincrementのprimary keyのせいで、DuplicateKeyExceptionが出るから、InsertAllOnSubmitは使えない。
            //Table.InsertAllOnSubmit(appended);
            // Table.DeleteAllOnSubmit(deleted);
            foreach (var d in deleted) {
                var entry = Table.SingleOrDefault(c => c.Position == d.Position && c.Owner == current.Owner);
                if (entry != null) {
                    Table.DeleteOnSubmit(entry);
                }
            }

            Update();
            updated.ResetModifiedFlag();
        }
    }
}
