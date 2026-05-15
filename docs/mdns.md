# BooTube ↔ BooDroid 間 mDNS-SD ディスカバリ

このドキュメントは、BooTube (Windows サーバ) を BooDroid (Android クライアント) などのクライアントから自動発見させるために使っている **mDNS Service Discovery (mDNS-SD)** の仕組みと実装を説明します。

実装ファイル：

- BooTube 側 (広告): [`ytplayer/server/MdnsAdvertiser.cs`](../ytplayer/server/MdnsAdvertiser.cs)
- BooTube 側 (起動 / 停止): [`ytplayer/server/YtServer.cs`](../ytplayer/server/YtServer.cs) の `StartMdns` / `Stop`
- BooDroid 側 (発見): [`app/src/main/java/io/github/toyota32k/boodroid/data/BooTubeDiscovery.kt`](https://github.com/toyota-m2k/boodroid)

---

## 1. 背景：なぜ mDNS が必要か

BooTube は「任意のユーザが任意の Windows PC にインストールする」前提のため、固有のドメイン名や固定 IP は持ちません。さらに 1 台のクライアント (BooDroid) が複数の BooTube に接続したり、PC が DHCP でリース更新を受けて IP が変わったりすることもあります。

| 課題 | mDNS が解決するか |
|---|---|
| クライアントは PC のホスト名 (`DESKTOP-A12B3C` 等) を入力したいが、Android の標準 DNS リゾルバは Windows ホスト名を解決できない | ○ サービス発見＋名前解決を一度に済ませられる |
| LAN 上にどんな BooTube が居るか一覧したい | ○ Browse でリスト取得 |
| PC の IP が変わっても追従したい | ○ Resolve を再実行すれば最新 IP が取れる |
| 自己署名 HTTPS の信頼アンカー (証明書 fingerprint) を伝達したい | ○ TXT レコードに fingerprint を載せる |

固定 IP / 静的 hosts / ルータ DHCP 予約 / NetBIOS / WS-Discovery / UPnP… 様々あったうえで mDNS-SD を選んだ理由は **(1) Android 公式 API (`NsdManager`) が直接サポートしていること**、**(2) macOS / iOS との互換性が高いこと**、**(3) 同 LAN 内で完結し外部依存ゼロ** です。

---

## 2. mDNS-SD の基本

### 2.1 マルチキャスト DNS (RFC 6762)

LAN ローカルでだけ通用する小さな DNS。普通の DNS と違うのは：

- 専用ポート: **UDP 5353** （普通の DNS は 53）
- 専用マルチキャストアドレス: **`224.0.0.251`** (IPv4) / `FF02::FB` (IPv6)
- 名前空間: **`.local.`** で終わるドメイン (`bootube.local.` 等)
- 中央サーバ無し: 各ホストが自分のレコードを直接アナウンスする

### 2.2 DNS-SD (RFC 6763)

mDNS の上に乗っかる「サービス発見」の慣習。「LAN 上にどんなプリンタやサーバがいるか」を見つけるための約束事。サービスは次の表記で識別されます：

```
_<service>._<proto>.local.
```

例：
- `_ipp._tcp.local.` — IPP (印刷)
- `_airplay._tcp.local.` — AirPlay
- **`_booapi._tcp.local.`** — BooTube プロトコル互換 REST API （本プロジェクト）

### 2.3 4 種類のレコード

DNS-SD で 1 つのサービスを発見するのに使う DNS レコード：

| 種類 | 内容 | 例 |
|---|---|---|
| **PTR** | サービスタイプ → インスタンス名 | `_booapi._tcp.local.` → `TOYOTA-PC._booapi._tcp.local.` |
| **SRV** | インスタンス名 → ホスト名:ポート | `TOYOTA-PC._booapi._tcp.local.` → `TOYOTA-PC.local.` ポート `3501` |
| **TXT** | インスタンス名 → メタ情報 (key=value のリスト) | `version=2`, `https=1`, `fp=AB:CD:...` |
| **A** | ホスト名 → IPv4 アドレス | `TOYOTA-PC.local.` → `192.168.0.153` |

### 2.4 1 回の発見でのパケットフロー

```
BooDroid                                BooTube
   |                                       |
   | 1. Browse: "Any _booapi._tcp.local.?"  |
   | -------> マルチキャスト UDP 5353 -----> |
   |                                       |
   |    2. Response (PTR/SRV/TXT/A 全部)     |
   | <--------------------------------------|
   |                                       |
   | 3. (必要なら) Resolve:                  |
   |    "Tell me about TOYOTA-PC._booapi.."  |
   | -------------------------------------> |
   |                                       |
   |    4. Response (SRV/TXT/A)             |
   | <--------------------------------------|
   |                                       |
   | 5. 取得した IP:port に HTTPS 接続       |
   | -------------------------------------> |
```

BooTube はステップ 1 を待たずに **gratuitous announcement** （自発的アナウンス）も定期的に送るので、BooDroid 側はリスニングしているだけでも見つけられる場合が多いです。

---

## 3. BooTube が広告する内容

BooTube 起動時、`YtServer.Start()` が `MdnsAdvertiser.Start()` を呼んで以下の DNS レコードをマルチキャストします：

| レコード | 値 |
|---|---|
| Service Type | `_booapi._tcp.local.` |
| Service Instance 名 | `Settings.ServerName` (空欄なら `Environment.MachineName`、例: `TOYOTA-PC`) |
| SRV ターゲット | `<MachineName>.local.` （例: `TOYOTA-PC.local.`） |
| ポート | `Settings.HttpsPort` (HTTPS 有効時) または `Settings.ServerPort` |
| TXT `version` | `2` |
| TXT `https` | `1` (HTTPS 有効) または `0` |
| TXT `fp` | HTTPS 証明書の SHA-256 (例: `AB:CD:12:34:...`)。HTTP のみのときは省略 |
| TXT `app` | `bootube` （他アプリと識別するため） |
| A | NIC の IPv4 アドレス全部 |

### 3.1 アナウンスのタイミング

- **起動直後**: gratuitous announcement を 800ms 間隔で 2 回送出 (RFC 6762 §8.3 推奨)
- **以後**: 30 秒周期で再アナウンス (TTL 切れ前に更新)
- **クエリ受信時**: マッチするクエリ (PTR / SRV / TXT / A) があれば即時応答
- **`Dispose` 時**: TTL=0 の "goodbye" レコードを送出してクライアント側キャッシュを即時失効させる

### 3.2 TXT の `app` キー

BooTube だけでなく、winui-secure-archive など同じ REST API を提供する別アプリも `_booapi._tcp` で広告できる設計です。アプリ識別子は TXT の `app` キーで区別します：

| アプリ | `app` 値 |
|---|---|
| BooTube | `bootube` |
| winui-secure-archive | `archive` (今後実装時) |
| 将来のアプリ | 任意 |

クライアント側はこのキーを見て表示ラベルやアイコンを切り替えるだけで、発見ロジック自体は変更不要です。

---

## 4. BooDroid の発見フロー

`BooTubeDiscovery.kt` が Android 標準の `NsdManager` を使って発見します。

### 4.1 ライフサイクル

`HostAddressDialog` を開くたびに `start()` し、閉じるとき `stop()` する短命運用です：

```kotlin
class HostAddressDialogViewModel {
    private var discovery: BooTubeDiscovery? = null
    fun startDiscovery() {
        discovery = BooTubeDiscovery(ctx).apply { start() }
    }
    fun stopDiscovery() {
        discovery?.stop()
        discovery = null
    }
}
```

`start()` は内部で `WifiManager.MulticastLock` を取得します（電力効率優先の Wi-Fi スリープでマルチキャストパケットがドロップされるのを防ぐ）。

### 4.2 Browse → Resolve の API 切替

Android 14 (API 34) で `resolveService` / `NsdServiceInfo.host` が deprecate されたので、両 API パスを持っています。

| API レベル | Browse | Resolve | アドレス取得 |
|---|---|---|---|
| 26〜33 | `NsdManager.discoverServices` | `NsdManager.resolveService` | `NsdServiceInfo.host` (deprecated) |
| 34+ | `NsdManager.discoverServices` | `NsdManager.registerServiceInfoCallback` | `NsdServiceInfo.hostAddresses` (List<InetAddress>) |

新 API は「IP/port が変化したら継続的にコールバックが来る」点で旧 API より優れているので、API 34+ 端末では DHCP 変動に自動追従できます。旧 API は同時 1 件しか resolve できないため `FAILURE_ALREADY_ACTIVE` 時にバックオフ・リトライしています。

### 4.3 `HostAddressEntity` への保存

発見・解決したサービスは `HostAddressEntity` として永続化されます：

```kotlin
data class HostAddressEntity(
    val name: String,                  // 表示名
    val address: String,               // "192.168.0.153:3501"
    val serviceName: String? = null,   // mDNS Instance 名 (再解決のキー)
    val fingerprint: String? = null,   // TXT fp= の値
    val httpsOnly: Boolean = false,    // TXT https=1 → true
)
```

接続時にもう一度 mDNS で `serviceName` を resolve すれば、PC の IP が変わっていても新しい IP に追従できます (`BooTubeDiscovery.resolveOnce`)。

---

## 5. TLS との連携 (fingerprint TOFU)

mDNS と HTTPS 自己署名証明書の信頼確立は別レイヤーですが、TXT レコードを介して **「発見と同時に信頼アンカーも取得する」** Trust-On-First-Use (TOFU) 方式で繋がっています。

```
[BooTube]  証明書を生成し SHA-256 fingerprint を計算
              ↓
            mDNS TXT に fp=AB:CD:... として乗せる
              ↓
[BooDroid] Discover で TXT を読み HostAddressEntity.fingerprint に保存
              ↓
            HTTPS 接続時 CompositeTrustManager が照合:
              ・システム CA で検証 → ダメなら
              ・登録済 fingerprint と一致 → 許可
              ・どちらも不可 → 拒否 (中間者検出)
```

`CompositeTrustManager` は OkHttp (REST API) と ExoPlayer (動画ストリーミング) の両方が **同じ `OkHttpClient`** を共有して使うので、信頼判定ロジックが 1 箇所で完結します。`HttpsURLConnection.setDefaultSSLSocketFactory` のようなグローバル設定は使っていません。

---

## 6. 複数インスタンス / 複数アプリへの対応

### 6.1 Q1: 同一 PC で BooTube を複数起動できるか

**できます。ただし各インスタンスで Settings の Server name を別々に設定してください。**

- ポートは Settings で別 (例: 3500 / 3502) にする必要があります (TCP listener が衝突するため)
- Service Instance 名 (`Settings.ServerName`) も別 (例: `Personal-BooTube` / `Family-BooTube`) にする必要があります
- 既定値の `Environment.MachineName` のまま 2 つ起動すると mDNS 上で衝突し、最後にアナウンスした側だけが見える/両方出るが見分けつかない、という挙動になります (RFC 6762 §8 の probe-and-resolve 自動リネームは現状未実装)

### 6.2 Q2: 同一 PC で BooTube + winui-secure-archive 等を共存できるか

**できます。前提：**

1. ポートが衝突していない (Settings で異なる値を設定)
2. Service Instance 名が衝突していない (各アプリのデフォルトで自然に異なる prefix にしておけば OK)

サービスタイプは両アプリ共通の `_booapi._tcp` を使い、TXT の `app` キーで区別します。

### 6.3 Q3: アプリごとにサービスタイプを分けるか

**分けない方針 (`_booapi._tcp` 共通)** を採用しています。理由：

- アプリが増えても BooDroid 側のディスカバリ列挙ロジックを変更しなくて済む (1 タイプを discover するだけで全アプリ拾える)
- 表示の区別は TXT `app` キーで賄える
- 「BooTube」というアプリ固有名がプロトコル名 (`_bootube._tcp`) に残ってしまう違和感を回避

---

## 7. デバッグ・トラブルシューティング

### 7.1 macOS / Linux

```bash
# サービス一覧 (Browse)
dns-sd -B _booapi._tcp local
# 期待出力:
# Add  2  12 local. _booapi._tcp. TOYOTA-PC

# 特定インスタンスの詳細 (Resolve)
dns-sd -L "TOYOTA-PC" _booapi._tcp local
# 期待出力:
# TOYOTA-PC._booapi._tcp.local. can be reached at TOYOTA-PC.local.:3501 (interface 12)
#  version=2 https=1 fp=AB:CD:... app=bootube
```

### 7.2 Windows

[Bonjour Browser](http://hobbyistsoftware.com/bonjourbrowser) などの GUI ツールが手軽です。あるいは PowerShell で：

```powershell
Resolve-DnsName -Name TOYOTA-PC.local -Type A
```

### 7.3 Android

```bash
adb logcat -s NsdManager Discovery
```

`Discovery` は BooDroid 側の `UtLog` タグで、`onServiceFound` / `Resolved` のメッセージが流れるはずです。

### 7.4 「LAN にいるはずなのに発見されない」

| 症状 | 原因 | 対処 |
|---|---|---|
| `dns-sd -B` で 1 度も出ない | Windows Defender Firewall が UDP 5353 をブロック | BooTube アプリへの inbound UDP 5353 を許可 |
| ゲスト Wi-Fi 接続時だけ見つからない | アクセスポイントの client isolation が有効 | 通常 Wi-Fi に繋ぎ替える、AP 設定変更 |
| 発見できるが接続失敗 (`SSLPeerUnverifiedException`) | 証明書再生成後に古い fingerprint が残っている | BooDroid のホスト一覧から古いエントリを削除して Discover し直す |
| `_bootube._tcp` で広告する古い BooTube が見えない | サービスタイプを `_booapi._tcp` にリネームした影響 | BooTube も新バージョンに揃える (互換性破壊済み) |

---

## 8. 既知の制限

- **mDNS レスポンダ実装** (`MdnsAdvertiser.cs`):
  - DNS name compression 未実装 (パケットサイズが少し大きいが LAN なら無視できる)
  - IPv4 のみ (IPv6 リンクローカルは未対応)
  - Probe-and-resolve (RFC 6762 §8) 未実装。同一名衝突は運用回避
  - 単一 TXT 文字列の長さ 255 バイト超は切り捨て

- **TLS の信頼判定**:
  - `CompositeTrustManager` はシステム CA → fingerprint pin の順で評価
  - 自己署名 BooTube 以外の自己署名サーバは fingerprint pin に登録されていなければ拒否
  - クライアント側の HTTPS 経路は `OkHttpClient` 1 個で REST API も ExoPlayer も統一 (`HttpsURLConnection` のグローバル設定には触れない)

- **想定環境**: 家庭内 LAN (一般的な Wi-Fi ルータ配下)。
  業務 LAN 環境で使う場合はマルチキャスト挙動・セグメンテーション・セキュリティ要件を再検討してください。
