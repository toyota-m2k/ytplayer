using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;

namespace ytplayer.common {
    public static class JsonExt {
        public static JsonValue SafeGetValue(this JsonObject json, string name) {
            try {
                return json[name];
            }
            catch (Exception) {
                return null;
            }
        }

        public static string GetString(this JsonObject json, string name) {
            var v = json.SafeGetValue(name);
            if (v == null) return null;
            switch (v.JsonType) {
                case JsonType.String:
                    return v;
                case JsonType.Boolean:
                    return v ? "true" : "false";
                default:
                    return v.ToString();
            }
        }

        public static long GetLong(this JsonObject json, string name) {
            var v = json.SafeGetValue(name);
            if (v == null) return 0;
            switch (v.JsonType) {
                case JsonType.Number:
                    return Convert.ToInt64((long)v);
                case JsonType.String:
                    return Convert.ToInt64((string)v);
                case JsonType.Boolean:
                    return v ? 1 : 0;
                default:
                    return 0;
            }
        }

        public static bool GetBoolean(this JsonObject json, string name) {
            var v = json.SafeGetValue(name);
            if (v == null) return false;
            switch (v.JsonType) {
                case JsonType.Boolean:
                    return v;
                case JsonType.Number:
                    return Convert.ToInt64((long)v) != 0;
                case JsonType.String:
                    return Convert.ToInt64((string)v) != 0;
                default:
                    return false;
            }
        }

        public static JsonArray GetList(this JsonObject json, string name) {
            var v = SafeGetValue(json, name);
            if (v == null || v.JsonType != JsonType.Array) {
                return null;
            }
            return v as JsonArray;
        }

        public static List<T> GetList<T>(this JsonObject json, string name, Func<JsonValue,T> j2t) {
            var v = GetList(json, name);
            if (v == null) return null;

            return v.Select(j2t).ToList();
        }

        public static JsonObject GetDictionary(this JsonObject json, string name) {
            var v = SafeGetValue(json, name);
            if (v == null || v.JsonType != JsonType.Object) {
                return null;
            }
            return v as JsonObject;
        }



    }
}
