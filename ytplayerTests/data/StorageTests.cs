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
            storage = new Storage("test.db");
            var dl = storage.DLTable;
            var entry = DLEntry.Create("1");
            dl.Add(entry);
            var list = dl.List;
            Assert.AreEqual(1, list.Count());
            dl.Add(DLEntry.Create("2"));
            dl.Add(DLEntry.Create("3"));
            dl.Add(DLEntry.Create("4"));
            var e = list.Single((v) => v.Url == "1");
            Assert.AreEqual(entry.Url, e.Url);
            e.Rating = Rating.EXCELLENT;
            e = list.Single((v) => v.Url == "3");
            e.Category = "hoge";
            dl.Update();
            storage.Dispose();
            storage = new Storage("test.db");
            dl = storage.DLTable;
            list = dl.List;
            Assert.AreEqual(4, list.Count());
            e = list.Single((v) => v.Url == "1");
            Assert.AreEqual(entry.Url, e.Url);
            Assert.AreEqual(Rating.EXCELLENT, e.Rating);
            e = list.Single((v) => v.Url == "3");
            Assert.AreEqual("3", e.Url);
            Assert.AreEqual(Rating.NORMAL, e.Rating);
            Assert.AreEqual("hoge", e.Category);
        }

        [TestMethod()]
        public void TMapStrageTest() {
            storage?.Dispose();
            storage = new Storage(":memory:");
            var table = storage.Context.GetTable<KVEntry>();
            table.InsertOnSubmit(new KVEntry() { Name = "a", iValue = 123 });
            storage.Context.SubmitChanges();
            table.InsertOnSubmit(new KVEntry() { Name = "b", sValue = "xyz" });
            storage.Context.SubmitChanges();
            table.InsertOnSubmit(new KVEntry() { Name = "c", iValue=999 });
            storage.Context.SubmitChanges();
            table.InsertOnSubmit(new KVEntry() { Name = "d", iValue = 888 });
            storage.Context.SubmitChanges();
            Assert.AreEqual(4, table.Count());

            KVEntry entry;
            entry = table.Single((e) => e.Name == "b");
            //Assert.AreEqual(2, entry.id);
            Assert.AreEqual(0, entry.iValue);
            Assert.AreEqual("xyz", entry.sValue);
            entry = table.Single((e) => e.Name == "c");
            Assert.AreEqual(999, entry.iValue);
            Assert.IsNull(entry.sValue);

            entry = table.Single((e) => e.Name == "d");
            Assert.AreEqual(888, entry.iValue);
            Assert.IsNull(entry.sValue);

            entry.iValue = 2000;
            storage.Context.SubmitChanges();
            Assert.IsTrue(true);
        }
    }
}