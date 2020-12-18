using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

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
            if (string.IsNullOrEmpty(path)) return false;
            return System.IO.Directory.Exists(path);
        }
        public static bool isFile(string path) {
            if (string.IsNullOrEmpty(path)) return false;
            return System.IO.File.Exists(path);
        }

        public static string getDirectoryName(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            return System.IO.Path.GetDirectoryName(path);
        }

        public static System.IO.DirectoryInfo createDirectories(string path) {
            try {
                return System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception e) {
                Logger.error(e);
                return null;
            }
        }

        public static bool safeDeleteFile(string path) {
            if(!isFile(path)) {
                return false;
            }
            try {
                System.IO.File.Delete(path);
                return true;
            } catch(Exception e) {
                Logger.error(e);
                return false;
            }
        }

        public static bool removeDirectories(string path) {
            try {
                System.IO.Directory.Delete(path, true);
                return true;
            }
            catch (Exception e) {
                Logger.error(e);
                return false;
            }
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
            foreach (var ap in appendPaths.Distinct(directoryPathComparer)) {
                var path = normalizeDirname(ap);
                if (!paths.Where((p) => isEqualDirectoryName(path, p)).Any()) {
                    result.Append(";").Append(path);
                }
            }
            return result.ToString();
        }

        //public static string SelectFolder(Window owner, string title, string initialFolder) {

        //    return FolderDialogBuilder.Create()
        //        .title(title)
        //        .initialDirectory(initialFolder)
        //        .GetFilePath(owner);
        //}

        //public static string SelectFileToOpen(Window owner, string title, string initialFolder, string defExt, params (string label, string wc)[] types) {
        //    return OpenFileDialogBuilder.Create()
        //        .title(title)
        //        .initialDirectory(initialFolder)
        //        .defaultExtension(defExt)
        //        .addFileTypes(types)
        //        .GetFilePath(owner);
        //}

        //public static string SelectFileToSave(Window owner, string title, string initialFolder, string defExt, params (string label, string wc)[] types) {
        //    return SaveFileDialogBuilder.Create()
        //        .title(title)
        //        .initialDirectory(initialFolder)
        //        .defaultExtension(defExt)
        //        .addFileTypes(types)
        //        .GetFilePath(owner);
        //}
    }
}