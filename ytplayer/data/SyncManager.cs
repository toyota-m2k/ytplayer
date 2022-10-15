using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ytplayer.download;
using ytplayer.common;

namespace ytplayer.data {
    public class SyncManager {
        private static readonly LoggerEx logger = new LoggerEx("SYNC");

        //public static Guid guid = Guid.NewGuid();
        private static bool busy = false;

        public interface ISyncProgress {
            void OnMessage(string msg);
            void OnProgress(int current, int total);
            bool IsCancelled { get; }
        }

        class DLEntryComparator : IEqualityComparer<DLEntry> {
            public bool Equals(DLEntry x, DLEntry y) {
                return y != null && x != null && x.KEY == y.KEY;
            }

            public int GetHashCode(DLEntry obj) {
                return obj.KEY.GetHashCode();
            }
        }

        public static async Task SyncFrom(string host, Storage storage, IReportOutput output, ISyncProgress progress) {
            if(busy) {
                return;
            }
            busy = true;

            if(!host.Contains(":")) {
                host += ":3500";
            }

            progress?.OnMessage("Waiting for item list ...");
            using (var client = new HttpClient()) {
                List<DLEntry> list;
                try {
                    output.StandardOutput("Start synchronizing items ...");
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
                            x.Size = c["Size"];
                        });
                    }).Except(storage.DLTable.List, new DLEntryComparator()).ToList(); // Where(v => !storage.DLTable.Contains(v.KEY));

                    int totalCount = list.Count();
                    progress?.OnMessage("Synchronizing List ...");
                    progress?.OnProgress(0, totalCount);

                    for(int i=0; i<totalCount; i++ ) {
                        var c = list[i];
                        try {
                            logger.debug($"id={c.KEY}, name={c.Name}");
                            logger.debug($"saving:{c.VPath}");
                            output.StandardOutput($"synchronizing item: {c.KEY} - {c.Name}");
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
                            logger.debug("SaveFile error.\n" + e.ToString());
                            output.ErrorOutput($"{e.Message}");
                        }
                        progress?.OnProgress(i + 1, totalCount);
                        if (progress?.IsCancelled ?? false) {
                            break;
                        }
                    }
                    progress?.OnMessage("Items Completed.");
                    output.StandardOutput("Complete synchronizing items.");

