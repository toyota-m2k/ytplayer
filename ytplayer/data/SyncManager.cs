using common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ytplayer.common;
using ytplayer.download;

namespace ytplayer.data {
    public class SyncManager {
        public static Guid guid = Guid.NewGuid();
        public static bool busy = false;

        class DLEntryComparator : IEqualityComparer<DLEntry> {
            public bool Equals(DLEntry x, DLEntry y) {
                return x.KEY == y.KEY;
            }

            public int GetHashCode(DLEntry obj) {
                return obj.KEY.GetHashCode();
            }
        }

        public static async Task SyncFrom(string host, Storage storage, IReportOutput output) {
            if(busy) {
                return;
            }
            busy = true;

            if(!host.Contains(":")) {
                host += ":3500";
            }

            using (var client = new HttpClient()) {
                IEnumerable<DLEntry> list;
                try {
                    output.StandardOutput("Start synchronizing...");
                    var res = await client.GetStringAsync($"http://{host}/ytplayer/sync");
                    var json = JsonObject.Parse(res);
                    list = (json["list"] as JsonArray)?.Select(v => {
                        var c = v as JsonObject;
                        return DLEntry.Create(c["Id"], c["Url"]).Apply(x => {
                            x.Name = c["Name"];
                            x.Status = (Status)(int)c["Status"];
                            x.Rating = (Rating)(int)c["Rating"];
                            x.Mark = (Mark)(int)c["Mark"];
                            x.Category = Settings.Instance.Categories.Get(c["Category"]);
                            x.Media = MediaFlag.VIDEO;
                            x.Date = DateTime.FromFileTimeUtc(c["Date"]);
                            x.Volume = c["Volume"];
                            x.Desc = c["Desc"];
                            x.DurationInSec = c["Duration"];
                            x.TrimStart = c["TrimStart"];
                            x.TrimEnd = c["TrimEnd"];
                            x.VPath = System.IO.Path.Combine(Settings.Instance.VideoPath, c["Filename"]);
                        });
                    }).Except(storage.DLTable.List, new DLEntryComparator()); // Where(v => !storage.DLTable.Contains(v.KEY));

                    foreach (var c in list) {
                        try {
                            Debug.WriteLine($"Sync:id={c.KEY}, name={c.Name}");
                            Debug.WriteLine($"Sync:  saving:{c.VPath}");
                            output.StandardOutput($"synchronizing :{c.KEY} - {c.Name}");
                            if (!PathUtil.isFile(c.VPath)) {
                                using (var inStream = await client.GetStreamAsync($"http://{host}/ytplayer/video?id={c.KEY}"))
                                using (var outStream = new FileStream(c.VPath, FileMode.Create)) {
                                    await inStream.CopyToAsync(outStream);
                                    await outStream.FlushAsync();
                                }
                            }
                            storage.DLTable.Add(c);
                        }
                        catch (Exception e) {
                            Debug.WriteLine("Sync: SaveFile error.\n" + e.ToString());
                            output.ErrorOutput($"{e.Message}");
                        }
                    }
                    output.StandardOutput("Complete synchronizing...");
                }
                catch (Exception e) {
                    Debug.WriteLine("Sync: GetList error.\n" + e.ToString());
                    output.ErrorOutput($"Sync Error: {e.Message}");
                }
                finally {
                    busy = false;
                }
            }
        }
    }
}
