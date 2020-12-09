using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.common {
    public class PathUtil {
        public static string normalizeDirname(string p) {
            if (p == null) return "";
            var r = p.Trim().Replace('/', '\\');
            if (r.EndsWith("\\")) {
                r = r.Substring(0, r.Length - 1);
            }
            return r;
        }
        public static bool isEqualDirectoryName(string p1, string p2) {
            return normalizeDirname(p1).Equals(normalizeDirname(p2), StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool isExists(string path) {
            return System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
        }
        public static bool isDirectory(string path) {
            return isExists(path) && System.IO.Directory.Exists(path);
        }
        public static bool isFile(string path) {
            return isExists(path);
        }

        public class DirectoryPathComparer : IEqualityComparer<string> {
            public bool Equals(string x, string y) {
                return isEqualDirectoryName(x, y);
            }

            public int GetHashCode(string obj) {
                return normalizeDirname(obj).GetHashCode();
            }
        }

        static Lazy<DirectoryPathComparer> lazyDirectoryPathComparer = new Lazy<DirectoryPathComparer>(() => {
            return new DirectoryPathComparer();
        });
        public static DirectoryPathComparer directoryPathComparer => lazyDirectoryPathComparer.Value;

        public static string appendPathString(string orgPath, params string[] appendPaths) {
            var result = new StringBuilder(orgPath);
            var paths = orgPath.Split(';');
            foreach(var ap in appendPaths.Distinct(directoryPathComparer)) {
                var path = normalizeDirname(ap);
                if (!paths.Where((p) => isEqualDirectoryName(path, p)).Any()) {
                    result.Append(";").Append(path);
                }
            }
            return result.ToString();
        }
    }
}