                    await SyncChaptersFrom(client, host, storage, output, progress);
                }
                catch (Exception e) {
                    logger.error(e);
                    output.ErrorOutput($"Sync Error: {e.Message}");
                }
                finally {
                    busy = false;
                }
            }

            
        }


        private static async Task SyncChaptersFrom(HttpClient client, string host, Storage storage, IReportOutput output, ISyncProgress progress) {
            try {
                progress?.OnMessage("Waiting for chapter list ...");
                output.StandardOutput("Start synchronizing chapters");
                var res = await client.GetStringAsync($"http://{host}/ytplayer/sync.chapter");
                var json = JsonValue.Parse(res) as JsonObject;
                if (json == null) {
                    output.StandardOutput("no chapters to be synchronized.");
                    return;
                }
                var groups = json.GetList("groups");
                int count = 0, totalCount = groups.Count;
                progress?.OnProgress(0, totalCount);
                progress?.OnMessage("Synchronizing Chapters...");
                foreach (var g in groups) {
                    progress?.OnProgress(++count, totalCount);
                    var gg = g as JsonObject;
                    if (gg == null) continue;
                    var owner = gg.GetString("owner");
                    var theirChapters = gg.GetList("chapters");
                    logger.error($"******************\nchapter group for {owner}");

                    if (Utils.IsNullOrEmpty(theirChapters)) {
                        logger.error($"why? their is null: {owner}");
                        continue;
                    }
                    var dl = storage.DLTable.Find(owner);
                    if(dl==null) {
                        // DLEntry は同期済みだから、これはあり得ないはずだが。。。
                        logger.error($"no entry: {owner}");
                        continue;
                    }

                    var myChapters = storage.ChapterTable.GetChapterEntries(owner);
                    if(!Utils.IsNullOrEmpty(myChapters)) {
                        logger.info($"some chapters is found for {owner}");
                        if(myChapters.Where(c=>!string.IsNullOrWhiteSpace(c.Label)).Any()) {
                            // 自分側にラベル付きのChapterが設定されている
                            output.StandardOutput($"Skipped-1 {dl.Name}");
                            logger.info("prefer my chapter list... my chapters have labels.");
                            continue;
                        }
                        if(!theirChapters.Where(c=>!string.IsNullOrWhiteSpace((c as JsonObject).GetString("label"))).Any()) {
                            // 相手側のChapterにラベルが設定されていない
                            output.StandardOutput($"Skipped-2 {dl.Name}");
                            logger.info("prefer my chapter list... their labels don't have labels.");
                            continue;
                        }
                        output.StandardOutput($"Removed: {dl.Name}");
                        logger.info($"OK, delete my chapters.");
                        storage.ChapterTable.Delete(myChapters.ToArray(), true);
                        //foreach (var x in myChapters) { logger.info($"--- deleted: {x.Owner}, {x.Position}, {x.Skip}, {x.Label}"); }
                    }
                    logger.info($"let's import thier chapters.");
                    storage.ChapterTable.AddAll(theirChapters.Select(c => ChapterEntry.Create(owner, (ulong)(c as JsonObject).GetLong("pos"), (c as JsonObject).GetBoolean("skip"), (c as JsonObject).GetString("label"))));
                    //foreach(var x in theirChapters.Select(c => ChapterEntry.Create(owner, (ulong)(c as JsonObject).GetLong("pos"), (c as JsonObject).GetBoolean("skip"), (c as JsonObject).GetString("label")))) {
                    //    logger.info($"+++ append: {x.Owner}, {x.Position}, {x.Skip}, {x.Label}");
                    //}
                    output.StandardOutput($"Imported {theirChapters.Count} chapters: {dl.Name}");
                }

                progress?.OnMessage("Chapters Completed.");
                output.StandardOutput("Complete synchronizing chapters.");
            }
            catch (Exception e) {
                logger.error(e);
            }
        }


        private const bool IsTest = false;

        private static string moveFile(string srcPath, string dstFolder) {
            try {
                if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstFolder)) {
                    return null;
                }
                if (PathUtil.isEqualDirectoryName(System.IO.Path.GetDirectoryName(srcPath), dstFolder)) {
                    return null;
                }
                if (!PathUtil.isFile(srcPath)) {
                    return null;
                }
                var name = System.IO.Path.GetFileName(srcPath);
                var dstPath = System.IO.Path.Combine(dstFolder, name);
                if (srcPath != dstPath) {
                    if (!PathUtil.isFile(dstPath) && !IsTest) {
                        System.IO.File.Move(srcPath, dstPath);
                    }
                    return dstPath;
                }
            } catch (Exception e) {
                Logger.error(e);
            }
            return null;
        }


        public static Task MoveData(string newVideoFolder, string newAudioFolder, 
            IEnumerable<DLEntry> sourceList, Storage storage, IReportOutput output, ISyncProgress progress,
            bool updateSettings) {
            return Task.Run(() => {
                if(sourceList == null) {
                    sourceList = storage.DLTable.List;
                }
                var list = sourceList.Where((v) => v.Status == Status.COMPLETED).ToList();
                int totalCount = list.Count();
                progress?.OnMessage("Moving Files ...");
                progress?.OnProgress(0, totalCount);
                for (var i = 0; i < totalCount; i++) {
                    var c = list[i];
                    if (!string.IsNullOrEmpty(newVideoFolder) && !string.IsNullOrEmpty(c.VPath)) {
                        var newPath = moveFile(c.VPath, newVideoFolder);
                        if (newPath != null) {
                            output.StandardOutput($"video file copied to {newPath}");
                            if (!IsTest) {
                                c.VPath = newPath;
                            }
                        } else {
                            output.ErrorOutput($"video file skppped ({c.VPath})");
                        }
                    }
                    if (!string.IsNullOrEmpty(newAudioFolder) && !string.IsNullOrEmpty(c.APath)) {
                        var newPath = moveFile(c.APath, newAudioFolder);
                        if (newPath != null) {
                            output.StandardOutput($"audio file copied to {newPath}");
                            if (!IsTest) {
                                c.APath = newPath;
                            }
                        } else {
                            output.ErrorOutput($"audio file skppped {c.APath}");
                        }
                    }
                    progress?.OnProgress(i + 1, totalCount);
                    //await Task.Delay(100);
                    if(progress?.IsCancelled??false) {
                        break;
                    }
                }
                progress?.OnMessage("Completed");
                storage.DLTable.Update();
                if(updateSettings) {
                    if(!string.IsNullOrEmpty(newVideoFolder)) {
                        Settings.Instance.VideoPath = newVideoFolder;
                    }
                    if (!string.IsNullOrEmpty(newAudioFolder)) {
                        Settings.Instance.AudioPath = newAudioFolder;
                    }
                    Settings.Instance.Serialize();
                }
            });
        }
    }
}
