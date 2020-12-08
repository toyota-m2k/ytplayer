using common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ytplayer.common;

namespace ytplayer {
    public class Settings {
        public WinPlacement Placement { get; set; } = new WinPlacement();
        public string DBPath { get; set; } = "";
        public string YoutubeDLPath { get; set; } = "";
        public string FFMpegPath { get; set; } = "";
        public string VideoPath { get; set; } = "";
        public string MusicPath { get; set; } = "";
        public bool UseWSL { get; set; } = false;

        private const string SETTINGS_FILE = "settings.xml";
        private static Settings sInstance = null;
        private static string sOrgCurrentPath = null;
        private static string sOrgPath = null;

        public static Settings Instance => sInstance;

        public static void Initialize() {
            if (sInstance == null) {
                sOrgCurrentPath = Environment.CurrentDirectory;
                sOrgPath = Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("path"));
                sInstance = Deserialize();
                sInstance.ApplyEnvironment();
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
        public string EnsureMusicPath {
            get {
                if (PathUtil.isDirectory(MusicPath)) {
                    return MusicPath;
                }
                return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            }
        }
        public string EnsureDBPath {
            get {
                if(!string.IsNullOrEmpty(DBPath)) {
                    return DBPath;
                }
                return System.IO.Path.Combine(sOrgCurrentPath, "ytp.db");
            }
        }
        public void ApplyEnvironment() {
            if (!UseWSL) {
                var path = PathUtil.appendPathString(sOrgPath, YoutubeDLPath, FFMpegPath);
                if (path != sOrgPath) {
                    Environment.SetEnvironmentVariable("path", path);
                }
            }
            Environment.CurrentDirectory = EnsureVideoPath;
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
            Object obj = null;

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
