using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ytplayer.data;
using ytplayer.download;
using ytplayer.server.lib;

namespace ytplayer.server {
    public interface IYtListSource:IReportOutput {
        IEnumerable<DLEntry> AllEntries { get; }
        IEnumerable<DLEntry> ListedEntries { get; }
        IEnumerable<DLEntry> SelectedEntries { get; }
        bool RegisterUrl(string url);

        DLEntry CurrentEntry { get; }
        DLEntry GetPrevEntry(string current, bool moveCursor);
        DLEntry GetNextEntry(string current, bool moveCursor);
        DLEntry GetEntry(string id);

        string CurrentId { get; set; }
    
        IEnumerable<ChapterEntry> GetChaptersOf(string id);
        IEnumerable<IGrouping<String, ChapterEntry>> GetChapters();
    }

    public class YtServer : IDisposable {
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
                    mServer = new HttpServer(mPort, Routes, Source);
                }
                if (mServer.Start()) {
                    IsListening = true;
                    Source?.StandardOutput($"BooServer has been started: port={mPort}");
                } else {
                    mServer = null;
                    Source?.ErrorOutput($"BooServer cannot be started: port={mPort}");
                }
            }
        }

        public void Stop() {
            if (mServer != null) {
                mServer.Stop();
                IsListening = false;
                Source?.StandardOutput($"BooServer was stopped: port={mPort}");
            }
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
                        Name = "ytplayer capability",
                        UrlRegex = @"/ytplayer/capability",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var json = new JsonObject(new Dictionary<string, JsonValue> {
                                {"cmd", "capability"},
                                {"serverName", "BooTube"},
                                {"version", 1},
                                {"category", true},
                                {"rating", true},
                                {"mark", true},
                                {"acceptRequest", true},
                                {"hasView", true},
                            });
                            //LoggerEx.debug(json.ToString());
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },
                    new Route {
                        Name = "ytplayer sync chapters",
                        UrlRegex = @"/ytplayer/sync.chapter",
                        Method="GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=sync.chapters");
                            var json = new JsonObject(new Dictionary<string, JsonValue> {
                                {"cmd", "sync.chapters"},
                                {"groups", new JsonArray(Source.GetChapters().Select( c=>
                                    new JsonObject(new Dictionary<string,JsonValue>{
                                    { "owner", c.Key },
                                    { "chapters", new JsonArray( c.Select( e=> {
                                        return new JsonObject(new Dictionary<string, JsonValue> {
                                            { "pos", e.Position },
                                            { "skip", e.Skip },
                                            { "label", e.Label },
                                        });
                                    }))},
                                })))},
                            });
                            //LoggerEx.debug(json.ToString());
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },

                    // SYNC: 端末間同期のために全リストを要求
                    new Route {
                        Name = "ytplayer sync",
                        UrlRegex = @"/ytplayer/sync/?$",
                        Method="GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=sync");
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
                                            {"TrimEnd", c.TrimEnd},
                                            {"Size", c.Size},
                                        });
                                    });
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "sync"},
                                {"list", new JsonArray(list) }
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },
                    // list: プレイリスト要求
                    new Route {
                        /*
                         * リスト要求
                         * list?c=(category)
                         *      &r=(rating)
                         *      &m=(mark(.mark)*)
                         *      &s=(0:all|1:listed|2:selected)
                         *      &t=(free word)
                         *      &d=(last downloaded time)
                         */
                        Name = "ytPlayer list",
                        UrlRegex = @"/ytplayer/list(?:\?.*)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=list");
                            var p = QueryParser.Parse(request.Url);
                            var category = p.GetValue("c");
                            var rating = Convert.ToInt32(p.GetValue("r")??"3");
                            var marks = (p.GetValue("m")??"0").Split('.').Select((v)=>Convert.ToInt32(v)).ToList();
                            var sourceType = Convert.ToInt32(p.GetValue("s")??"0");
                            var search = p.GetValue("t");
                            var date = Convert.ToInt64(p.GetValue("d")??"0");
                            var current = DateTime.UtcNow.ToFileTimeUtc();
                            var source = SourceOf(sourceType);
                            if(null==source) {
                                return HttpBuilder.ServiceUnavailable();
                            }
                            if(sourceType==0) {
                                source = source
                                        .Where(c => c.Status==Status.COMPLETED && c.Media.HasVideo())
                                        .Where(c => string.IsNullOrEmpty(category) || category=="All" || c.Category.Label == category)
                                        .Where(c => (int)c.Rating >= rating)
                                        .Where(c => (marks.Count==1&&marks[0]==0) || marks.IndexOf((int)c.Mark)>=0)
                                        .Where(e => string.IsNullOrEmpty(search) || (e.Name?.ContainsIgnoreCase(search) ?? false)|| (e.Desc?.ContainsIgnoreCase(search) ?? false))
                                        .Where(e => e.LongDate>date);
                            }

                            var list = new JsonArray();
                            if(date==0 || date<Storage.LastUpdated) {
                                list.AddRange(
                                    source
                                    .Select((v) => new JsonObject(new Dictionary<string,JsonValue>() {
                                        {"id", v.KEY },
                                        {"name", v.Name },
                                        {"start", $"{v.TrimStart}"},
                                        {"end", $"{v.TrimEnd}" },
                                        {"volume",$"{v.Volume}" },
                                        {"type", v.BooType},
                                        {"size", v.Size},
                                        {"duration", v.DurationInSec},
                                        //{"rating", $"{(int)v.Rating}" },
                                        //{"mark", $"{(int)v.Mark}" },
                                        //{"category", $"{v.Category.Label}" }
                                    })));
                            }

                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "list"},
                                {"date", $"{current}" },
                                {"list",  list}
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                         }
                    },
                    // CHECK: 前回のプレイリストから変更されたかどうかのチェック
                    new Route {
                        Name = "ytPlayer check update",
                        UrlRegex = @"/ytplayer/check(?:\?\w+)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput($"BooServer: cmd=check");
                            var date = Convert.ToInt64(QueryParser.Parse(request.Url).GetValue("date"));
                            var sb = new StringBuilder();
                            var f = (date>Storage.LastUpdated) ? 1 : 0;
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "check"},
                                {"update", $"{f}" }
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },
                    // VIDEO:ビデオストリーム要求
                    new Route {
                        Name = "ytPlayer video",
                        UrlRegex = @"/ytplayer/video\?\w+",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var source = Source?.AllEntries;
                            if(null==source) {
                                return HttpBuilder.ServiceUnavailable();
                            }

                            var id = QueryParser.Parse(request.Url)["id"];
                            var entry = source.Single(e => e.KEY==id);
                            var range = request.Headers.GetValue("Range");
                            if(null==range) {
                                //Source?.StandardOutput($"BooServer: cmd=video({id})");
                                return new StreamingHttpResponse(request, entry.Path,entry.ContentType, 0, 0);
                            } 
                            
                            var match = RegRange.Match(range);
                            var ms = match.Groups["start"];
                            var me = match.Groups["end"];
                            var start = ms.Success ? Convert.ToInt64(ms.Value) : 0;
                            var end = me.Success ? Convert.ToInt64(me.Value) : 0;
                            return new StreamingHttpResponse(request, entry.Path,entry.ContentType, start, end);
                        }
                    },

                    // chapter: チャプターリスト要求
                    new Route {
                        Name = "ytPlayer chapters",
                        UrlRegex = @"/ytplayer/chapter\?\w+",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var id = QueryParser.Parse(request.Url)["id"];
                            var chapters = Source?.GetChaptersOf(id);
                            if(null==chapters) {
                                Source?.ErrorOutput($"BooServer: cmd=chapter ({id}) no chapters.");
                                return HttpBuilder.ServiceUnavailable();
                            }
                            //Source?.StandardOutput($"BooServer: cmd=chapter ({id})");
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                { "cmd", "chapter"},
                                { "id", $"{id}" },
                                { "chapters", new JsonArray(
                                    chapters.Select(c => new JsonObject(new Dictionary<string,JsonValue>(){
                                            { "position", c.Position },
                                            { "label", c.Label },
                                            { "skip", c.Skip }
                                    }))) },
                                }
                            );
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },

                    // current: カレントアイテムのget/set
                    new Route {
                        Name = "ytPlayer Current Item",
                        UrlRegex = @"/ytplayer/current",
                        Method = "GET",
                        Callable = request => {
                            //Source?.StandardOutput("BooServer: cmd=current (get)");
                            Logger.debug("YtServer: Current (Get)");
                            var id = Source.CurrentId ?? "";
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                { "cmd", "current"},
                                { "id", $"{id}" },
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },

                    new Route {
                        Name = "ytPlayer Current Item",
                        UrlRegex = @"/ytplayer/current",
                        Method = "PUT",
                        Callable = request => {
                            //Source?.StandardOutput("BooServer: cmd=current (put)");
                            Logger.debug("YtServer: Current (Put)");
                            // if(!request.Headers.TryGetValue("Content-Type", out string type)) {
                            //     return HttpBuilder.BadRequest();
                            // }
                            try {
                                var json = new JsonHelper(request.Content);
                                var id = json.GetString("id");
                                Source.CurrentId = id;
                                return new TextHttpResponse(request, json.ToString(), "application/json");
                            } catch(Exception e) {
                                Logger.error(e);
                                return HttpBuilder.InternalServerError();
                            }
                        }
                    },

                    new Route {
                        Name = "ytPlayer Get Reputation",
                        UrlRegex = @"/ytplayer/reputation\?\w+",
                        Method = "GET",
                        Callable = request => {
                            try {
                                var id = QueryParser.Parse(request.Url)["id"];
                                var entry = Source?.AllEntries.SingleOrDefault(e => e.Id == id);
                                if(entry != null) {
                                    var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                        { "cmd", "reputation"},
                                        { "id", $"{id}" },
                                        { "rating", $"{(int)entry.Rating}" },
                                        { "mark", $"{(int)entry.Mark}" },
                                        { "category", $"{entry.Category.Label}" }
                                    });
                                    return new TextHttpResponse(request, json.ToString(), "application/json");
                                }
                            } catch(Exception e) {
                                Logger.error(e);
                            }
                            return HttpBuilder.InternalServerError();
                        }
                    },

                    new Route {
                        Name = "ytPlayer Set Reputation",
                        UrlRegex = @"/ytplayer/reputation",
                        Method = "PUT",
                        Callable = request => {
                            Logger.debug("YtServer: Reputation");
                            // if(!request.Headers.TryGetValue("Content-Type", out _)) {
                            //     return HttpBuilder.BadRequest();
                            // }
                            try {
                                var json = new JsonHelper(request.Content);
                                var id = json.GetString("id");
                                var iRating = json.GetInt("rating",-1);
                                var iMark = json.GetInt("mark", -1);
                                var category = json.GetString("category", null);
                                var entry = Source?.AllEntries.SingleOrDefault(e => e.Id == id);
                                if(entry != null) {
                                    if(iRating>=0) {
                                        entry.Rating = (Rating)iRating;
                                    }
                                    if(iMark>=0) {
                                        entry.Mark = (Mark)iMark;
                                    }
                                    if(category!=null) {
                                        entry.Category = Settings.Instance.Categories.Get(category);
                                    }
                                }
                                var jsonR = new JsonObject(new Dictionary<string, JsonValue>() {
                                    { "cmd", "reputation"},
                                    { "id", $"{id}" },
                                });
                                return new TextHttpResponse(request, jsonR.ToString(), "application/json");
                            } catch(Exception e) {
                                Logger.error(e);
                                return HttpBuilder.InternalServerError();
                            }
                        }
                    },

                    // category：全カテゴリリストの要求
                    new Route {
                        Name = "ytPlayer Categories",
                        UrlRegex = @"/ytplayer/category",
                        Method = "GET",
                        Callable = request => {
                            //Source?.StandardOutput("BooServer: cmd=category");
                            Logger.debug("YtServer: Category");
                            var list = Settings.Instance.Categories.SerializableList;
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "category"},
                                {"categories", new JsonArray(
                                    list.Select((v)=> new JsonObject(new Dictionary<string,JsonValue>(){
                                        {"label",v.Label},
                                        {"color", $"{v.Color}"},
                                        {"sort", $"{v.SortIndex}"}})).ToArray())},
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },
                    // REGISTER: urlの登録/DL要求
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
                            //Source?.StandardOutput($"BooServer: cmd=register {result}:{url}");
                            var json = new JsonObject(new Dictionary<string,JsonValue>(){
                                {"cmd", "register" },
                                {"result", $"{result}" },
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },
                    // SEQ: PlayListを使わない、シーケンシャルアクセス用(next/prev)
                    new Route {
                        Name = "ytPlayer sequential operation.",
                        UrlRegex = @"/ytplayer/seq(?:\?w+)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var p = QueryParser.Parse(request.Url);
                            var item = p.GetValue("item") ?? "current";
                            var org = p.GetValue("org");
                            var move = Convert.ToBoolean(p.GetValue("move")??"true");
                            DLEntry entry = null;
                            switch(item.ToLower()) {
                                default:
                                case "current":
                                    entry = Source.CurrentEntry;
                                    break;
                                case "next":
                                    if(string.IsNullOrEmpty(org)) {
                                        org = Source.CurrentEntry?.Id;
                                    }
                                    if(!string.IsNullOrEmpty(org)) {
                                        entry = Source.GetNextEntry(org,move);
                                    }
                                    break;
                                case "prev":
                                    if(string.IsNullOrEmpty(org)) {
                                        org = Source.CurrentEntry?.Id;
                                    }
                                    if(!string.IsNullOrEmpty(org)) {
                                        entry = Source.GetPrevEntry(org,move);
                                    }
                                    break;
                            }
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "seq"},
                                {"id", entry?.Id ?? null },
                                {"title", entry?.NameToDisplay ?? null},
                                {"start", entry?.TrimStart ?? null },
                                {"end", entry?.TrimEnd?? null },
                                {"volume", entry?.Volume?? null },
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    },

                    //new Route {
                    //    Name = "ytPlayer Web Page",
                    //    UrlRegex = @"/ytplayer/?$",
                    //    Method = "GET",
                    //    Callable = (HttpRequest request) => {
                    //        var wpRoot = Settings.Instance.WebPageRoot;
                    //        if(string.IsNullOrEmpty(wpRoot)) {
                    //            return HttpBuilder.InternalServerError();
                    //        }
                    //        return new FileHttpResponse(request, Path.Combine(wpRoot, "index.html"), "text/html");
                    //    }
                    //},
                    new Route {
                        Name = "ytPlayer Web Page",
                        UrlRegex = @"/*",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            LoggerEx.debug(request.Url);
                            var wpRoot = Settings.Instance.WebPageRoot;
                            if(string.IsNullOrEmpty(wpRoot)) {
                                return HttpBuilder.InternalServerError();
                            }
                            var itemPath = request.Url.Substring(1).Replace('/', '\\').Split('?').First();
                            if(itemPath.IsEmpty()) {
                                itemPath = "index.html";
                            }
                            var ext = Path.GetExtension(itemPath);
                            var type = "text/plain";
                            switch(ext.ToLower()) {
                                case ".css":
                                    type = "text/css";
                                    break;
                                case ".js":
                                    type = "text/javascript";
                                    break;
                                case ".htm":
                                case ".html":
                                    type = "text/html";
                                    break;
                                default:
                                    break;
                            }
                            return new FileHttpResponse(request, Path.Combine(wpRoot, itemPath), type);
                        }
                    },
                };
            }
        }
    }
}
