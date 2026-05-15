// Copyright (C) 2016 by David Jeske, Barend Erasmus and donated to the public domain

using log4net;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleHttpServer {
    public class HttpProcessor {

        #region Fields

        //private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        private List<Route> Routes = new List<Route>();

        private static readonly ILog log = LogManager.GetLogger(typeof(HttpProcessor));

        #endregion

        #region Constructors

        public HttpProcessor() {
        }

        #endregion

        #region Public Methods
        public void HandleClient(TcpClient tcpClient) {
            Task.Run(() => {
                try {
                    // SSL対応のため、入出力で同じStreamを使う必要がある（SslStreamは1コネクションで1インスタンス）。
                    using (var stream = WrapStream(tcpClient)) {
                        HttpRequest request = GetRequest(stream, stream);

                        // route and handle the request...
                        IHttpResponse response = RouteRequest(stream, stream, request) ?? HttpBuilder.InternalServerError();

                        Console.WriteLine("{0} {1}", response.ToString(), request.Url);
                        response.WriteResponse(stream);
                        stream.Flush();
                    }
                }
                catch (Exception e) {
                    log.Error(e);
                }
                finally {
                    try { tcpClient.Close(); } catch { /* ignore */ }
                }
            });
        }

        // this formats the HTTP response...

        public void AddRoute(Route route) {
            this.Routes.Add(route);
        }

        #endregion

        #region Private Methods

        private static string Readline(Stream stream) {
            var sb = new StringBuilder();
            while (true) {
                int c = stream.ReadByte();
                if (c == -1) {
                    // 接続が閉じられた / タイムアウト到達。EOSを例外で抜けて呼び出し側のtry/catchで扱う。
                    throw new IOException("End of stream while reading line.");
                }
                if (c == '\n') break;
                if (c == '\r') continue;
                sb.Append((char)c);
            }
            return sb.ToString();
        }

        private static void Write(Stream stream, string text) {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// TcpClientから1コネクションぶんの読み書きストリームを取り出す。
        /// SSL派生クラスはここをoverrideしてSslStreamを返す。
        /// </summary>
        protected virtual Stream WrapStream(TcpClient tcpClient) {
            // ハンドシェイク中のslowloris等を避けるための保険
            tcpClient.ReceiveTimeout = 30000;
            tcpClient.SendTimeout = 30000;
            return tcpClient.GetStream();
        }

        protected virtual IHttpResponse RouteRequest(Stream inputStream, Stream outputStream, HttpRequest request) {

            List<Route> routes = this.Routes.Where(x => Regex.Match(request.Url, x.UrlRegex).Success).ToList();

            if (!routes.Any()) {
                return HttpBuilder.NotFound();
            }

            Route route = routes.FirstOrDefault(x => x.Method == request.Method);

            if (route == null) {
                return HttpBuilder.MethodNotAllowed();
            }

            // extract the path if there is one
            var match = Regex.Match(request.Url, route.UrlRegex);
            if (match.Groups.Count > 1) {
                request.Path = match.Groups[1].Value;
            } else {
                request.Path = request.Url;
            }

            // trigger the route handler...
            request.Route = route;
            try {
                return route.Callable(request);
            }
            catch (Exception ex) {
                log.Error(ex);
                return HttpBuilder.InternalServerError();
            }

        }

        private HttpRequest GetRequest(Stream inputStream, Stream outputStream) {
            //Read Request Line
            string request = Readline(inputStream);

            string[] tokens = request.Split(' ');
            if (tokens.Length != 3) {
                throw new Exception("invalid http request line");
            }
            string method = tokens[0].ToUpper();
            string url = tokens[1];
            string protocolVersion = tokens[2];

            //Read Headers
            Dictionary<string, string> headers = new Dictionary<string, string>();
            string line;
            while ((line = Readline(inputStream)) != null) {
                if (line.Equals("")) {
                    break;
                }

                int separator = line.IndexOf(':');
                if (separator == -1) {
                    throw new Exception("invalid http header line: " + line);
                }
                string name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' ')) {
                    pos++;
                }

                string value = line.Substring(pos, line.Length - pos);
                headers.Add(name, value);
            }

            string content = null;
            if (headers.ContainsKey("Content-Length")) {
                int totalBytes = Convert.ToInt32(headers["Content-Length"]);
                int bytesLeft = totalBytes;
                byte[] bytes = new byte[totalBytes];

                while (bytesLeft > 0) {
                    byte[] buffer = new byte[bytesLeft > 1024 ? 1024 : bytesLeft];
                    int n = inputStream.Read(buffer, 0, buffer.Length);
                    buffer.CopyTo(bytes, totalBytes - bytesLeft);

                    bytesLeft -= n;
                }

                content = Encoding.ASCII.GetString(bytes);
            }


            return new HttpRequest() {
                Method = method,
                Url = url,
                Headers = headers,
                Content = content
            };
        }

        #endregion
    }
}
