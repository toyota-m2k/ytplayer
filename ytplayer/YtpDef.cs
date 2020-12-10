using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer {
    public static class YtpDef {
        public const string DB_EXT = "ytpdb";
        public const string DEFAULT_DBNAME = "default";
        public static string DEFAULT_DB_FILENAME => $"{DEFAULT_DBNAME}.{DB_EXT}";
    }
}
