using io.github.toyota32k.toolkit.utils;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ytplayer.common;

namespace ytplayer.dialog {
    /// <summary>
    /// BooTube サーバへのペアリング情報 (host:port + fingerprint) を QR で表示するダイアログ。
    ///
    /// QR の中身は `bootube://<host>:<port>?fp=<fp>&name=<name>&svc=<svc>&https=<0|1>` 形式。
    /// BooDroid 側では bootube:// スキームの intent-filter で受け取って HostAddressEntity に変換する。
    /// </summary>
    public partial class PairingQrDialog : Window {
        private readonly int _port;
        private readonly bool _isHttps;
        private readonly string _serverName;
        private readonly string _fingerprint;
        private readonly List<string> _hostCandidates;

        public PairingQrDialog(string serverName, int port, bool isHttps, string fingerprint) {
            _serverName = serverName ?? "BooTube";
            _port = port;
            _isHttps = isHttps;
            _fingerprint = fingerprint ?? "";
            _hostCandidates = BuildHostCandidates();

            InitializeComponent();

            foreach (var h in _hostCandidates) HostCombo.Items.Add(h);
            if (HostCombo.Items.Count > 0) HostCombo.SelectedIndex = 0;
        }

        /// <summary>
        /// 利用可能なホスト識別子を優先度順に列挙: マシン名.local → 各 IPv4 アドレス。
        /// マシン名.local は同一 LAN で mDNS が動く環境では便利だが、Android 標準では DNS 解決
        /// できないので、LAN 内 IPv4 を主候補とする。
        /// </summary>
        private List<string> BuildHostCandidates() {
            var list = new List<string>();
            try {
                foreach (var ip in CertificateGenerator.GetLocalIPv4Addresses()) {
                    list.Add(ip.ToString());
                }
            } catch (Exception e) { LoggerEx.error(e); }
            // フォールバック / ホスト名ベースの候補
            try {
                var name = Environment.MachineName;
                if (!string.IsNullOrEmpty(name)) list.Add(name + ".local");
            } catch { }
            if (list.Count == 0) list.Add("localhost");
            return list;
        }

        private void OnHostChanged(object sender, SelectionChangedEventArgs e) {
            UpdateQr();
        }

        private void UpdateQr() {
            var host = HostCombo.SelectedItem as string ?? _hostCandidates.FirstOrDefault();
            if (host == null) return;

            // bootube://<host>:<port>?fp=...&name=...&svc=...&https=...
            var sb = new StringBuilder();
            sb.Append("bootube://");
            sb.Append(host);
            sb.Append(":").Append(_port);
            sb.Append("?fp=").Append(Uri.EscapeDataString(_fingerprint));
            sb.Append("&name=").Append(Uri.EscapeDataString(_serverName));
            sb.Append("&svc=").Append(Uri.EscapeDataString(_serverName));
            sb.Append("&https=").Append(_isHttps ? "1" : "0");
            var uri = sb.ToString();

            UriText.Text = uri;
            QrImage.Source = GenerateQr(uri);
        }

        private static BitmapImage GenerateQr(string content) {
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q)) {
                var pngQr = new PngByteQRCode(data);
                var pngBytes = pngQr.GetGraphic(20); // 20 px / module
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(pngBytes)) {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                }
                bmp.Freeze();
                return bmp;
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        /// <summary>
        /// Settings から現状の BooTube 設定を読み出して PairingQrDialog を構築する。
        /// HTTPS が有効でない、または PFX が読めない場合は null を返す（呼び出し側でメッセージ等を出す）。
        /// </summary>
        public static PairingQrDialog CreateFromSettings(Window owner) {
            var s = Settings.Instance;
            if (!s.EnableServer) return null;

            int port = s.EnableHttps ? s.HttpsPort : s.ServerPort;
            string fp = "";
            if (s.EnableHttps) {
                try {
                    using (var cert = new X509Certificate2(
                            s.PfxPath, s.PfxPassword,
                            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet)) {
                        fp = CertificateGenerator.ComputeSha256Fingerprint(cert);
                    }
                } catch (Exception e) { LoggerEx.error(e); }
            }

            var dlg = new PairingQrDialog(s.EnsureServerName, port, s.EnableHttps, fp) { Owner = owner };
            return dlg;
        }
    }
}
