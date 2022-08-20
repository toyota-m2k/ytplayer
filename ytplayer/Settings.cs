using io.github.toyota32k.toolkit.utils;
using System;
using System.Diagnostics;
using ytplayer.data;
using ytplayer.download;

namespace ytplayer {
    public class Settings {
        public WinPlacement Placement { get; set; } = new WinPlacement();
        public WinPlacement PlayerPlacement { get; set; } = new WinPlacement();
        public WinPlacement BrowserPlacement { get; set; } = new WinPlacement();

        public string DBPath { get; set; } = "";
        public string YoutubeDLPath { get; set; } = "";
        public string FFMpegPath { get; set; } = "";
        public string VideoPath { get; set; } = "";
        public string AudioPath { get; set; } = "";
        public string WorkPath { get; set; } = "";          // 無音抽出用Wavファイル作成先
        public bool EnableServer { get; set; } = false;
        public string WebPageRoot { get; set; } = "";
        public int ServerPort { get; set; } = 3500;
        public string SyncPeer { get; set; } = "";

        public DeterminationList Determinations { get; set; } = new DeterminationList();
        public CategoryList Categories { get; set; } = new CategoryList();
        public SearchHistory SearchHistories { get; set; } = new SearchHistory();
        public bool[] Ratings { get; set; }
        public string LastPlayingUrl { get; set; } = null;
        public double LastPlayingPos { get; set; } = 0;
        public bool RestartOnLoaded { get; set; } = false;
        public Sorter SortInfo { get; set; } = new Sorter();
        public bool AcceptList { get; set; } = false;

        private const string SETTINGS_FILE = "settings.xml";
        private static Settings sInstance = null;
        private static string sOrgCurrentPath = null;
        private static string sOrgPath = null;

        public static Settings Instance => sInstance;

        public static string AppPath => sOrgCurrentPath;

        public static void Initialize() {
            if (sInstance == null) {
                sOrgCurrentPath = Environment.CurrentDirectory;
                sOrgPath = Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("path"));
                sInstance = Deserialize();
                sInstance.ApplyEnvironment();
                sInstance.Categories.Initialize();
            }
        }

        public static void Terminate() {
            if (sInstance != null) {
                sInstance.Serialize();
            }
        }

        private static string SettingFilePath => System.IO.Path.Combine(sOrgCurrentPath, SETTINGS_FILE);

        [System.Xml.Serialization.XmlIgnore]
        public string EnsureVideoPath {
            get {
                if (PathUtil.isDirectory(VideoPath)) {
                    return VideoPath;
                }
                return Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string EnsureAudioPath {
            get {
                if (PathUtil.isDirectory(AudioPath)) {
                    return AudioPath;
                }
                return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            }
        }
        [System.Xml.Serialization.XmlIgnore]
        public string EnsureWorkPath {
            get {
                if (PathUtil.isDirectory(WorkPath)) {
                    return WorkPath;
                }
                return System.IO.Path.GetTempPath();
            }
        }

        public static string ComplementDBPath(string dbPath) {
            dbPath = dbPath?.Trim();
            if (!string.IsNullOrEmpty(dbPath)) {
                var dir = System.IO.Path.GetDirectoryName(dbPath);
                var name = System.IO.Path.GetFileName(dbPath);
                if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(name)) {
                    return dbPath;
                }
                if (!string.IsNullOrEmpty(name)) {
                    return System.IO.Path.Combine(sOrgCurrentPath, name);
                }
            }
            return System.IO.Path.Combine(sOrgCurrentPath, YtpDef.DEFAULT_DB_FILENAME);
        }

        public string EnsureDBPath => ComplementDBPath(DBPath);

        public void ApplyEnvironment() {
            var path = PathUtil.appendPathString(sOrgPath, YoutubeDLPath, FFMpegPath);
            if (path != sOrgPath) {
                Environment.SetEnvironmentVariable("path", path);
            }
            //Environment.CurrentDirectory = EnsureVideoPath;
        }

        public void Serialize() {
            System.IO.StreamWriter sw = null;
            try {

                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                //書き込むファイルを開く（UTF-8 BOM無し）
                sw = new System.IO.StreamWriter(SettingFilePath, false, new System.Text.UTF8Encoding(false));
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

        private static Settings Deserialize() {
            System.IO.StreamReader sr = null;
            object obj = null;

            try {
                //XmlSerializerオブジェクトを作成
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));

                //読み込むファイルを開く
                sr = new System.IO.StreamReader(SettingFilePath, new System.Text.UTF8Encoding(false));

                //XMLファイルから読み込み、逆シリアル化する
                obj = serializer.Deserialize(sr);
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                obj = new Settings();
            }
            finally {
                if (null != sr) {
                    //ファイルを閉じる
                    sr.Close();
                }
            }
            return (Settings)obj;
        }
    }
}
