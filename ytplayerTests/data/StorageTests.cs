using Microsoft.VisualStudio.TestTools.UnitTesting;
using ytplayer.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.Linq.Mapping;

namespace ytplayer.data.Tests {
    [TestClass()]
    public class StorageTests {
        private Storage storage;
        [TestInitialize]
        public void TestInitialize() {
            Debug.WriteLine("TestInitialize");
        }

        [TestCleanup]
        public void TestCleanup() {
            Debug.WriteLine("TestCleanup");
            storage?.Dispose();
            storage = null;
        }
        [ClassCleanup]
        public static void ClassCleanup() {
            Debug.WriteLine("ClassCleanup");
        }
        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx) {
            Debug.WriteLine("ClassInitialize");
        }

        [TestMethod()]
        public void MainTableTest() {
            storage?.Dispose();
            System.IO.File.Delete("test.db");
            storage = Storage.OpenDB("test.db");
            var dl = storage.DLTable;
            var entry = DLEntry.Create("1", "1");
            dl.Add(entry);
            var list = dl.List;
            Assert.AreEqual(1, list.Count());
            dl.Add(DLEntry.Create("2", "2"));
            dl.Add(DLEntry.Create("3", "3"));
            dl.Add(DLEntry.Create("4", "4"));
            var e = list.Single((v) => v.Url == "1");
            Assert.AreEqual(entry.Url, e.Url);
            e.Rating = Rating.GOOD;
            e = list.Single((v) => v.Url == "3");
            e.Desc = "hoge";
            dl.Update();
            storage.Dispose();
            storage = Storage.OpenDB("test.db");
            dl = storage.DLTable;
            list = dl.List;
            Assert.AreEqual(4, list.Count());
            e = list.Single((v) => v.Url == "1");
            Assert.AreEqual(entry.Url, e.Url);
            Assert.AreEqual(Rating.GOOD, e.Rating);
            e = list.Single((v) => v.Url == "3");
            Assert.AreEqual("3", e.Url);
            Assert.AreEqual(Rating.NORMAL, e.Rating);
            Assert.AreEqual("hoge", e.Desc);
        }

        [TestMethod()]
        public void TMapStrageTest() {
            storage?.Dispose();
            storage = Storage.OpenDB(":memory");
            var table = storage.KVTable;
            table.Add(new KVEntry("a", 123));
            table.Update();
            table.Add(new KVEntry("b", "xyz"));
            table.Update();
            table.Add(new KVEntry("c", 999 ));
            table.Update();
            table.Add(new KVEntry("d", 888));
            table.Update();
            Assert.AreEqual(4, table.Table.Count());

            KVEntry entry;
            entry = table.Table.Single((e) => e.KEY == "b");
            //Assert.AreEqual(2, entry.id); 
            Assert.AreEqual(0, entry.IntValue);
            Assert.AreEqual("xyz", entry.StringValue);
            entry = table.Table.Single((e) => e.KEY == "c");
            Assert.AreEqual(999, entry.IntValue);
            Assert.IsNull(entry.StringValue);

            entry = table.Table.Single((e) => e.KEY == "d");
            Assert.AreEqual(888, entry.IntValue);
            Assert.IsNull(entry.StringValue);

            entry.IntValue = 2000;
            table.Update();
            Assert.IsTrue(true);
        }
    }
}