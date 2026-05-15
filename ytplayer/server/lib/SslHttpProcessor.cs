using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SimpleHttpServer {
    /// <summary>
    /// SslStreamで包んだTLSサーバ用Processor。
    /// AuthenticateAsServerはコネクションごとに1回だけ実行する必要があるため、
    /// HttpProcessor.WrapStreamをoverrideして、入出力で同じSslStreamインスタンスを使い回す。
    /// </summary>
    public class SslHttpProcessor : HttpProcessor {
        private readonly X509Certificate2 _cert;

        public SslHttpProcessor(X509Certificate2 cert) {
            _cert = cert;
        }

        protected override Stream WrapStream(TcpClient tcpClient) {
            tcpClient.ReceiveTimeout = 30000;
            tcpClient.SendTimeout = 30000;

            var ssl = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
            // Tls13はFW4.8でも定数として存在するが、実ネゴはOSのSChannel依存(Win11/Server2022以降)。
            // Tls12のみネゴれる環境ではTls12にフォールバックする。
            ssl.AuthenticateAsServer(
                _cert,
                clientCertificateRequired: false,
                enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                checkCertificateRevocation: false);
            return ssl;
        }
    }
}
