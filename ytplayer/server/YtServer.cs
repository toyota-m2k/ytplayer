using common;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ytplayer.common;
using ytplayer.data;
using ytplayer.server.lib;

namespace ytplayer.server {
    public interface IYtListSource {
        IEnumerable<DLEntry> AllEntries { get; }
        IEnumerable<DLEntry> ListedEntries { get; }
        IEnumerable<DLEntry> SelectedEntries { get; }
        bool RegisterUrl(string url);
    }

    public class YtServer {
        private int mPort;
        private HttpServer mServer;
        //private Regex mRegex = new Regex(@"/wfplayer/cmd/(?<cmd>[a-zA-Z]+)(/(?<param>\w*))?");
        private WeakReference<IYtListSource> mStorage;
        private IYtListSource Source => mStorage.GetValue();

        public bool IsListening { get; private set; } = false;

        //public static YtServer CreateInstance(int port = 3500) {
        //    return new YtServer(port);
        //}

        public YtServer(IYtListSource s, int port = 3500) {
            mPort = port;
            mStorage = new WeakReference<IYtListSource>(s);
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

        enum SourceType {
            DB = 0,
            Listed = 1,
            Selected = 2,
        }

        public IEnumerable<DLEntry> SourceOf(int type) {
            switch(type) {
                case (int)SourceType.DB:
                default:
                    return Source?.AllEntries;
                case (int)SourceType.Listed:
                    return Source?.ListedEntries;
                case (int)SourceType.Selected:
                    return Source?.SelectedEntries;
            }
        }

        private void InitRoutes() {
            if (null == Routes) {
                Routes = new List<Route>
                {
                    new Route {
                        Name = "ytplayer sync",
                        UrlRegex = @"/ytplayer/sync",
                        Method="GET",
                        Callable = (HttpRequest request) => {
                            var /*IEnumerable<JsonObject>*/ list =
                            Source?.AllEntries
                                .Where(c=>c.Status==Status.COMPLETED && c.Media.HasVideo() && (int)c.Rating>=(int)Rating.NORMAL)
                                .Select( c=> {
                                        return new JsonObject(new Dictionary<string,JsonValue>() {
                                            {"Id", c.Id },
                                            {"Url", c.Url },
                                            {"Name", c.Name },
                                            {"Filename", Path.GetFileName(c.Path) },
                                            {"Status", (int)c.Status },
                                            {"Rating", (int)c.Rating },
                                            {"Mark", (int)c.Mark },
                                            {"Category", c.Category.Label },
                                            {"Media", (int)c.Media },
                                            {"Date", c.Date.ToFileTimeUtc() },
                                            {"Volume", c.Volume },
                                            {"Desc", c.Desc },
                                            {"Duration", c.DurationInSec },
                                            {"TrimStart", c.TrimStart},
                                            {"TrimEnd", c.TrimEnd}
                                        });
                                    });
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "sync"},
                                {"list", new JsonArray(list) }
                            });
                            return new TextHttpResponse(json.ToString(), "application/json");
                        }
                    },
                    new Route {
                        Name = "ytPlayer list",
                        UrlRegex = @"/ytplayer/list(?:\?.*)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var p = QueryParser.Parse(request.Url);
                            var category = p.GetValue("c");
                            var rating = Convert.ToInt32(p.GetValue("r")??"3");
                            var marks = (p.GetValue("m")??"0").Split('.').Select((v)=>Convert.ToInt32(v)).ToList();
                            var sourceType = Convert.ToInt32(p.GetValue("s")??"0");
                            var search = p.GetValue("t");

                            var source = SourceOf(sourceType);
                            if(null==source) {
                                return HttpBuilder.ServiceUnavailable();
                            }
                            Logger.debug("YtServer: List");
                            var sb = new StringBuilder();
                            sb.Append("{\"cmd\":\"list\", \"list\":[");
                            sb.Append(string.Join(",",
                                source
                                    .Where((c) => c.Status==Status.COMPLETED && c.Media.HasVideo())
                                    .Where((c) => string.IsNullOrEmpty(category) || category=="All" || c.Category.Label == category)
                                    .Where((c) => (int)c.Rating >= rating)
                                    .Where((c) => (marks.Count==1&&marks[0]==0) || marks.IndexOf((int)c.Mark)>=0)
                                    .Where((e) => string.IsNullOrEmpty(search) || (e.Name?.ContainsIgnoreCase(search) ?? false)|| (e.Desc?.ContainsIgnoreCase(search) ?? false))
                                    .Select((v) => $"{{\"id\":\"{v.KEY}\",\"name\":\"{v.Name}\",\"start\":\"{v.TrimStart}\",\"end\":\"{v.TrimEnd}\"}}")));
                            sb.Append("]}");
                            return new TextHttpResponse(sb.ToString(), "application/json");
                         }
                    },
                    new Route {
                        Name = "ytPlayer video",
                        UrlRegex = @"/ytplayer/video?\w+",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var source = Source?.AllEntries;
                            if(null==source) {
                                return HttpBuilder.ServiceUnavailable();
                            }

                            var id = QueryParser.Parse(request.Url)["id"];
                            var entry = source.Where((e)=>e.KEY==id).Single();
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
                        Name = "ytPlayer Categories",
                        UrlRegex = @"/ytplayer/category",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            Logger.debug("YtServer: Category");
                            var list = Settings.Instance.Categories.SerializableList;
                            var sb = new StringBuilder();
                            sb.Append("{\"cmd\":\"category\", \"categories\":[");
                            sb.Append(string.Join(",",
                                list.Select((v)=>$"{{\"label\":\"{v.Label}\",\"color\"=\"{v.Color}\",\"sort\"=\"{v.SortIndex}\"}}")));
                            sb.Append("]}");
                            return new TextHttpResponse(sb.ToString(), "application/json");
                        }
                    },

                    new Route {
                        Name = "ytPlayer Request Download",
                        UrlRegex = @"/ytplayer/register",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var url = QueryParser.Parse(request.Url)["url"];
                            bool accepted = false;
                            if(!string.IsNullOrEmpty(url)) {
                                accepted = Source?.RegisterUrl(url) ?? false;
                            }
                            string result = accepted ? "accepted" : "rejected";
                            return new TextHttpResponse($"{{\"cmd\":\"register\",\"result\":\"{result}\"}}", "application/json");
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
