using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using ytplayer.common;

namespace ytplayer {
    //public class MyAddress {
    //    public static void GetIPAddress() {
    //        // ホスト名を取得する
    //        string hostname = Dns.GetHostName();

    //        // ホスト名からIPアドレスを取得する
    //        IPHostEntry ipInfo = Dns.GetHostByName(hostname);
    //        foreach (IPAddress address in ipInfo.AddressList) {
    //            Console.WriteLine(address.ToString());
    //        }
    //    }
    //}

    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application {

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            Settings.Initialize();
        }
    }
}
