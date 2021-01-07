using common;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ytplayer.data;
using ytplayer.server.lib;

namespace ytplayer.server {
    public class YtServer {
        private int mPort;
        private HttpServer mServer;
        //private Regex mRegex = new Regex(@"/wfplayer/cmd/(?<cmd>[a-zA-Z]+)(/(?<param>\w*))?");
        private WeakReference<Storage> mStorage;
        private Storage Storage => mStorage.GetValue();

        public bool IsListening { get; private set; } = false;

        //public static YtServer CreateInstance(int port = 3500) {
        //    return new YtServer(port);
        //}

        public YtServer(Storage s, int port = 3500) {
            mPort = port;
            mStorage = new WeakReference<Storage>(s);
            InitRoutes();
        }

        public void Start() {
            if (!IsListening) {
                if (null == mServer) {
                    mServer = new HttpServer(mPort, Routes);
                }
                mServer.Start();
                IsListening = true;
            }
        }

        public void Stop() {
            mServer?.Stop();
            IsListening = false;
        }

        public void Dispose() {
            Stop();
            mServer = null;
        }

        public List<Route> Routes { get; set; } = null;

        public Regex RegRange = new Regex(@"bytes=(?<start>\d+)(?:-(?<end>\d+))?");

        private void InitRoutes() {
            if (null == Routes) {
                Routes = new List<Route>
                {
                    new Route {
                        Name = "ytPlayer list",
                        UrlRegex = @"/ytplayer/list(?:\?.*)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var p = QueryParser.Parse(request.Url);
                            var category = p.GetValue("c");
                            var rating = Convert.ToInt32(p.GetValue("r")??"0");
                            var mark = Convert.ToInt32(p.GetValue("m")??"0");
                            var search = p.GetValue("s");

                            var storage = Storage;
                            if(null==storage) {
                                return HttpBuilder.ServiceUnavailable();
                            }
                            var sb = new StringBuilder();
                            sb.Append("{\"cmd\":\"list\", \"list\":[");
                            sb.Append(string.Join(",",
                                storage.DLTable.List
                                    .Where((c) => string.IsNullOrEmpty(category) || c.Category.Label == category)
                                    .Where((c) => (int)c.Rating > rating)
                                    .Where((c) => mark==0 || (int)c.Mark == mark)
                                    .Where((e) => string.IsNullOrEmpty(search) || (e.Name?.ContainsIgnoreCase(search) ?? false)|| (e.Desc?.ContainsIgnoreCase(search) ?? false))
                                    .Where((c) => ((int)c.Media & (int)MediaFlag.VIDEO) == (int)MediaFlag.VIDEO)
                                    .Select((v) => $"{{\"id\":\"{v.KEY}\",\"name\":\"{v.Name}\"}}")));
                            sb.Append("]}");
                            return new TextHttpResponse(sb.ToString(), "application/json");
                         }
                    },
                    new Route {
                        Name = "ytPlayer video",
                        UrlRegex = @"/ytplayer/video?\w+",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var id = QueryParser.Parse(request.Url)["id"];
                            var entry = Storage.DLTable.List.Where((e)=>e.KEY==id).Single();

                            var range = request.Headers.GetValue("Range");
                            if(null==range) {
                                return new StreamingHttpResponse(entry.VPath,"video/mp4", 0, 0);
                            } else {
                                var m = RegRange.Match(range);
                                var s = m.Groups["start"];
                                var e = m.Groups["end"];
                                var start = s.Success ? Convert.ToInt64(s.Value) : 0;
                                var end = e.Success ? Convert.ToInt64(s.Value) : 0;
                                return new StreamingHttpResponse(entry.VPath,"video/mp4", start, end);
                            }
                        }
                    },
                    new Route {
                        Name = "ytPlayer TestPage",
                        UrlRegex = @"/ytplayer/test",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            return new TextHttpResponse(
@"<!doctype html><html>
<head><title>Test Page</title></head>
<body>
Test Page.<br>
<video id='videoPlayer' controls>
  <source src='http://localhost:3500/ytplayer/video?id=1B4pZBmI_gU' type='video/mp4'/>
</video>
</body></html>"                 ,"text/html");
                        }
                    },
                };
            }
        }
    }
}
