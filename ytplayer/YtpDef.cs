namespace ytplayer {
    public static class YtpDef {
        public const string YTDLP_EXE = "yt-dlp.exe";
        public const string DB_EXT = "ytpdb";
        public const string DEFAULT_DBNAME = "default";
        public static string DEFAULT_DB_FILENAME => $"{DEFAULT_DBNAME}.{DB_EXT}";
    }
}
