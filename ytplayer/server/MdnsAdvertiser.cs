using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ytplayer.server {
    /// <summary>
    /// 最小限の mDNS-SD レスポンダ。
    ///
    /// BooTube が _booapi._tcp.local サービスを LAN に広告する目的に特化していて、汎用 mDNS の
    /// 全機能（recursion / cache coherency / unicast response 切替 等）は実装していない。
    /// 主要動作:
    ///   - UDP 5353 / 224.0.0.251 で待ち受け、PTR/SRV/TXT/A 質問に応答する
    ///   - 起動直後に gratuitous announcement、その後 30 秒ごとに再広告
    ///   - Dispose 時に TTL=0 の goodbye パケットを送出
    /// 参考: RFC 6762 (mDNS), RFC 6763 (DNS-SD)
    /// </summary>
    public class MdnsAdvertiser : IDisposable {
        // 全 BooTube プロトコル互換アプリ (winui-secure-archive 等) で共用するサービスタイプ。
        // アプリ識別は TXT の "app=bootube" / "app=archive" 等で行う。詳細は docs/mdns.md 参照。
        public const string ServiceType = "_booapi._tcp";
        // このアプリ識別子 (TXT app=...)。同 LAN に他 BooAPI 互換アプリがいる場合の識別用。
        public const string AppId = "bootube";
        private const int MdnsPort = 5353;
        private const uint TtlAnnouncement = 120;   // RFC 6762 推奨: 2 分
        private const uint TtlHostAddress = 120;
        private const int AnnounceIntervalMs = 30000;
        private static readonly IPAddress MulticastV4 = IPAddress.Parse("224.0.0.251");

        private readonly LoggerEx logger = new LoggerEx("MDNS");

        private readonly object _lock = new object();
        private readonly List<UdpClient> _sockets = new List<UdpClient>();
        private CancellationTokenSource _cts;

        private string _instance;          // 例: "BooTube on DESKTOP-A12B3C"
        private string _hostLocal;         // 例: "DESKTOP-A12B3C.local"
        private ushort _port;
        private List<string> _txt = new List<string>();
        private List<IPAddress> _addresses = new List<IPAddress>();

        public bool IsRunning {
            get { lock (_lock) return _cts != null; }
        }

        public void Start(string instanceName, int port, bool isHttps, string fingerprint) {
            lock (_lock) {
                if (_cts != null) return;

                _instance = SanitizeInstanceName(instanceName);
                _hostLocal = SanitizeHostname(Environment.MachineName) + ".local";
                _port = (ushort)port;
                _txt = new List<string> {
                    "version=2",
                    isHttps ? "https=1" : "https=0",
                    "app=" + AppId,
                    // クライアント側の表示用に「hostname.local」も TXT に乗せる。
                    // (NsdManager は SRV ターゲット名を露出しないので、自前で TXT に入れる)
                    "hostname=" + _hostLocal,
                };
                if (!string.IsNullOrEmpty(fingerprint)) {
                    _txt.Add("fp=" + fingerprint);
                }
                _addresses = GetLocalIPv4Addresses();

                _cts = new CancellationTokenSource();
                BindSockets();
            }

            // 初回広告 (RFC 6762 8.3 に倣い少しずらして 2 回送る)
            SendUnsolicitedAnnouncement();
            Task.Run(async () => {
                try { await Task.Delay(800, _cts.Token); SendUnsolicitedAnnouncement(); } catch { }
            });

            // 周期再広告ループ
            Task.Run(() => AnnounceLoop(_cts.Token));
        }

        public void Dispose() {
            CancellationTokenSource cts;
            List<UdpClient> sockets;
            lock (_lock) {
                cts = _cts;
                if (cts == null) return;
                _cts = null;
                sockets = new List<UdpClient>(_sockets);
                _sockets.Clear();
            }

            try { SendGoodbye(sockets); } catch (Exception e) { logger.error(e); }

            try { cts.Cancel(); } catch { }
            foreach (var s in sockets) {
                try { s.Close(); } catch { }
            }
        }

        // ---- Sockets ----------------------------------------------------------------------

        private void BindSockets() {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (!nic.SupportsMulticast) continue;
                var ipProps = nic.GetIPProperties();
                foreach (var ua in ipProps.UnicastAddresses) {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(ua.Address)) continue;
                    UdpClient sock = null;
                    try {
                        sock = new UdpClient(AddressFamily.InterNetwork);
                        sock.ExclusiveAddressUse = false;
                        sock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        sock.Client.Bind(new IPEndPoint(ua.Address, MdnsPort));
                        sock.JoinMulticastGroup(MulticastV4, ua.Address);
                        sock.MulticastLoopback = false;
                        _sockets.Add(sock);
                        logger.debug($"mDNS bound on {ua.Address}");
                        var s = sock;
                        Task.Run(() => ListenLoop(s, _cts.Token));
                    } catch (Exception e) {
                        logger.error($"mDNS bind on {ua.Address} failed: {e.Message}");
                        try { sock?.Close(); } catch { }
                    }
                }
            }
        }

        private async Task ListenLoop(UdpClient sock, CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                UdpReceiveResult result;
                try {
                    result = await sock.ReceiveAsync().ConfigureAwait(false);
                } catch (ObjectDisposedException) {
                    return;
                } catch (Exception e) {
                    if (ct.IsCancellationRequested) return;
                    logger.error($"mDNS receive: {e.Message}");
                    continue;
                }
                try {
                    HandleQuery(result.Buffer, sock);
                } catch (Exception e) {
                    logger.error($"mDNS handle: {e.Message}");
                }
            }
        }

        private async Task AnnounceLoop(CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested) {
                    await Task.Delay(AnnounceIntervalMs, ct).ConfigureAwait(false);
                    SendUnsolicitedAnnouncement();
                }
            } catch (TaskCanceledException) { }
        }

        // ---- Query handling ---------------------------------------------------------------

        private void HandleQuery(byte[] buffer, UdpClient sock) {
            if (buffer.Length < 12) return;
            int flags = (buffer[2] << 8) | buffer[3];
            if ((flags & 0x8000) != 0) return; // QR=1 → response、無視
            int qdcount = (buffer[4] << 8) | buffer[5];

            int offset = 12;
            bool needRespond = false;
            for (int i = 0; i < qdcount; i++) {
                string qname;
                if (!TryReadName(buffer, ref offset, out qname)) return;
                if (offset + 4 > buffer.Length) return;
                int qtype = (buffer[offset] << 8) | buffer[offset + 1];
                offset += 4; // type + class

                string lower = qname.ToLowerInvariant();
                if (qtype == 12 && lower == ServiceType + ".local.") needRespond = true;             // PTR for service browse
                else if (qtype == 12 && lower == "_services._dns-sd._udp.local.") needRespond = true; // service enumeration
                else if (lower == InstanceFqdn().ToLowerInvariant() && (qtype == 33 || qtype == 16 || qtype == 255)) needRespond = true;
                else if (lower == HostFqdn().ToLowerInvariant() && (qtype == 1 || qtype == 255)) needRespond = true;
            }

            if (needRespond) {
                var packet = BuildResponsePacket(includePTR: true, ttlOverride: null);
                SendOnSocket(sock, packet);
            }
        }

        // ---- Sending ---------------------------------------------------------------------

        private void SendUnsolicitedAnnouncement() {
            var packet = BuildResponsePacket(includePTR: true, ttlOverride: null);
            List<UdpClient> sockets;
            lock (_lock) sockets = new List<UdpClient>(_sockets);
            foreach (var s in sockets) SendOnSocket(s, packet);
        }

        private void SendGoodbye(List<UdpClient> sockets) {
            // TTL=0 で同じ内容を流すと、リスナ側がキャッシュからエントリを除去する。
            var packet = BuildResponsePacket(includePTR: true, ttlOverride: 0);
            foreach (var s in sockets) SendOnSocket(s, packet);
        }

        private void SendOnSocket(UdpClient sock, byte[] packet) {
            try {
                sock.Send(packet, packet.Length, new IPEndPoint(MulticastV4, MdnsPort));
            } catch (ObjectDisposedException) {
                // socket already closed
            } catch (Exception e) {
                logger.error($"mDNS send: {e.Message}");
            }
        }

        // ---- DNS message construction ----------------------------------------------------

        // FQDN 表記で末尾ドット付きに揃える。
        // DNS パケットから TryReadName で取り出したクエリ名も末尾ドット付きなので、比較が一致するようにするため。
        // WriteName は末尾の空ラベルを skip するので、ドット付き/無しどちらでも生成バイト列は同じ。
        private string InstanceFqdn() => _instance + "." + ServiceType + ".local.";
        private string HostFqdn() => _hostLocal + ".";

        /// <summary>
        /// 1パケットに PTR + SRV + TXT + (A 各 IP 分) を全部入れる Multi-RR レスポンス。
        /// クライアント側は PTR/SRV/TXT/A を一度に取れる。
        /// </summary>
        private byte[] BuildResponsePacket(bool includePTR, uint? ttlOverride) {
            var ms = new MemoryStream();
            // Header
            WriteUInt16(ms, 0);           // ID
            WriteUInt16(ms, 0x8400);      // Flags: QR=1, AA=1
            WriteUInt16(ms, 0);           // QDCOUNT
            int answerCount = (includePTR ? 1 : 0) + 1 /*SRV*/ + 1 /*TXT*/ + _addresses.Count;
            WriteUInt16(ms, (ushort)answerCount);
            WriteUInt16(ms, 0);           // NSCOUNT
            WriteUInt16(ms, 0);           // ARCOUNT

            uint ttl = ttlOverride ?? TtlAnnouncement;

            if (includePTR) {
                // PTR: _booapi._tcp.local. -> Instance._booapi._tcp.local.
                WriteRecord(ms, ServiceType + ".local", 12 /*PTR*/, 0x0001 /*IN, no flush bit*/, ttl, () => {
                    WriteName(ms, InstanceFqdn());
                });
            }
            // SRV: Instance -> 0 0 port host
            WriteRecord(ms, InstanceFqdn(), 33 /*SRV*/, 0x8001 /*IN + cache-flush*/, ttl, () => {
                WriteUInt16(ms, 0);            // priority
                WriteUInt16(ms, 0);            // weight
                WriteUInt16(ms, _port);        // port
                WriteName(ms, HostFqdn());
            });
            // TXT: Instance -> "k=v" "k=v" ...
            WriteRecord(ms, InstanceFqdn(), 16 /*TXT*/, 0x8001, ttl, () => {
                if (_txt.Count == 0) {
                    ms.WriteByte(0); // empty TXT must contain a single empty string
                } else {
                    foreach (var kv in _txt) {
                        var bytes = Encoding.UTF8.GetBytes(kv);
                        if (bytes.Length > 255) continue;
                        ms.WriteByte((byte)bytes.Length);
                        ms.Write(bytes, 0, bytes.Length);
                    }
                }
            });
            // A: hostname.local -> each IP
            foreach (var ip in _addresses) {
                WriteRecord(ms, HostFqdn(), 1 /*A*/, 0x8001, TtlHostAddress, () => {
                    var bytes = ip.GetAddressBytes();
                    ms.Write(bytes, 0, bytes.Length);
                });
            }

            return ms.ToArray();
        }

        private static void WriteRecord(MemoryStream ms, string name, ushort type, ushort cls, uint ttl, Action writeData) {
            WriteName(ms, name);
            WriteUInt16(ms, type);
            WriteUInt16(ms, cls);
            WriteUInt32(ms, ttl);
            // RDLENGTH placeholder
            long lenPos = ms.Position;
            WriteUInt16(ms, 0);
            long startPos = ms.Position;
            writeData();
            long endPos = ms.Position;
            int rdlen = (int)(endPos - startPos);
            ms.Position = lenPos;
            WriteUInt16(ms, (ushort)rdlen);
            ms.Position = endPos;
        }

        private static void WriteName(MemoryStream ms, string fqdn) {
            // Name compression は省略 (パケットサイズは増えるが実装が単純で済む)
            foreach (var label in fqdn.Split('.')) {
                if (string.IsNullOrEmpty(label)) continue;
                var bytes = Encoding.UTF8.GetBytes(label);
                if (bytes.Length > 63) {
                    // 63 バイトを超えるラベルは仕様外なので切り詰め
                    Array.Resize(ref bytes, 63);
                }
                ms.WriteByte((byte)bytes.Length);
                ms.Write(bytes, 0, bytes.Length);
            }
            ms.WriteByte(0);
        }

        private static void WriteUInt16(MemoryStream ms, ushort v) {
            ms.WriteByte((byte)(v >> 8));
            ms.WriteByte((byte)(v & 0xFF));
        }

        private static void WriteUInt32(MemoryStream ms, uint v) {
            ms.WriteByte((byte)((v >> 24) & 0xFF));
            ms.WriteByte((byte)((v >> 16) & 0xFF));
            ms.WriteByte((byte)((v >> 8) & 0xFF));
            ms.WriteByte((byte)(v & 0xFF));
        }

        // ---- DNS name parsing -------------------------------------------------------------

        /// <summary>
        /// バッファ中の DNS 形式の名前を読み取る。コンプレッションポインタ (0xC0xx) も追従する。
        /// 末尾に "." を付けた fqdn を返す。失敗時 false。
        /// </summary>
        private static bool TryReadName(byte[] buffer, ref int offset, out string name) {
            var sb = new StringBuilder();
            int cursor = offset;
            int? returnOffset = null; // 圧縮ポインタを辿った場合の元の位置
            int hops = 0;
            while (true) {
                if (cursor >= buffer.Length) { name = null; return false; }
                int len = buffer[cursor];
                if (len == 0) {
                    cursor++;
                    if (returnOffset.HasValue) offset = returnOffset.Value;
                    else offset = cursor;
                    name = sb.ToString() + (sb.Length > 0 ? "." : "");
                    return true;
                }
                if ((len & 0xC0) == 0xC0) {
                    if (cursor + 1 >= buffer.Length) { name = null; return false; }
                    if (++hops > 32) { name = null; return false; } // ループ防止
                    int pointer = ((len & 0x3F) << 8) | buffer[cursor + 1];
                    if (!returnOffset.HasValue) returnOffset = cursor + 2;
                    cursor = pointer;
                    continue;
                }
                if (cursor + 1 + len > buffer.Length) { name = null; return false; }
                if (sb.Length > 0) sb.Append('.');
                sb.Append(Encoding.UTF8.GetString(buffer, cursor + 1, len));
                cursor += 1 + len;
            }
        }

        // ---- Name helpers -----------------------------------------------------------------

        private static string SanitizeInstanceName(string s) {
            if (string.IsNullOrWhiteSpace(s)) return "BooTube";
            return s.Replace('.', '_').Trim();
        }

        private static string SanitizeHostname(string s) {
            if (string.IsNullOrWhiteSpace(s)) return "bootube";
            return s.Replace('.', '-').Replace(' ', '-').Trim();
        }

        private static List<IPAddress> GetLocalIPv4Addresses() {
            var list = new List<IPAddress>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in nic.GetIPProperties().UnicastAddresses) {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(ua.Address)) continue;
                    list.Add(ua.Address);
                }
            }
            return list.Distinct().ToList();
        }
    }
}
