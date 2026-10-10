using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NeverNight.StageBuild
{
    /// <summary>
    /// Docs/Area1/area1_layout.json の読み込み (エディタ専用)。
    /// 座標は全シーン共通のワールド座標 (1 マップセル = 3 単位, 地上 y=0)。
    /// rects[].world / passage.world の xMin/xMax/yMin/yMax をそのまま使う。
    /// </summary>
    public class Area1Layout
    {
        public const string LayoutPath = "Docs/Area1/area1_layout.json";

        public class Room
        {
            public string Id, Name, Scene, Intent, Boss, Transport;
            public bool Outdoor, Speaker;
            public List<RectInt> Rects = new List<RectInt>();
            public List<string> Features = new List<string>();
        }

        public class Link
        {
            public string A, B, Gate, Kind;
            public bool OneWay, Zipline, SameScene;
            public RectInt Passage;
            public bool HasPassage;
        }

        public class Reward
        {
            public string Room, Kind, Name, Req;
        }

        public Dictionary<string, string> SceneNames = new Dictionary<string, string>();
        public Dictionary<string, List<string>> SceneRooms = new Dictionary<string, List<string>>();
        public List<Room> Rooms = new List<Room>();
        public List<Link> Links = new List<Link>();
        public List<Reward> Rewards = new List<Reward>();

        private static Area1Layout _cache;
        private static DateTime _cacheTime;

        /// <summary>レイアウトを読む (ファイルが更新されていれば読み直す)。</summary>
        public static Area1Layout Load()
        {
            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", LayoutPath);
            var time = File.GetLastWriteTimeUtc(path);
            if (_cache != null && time == _cacheTime)
                return _cache;

            var root = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(path, Encoding.UTF8));
            var layout = new Area1Layout();

            foreach (var kv in (Dictionary<string, object>)root["scenes"])
            {
                var s = (Dictionary<string, object>)kv.Value;
                layout.SceneNames[kv.Key] = Str(s, "name");
                layout.SceneRooms[kv.Key] = ((List<object>)s["rooms"]).Select(o => (string)o).ToList();
            }

            foreach (Dictionary<string, object> r in (List<object>)root["rooms"])
            {
                var room = new Room
                {
                    Id = Str(r, "id"), Name = Str(r, "name"), Scene = Str(r, "scene"),
                    Intent = Str(r, "intent"), Boss = Str(r, "boss"), Transport = Str(r, "transport"),
                    Outdoor = Bool(r, "outdoor"), Speaker = Bool(r, "speaker"),
                };
                foreach (Dictionary<string, object> rc in (List<object>)r["rects"])
                    room.Rects.Add(WorldRect((Dictionary<string, object>)rc["world"]));
                if (r.TryGetValue("features", out var f) && f is List<object> fl)
                    room.Features = fl.Select(o => o as string).ToList();
                layout.Rooms.Add(room);
            }

            foreach (Dictionary<string, object> l in (List<object>)root["links"])
            {
                var link = new Link
                {
                    A = Str(l, "a"), B = Str(l, "b"), Gate = Str(l, "gate"), Kind = Str(l, "kind"),
                    OneWay = Bool(l, "oneWay"), Zipline = Bool(l, "zipline"), SameScene = Bool(l, "sameScene"),
                };
                if (l.TryGetValue("passage", out var p) && p is Dictionary<string, object> pd)
                {
                    link.Passage = WorldRect((Dictionary<string, object>)pd["world"]);
                    link.HasPassage = true;
                }
                layout.Links.Add(link);
            }

            foreach (Dictionary<string, object> w in (List<object>)root["rewards"])
                layout.Rewards.Add(new Reward { Room = Str(w, "room"), Kind = Str(w, "k"), Name = Str(w, "n"), Req = Str(w, "req") });

            _cache = layout;
            _cacheTime = time;
            return layout;
        }

        public Room GetRoom(string id) => Rooms.FirstOrDefault(r => r.Id == id);

        public IEnumerable<Room> RoomsInScene(string sceneId) => Rooms.Where(r => r.Scene == sceneId);

        public string SceneOfRoom(string roomId) => GetRoom(roomId)?.Scene;

        /// <summary>シーン表示名 (AreaTitle 用)。</summary>
        public string SceneTitle(string sceneId) =>
            SceneNames.TryGetValue(sceneId, out var n) ? n : sceneId;

        private static RectInt WorldRect(Dictionary<string, object> w)
        {
            var xMin = Mathf.RoundToInt(Num(w, "xMin"));
            var xMax = Mathf.RoundToInt(Num(w, "xMax"));
            var yMin = Mathf.RoundToInt(Num(w, "yMin"));
            var yMax = Mathf.RoundToInt(Num(w, "yMax"));
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static string Str(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) ? v as string : null;

        private static bool Bool(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) && v is bool b && b;

        private static float Num(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) && v is double x ? (float)x : 0f;

        /// <summary>依存なしの最小 JSON パーサ (object=Dictionary, array=List, number=double)。</summary>
        public static class MiniJson
        {
            public static object Parse(string json)
            {
                var i = 0;
                var v = Value(json, ref i);
                return v;
            }

            private static void Ws(string s, ref int i)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            private static object Value(string s, ref int i)
            {
                Ws(s, ref i);
                var c = s[i];
                if (c == '{') return Obj(s, ref i);
                if (c == '[') return Arr(s, ref i);
                if (c == '"') return Str(s, ref i);
                if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
                if (s.Substring(i, Math.Min(5, s.Length - i)) == "false") { i += 5; return false; }
                if (s.Substring(i, Math.Min(4, s.Length - i)) == "null") { i += 4; return null; }
                var start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
            }

            private static Dictionary<string, object> Obj(string s, ref int i)
            {
                var d = new Dictionary<string, object>();
                i++;
                Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Ws(s, ref i);
                    var k = Str(s, ref i);
                    Ws(s, ref i);
                    i++; // :
                    d[k] = Value(s, ref i);
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; // }
                    return d;
                }
            }

            private static List<object> Arr(string s, ref int i)
            {
                var l = new List<object>();
                i++;
                Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i));
                    Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; // ]
                    return l;
                }
            }

            private static string Str(string s, ref int i)
            {
                var sb = new StringBuilder();
                i++; // "
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++;
                        var e = s[i];
                        switch (e)
                        {
                            case 'n': sb.Append('\n'); break;
                            case 't': sb.Append('\t'); break;
                            case 'r': sb.Append('\r'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'u':
                                sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16));
                                i += 4;
                                break;
                            default: sb.Append(e); break;
                        }
                        i++;
                    }
                    else
                    {
                        sb.Append(s[i++]);
                    }
                }
                i++;
                return sb.ToString();
            }
        }
    }
}
