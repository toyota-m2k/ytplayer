using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ytplayer;

namespace ytplayer.browser {
    public class Bookmarks : ObservableCollection<Bookmarks.BookmarkRec>, IDisposable {
        public class BookmarkRec {
            public string Name { get; set; }
            public string Url { get; set; }
            public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Url : Name;

            public BookmarkRec() {

            }

            public BookmarkRec(string name, string url) {
                Name = name;
                Url = url;
            }
        }

        private const string FILENAME = "bookmarks.xml";
        private static string BookmarkFilePath = Path.Combine(Settings.AppPath, FILENAME);

        public static Bookmarks CreateInstance() {
            System.IO.StreamReader sr = null;
            object obj = null;

            try {
                //XmlSerializerオブジェクトを作成
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Bookmarks));

                //読み込むファイルを開く
                sr = new System.IO.StreamReader(BookmarkFilePath, new System.Text.UTF8Encoding(false));

                //XMLファイルから読み込み、逆シリアル化する
                obj = serializer.Deserialize(sr);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                obj = new Bookmarks();
            }
            finally {
                if (null != sr) {
                    //ファイルを閉じる
                    sr.Close();
                }
            }
            return (Bookmarks)obj;
        }
        public void Dispose() {
            System.IO.StreamWriter sw = null;
            try {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Bookmarks));
                //書き込むファイルを開く（UTF-8 BOM無し）
                sw = new System.IO.StreamWriter(BookmarkFilePath, false, new System.Text.UTF8Encoding(false));
                //シリアル化し、XMLファイルに保存する
                serializer.Serialize(sw, this);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
            }
            finally {
                //ファイルを閉じる
                if (null != sw) {
                    sw.Close();
                }
            }
        }
        public void AddBookmark(string name, string url) {
            var rec = FindBookmark(url);
            if (rec != null) {
                this.Move(IndexOf(rec), 0);
            } else {
                this.Insert(0, new BookmarkRec(name, url));
            }
        }
        
        public BookmarkRec FindBookmark(string url) {
            var org = this.Where((bm) => bm.Url == url);
            if (!Utils.IsNullOrEmpty(org)) {
                return org.First();
            }
            return null;
        }

        public BookmarkRec RemoveBookmark(string url) {
            var r = FindBookmark(url);
            if (null != r) {
                Remove(r);
            }
            return r;
        }

        public BookmarkRec BringUpBookmark(string url) {
            var r = FindBookmark(url);
            if (null != r) {
                var i = IndexOf(r);
                if (i != 0) {
                    Move(i, 0);
                }
            }
            return r;
        }
    }
}

