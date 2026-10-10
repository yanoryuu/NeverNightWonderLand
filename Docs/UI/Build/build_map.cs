using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NeverNight.StageBuild;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 全体マップ (MapUI) と HUD のミニマップの組み立てレシピ (Docs/UI/MapScreen.md)。何度流してもよい。
// 先に仮素材を作っておく: py -3 Docs/UI/Build/gen_map_icons.py
//   unity command run_script --file Docs/UI/Build/build_map.cs --entry BuildMap.All      (下の 4 つを順に。レイアウトを変えたらこれ)
//   unity command run_script --file Docs/UI/Build/build_map.cs --entry BuildMap.Import   (仮素材のインポート設定)
//   unity command run_script --file Docs/UI/Build/build_map.cs --entry BuildMap.Export   (area1_layout.json + ステージの配置 → MapData)
//   unity command run_script --file Docs/UI/Build/build_map.cs --entry BuildMap.Screen   (MapUI.prefab / MapUI.unity。Build Settings と PlayerScene の UISceneBootstrap に登録)
//   unity command run_script --file Docs/UI/Build/build_map.cs --entry BuildMap.Minimap  (HUD.prefab のミニマップを作り直す)
// どれも --timeout_ms 600000 --timeout 620 を付ける (Export はステージを開いて読むので数十秒かかる)。
// 地図の絵 (部屋・通路・アイコン) はここで MapData から事前配置する。実行時は表示/非表示・位置・拡大だけ。
public static class BuildMap
{
    const string LayoutPath = "Docs/Area1/area1_layout.json";
    const string DataPath = "Assets/Data/Map/Area1MapData.asset";
    const string ArtDir = "Assets/LocalData/UI/_temp/map/";
    const string FontPath = "Assets/Font/YDW_bananaslip_plus_240809/YDWbananaslipplus SDF.asset";
    const string PrefabPath = "Assets/Prefabs/MapUI.prefab";
    const string ScenePath = "Assets/Scenes/UI/MapUI.unity";
    const string HudPrefabPath = "Assets/Prefabs/HUD.prefab";
    const string PlayerScenePath = "Assets/Scenes/PlayerScene.unity";
    const string AreaName = "鳴りやまぬ遊園地";

    // ---- 色 (仮の単色。ポーズ画面の紙と文字の色味) ----
    static readonly Color Maroon = new Color32(73, 0, 0, 255);       // 文字・縁取り (#490000)
    static readonly Color Paper = new Color32(214, 172, 112, 255);   // 紙 (paper_pause の地の色)
    static readonly Color PaperDark = new Color32(196, 150, 96, 255); // 地図の下地
    static readonly Color Backdrop = new Color32(40, 10, 8, 240);    // 画面の暗幕 (background_pause の色)
    static readonly Color LinkColor = new Color32(73, 0, 0, 215);
    static readonly Color ZiplineColor = new Color32(120, 45, 25, 200);

    // 区画 (layout の zone)。B (最初のボスの間) はチュートリアルに含める
    static readonly (string id, string name, Color color)[] Zones =
    {
        ("T", "チュートリアル", new Color32(158, 128, 176, 255)),
        ("H", "拠点", new Color32(232, 196, 92, 255)),
        ("U", "地上", new Color32(126, 166, 112, 255)),
        ("W", "観覧車", new Color32(112, 152, 190, 255)),
        ("D", "地下", new Color32(164, 112, 82, 255)),
        ("A", "クライマックス", new Color32(196, 92, 92, 255)),
    };

    // ---- 大きさ (Content の単位 = ワールド単位) ----
    public static float RoomBorderMultiplier = 3f; // 部屋の縁 (map_room の縁 4px ÷ これ = ワールド単位)
    public static float LinkThickness = 1.5f;
    public static float ZiplineThickness = 1.2f;
    public static float LinkExtend = 1.5f;         // 通路の線を部屋の中まで伸ばす長さ
    public static float IconSize = 26f;             // アイコンの画面上の大きさ (px, 拡大率に関係なく一定)
    public static float IconSpacing = 9f;           // アイコンどうしの最小の間隔 (ワールド単位。標準のズームで重ならない程度)

