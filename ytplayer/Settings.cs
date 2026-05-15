using io.github.toyota32k.toolkit.utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ytplayer.data;
using ytplayer.download;
using static io.github.toyota32k.toolkit.utils.PathUtil;

namespace ytplayer {
    public class Settings {
        static readonly string AppName = "BooTube";

        public WinPlacement Placement { get; set; } = new WinPlacement();
        public WinPlacement PlayerPlacement { get; set; } = new WinPlacement();
        public WinPlacement BrowserPlacement { get; set; } = new WinPlacement();

        public string DBPath { get; set; } = "";
        public string YoutubeDLPath { get; set; } = "";
        public string FFMpegPath { get; set; } = "";
        public string VideoPath { get; set; } = "";
        public string AudioPath { get; set; } = "";
        public string WorkPath { get; set; } = "";          // 無音抽出用Wavファイル作成先
        public string WebPageRoot { get; set; } = "";
        public string SyncPeer { get; set; } = "";
        // Sync 接続時に HTTPS を使うか (mDNS discovery でピアを選択した場合は自動で上書きされる)。
        public bool SyncUseHttps { get; set; } = false;
        // HTTPS Sync 時の期待サーバ証明書 SHA-256 フィンガープリント (pin-to-fingerprint)。
        public string SyncPeerFingerprint { get; set; } = "";
        // mDNS/NSD でクライアントから見えるサーバ名。空ならマシン名を使う。
        public string ServerName { get; set; } = AppName;

        [System.Xml.Serialization.XmlIgnore]
        public string EnsureServerName =>
            string.IsNullOrWhiteSpace(ServerName) ? Environment.MachineName : ServerName;

        // HTTPS / TLS
        public bool EnableHttp { get; set; } = false;
        public bool EnableHttps { get; set; } = false;
        public int HttpsPort { get; set; } = 3501;
        public int HttpPort { get; set; } = 3500;
        public string PfxPath { get; set; } = "";
        // PFXパスワードはDPAPI(CurrentUser)で暗号化してBase64で保存する。生のパスワードは settings.xml に書き出さない。
        public string PfxPasswordEncrypted { get; set; } = "";
        // mDNSのAdvertizing を有効化するか？
        public bool EnableMdnAdvertizing { get; set; } = true;
        [System.Xml.Serialization.XmlIgnore]
        public bool ServerEnabled => EnableHttp || EnableHttps;

        //[System.Xml.Serialization.XmlIgnore]
        //public bool HttpsOnly => EnableHttps && !EnableHttps;

        [System.Xml.Serialization.XmlIgnore]
        public string PfxPassword {
            get {
                if (string.IsNullOrEmpty(PfxPasswordEncrypted)) return "";
                try {
                    var bytes = Convert.FromBase64String(PfxPasswordEncrypted);
                    var plain = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plain);
                } catch (Exception e) {
                    Debug.WriteLine(e);
                    return "";
                }
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    PfxPasswordEncrypted = "";
                    return;
                }
                try {
                    var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
                    PfxPasswordEncrypted = Convert.ToBase64String(bytes);
                } catch (Exception e) {
                    Debug.WriteLine(e);
                    PfxPasswordEncrypted = "";
                }
            }
        }

        public DeterminationList Determinations { get; set; } = new DeterminationList();
        public CategoryList Categories { get; set; } = new CategoryList();
        public SearchHistory SearchHistories { get; set; } = new SearchHistory();
        public bool[] Ratings { get; set; }
        public string LastPlayingUrl { get; set; } = null;
        public double LastPlayingPos { get; set; } = 0;
        public bool RestartOnLoaded { get; set; } = false;
        public Sorter SortInfo { get; set; } = new Sorter();
        public bool AcceptList { get; set; } = false;
        // exports
        public string ExportPath { get; set; } = "";
        public bool ExportOnlyAudio { get; set; } = false;
        public bool ExportSplit { get; set; } = false;

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

        public static string appendPathString(string orgPath, params string[] appendPaths) {
            bool modified = false;
            var source = orgPath.Split(';').Where(it=>!string.IsNullOrWhiteSpace(it)).ToList();
            foreach (string item in appendPaths.Distinct(directoryPathComparer)) {
                string path = normalizeDirname(item);
                if (!source.Where((string p) => isEqualDirectoryName(path, p)).Any()) {
                    source.Add(path);
                    modified = true;
                }
            }
            if(modified) {
                return string.Join(";", source);
            } else {
                return orgPath;
            }
        }


        public void ApplyEnvironment() {
            var path = appendPathString(sOrgPath, YoutubeDLPath, FFMpegPath);
            if (path != sOrgPath) {
                Environment.SetEnvironmentVariable("path", path);
            }
            //Environment.CurrentDirectory = EnsureVideoPath;
        }

        public void Serialize() {
            System.IO.StreamWriter sw = null;
            try {

                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
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
