using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ytplayer.common {
    /**
     * HTTPでJSON形式を利用するためのヘルパークラス
     * Vf* で必要なものだけ実装
     */
    public class JsonHelper {
        public JsonValue json;
        public JsonValue JSON => json;
        public JsonObject JSONObject => json as JsonObject;
        public JsonArray JSONArray => json as JsonArray;
        public JsonValue JSONValue => json as JsonValue;

        #region Composing JSON

        /**
         * 空のJSONインスタンスを作成
         */
        public JsonHelper() {
            json = new JsonObject();
        }



        /**
         * 文字列属性を追加
         */
        public void PutString(string key, string value) {
            JSONObject?.Add(key, value);
        }

        /**
         * JSON文字列を生成
         */
        public override string ToString() {
            return JSONObject?.ToString() ?? "";
        }

        /**
         * JSON文字列を生成して IHttpContent として取得
         */
        //public HttpStringContent AsHttpContent() {
        //    return new HttpStringContent(ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
        //}

        #endregion

        #region Parsing JSON

        /**
         * JSON文字列でJSONオブジェクトを初期化
         */
        public JsonHelper(string jsonString) {
            var val = JsonValue.Parse(jsonString);
            if (null!=val) { 
                SetJsonSource(val);
                return;
            }
            //if (JsonObject.TryParse(jsonString, out var obj))
            //{
            //    json = obj;
            //    return;
            //}
            //if (JsonArray.TryParse(jsonString, out var ary))
            //{
            //    json = ary;
            //    return;
            //}
            json = new JsonObject();
        }

        public JsonHelper(JsonValue srcJson) {
            SetJsonSource(srcJson);
        }

        public void SetJsonSource(JsonValue srcJson) {
            json = srcJson;

            //switch (srcJson.JsonType) {
            //    case JsonType.Object:
            //        json = srcJson;
            //        break;
            //    case JsonType.Array:
            //        json = srcJson;
            //        break;
            //    default:
            //        json = srcJson;
            //        break;
            //}
        }

        /**
         * JSON形式のIHttpContentからJSONオブジェクトを生成
         */
        //public static async Task<JsonHelper> FromHttpContent(IHttpContent c) {
        //    if (null != c) {
        //        try {
        //            return new JsonHelper(await c.ReadAsStringAsync());
        //        }
        //        catch (Exception e) {
        //            CmLog.error(e, "JsonHelper: format error");
        //        }
        //    }
        //    return null;
        //}

        /**
         * JSONオブジェクトに、IHttpContentを設定（内容を置換）
         */
        //public async Task SetHttpContentAsync(IHttpContent c) {
        //    json = JsonObject.Parse(await c.ReadAsStringAsync());
        //}

        private JsonValue GetValue(string name) {
            var v = JSONObject?[name];
            //if (null != v) {
            //    switch (v.JsonType) {
            //        case JsonType.Null:
            //        case JsonType.Array:
            //        case JsonType.Object:
            //            return null;
            //        default:
            //            break;
            //    }
            //}
            return v;
        }

        /**
         * Long型属性値を取得
         */
        public long GetLong(string name) {
            try {
                var v = GetValue(name);
                if (null == v) {
                    return 0;
                }
                switch (v.JsonType) {
                    case JsonType.String:
                        return Convert.ToInt64((string)v);
                    case JsonType.Number:
                        return Convert.ToInt64((long)v);
                    case JsonType.Boolean:
                        return (bool)v ? 1 : 0;
                    default:
                        return 0;
                }
            }
            catch (Exception e) {
                Debug.WriteLine("JsonHelper GetLong() error\n"+ e.StackTrace);
                return 0;
            }
        }

        /**
         * int型属性値を取得
         */
        public int GetInt(string name) {
            return (int)GetLong(name);
        }


        /**
         * String型属性値を取得
         */
        public string GetString(string name) {
            try {
                var v = GetValue(name);
                if (null == v) {
                    return "";
                }
                switch (v.JsonType) {
                    case JsonType.String:
                        return v;
                    case JsonType.Number:
                        return Convert.ToString((long)v);
                    case JsonType.Boolean:
                        return v ? "1" : "0";
                    default:
                        return "";
                }
            }
            catch (Exception e) {
                Debug.WriteLine("JsonHelper GetString() error\n" + e.StackTrace);
                return "";
            }
        }

        /**
         * 文字列配列型の値を取得
         */
        public List<string> GetStringList(string name) {
            try {
                var ja = JSONObject?[name];
                if (null != ja && ja.JsonType==JsonType.Array) {
                    return ((JsonArray)ja).Select(v => (string)v).ToList();
                }
            }
            catch (Exception e) {
                Debug.WriteLine("JsonHelper GetStringList() error\n" + e.StackTrace);
            }
            return null;
        }
        #endregion
    }
}