    static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

    static Sprite Art(string file)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + file);
        if (s == null) throw new Exception("sprite missing (BuildMap.Import を先に): " + file);
        return s;
    }

    public static string All()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Import());
        sb.AppendLine(Export());
        sb.AppendLine(Screen());
        sb.AppendLine(Minimap());
        return sb.ToString();
    }

    #region Import

    public static string Import()
    {
        AssetDatabase.Refresh();
        var n = 0;
        foreach (var path in Directory.GetFiles(ArtDir, "*.png").Select(p => p.Replace('\\', '/')))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.spriteBorder = Path.GetFileName(path) == "map_room.png" ? new Vector4(4, 4, 4, 4) : Vector4.zero;
            importer.SaveAndReimport();
            n++;
        }

        return $"Import: {n} sprites";
    }

    #endregion

    #region Export (JSON + ステージの配置 → MapData)

    class Scanned
    {
        public string Scene;
        public MapData.IconKind Kind;
        public Vector2 Pos;
        public string Flag;
        public string TargetScene; // 乗り物の行き先
    }

    public static string Export()
    {
        var log = new StringBuilder();
        var root = (Dictionary<string, object>)Area1Layout.MiniJson.Parse(
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", LayoutPath), Encoding.UTF8));
        var scale = Num(root, "scale", 3f);
        var groundCellY = Num(root, "groundCellY", 34f);

        // ---- 部屋 ----
        var rooms = new List<MapData.Room>();
        foreach (Dictionary<string, object> r in (List<object>)root["rooms"])
        {
            var zone = Str(r, "zone");
            if (zone == "B") zone = "T";
            var transport = Str(r, "transport");
            string flag = null;
            if (!string.IsNullOrEmpty(transport))
            {
                var m = Regex.Match(transport, @"flag\s+([A-Za-z0-9_]+)");
                if (m.Success) flag = m.Groups[1].Value;
            }

            rooms.Add(new MapData.Room
            {
                Id = Str(r, "id"),
                Name = Str(r, "name") ?? "",
                Scene = Str(r, "scene") ?? "",
                Zone = zone,
                Rects = ((List<object>)r["rects"]).Cast<Dictionary<string, object>>().Select(rc => WorldRect((Dictionary<string, object>)rc["world"])).ToArray(),
                RequiredFlag = flag ?? "",
            });
        }

        var tmp = ScriptableObject.CreateInstance<MapData>();
        tmp.EditorSet(AreaName, default, Array.Empty<MapData.Zone>(), rooms.ToArray(), Array.Empty<MapData.Link>(), Array.Empty<MapData.Icon>());
        MapData.Room Room(string id) => rooms.FirstOrDefault(x => x.Id == id);

        // ---- ステージの配置を読む (拠点・ショップ・鍛冶屋・ボス・スピーカー・乗り物) ----
        var scanned = ScanScenes(rooms.Where(x => !x.IsTransport).Select(x => x.Scene).Distinct().ToList(), log);

        // ---- 通路 ----
        var links = new List<MapData.Link>();
        foreach (Dictionary<string, object> l in (List<object>)root["links"])
        {
            var a = Room(Str(l, "a"));
            var b = Room(Str(l, "b"));
            if (a == null || b == null) { log.AppendLine($"WARN link {Str(l, "a")}-{Str(l, "b")}: room missing"); continue; }

            var link = new MapData.Link
            {
                A = a.Id, B = b.Id, OneWay = Bool(l, "oneWay"), Zipline = Bool(l, "zipline"), Gate = Str(l, "gate") ?? "", RequiredFlag = "",
            };

            if (l.TryGetValue("passage", out var p) && p is Dictionary<string, object> pd)
            {
                var passage = WorldRect((Dictionary<string, object>)pd["world"]);
                var ra = NearestRect(a, passage.center);
                var rb = NearestRect(b, passage.center);
                var gapX = Mathf.Max(ra.xMin, rb.xMin) - Mathf.Min(ra.xMax, rb.xMax);
                var gapY = Mathf.Max(ra.yMin, rb.yMin) - Mathf.Min(ra.yMax, rb.yMax);
                var aCenter = ra.center;
                if (gapY > gapX)
                {
                    // 上下につながる: 縦の線
                    var lo = new Vector2(passage.center.x, passage.yMin - LinkExtend);
                    var hi = new Vector2(passage.center.x, passage.yMax + LinkExtend);
                    (link.From, link.To) = aCenter.y > passage.center.y ? (hi, lo) : (lo, hi);
                }
                else
                {
                    var lo = new Vector2(passage.xMin - LinkExtend, passage.center.y);
                    var hi = new Vector2(passage.xMax + LinkExtend, passage.center.y);
                    (link.From, link.To) = aCenter.x > passage.center.x ? (hi, lo) : (lo, hi);
                }
            }
            else
            {
                // 通路の矩形が無い (ジップライン): 乗り場から到着側の部屋の縁まで
                var tr = scanned.FirstOrDefault(s => s.Scene == a.Scene && s.TargetScene == b.Scene && s.TargetScene != null);
                link.From = tr != null ? tr.Pos : a.Center;
                link.To = ClosestPoint(NearestRect(b, link.From), link.From);
                if (tr != null) link.RequiredFlag = tr.Flag ?? "";
            }

            links.Add(link);
        }

        // ---- アイコン ----
        var icons = new List<MapData.Icon>();
        foreach (var s in scanned)
        {
            var roomIndex = tmp.FindRoom(s.Scene, s.Pos, 1.5f);
            var room = roomIndex >= 0 ? rooms[roomIndex] : NearestRoom(rooms, s.Scene, s.Pos);
            var kind = s.Kind;
            if (kind == MapData.IconKind.Elevator && s.TargetScene != null
                && links.Any(k => k.Zipline && Room(k.A)?.Scene == s.Scene && Room(k.B)?.Scene == s.TargetScene))
                kind = MapData.IconKind.Zipline;
            icons.Add(new MapData.Icon { Kind = kind, Room = room?.Id ?? "", Position = s.Pos, Flag = s.Flag ?? "", Label = s.TargetScene ?? "" });
            log.AppendLine($"icon {kind} {s.Scene} ({s.Pos.x:F1},{s.Pos.y:F1}) room={room?.Id} flag={s.Flag}");
        }

        foreach (Dictionary<string, object> x in (List<object>)root["exits"])
        {
            var at = (List<object>)x["at"];
            var pos = new Vector2((float)(double)at[0] * scale, (groundCellY - (float)(double)at[1]) * scale);
            var rot = Str(x, "dir") switch { "up" => 90f, "left" => 180f, "down" => 270f, _ => 0f };
            icons.Add(new MapData.Icon { Kind = MapData.IconKind.Exit, Room = Str(x, "from"), Position = pos, Rotation = rot, Flag = "", Label = Str(x, "n") });
            log.AppendLine($"icon Exit {Str(x, "n")} room={Str(x, "from")} ({pos.x:F1},{pos.y:F1})");
        }

        // 近すぎるアイコン (拠点の隣のショップなど) は上にずらして重ならないようにする
        for (var i = 1; i < icons.Count; i++)
        {
            for (var guard = 0; guard < 8 && icons.Take(i).Any(o => (o.Position - icons[i].Position).sqrMagnitude < IconSpacing * IconSpacing); guard++)
                icons[i].Position += new Vector2(0f, IconSpacing);
        }

        // ---- 範囲 ----
        var all = rooms.SelectMany(r => r.Rects).ToList();
        var bounds = Rect.MinMaxRect(all.Min(r => r.xMin), all.Min(r => r.yMin), all.Max(r => r.xMax), all.Max(r => r.yMax));

        var zones = Zones.Select(z => new MapData.Zone { Id = z.id, Name = z.name, Color = z.color }).ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(DataPath) ?? "Assets/Data/Map");
        var data = AssetDatabase.LoadAssetAtPath<MapData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MapData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }

        data.EditorSet(AreaName, bounds, zones, rooms.ToArray(), links.ToArray(), icons.ToArray());
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        UnityEngine.Object.DestroyImmediate(tmp);
        return $"Export: rooms={rooms.Count} links={links.Count} icons={icons.Count} bounds={bounds}\n{log}";
    }

    /// <summary>ステージを開いて (保存しない) 拠点・ショップ・鍛冶屋・ボス・スピーカー・乗り物の位置とフラグを読む。</summary>
    static List<Scanned> ScanScenes(List<string> scenes, StringBuilder log)
    {
        var result = new List<Scanned>();
        var guids = new[] { typeof(SavePoint), typeof(ShopKeeper), typeof(Blacksmith), typeof(BossArena), typeof(BreakableSpeaker), typeof(TransportInteractable) }
            .Select(t => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(FindScript(t)))).ToArray();

        foreach (var sceneName in scenes)
        {
            var path = $"Assets/Scenes/Area1/{sceneName}.unity";
            if (!File.Exists(path)) { log.AppendLine("WARN scene missing " + path); continue; }
            var text = File.ReadAllText(path);
            if (!guids.Any(g => !string.IsNullOrEmpty(g) && text.Contains(g)))
                continue;

            var scene = SceneManager.GetSceneByPath(path);
            var opened = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                var roots = scene.GetRootGameObjects();
                T[] All<T>() where T : Component => roots.SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

                foreach (var c in All<SavePoint>())
                    result.Add(new Scanned { Scene = sceneName, Kind = MapData.IconKind.Save, Pos = c.transform.position });
                foreach (var c in All<ShopKeeper>())
                    result.Add(new Scanned { Scene = sceneName, Kind = MapData.IconKind.Shop, Pos = c.transform.position });
                foreach (var c in All<Blacksmith>())
                    result.Add(new Scanned { Scene = sceneName, Kind = MapData.IconKind.Smith, Pos = c.transform.position });
                foreach (var c in All<BossArena>())
                {
                    var so = new SerializedObject(c);
                    var boss = so.FindProperty("_boss").objectReferenceValue as Component;
                    result.Add(new Scanned
                    {
                        Scene = sceneName, Kind = MapData.IconKind.Boss,
                        Pos = boss != null ? boss.transform.position : c.transform.position,
                        Flag = so.FindProperty("_flagId").stringValue,
                    });
                }

                foreach (var c in All<BreakableSpeaker>())
                    result.Add(new Scanned { Scene = sceneName, Kind = MapData.IconKind.Speaker, Pos = c.transform.position, Flag = new SerializedObject(c).FindProperty("_flagId").stringValue });
                foreach (var c in All<TransportInteractable>())
                {
                    var so = new SerializedObject(c);
                    var req = so.FindProperty("_requiredFlags");
                    var flag = req.arraySize > 0 ? req.GetArrayElementAtIndex(0).stringValue : so.FindProperty("_activateFlag").stringValue;
                    result.Add(new Scanned
                    {
                        Scene = sceneName, Kind = MapData.IconKind.Elevator, Pos = c.transform.position, Flag = flag,
                        TargetScene = so.FindProperty("_sceneName").stringValue,
                    });
                }
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true); // 保存しない
            }
        }

        return result;
    }

    static MonoScript FindScript(Type t) =>
        AssetDatabase.FindAssets(t.Name + " t:MonoScript").Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(s => s != null && s.GetClass() == t);

    static Rect NearestRect(MapData.Room room, Vector2 p) =>
        room.Rects.OrderBy(r => (ClosestPoint(r, p) - p).sqrMagnitude).First();

    static Vector2 ClosestPoint(Rect r, Vector2 p) => new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));

    static MapData.Room NearestRoom(List<MapData.Room> rooms, string scene, Vector2 p) =>
        rooms.Where(r => r.Scene == scene).OrderBy(r => r.Rects.Min(rc => (ClosestPoint(rc, p) - p).sqrMagnitude)).FirstOrDefault();

    static Rect WorldRect(Dictionary<string, object> w) =>
        Rect.MinMaxRect(Num(w, "xMin"), Num(w, "yMin"), Num(w, "xMax"), Num(w, "yMax"));

    static string Str(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) ? v as string : null;
    static bool Bool(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) && v is bool b && b;
    static float Num(Dictionary<string, object> d, string k, float fallback = 0f) => d.TryGetValue(k, out var v) && v is double x ? (float)x : fallback;

    #endregion

    #region 地図の絵 (全体マップ・ミニマップ共通)

    static MapData LoadData()
    {
        var data = AssetDatabase.LoadAssetAtPath<MapData>(DataPath);
        if (data == null) throw new Exception("MapData がありません。BuildMap.Export を先に");
        return data;
    }

    static RectTransform R(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent, float inset = 0f)
    {
        var rt = R(name, parent, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    static Image Img(RectTransform rt, Sprite sprite, Color color)
    {
        rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        var rt = R(name, parent, pos, size);
        rt.gameObject.AddComponent<CanvasRenderer>();
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Font;
        t.fontSharedMaterial = Font.material;
        t.text = text;
        t.fontSize = fontSize;
        t.enableAutoSizing = false;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static MapDrawing.IconStyle[] IconStyles()
    {
        MapDrawing.IconStyle St(string name, string normal, string done, Color nc, Color dc) =>
            new MapDrawing.IconStyle { Name = name, Normal = Art(normal), Done = done != null ? Art(done) : null, NormalColor = nc, DoneColor = dc };

        var gray = new Color32(150, 130, 115, 255);
        // MapData.IconKind の順
        return new[]
        {
            St("拠点 (セーブ)", "icon_save.png", null, new Color32(255, 236, 160, 255), Color.white),
            St("ショップ", "icon_shop.png", null, new Color32(255, 214, 120, 255), Color.white),
            St("鍛冶屋", "icon_smith.png", null, new Color32(210, 210, 220, 255), Color.white),
            St("ボス", "icon_boss.png", "icon_boss_done.png", new Color32(235, 80, 70, 255), gray),
            St("スピーカー", "icon_speaker.png", "icon_speaker_done.png", new Color32(240, 140, 200, 255), gray),
            St("別エリアへの出口", "icon_exit.png", null, Color.white, Color.white),
            St("近道 (ジップライン)", "icon_zipline.png", null, gray, new Color32(140, 230, 220, 255)), // 開通前は灰
            St("近道 (リフト・エレベーター)", "icon_elevator.png", null, gray, new Color32(140, 230, 220, 255)),
        };
    }

    /// <summary>parent の下に地図の絵 (Content) を作り、MapDrawing を設定する。</summary>
    static MapDrawing BuildDrawing(GameObject host, RectTransform parent, MapData data, float iconScale, bool markerInContent, out RectTransform content)
    {
        var drawing = host.GetComponent<MapDrawing>() ?? host.AddComponent<MapDrawing>();
        content = R("Content", parent, Vector2.zero, Vector2.zero);

        var linkRoot = R("Links", content, Vector2.zero, Vector2.zero);
        var roomRoot = R("Rooms", content, Vector2.zero, Vector2.zero);
        var iconRoot = R("Icons", content, Vector2.zero, Vector2.zero);

        var roomSprite = Art("map_room.png");
        var oneWay = Art("icon_oneway.png");
        var linkGraphics = new List<MapDrawing.LinkGraphic>();
        for (var i = 0; i < data.Links.Length; i++)
        {
            var l = data.Links[i];
            var d = l.To - l.From;
            var rt = R($"Link{i:00}_{l.A}_{l.B}", linkRoot, (l.From + l.To) * 0.5f, new Vector2(d.magnitude, l.Zipline ? ZiplineThickness : LinkThickness));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            Img(rt, null, l.Zipline ? ZiplineColor : LinkColor);
            RectTransform arrow = null;
            if (l.OneWay)
            {
                // 矢印は線の子 (線の向き = A → B に回る。線と一緒に出し入れする)
                arrow = R("OneWay", rt, Vector2.zero, new Vector2(IconSize * 0.8f, IconSize * 0.8f));
                Img(arrow, oneWay, Color.white);
            }

            rt.gameObject.SetActive(false);
            linkGraphics.Add(new MapDrawing.LinkGraphic { Root = rt.gameObject, Arrow = arrow });
        }

        var roomGraphics = new List<MapDrawing.RoomGraphic>();
        foreach (var r in data.Rooms)
        {
            var zone = data.GetZone(r.Zone);
            var color = zone != null ? zone.Color : Color.white;
            if (r.IsTransport)
                color = Color.Lerp(color, PaperDark, 0.45f); // 乗り物の通路は薄く
            var room = R("Room_" + r.Id, roomRoot, Vector2.zero, Vector2.zero);
            for (var k = 0; k < r.Rects.Length; k++)
            {
                var rc = r.Rects[k];
                var block = R($"Block{k}", room, rc.center, rc.size);
                var img = Img(block, roomSprite, color);
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = RoomBorderMultiplier;
            }

            room.gameObject.SetActive(false);
            roomGraphics.Add(new MapDrawing.RoomGraphic { RoomId = r.Id, Root = room.gameObject });
        }

        var styles = IconStyles();
        var iconGraphics = new List<MapDrawing.IconGraphic>();
        for (var i = 0; i < data.Icons.Length; i++)
        {
            var ic = data.Icons[i];
            var rt = R($"Icon{i:00}_{ic.Kind}_{ic.Room}", iconRoot, ic.Position, new Vector2(IconSize, IconSize));
            rt.localRotation = Quaternion.Euler(0, 0, ic.Rotation);
            var style = styles[(int)ic.Kind];
            Img(rt, style.Normal, style.NormalColor);
            rt.gameObject.SetActive(false);
            iconGraphics.Add(new MapDrawing.IconGraphic { Image = rt.GetComponent<Image>() });
        }

        RectTransform marker;
        if (markerInContent)
        {
            marker = R("PlayerMarker", content, Vector2.zero, new Vector2(IconSize * 1.15f, IconSize * 1.15f));
        }
        else
        {
            marker = R("PlayerMarker", parent, Vector2.zero, new Vector2(IconSize * 1.15f, IconSize * 1.15f));
        }

        Img(marker, Art("map_marker.png"), Color.white);

        drawing.EditorSet(content, roomGraphics.ToArray(), linkGraphics.ToArray(), iconGraphics.ToArray(), styles, marker, iconScale);
        return drawing;
    }

    static void ClearChildren(Transform t)
    {
        for (var i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    #endregion

    #region Screen (MapUI)

    public static string Screen()
    {
        var data = LoadData();
        var log = new StringBuilder();
        var root = new GameObject("MapUI", typeof(RectTransform));
        root.layer = 5;
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9; // アイテム画面と同じ (同時には開かない)。ポーズ (10) より下
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var view = root.AddComponent<MapScreenView>();
        var scope = root.AddComponent<MapLifetimeScope>();

        var screen = Stretch("Root", root.transform);
        Img(Stretch("Backdrop", screen), null, Backdrop);
        Img(R("Paper", screen, Vector2.zero, new Vector2(1800f, 1010f)), null, Paper);

        Txt("Title", screen, "全体マップ", new Vector2(-640f, 466f), new Vector2(500f, 60f), 44f, Maroon, TextAlignmentOptions.MidlineLeft);
        Txt("AreaName", screen, data.AreaName, new Vector2(-150f, 466f), new Vector2(700f, 60f), 36f, Maroon, TextAlignmentOptions.Midline);
        var zoomText = Txt("Zoom", screen, "ズーム 2/3", new Vector2(450f, 466f), new Vector2(240f, 50f), 26f, Maroon, TextAlignmentOptions.MidlineRight);

        // 地図の窓 (はみ出しは RectMask2D で切る)
        var viewport = R("Viewport", screen, new Vector2(-150f, 15f), new Vector2(1440f, 830f));
        Img(viewport, null, PaperDark);
        viewport.gameObject.AddComponent<RectMask2D>();
        var drawingHost = viewport.gameObject;
        var drawing = BuildDrawing(drawingHost, viewport, data, 1f, true, out _);
        var cursor = R("Cursor", viewport, Vector2.zero, new Vector2(40f, 40f));
        Img(cursor, Art("icon_cursor.png"), new Color(1f, 1f, 1f, 0.55f));
        var frame = R("Frame", screen, viewport.anchoredPosition, viewport.sizeDelta + new Vector2(8f, 8f));
        var frameImg = Img(frame, Art("map_room.png"), Maroon);
        frameImg.type = Image.Type.Sliced;
        frameImg.fillCenter = false;
        frameImg.pixelsPerUnitMultiplier = 1f;

        // 下の欄: 場所名と操作ガイド
        var place = Txt("Place", screen, data.AreaName, new Vector2(-150f, -430f), new Vector2(1440f, 46f), 34f, Maroon, TextAlignmentOptions.Midline);
        var kb = Txt("GuideKeyboard", screen, "移動 WASD・矢印　　ズーム Q / E　　現在地へ Space　　閉じる M・Esc",
            new Vector2(-150f, -474f), new Vector2(1440f, 36f), 24f, Maroon, TextAlignmentOptions.Midline);
        var pad = Txt("GuideGamepad", screen, "移動 左スティック・十字キー　　ズーム L1 / R1　　現在地へ ×　　閉じる タッチパッド・R3・○",
            new Vector2(-150f, -474f), new Vector2(1440f, 36f), 24f, Maroon, TextAlignmentOptions.Midline);
        pad.gameObject.SetActive(false);

        BuildLegend(screen, data);

        var so = new SerializedObject(view);
        so.FindProperty("_root").objectReferenceValue = screen.gameObject;
        so.FindProperty("_drawing").objectReferenceValue = drawing;
        so.FindProperty("_placeText").objectReferenceValue = place;
        so.FindProperty("_zoomText").objectReferenceValue = zoomText;
        so.FindProperty("_keyboardGuide").objectReferenceValue = kb.gameObject;
        so.FindProperty("_gamepadGuide").objectReferenceValue = pad.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        var sso = new SerializedObject(scope);
        sso.FindProperty("_mapData").objectReferenceValue = data;
        sso.FindProperty("_mapScreenView").objectReferenceValue = view;
        sso.ApplyModifiedPropertiesWithoutUndo();

        screen.gameObject.SetActive(false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        // UI シーン (ゲーム中に Additive で重ねる)。開いているシーンはそのまま
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        PrefabUtility.InstantiatePrefab(prefab, scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);
        AddToBuildSettings(ScenePath, log);
        log.Append(AddToPlayerSceneBootstrap("MapUI"));
        log.Append(CheckFont(prefab));
        return "MapUI built\n" + log;
    }

    static void BuildLegend(RectTransform screen, MapData data)
    {
        var legend = R("Legend", screen, new Vector2(735f, 15f), new Vector2(290f, 830f));
        Img(legend, null, new Color(1f, 1f, 1f, 0.12f));
        Txt("Heading", legend, "凡例", new Vector2(0f, 385f), new Vector2(260f, 44f), 32f, Maroon, TextAlignmentOptions.Midline);

        var styles = IconStyles();
        var rows = new List<(Sprite sprite, Color color, string label, float rot)>
        {
            (Art("map_marker.png"), Color.white, "現在地", 0f),
            (styles[0].Normal, styles[0].NormalColor, "拠点 (セーブ)", 0f),
            (styles[1].Normal, styles[1].NormalColor, "ショップ", 0f),
            (styles[2].Normal, styles[2].NormalColor, "鍛冶屋", 0f),
            (styles[3].Normal, styles[3].NormalColor, "ボス", 0f),
            (styles[3].Done, styles[3].DoneColor, "ボス (撃破)", 0f),
            (styles[4].Normal, styles[4].NormalColor, "スピーカー", 0f),
            (styles[4].Done, styles[4].DoneColor, "スピーカー (破壊)", 0f),
            (styles[5].Normal, styles[5].NormalColor, "別エリアへの出口", 0f),
            (styles[6].Normal, styles[6].DoneColor, "近道 ジップライン", 0f),
            (styles[7].Normal, styles[7].DoneColor, "近道 リフト", 0f),
            (styles[7].Normal, styles[7].NormalColor, "近道 (未開通)", 0f),
            (Art("icon_oneway.png"), Color.white, "一方通行", 0f),
        };

        var y = 340f;
        foreach (var (sprite, color, label, rot) in rows)
        {
            var icon = R("Icon_" + label, legend, new Vector2(-112f, y), new Vector2(28f, 28f));
            icon.localRotation = Quaternion.Euler(0, 0, rot);
            Img(icon, sprite, color);
            Txt("Label_" + label, legend, label, new Vector2(18f, y), new Vector2(220f, 32f), 22f, Maroon, TextAlignmentOptions.MidlineLeft);
            y -= 34f;
        }

        y -= 10f;
        foreach (var z in data.Zones)
        {
            var sw = R("Zone_" + z.Id, legend, new Vector2(-112f, y), new Vector2(30f, 20f));
            var img = Img(sw, Art("map_room.png"), z.Color);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            Txt("ZoneLabel_" + z.Id, legend, z.Name, new Vector2(18f, y), new Vector2(220f, 32f), 22f, Maroon, TextAlignmentOptions.MidlineLeft);
            y -= 32f;
        }
    }

    static string CheckFont(GameObject prefab)
    {
        var font = Font;
        var missing = new HashSet<char>();
        foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
            foreach (var c in t.text.Where(c => !char.IsWhiteSpace(c) && !font.HasCharacter(c, false, false)))
                missing.Add(c);
        foreach (var r in LoadData().Rooms)
            foreach (var c in (r.Name ?? "").Where(c => !char.IsWhiteSpace(c) && !font.HasCharacter(c, false, false)))
                missing.Add(c);
        return missing.Count == 0 ? "font: all glyphs present\n" : $"WARN font missing: {new string(missing.ToArray())}\n";
    }

    static void AddToBuildSettings(string path, StringBuilder log)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path))
            return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        log.AppendLine("Build Settings += " + path);
    }

    /// <summary>PlayerScene の UISceneBootstrap に足す (PlayerScene が開いていなければ開いて保存して閉じる)。</summary>
    static string AddToPlayerSceneBootstrap(string sceneName)
    {
        var scene = SceneManager.GetSceneByPath(PlayerScenePath);
        var opened = false;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(PlayerScenePath, OpenSceneMode.Additive);
            opened = true;
        }

        var log = "";
        var changed = false;
        foreach (var boot in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UISceneBootstrap>(true)))
        {
            var so = new SerializedObject(boot);
            var arr = so.FindProperty("_uiSceneNames");
            var names = Enumerable.Range(0, arr.arraySize).Select(i => arr.GetArrayElementAtIndex(i).stringValue).ToList();
            if (names.Contains(sceneName))
                continue;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).stringValue = sceneName;
            so.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
            log += $"PlayerScene bootstrap += {sceneName}\n";
        }

        if (changed)
            EditorSceneManager.SaveScene(scene);
        if (opened)
            EditorSceneManager.CloseScene(scene, true);
        return log;
    }

    #endregion

    #region Minimap (HUD)

    /// <summary>HUD.prefab の Minimap の中身を地図データ方式に作り直す (枠の位置・大きさはそのまま)。</summary>
    public static string Minimap()
    {
        var data = LoadData();
        var root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            var view = root.GetComponentsInChildren<MinimapView>(true).FirstOrDefault()
                       ?? throw new Exception("HUD.prefab に MinimapView がありません");
            var host = (RectTransform)view.transform;
            ClearChildren(host);

            // 枠 (自身の Image) は濃い色、中に紙の地図
            var frame = host.GetComponent<Image>();
            if (frame != null)
            {
                frame.sprite = null;
                frame.color = new Color32(73, 0, 0, 230);
            }

            var viewport = Stretch("Viewport", host, 5f);
            Img(viewport, null, PaperDark);
            viewport.gameObject.AddComponent<RectMask2D>();
            var drawing = BuildDrawing(viewport.gameObject, viewport, data, 0.7f, false, out _);

            var so = new SerializedObject(view);
            so.FindProperty("_drawing").objectReferenceValue = drawing;
            so.FindProperty("_mapRoot").objectReferenceValue = host.gameObject;
            so.FindProperty("_zoom").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var scope = root.GetComponentsInChildren<UILifetimeScope>(true).FirstOrDefault();
            if (scope != null)
            {
                var sso = new SerializedObject(scope);
                sso.FindProperty("_mapData").objectReferenceValue = data;
                sso.FindProperty("_minimapView").objectReferenceValue = view;
                sso.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            return $"Minimap rebuilt (scope={(scope != null)})";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    #endregion
}
