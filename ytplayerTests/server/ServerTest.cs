/**
 * Copyright (c) 2021 @toyota-m2k.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ytplayer.server.lib;

namespace ytplayerTests.server {
    /// <summary>
    /// ServerTest の概要の説明
    /// </summary>
    [TestClass]
    public class ServerTest {
        public ServerTest() {
            //
            // TODO: コンストラクター ロジックをここに追加します
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///現在のテストの実行についての情報および機能を
        ///提供するテスト コンテキストを取得または設定します。
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region 追加のテスト属性
        //
        // テストを作成する際には、次の追加属性を使用できます:
        //
        // クラス内で最初のテストを実行する前に、ClassInitialize を使用してコードを実行してください
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // クラス内のテストをすべて実行したら、ClassCleanup を使用してコードを実行してください
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 各テストを実行する前に、TestInitialize を使用してコードを実行してください
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 各テストを実行した後に、TestCleanup を使用してコードを実行してください
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestUrlQueryParser() {
            string url1 = "http://hoge.com/fuga?aaa=111";
            string url2 = "http://hoge.com/fuga?aaa";
            string url3 = "http://hoge.com/fuga?aaa=111&bbb=222";
            string url4 = "http://hoge.com/fuga?aaa=111&bbb";
            string url5 = "http://hoge.com/fuga?aaa=111&bbb&ccc=333";
            string url6 = "http://hoge.com/fuga?aaa&bbb&ccc";

            var dic = QueryParser.Parse(url1);
            Assert.AreEqual("111", dic["aaa"]);
            dic = QueryParser.Parse(url2);
            Assert.AreEqual("", dic["aaa"]);
            dic = QueryParser.Parse(url3);
            Assert.AreEqual("111", dic["aaa"]);
            Assert.AreEqual("222", dic["bbb"]);
            dic = QueryParser.Parse(url4);
            Assert.AreEqual("111", dic["aaa"]);
            Assert.AreEqual("", dic["bbb"]);
            dic = QueryParser.Parse(url5);
            Assert.AreEqual("111", dic["aaa"]);
            Assert.AreEqual("", dic["bbb"]);
            Assert.AreEqual("333", dic["ccc"]);
            dic = QueryParser.Parse(url6);
            Assert.AreEqual("", dic["aaa"]);
            Assert.AreEqual("", dic["bbb"]);
            Assert.AreEqual("", dic["ccc"]);
        }
    }
}
