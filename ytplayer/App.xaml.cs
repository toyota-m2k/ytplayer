using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ytplayer {
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application {
        bool isEqualDirectoryName(string p1, string p2) {
            string normalize(string p) {
                var r = p.Trim().Replace('/', '\\');
                if (!r.EndsWith("\\")) {
                    r += "\\";
                }
                return r;
            }
            return normalize(p1).Equals(p2, StringComparison.CurrentCultureIgnoreCase);
        }

        private void initializePathEnv() {
            var path = Environment.ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("path"));
            var paths = path.Split(';');
            foreach (var p in paths) {
                Debug.WriteLine(p);
            }
            var ytdpath = "d:\\bin\\tools\\";
            if (!paths.Where((p) => isEqualDirectoryName(ytdpath, p)).Any()) {
                path = path += (";" + ytdpath);
                Environment.SetEnvironmentVariable("path", path);
            }
        }


        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            initializePathEnv();
        }
    }
}
