using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NeverNight.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace NeverNight.StageBuild
{
    /// <summary>
    /// エリア1 のシーン組み立て API (エディタ専用・静的メソッド)。Unity CLI の eval_file から呼ぶ。
    /// 手順と例は Docs/Area1/SceneBuildGuide.md。
    ///
    /// 座標はすべて全シーン共通のワールド座標 (整数 = タイルのセル境界)。
    /// 矩形引数は (x0, y0, x1, y1) = 左下〜右上の境界 (x1/y1 は含まない) で、セル [x0,x1) × [y0,y1) を塗る。
    ///
    /// 典型的な流れ (1 シーン = eval_file 2 回):
    ///   1回目: Area1StageBuilder.OpenOrCreate("A1_T2");          // シーンを開く (同じ呼び出しで塗らない)
    ///   2回目: Area1StageBuilder.BuildBase("A1_T2");            // 骨組み+岩+掘削+遷移+背景 (シーンを丸ごと作り直す)
    ///          Solid(...) / OneWay(...) / Enemy(...) / Hint(...) ... // 部屋の中身
    ///          return Area1StageBuilder.Finish();               // タイル再評価+検証+保存
    /// </summary>
    public static partial class Area1StageBuilder
    {
        public const string SceneDir = "Assets/Scenes/Area1";
        public const int RockMargin = 6;       // 部屋の外側に置く当たり付きの岩
        public const int BackfillMargin = 26;  // 見た目だけの埋め (カメラが外を映しても空洞が見えない)
        public const int ExitExtension = 3;    // 別シーンへの通路を部屋の外へ掘り延ばす長さ
        public const int SealRing = 2;         // 岩の外周をプレーンな coverbox で塞ぐ幅 (縁の装飾を出さない)

        // レイヤー
        public const int LayerDefault = 0, LayerIgnoreRaycast = 2, LayerEnemy = 9, LayerGround = 10, LayerInteractable = 11;

        // 描画順
        public const int OrderSky = -100, OrderFarBg = -95, OrderRoomBackdrop = -90, OrderBackfill = -10,
            OrderRockDecor = -8, OrderWallDecor = -7, OrderProp = -5, OrderTerrain = 0, OrderDeco = 1, OrderPlatforms = 2, OrderGimmick = 3;

        private const string TileRoot = "Assets/LocalData/StageAsset/Tiles";
        // エリア1 専用の RuleTile (内部は絵なし・縁は切り抜き版。下の Backfill に岩肌を敷く)。旧シーンの StageTerrainRule は使わない
        private const string RuleTilePath = Area1AssetPrep.A1TerrainRulePath;
        private const string ArtRoot = Area1AssetPrep.ArtRoot;
        // Drive「プロップス素材」(2026-10-03 PendingChanges 14): コイン・糸巻き・ミシン針・まち針・布カッター (1024px, PPU 1024 = 1 単位, 中心ピボット)
        public const string PropsRoot = "Assets/LocalData/StageAsset/Props";
        public const string CoinPrefabPath = "Assets/Prefabs/CoinPiece.prefab";
        // Drive の新素材 (2026-10-08 PendingChanges 42。インポートは SceneScripts/prep_newart.cs)
        public const string GimmickRoot = "Assets/LocalData/StageAsset/gimmick";
        public const string CharacterRoot = "Assets/LocalData/Character";

        // 現在組み立て中のシーン Id (BuildBase で設定。Finish/Validate が使う)
        private static string _sceneId;

        // ================================================================== シーン

        public static string ScenePath(string sceneId) => $"{SceneDir}/{sceneId}.unity";

        /// <summary>
        /// シーンを開く (無ければ空のシーンを作って保存)。**この呼び出しの中でタイルを塗らないこと**
        /// (StageTileRules.md: 開いた直後の同一呼び出しで SetTile すると消える現象あり)。
        /// 開いているシーンの未保存の変更は破棄する。
        /// </summary>
        public static string OpenOrCreate(string sceneId)
        {
            Area1AssetPrep.EnsureFolder(SceneDir);
            var path = ScenePath(sceneId);
            Scene scene;
            if (File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
            }
            _sceneId = sceneId;
            EnsureInBuildSettings(path);
            return $"opened {scene.path}";
        }

        private static Scene ActiveScene => SceneManager.GetActiveScene();

        private static string CurrentId
        {
            get
            {
                if (!string.IsNullOrEmpty(_sceneId) && ActiveScene.name == _sceneId)
                    return _sceneId;
                return _sceneId = ActiveScene.name;
            }
        }

        /// <summary>
        /// 開いているシーン (名前 = sceneId) を**丸ごと作り直す**: 既存のルートオブジェクトを全削除し、
        /// 必須の骨組み (§3) + Grid/タイルマップ (§4) + レイアウトからの岩埋め・掘削 + 遷移ゾーン/スポーン +
        /// 背景 + CameraLimit を置く。部屋の中身はこの後に呼ぶ。
        /// </summary>
        public static string BuildBase(string sceneId)
        {
            var scene = ActiveScene;
            if (scene.name != sceneId)
                throw new InvalidOperationException($"アクティブシーンが {scene.name} です。先に OpenOrCreate(\"{sceneId}\") を別の呼び出しで実行してください");
            _sceneId = sceneId;
            DecorDensity = 1f;
            RockTints.Clear();
            PlateRects.Clear();

            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            var log = new StringBuilder();
            BuildSkeleton(sceneId);
            log.AppendLine(Carve(sceneId));
            log.AppendLine(PlaceTransitions(sceneId));
            log.AppendLine(Background(sceneId));
            EnsureInBuildSettings(scene.path);
            EditorSceneManager.MarkSceneDirty(scene);
            return log.ToString();
        }

        /// <summary>
        /// 骨組み: PlayerSceneBootstrap / StageInfo / DamageFloaterSpawner / CameraLimit / Grid。
        /// UI はステージに置かない (PlayerScene が StageUI / ShopUI などの UI シーンを Additive で重ねる)。
        /// ステージ側はデータだけ: StageInfo (部屋名 = AreaTitle 用 + シーン開始のリセット)。
        /// </summary>
        public static void BuildSkeleton(string sceneId)
        {
            var layout = Area1Layout.Load();

            var boot = new GameObject("PlayerSceneBootstrap");
            var b = boot.AddComponent<UISceneBootstrap>();
            Set(b, "_uiSceneNames", new[] { "PlayerScene" });

            var info = new GameObject("StageInfo").AddComponent<StageInfo>();
            Set(info, "_areaName", layout.SceneTitle(sceneId));

            var floaterGo = new GameObject("DamageFloaterSpawner");
            var floater = floaterGo.AddComponent<DamageFloaterSpawner>();
            var floaterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DamageFloater.prefab");
            if (floaterPrefab != null)
                Set(floater, "_floaterPrefab", floaterPrefab.GetComponent<DamageFloater>());

            var rooms = layout.RoomsInScene(sceneId).ToList();
            var minY = rooms.SelectMany(r => r.Rects).Select(r => r.yMin).DefaultIfEmpty(0).Min();
            var limit = new GameObject("CameraLimit").AddComponent<CameraLimit>();
            Set(limit, "_minY", (float)minY + 2f);

            // Grid + tilemaps
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var backfill = NewTilemap(grid.transform, "Backfill", OrderBackfill, LayerDefault);

            var terrain = NewTilemap(grid.transform, "Terrain", OrderTerrain, LayerGround);
            var rb = terrain.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var tc = terrain.gameObject.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var comp = terrain.gameObject.AddComponent<CompositeCollider2D>();
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            comp.generationType = CompositeCollider2D.GenerationType.Synchronous;

            NewTilemap(grid.transform, "Deco", OrderDeco, LayerDefault);

            var platforms = NewTilemap(grid.transform, "Platforms", OrderPlatforms, LayerGround);
            var pc = platforms.gameObject.AddComponent<TilemapCollider2D>();
            pc.usedByEffector = true;
            var eff = platforms.gameObject.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 180f;
            backfill.GetComponent<TilemapRenderer>().sortingOrder = OrderBackfill;

            // 配置物のグループ
            foreach (var g in new[] { "Transitions", "Terrain Objects", "Gimmicks", "Pickups", "Enemies", "TutorialHints", "Props", "Background" })
                new GameObject(g);
        }

        private static Tilemap NewTilemap(Transform parent, string name, int order, int layer)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            tm.tileAnchor = new Vector3(0.5f, 0.5f, 0);
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = order;
            return tm;
        }

        // ================================================================== レイアウト

        /// <summary>このシーンが接するリンク (同一シーン内の通路 / 他シーンへの通路)。</summary>
        public static IEnumerable<Area1Layout.Link> LinksOf(string sceneId)
        {
            var layout = Area1Layout.Load();
            return layout.Links.Where(l => layout.SceneOfRoom(l.A) == sceneId || layout.SceneOfRoom(l.B) == sceneId);
        }

        public enum ExitDir { Left, Right, Up, Down }

        public class BoundaryExit
        {
            public Area1Layout.Link Link;
            public string OurRoom, OtherRoom, OtherScene;
            public ExitDir Dir;
            public RectInt Passage;   // レイアウト上の開口
            public RectInt Carved;    // 実際に掘る範囲 (開口 + 外側への延長)
            public bool CanExit;      // こちらから出られる (一方通行の受け側は false)
            public bool CanArrive;    // 向こうから来る (一方通行の送り側は false)
            public bool Transport;    // 相手がシーンを持たない移動装置の部屋 (E1/WL)。掘るだけで遷移ゾーン/スポーンは置かない (TransportAt で自前配置)
        }

        /// <summary>他シーンへの通路を列挙 (向き・掘る範囲付き)。ジップライン等の通路無しリンクは除外。</summary>
        public static List<BoundaryExit> BoundaryExits(string sceneId)
        {
            var layout = Area1Layout.Load();
            var result = new List<BoundaryExit>();
            foreach (var l in LinksOf(sceneId))
            {
                var sa = layout.SceneOfRoom(l.A);
                var sb = layout.SceneOfRoom(l.B);
                if (sa == sb || !l.HasPassage || l.Zipline)
                    continue;
                var ours = sa == sceneId ? l.A : l.B;
                var other = sa == sceneId ? l.B : l.A;
                var room = layout.GetRoom(ours);
                var p = l.Passage;
                var dir = ExitDir.Right;
                foreach (var r in room.Rects)
                {
                    if (p.xMin == r.xMax && Overlap(p.yMin, p.yMax, r.yMin, r.yMax)) { dir = ExitDir.Right; break; }
                    if (p.xMax == r.xMin && Overlap(p.yMin, p.yMax, r.yMin, r.yMax)) { dir = ExitDir.Left; break; }
                    if (p.yMin == r.yMax && Overlap(p.xMin, p.xMax, r.xMin, r.xMax)) { dir = ExitDir.Up; break; }
                    if (p.yMax == r.yMin && Overlap(p.xMin, p.xMax, r.xMin, r.xMax)) { dir = ExitDir.Down; break; }
                }

                var carved = p;
                switch (dir)
                {
                    case ExitDir.Right: carved.xMax += ExitExtension; break;
                    case ExitDir.Left: carved.xMin -= ExitExtension; break;
                    case ExitDir.Up: carved.yMax += ExitExtension; break;
                    case ExitDir.Down: carved.yMin -= ExitExtension; break;
                }

                var weAreA = ours == l.A;
                var otherScene = layout.SceneOfRoom(other);
                var transport = string.IsNullOrEmpty(otherScene);
                result.Add(new BoundaryExit
                {
                    Link = l, OurRoom = ours, OtherRoom = other, OtherScene = otherScene,
                    Dir = dir, Passage = p, Carved = carved,
                    CanExit = !transport && (!l.OneWay || weAreA),
                    CanArrive = !transport && (!l.OneWay || !weAreA),
                    Transport = transport,
                });
            }
            return result;
        }

        private static bool Overlap(int a0, int a1, int b0, int b1) => a0 < b1 && b0 < a1;

        /// <summary>シーンの空間 (部屋 + 同一シーン内の通路 + 他シーンへの掘削) の矩形一覧。</summary>
        public static List<RectInt> OpenRects(string sceneId)
        {
            var layout = Area1Layout.Load();
            var list = layout.RoomsInScene(sceneId).SelectMany(r => r.Rects).ToList();
            foreach (var l in LinksOf(sceneId))
                if (l.HasPassage && layout.SceneOfRoom(l.A) == layout.SceneOfRoom(l.B))
                    list.Add(l.Passage);
            list.AddRange(BoundaryExits(sceneId).Select(e => e.Carved));
            return list;
        }

        public static RectInt Bounds(string sceneId)
        {
            var rects = OpenRects(sceneId);
            var xMin = rects.Min(r => r.xMin);
            var yMin = rects.Min(r => r.yMin);
            var xMax = rects.Max(r => r.xMax);
            var yMax = rects.Max(r => r.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        /// <summary>
        /// 岩で埋めてから部屋・通路を掘る。屋外の部屋 (outdoor) は部屋の上を空まで開ける。
        /// Backfill (見た目だけの coverbox) を外側に広く敷く。岩の外周は縁飾りが出ないようプレーンな coverbox で塞ぐ。
        /// </summary>
        public static string Carve(string sceneId)
        {
            var layout = Area1Layout.Load();
            var b = Bounds(sceneId);
            var rock = Expand(b, RockMargin);
            var fill = Expand(b, BackfillMargin);

            var terrain = Map("Terrain");
            var backfill = Map("Backfill");
            terrain.ClearAllTiles();
            backfill.ClearAllTiles();
            Map("Platforms").ClearAllTiles();
            Map("Deco").ClearAllTiles();

            var open = new HashSet<Vector2Int>();
            foreach (var r in OpenRects(sceneId))
                AddCells(open, r);
            foreach (var room in layout.RoomsInScene(sceneId).Where(r => r.Outdoor))
                foreach (var r in room.Rects)
                    AddCells(open, new RectInt(r.xMin, r.yMin, r.width, fill.yMax - r.yMin));

            // 出口の先の「見た目だけ開けた通路」(2026-10-03 ユーザー指示「出入り口はブロックで埋めずに開けて」)
            var views = ComputeExitViews(sceneId, open, fill);
            var viewCells = new HashSet<Vector2Int>();
            var wallCells = new HashSet<Vector2Int>();
            foreach (var v in views)
            {
                AddCells(viewCells, v.View);
                AddCells(wallCells, v.WallA);
                AddCells(wallCells, v.WallB);
            }
            wallCells.ExceptWith(open);
            wallCells.ExceptWith(viewCells);

            var rule = RuleTile;
            var cover = Tile("stagetile_coverbox");      // Backfill の仮置き (Finish の PaintRock で岩肌に置き換わる)
            var seal = Tile(Area1AssetPrep.RockSealTileName); // 絵なし・当たりあり (縁飾りが出ない)
            var tPos = new List<Vector3Int>();
            var tTiles = new List<TileBase>();
            for (var x = rock.xMin; x < rock.xMax; x++)
            for (var y = rock.yMin; y < rock.yMax; y++)
            {
                var c = new Vector2Int(x, y);
                if (open.Contains(c) || viewCells.Contains(c) || wallCells.Contains(c))
                    continue;
                var isSeal = x < rock.xMin + SealRing || x >= rock.xMax - SealRing || y < rock.yMin + SealRing || y >= rock.yMax - SealRing;
                tPos.Add(new Vector3Int(x, y, 0));
                tTiles.Add(isSeal ? seal : rule);
            }
            // 開けた通路の両側の壁 (岩の外周・Backfill の所も縁が出る RuleTile にして、床・天井の線を画面の外まで続ける)
            foreach (var c in wallCells)
            {
                tPos.Add(new Vector3Int(c.x, c.y, 0));
                tTiles.Add(rule);
            }
            terrain.SetTiles(tPos.ToArray(), tTiles.ToArray());

            // 当たりの止め: 元の掘削の端 (= 以前は岩の壁だった所) に絵なしの当たりを残す (ExitStop タイルマップ)。
            // 遷移ゾーンは今までどおりこの手前で反応し、プレイヤーが場外へ出ることはない
            var stop = ExitStopMap();
            stop.ClearAllTiles();
            var sPos = new List<Vector3Int>();
            foreach (var v in views)
                for (var x = v.Stop.xMin; x < v.Stop.xMax; x++)
                for (var y = v.Stop.yMin; y < v.Stop.yMax; y++)
                    sPos.Add(new Vector3Int(x, y, 0));
            stop.SetTiles(sPos.ToArray(), Enumerable.Repeat<TileBase>(seal, sPos.Count).ToArray());

            var bPos = new List<Vector3Int>();
            for (var x = fill.xMin; x < fill.xMax; x++)
            for (var y = fill.yMin; y < fill.yMax; y++)
            {
                if (open.Contains(new Vector2Int(x, y)) || viewCells.Contains(new Vector2Int(x, y)))
                    continue;
                if (rock.Contains(new Vector2Int(x, y)) && x >= rock.xMin + SealRing && x < rock.xMax - SealRing
                    && y >= rock.yMin + SealRing && y < rock.yMax - SealRing)
                    continue; // 岩の内側は Terrain が描く
                bPos.Add(new Vector3Int(x, y, 0));
            }
            backfill.SetTiles(bPos.ToArray(), Enumerable.Repeat<TileBase>(cover, bPos.Count).ToArray());

            return $"[Carve] bounds={b} rockCells={tPos.Count} backfill={bPos.Count} open={open.Count} exitViews="
                   + string.Join(",", views.Select(v => $"{v.Exit.OtherScene ?? v.Exit.OtherRoom}:{v.Length}{(v.Sky ? "(sky)" : "")}"));
        }

        // ================================================================== 出口の先の見通し

        /// <summary>出口の先を見た目で開けておく長さ (セル)。カメラの半幅 ≈9.8 + 止めの 3 より長く</summary>
        public const int ExitViewLength = 14;
        /// <summary>見通しの止め (当たり) の厚さ。元の掘削の端から</summary>
        public const int ExitStopThickness = 2;

        public class ExitView
        {
            public BoundaryExit Exit;
            public RectInt View;          // 見た目だけ開ける範囲 (掘削の端の外側)
            public RectInt Stop;          // 当たりを残す範囲 (View の掘削側の端 ExitStopThickness セル)
            public RectInt WallA, WallB;  // 通路の両側の壁 (縁を出す RuleTile)。空の通路 (Sky) は床だけ
            public int Length;
            public bool Sky;              // 屋外どうしの横の出口: 床から上を空まで開ける
            public bool Dark;             // 屋内の通路: 奥へ暗くする
        }

        // 最後に Carve したシーンの見通し (Background / Validate が使う)
        private static readonly List<ExitView> ExitViews = new List<ExitView>();
        private static string _exitViewsScene;

        /// <summary>このシーンの出口の見通し (最後の Carve の結果。別シーンなら空)。</summary>
        public static IReadOnlyList<ExitView> ExitViewsOf(string sceneId) =>
            _exitViewsScene == sceneId ? ExitViews : (IReadOnlyList<ExitView>)Array.Empty<ExitView>();

        /// <summary>
        /// 各出口 (他シーンへの通路・移動装置の部屋への通路) の掘削の端の外側に、通路の断面のまま ExitViewLength セルの
        /// 「見通し」を作る。シーン内の他の空間に 2 セル以内まで近づいたら手前で止める (見た目で別の場所とつながらないように)。
        /// 屋外どうしの横の出口は床から上を空まで開ける (境の岩の柱を作らない)。
        /// </summary>
        private static List<ExitView> ComputeExitViews(string sceneId, HashSet<Vector2Int> open, RectInt fill)
        {
            var layout = Area1Layout.Load();
            bool Outdoor(string roomId) { var rm = layout.GetRoom(roomId); return rm != null && rm.Outdoor; }
            bool Indoor(string roomId) { var rm = layout.GetRoom(roomId); return rm != null && !rm.Outdoor; }
            var result = new List<ExitView>();
            foreach (var e in BoundaryExits(sceneId))
            {
                var p = e.Passage;
                var z = e.Carved;
                var horizontal = e.Dir == ExitDir.Left || e.Dir == ExitDir.Right;
                var sky = horizontal && Outdoor(e.OurRoom) && Outdoor(e.OtherRoom);
                int a0, a1; // 断面 (横の出口 = y、縦の出口 = x)
                if (horizontal) { a0 = p.yMin; a1 = sky ? fill.yMax : p.yMax; }
                else { a0 = p.xMin; a1 = p.xMax; }

                RectInt Slice(int k, int pad)
                {
                    switch (e.Dir)
                    {
                        case ExitDir.Right: return new RectInt(z.xMax + k, a0 - pad, 1, a1 - a0 + 2 * pad);
                        case ExitDir.Left: return new RectInt(z.xMin - 1 - k, a0 - pad, 1, a1 - a0 + 2 * pad);
                        case ExitDir.Up: return new RectInt(a0 - pad, z.yMax + k, a1 - a0 + 2 * pad, 1);
                        default: return new RectInt(a0 - pad, z.yMin - 1 - k, a1 - a0 + 2 * pad, 1);
                    }
                }

                var len = 0;
                for (var k = 0; k < ExitViewLength; k++)
                {
                    var s = Slice(k, 0);
                    if (!fill.Contains(s.min) || !fill.Contains(s.max - Vector2Int.one))
                        break;
                    // 先 2 セルまで含めて、他の空間 (部屋・通路) に近づいたら止める
                    var near = false;
                    for (var f = 0; f <= 2 && !near; f++)
                    {
                        var probe = Slice(k + f, sky ? 0 : 2);
                        for (var x = probe.xMin; x < probe.xMax && !near; x++)
                        for (var y = probe.yMin; y < probe.yMax && !near; y++)
                        {
                            var c = new Vector2Int(x, y);
                            near = open.Contains(c) && !z.Contains(c);
                        }
                    }
                    if (near)
                        break;
                    len = k + 1;
                }
                if (len == 0)
                    continue;

                var first = Slice(0, 0);
                var last = Slice(len - 1, 0);
                var view = new RectInt(Mathf.Min(first.xMin, last.xMin), Mathf.Min(first.yMin, last.yMin), 0, 0);
                view.xMax = Mathf.Max(first.xMax, last.xMax);
                view.yMax = Mathf.Max(first.yMax, last.yMax);
                var stopSlice = Slice(Mathf.Min(ExitStopThickness, len) - 1, 0);
                var stop = new RectInt(Mathf.Min(first.xMin, stopSlice.xMin), Mathf.Min(first.yMin, stopSlice.yMin), 0, 0);
                stop.xMax = Mathf.Max(first.xMax, stopSlice.xMax);
                stop.yMax = Mathf.Max(first.yMax, stopSlice.yMax);

                RectInt wallA, wallB;
                if (horizontal)
                {
                    wallA = new RectInt(view.xMin, view.yMin - 2, view.width, 2);                      // 床
                    wallB = sky ? new RectInt() : new RectInt(view.xMin, view.yMax, view.width, 2);    // 天井
                }
                else
                {
                    wallA = new RectInt(view.xMin - 2, view.yMin, 2, view.height);
                    wallB = new RectInt(view.xMax, view.yMin, 2, view.height);
                }

                result.Add(new ExitView
                {
                    Exit = e, View = view, Stop = stop, WallA = wallA, WallB = wallB, Length = len, Sky = sky,
                    Dark = !sky && (Indoor(e.OurRoom) || Indoor(e.OtherRoom)),
                });
            }
            ExitViews.Clear();
            ExitViews.AddRange(result);
            _exitViewsScene = sceneId;
            return result;
        }

        /// <summary>
        /// レシピが出口の見通しの両側の岩を消した (空に開けた: MB1 の観覧車のてっぺん等) 場合、その見通しの
        /// 暗がり (ExitFade_*) と背景板 (Backdrop_ExitView_*) は空に浮いて見えるので消す。Finish が呼ぶ。
        /// </summary>
        private static void PruneExitViewDecor()
        {
            var id = CurrentId;
            var terrain = Map("Terrain");
            var group = Group("Background");
            foreach (var v in ExitViewsOf(id))
            {
                int total = 0, rock = 0;
                foreach (var r in new[] { v.WallA, v.WallB })
                    for (var x = r.xMin; x < r.xMax; x++)
                    for (var y = r.yMin; y < r.yMax; y++)
                    {
                        total++;
                        if (terrain.HasTile(new Vector3Int(x, y, 0))) rock++;
                    }
                if (total == 0 || rock * 2 >= total)
                    continue;
                foreach (var n in new[] { $"ExitFade_{v.Exit.OtherScene ?? v.Exit.OtherRoom}", $"Backdrop_ExitView_{v.Exit.OurRoom}-{v.Exit.OtherRoom}" })
                {
                    var t = group.Find(n);
                    if (t != null)
                        Object.DestroyImmediate(t.gameObject);
                }
            }
        }

        /// <summary>出口の止め (絵なし・当たりあり) のタイルマップ。無ければ Grid の下に作る (古いシーンでも Carve で足される)。</summary>
        private static Tilemap ExitStopMap()
        {
            var grid = ActiveScene.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid");
            if (grid == null)
                throw new InvalidOperationException("Grid がありません (BuildBase 済みか確認)");
            var t = grid.transform.Find("ExitStop");
            if (t != null)
                return t.GetComponent<Tilemap>();
            var tm = NewTilemap(grid.transform, "ExitStop", OrderTerrain, LayerGround);
            tm.gameObject.AddComponent<TilemapCollider2D>();
            return tm;
        }

        private static RectInt Expand(RectInt r, int m) => new RectInt(r.xMin - m, r.yMin - m, r.width + 2 * m, r.height + 2 * m);

        private static void AddCells(HashSet<Vector2Int> set, RectInt r)
        {
            for (var x = r.xMin; x < r.xMax; x++)
            for (var y = r.yMin; y < r.yMax; y++)
                set.Add(new Vector2Int(x, y));
        }

        /// <summary>
        /// 他シーンへの通路ごとに SceneTransitionZone (ExitTo&lt;先&gt;) と PlayerSpawnPoint (SpawnFrom&lt;先&gt;, Id=From&lt;先&gt;) を置く。
        /// 横の出口: 床 = 通路の下端 (岩が出口ゾーンの全幅の下に通る)。スポーンは部屋の内側 2.5、床+1.5。
        /// 縦の出口: 上=到着は穴の真下から落ちてくる / 下=到着は穴の脇の床。位置は MoveSpawn で調整可。
        /// 最初のスポーンを SpawnDefault (Id 空) にも複製する (SetDefaultSpawn で上書き可)。
        /// </summary>
        public static string PlaceTransitions(string sceneId)
        {
            var group = Group("Transitions");
            var log = new StringBuilder("[Transitions]");
            Vector2? first = null;
            foreach (var e in BoundaryExits(sceneId))
            {
                var p = e.Passage;
                if (e.CanExit)
                {
                    var zone = new GameObject("ExitTo" + e.OtherScene);
                    zone.layer = LayerIgnoreRaycast;
                    zone.transform.SetParent(group, false);
                    // ゾーンは通路の外側半分〜延長部 (部屋の縁から 0.5 入った所から)
                    var z = e.Carved;
                    Rect zr;
                    switch (e.Dir)
                    {
                        case ExitDir.Right: zr = Rect.MinMaxRect(p.xMin + 0.5f, p.yMin, z.xMax, p.yMax); break;
                        case ExitDir.Left: zr = Rect.MinMaxRect(z.xMin, p.yMin, p.xMax - 0.5f, p.yMax); break;
                        case ExitDir.Up: zr = Rect.MinMaxRect(p.xMin, p.yMin + 1f, p.xMax, z.yMax); break;
                        default: zr = Rect.MinMaxRect(p.xMin, z.yMin, p.xMax, p.yMax - 1f); break;
                    }
                    zone.transform.position = zr.center;
                    var col = zone.AddComponent<BoxCollider2D>();
                    col.isTrigger = true;
                    col.size = zr.size;
                    var stz = zone.AddComponent<SceneTransitionZone>();
                    Set(stz, "_sceneName", e.OtherScene);
                    Set(stz, "_entranceId", "From" + sceneId);
                    log.Append($" exit->{e.OtherScene}({e.Dir})");
                }

                if (e.CanArrive)
                {
                    Vector2 pos;
                    switch (e.Dir)
                    {
                        case ExitDir.Right: pos = new Vector2(p.xMin - 2.5f, p.yMin + 1.5f); break;
                        case ExitDir.Left: pos = new Vector2(p.xMax + 2.5f, p.yMin + 1.5f); break;
                        case ExitDir.Up: pos = new Vector2(p.center.x, p.yMin - 1.5f); break;
                        default: pos = new Vector2(p.xMax + 1.5f, p.yMax + 1.5f); break;
                    }
                    Spawn("From" + e.OtherScene, pos);
                    first ??= pos;
                    log.Append($" spawn<-{e.OtherScene}");
                }
            }

            if (first.HasValue)
                SetDefaultSpawn(first.Value.x, first.Value.y);
            return log.ToString();
        }

        /// <summary>PlayerSpawnPoint を置く (同じ Id があれば移動)。Id 空 = SpawnDefault。</summary>
        public static GameObject Spawn(string id, Vector2 pos)
        {
            var name = string.IsNullOrEmpty(id) ? "SpawnDefault" : "Spawn" + id;
            var go = FindInGroup("Transitions", name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(Group("Transitions"), false);
                var sp = go.AddComponent<PlayerSpawnPoint>();
                Set(sp, "_id", id ?? "");
            }
            go.transform.position = pos;
            return go;
        }

        public static GameObject SetDefaultSpawn(float x, float y) => Spawn("", new Vector2(x, y));

        /// <summary>入口スポーンの位置を変える。fromScene = 遷移元シーン Id (例 "A1_T1")。</summary>
        public static GameObject MoveSpawn(string fromScene, float x, float y) => Spawn("From" + fromScene, new Vector2(x, y));

        // ================================================================== 地形の塗り

        public static Tilemap Map(string name)
        {
            var grid = ActiveScene.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid");
            var t = grid != null ? grid.transform.Find(name) : null;
            if (t == null)
                throw new InvalidOperationException($"Grid/{name} がありません (BuildBase 済みか確認)");
            return t.GetComponent<Tilemap>();
        }

        private static TileBase _rule;
        public static TileBase RuleTile
        {
            get
            {
                if (_rule != null)
                    return _rule;
                _rule = AssetDatabase.LoadAssetAtPath<TileBase>(RuleTilePath);
                if (_rule == null)
                    throw new InvalidOperationException(RuleTilePath + " がありません。Area1AssetPrep.PrepareRock() を先に実行");
                return _rule;
            }
        }

        /// <summary>Tiles/ (または Tiles/Area1/) のタイルを名前で読む。</summary>
        public static TileBase Tile(string name)
        {
            var t = AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/{name}.asset")
                    ?? AssetDatabase.LoadAssetAtPath<TileBase>($"{TileRoot}/Area1/{name}.asset");
            if (t == null)
                throw new ArgumentException("tile not found: " + name);
            return t;
        }

        private static void FillRect(Tilemap map, int x0, int y0, int x1, int y1, TileBase tile)
        {
            if (x1 <= x0 || y1 <= y0)
                return;
            var n = (x1 - x0) * (y1 - y0);
            var pos = new Vector3Int[n];
            var tiles = new TileBase[n];
            var i = 0;
            for (var x = x0; x < x1; x++)
            for (var y = y0; y < y1; y++)
            {
                pos[i] = new Vector3Int(x, y, 0);
                tiles[i++] = tile;
            }
            map.SetTiles(pos, tiles);
        }

        /// <summary>岩 (当たりあり、RuleTile) で [x0,x1)×[y0,y1) を埋める。</summary>
        public static void Solid(int x0, int y0, int x1, int y1) => FillRect(Map("Terrain"), x0, y0, x1, y1, RuleTile);

        /// <summary>上面 top の床を厚み thickness で敷く (= Solid(x0, top-thickness, x1, top))。</summary>
        public static void Ground(int x0, int x1, int top, int thickness = 3) => Solid(x0, top - thickness, x1, top);

        /// <summary>岩を消して空間にする (Backfill も消して背景を見せる)。</summary>
        public static void Clear(int x0, int y0, int x1, int y1)
        {
            FillRect(Map("Terrain"), x0, y0, x1, y1, null);
            FillRect(Map("Backfill"), x0, y0, x1, y1, null);
        }

        /// <summary>
        /// すり抜け床 (floor_08 l/m/r) を上面 top で [x0,x1) に敷く。スプライト (高さ 0.79) の上端を
        /// セル行 top-1 の上端に合わせる (Tilemap.SetTransformMatrix)。当たりは Platforms の TilemapCollider2D (+PlatformEffector2D)。
        /// </summary>
        public static void OneWay(int x0, int x1, int top)
        {
            var map = Map("Platforms");
            var l = Tile("stagetile_floor_08_l");
            var m = Tile("stagetile_floor_08_m");
            var r = Tile("stagetile_floor_08_r");
            var y = top - 1;
            var sprite = (m as UnityEngine.Tilemaps.Tile)?.sprite;
            var h = sprite != null ? sprite.bounds.size.y : 0.79f;
            var offset = Matrix4x4.Translate(new Vector3(0, 0.5f - h * 0.5f, 0));
            for (var x = x0; x < x1; x++)
            {
                var tile = x1 - x0 == 1 ? m : x == x0 ? l : x == x1 - 1 ? r : m;
                var p = new Vector3Int(x, y, 0);
                map.SetTile(p, tile);
                map.SetTransformMatrix(p, offset);
            }
        }

        /// <summary>
        /// 坂 (ascent/descent_0N, N=1:45° 2:26.6° 3:18.4°) を並べる。(xLow, yLow) = 坂の低い側の端点 (整数)。
        /// ascending=true: 右へ上る / false: 左へ上る (低端が右)。count 枚で count セル上昇。
        /// 坂の胴体は下 1 行だけなので、その下 (yLow-1 より下) は自分で Solid を置くこと。
        /// </summary>
        public static void Slope(int xLow, int yLow, int width, int count, bool ascending)
        {
            var map = Map("Terrain");
            var tile = Tile((ascending ? "stagetile_ascent_0" : "stagetile_descent_0") + width);
            var marker = Tile("stagetile_marker_solid");
            var spriteTile = tile as UnityEngine.Tilemaps.Tile;
            for (var i = 0; i < count; i++)
            {
                // 足元 (w×2 セル) の左下
                var fx = ascending ? xLow + i * width : xLow - (i + 1) * width;
                var fy = yLow - 1 + i;
                // スプライトの中心が足元の中心に来るアンカーを逆算
                var center = new Vector2(fx + width * 0.5f, fy + 1f);
                var off = spriteTile != null ? (Vector2)spriteTile.transform.GetColumn(3) : Vector2.zero;
                var sc = spriteTile != null && spriteTile.sprite != null ? (Vector2)spriteTile.sprite.bounds.center : Vector2.zero;
                var ax = Mathf.RoundToInt(center.x - off.x - sc.x - 0.5f);
                var ay = Mathf.RoundToInt(center.y - off.y - sc.y - 0.5f);
                for (var x = fx; x < fx + width; x++)
                for (var y = fy; y < fy + 2; y++)
                    map.SetTile(new Vector3Int(x, y, 0), marker);
                map.SetTile(new Vector3Int(ax, ay, 0), tile);
            }
        }

        // ================================================================== 汎用の配置

        public static Transform Group(string name)
        {
            var go = ActiveScene.GetRootGameObjects().FirstOrDefault(g => g.name == name) ?? new GameObject(name);
            return go.transform;
        }

        private static GameObject FindInGroup(string group, string name)
        {
            var g = Group(group);
            var t = g.Find(name);
            return t != null ? t.gameObject : null;
        }

        /// <summary>空の GameObject をグループ配下に作る。</summary>
        public static GameObject NewObject(string name, Vector2 pos, int layer = 0, string group = "Gimmicks")
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(Group(group), false);
            go.transform.position = pos;
            return go;
        }

        public static BoxCollider2D Box(GameObject go, Vector2 size, bool trigger, Vector2 offset = default)
        {
            var c = go.AddComponent<BoxCollider2D>();
            c.size = size;
            c.offset = offset;
            c.isTrigger = trigger;
            return c;
        }

        /// <summary>Area1Gen/ からの相対パスで絵を読む。"Assets/" で始まるパスはそのまま (例 PropsRoot + "/coin.png")。</summary>
        public static Sprite Art(string relPath)
        {
            var path = relPath.StartsWith("Assets/") ? relPath : $"{ArtRoot}/{relPath}";
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null)
                throw new ArgumentException("sprite not found: " + relPath + " (Area1AssetPrep.ImportArt 済み?)");
            return s;
        }

        /// <summary>SpriteRenderer を付ける。tiledSize を渡すと Tiled 描画 (FullRect 必須)。</summary>
        public static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order, Vector2? tiledSize = null, Color? color = null)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color ?? Color.white;
            if (tiledSize.HasValue)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = tiledSize.Value;
            }
            return sr;
        }

        /// <summary>飾りのスプライト (当たり無し)。pos はスプライトのピボット位置 (manifest のピボット: 多くは bottom)。</summary>
        public static GameObject Prop(string relPath, float x, float y, float scale = 1f, int order = OrderProp, bool flipX = false, Color? color = null)
        {
            var go = NewObject(Path.GetFileNameWithoutExtension(relPath), new Vector2(x, y), LayerDefault, "Props");
            go.transform.localScale = new Vector3(scale, scale, 1);
            var sr = AddSprite(go, Art(relPath), order, null, color);
            sr.flipX = flipX;
            return go;
        }

        /// <summary>
        /// SerializedObject 経由でフィールドを設定する (private [SerializeField] を名前で)。
        /// 対応: int/float/bool/string/enum(int or 名前)/Vector2/Vector3/Color/Object/string[]/Object[]/Vector2[]。
        /// </summary>
        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
                throw new ArgumentException($"{target.GetType().Name} に {field} がありません");
            Assign(p, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(SerializedProperty p, object value)
        {
            switch (value)
            {
                case null when p.propertyType == SerializedPropertyType.ObjectReference: p.objectReferenceValue = null; break;
                case string s when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = Array.IndexOf(p.enumNames, s); break;
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.intValue = i; break;
                case int i when p.propertyType == SerializedPropertyType.Float: p.floatValue = i; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case double d: p.floatValue = (float)d; break;
                case Enum e: p.intValue = Convert.ToInt32(e); break;
                case Vector2 v when p.propertyType == SerializedPropertyType.Vector3: p.vector3Value = v; break;
                case Vector2 v: p.vector2Value = v; break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                case Object o: p.objectReferenceValue = o; break;
                case LayerMask m: p.intValue = m.value; break;
                case System.Collections.IList list:
                    p.arraySize = list.Count;
                    for (var i = 0; i < list.Count; i++)
                        Assign(p.GetArrayElementAtIndex(i), list[i]);
                    break;
                default: throw new ArgumentException($"unsupported value {value?.GetType().Name} for {p.propertyPath}");
            }
        }

        /// <summary>コンポーネントを付けてフィールドをまとめて設定する。fields = ("_name", value) の並び。</summary>
        public static T AddComp<T>(GameObject go, params (string field, object value)[] fields) where T : Component
        {
            var c = go.AddComponent<T>();
            foreach (var (f, v) in fields)
                Set(c, f, v);
            return c;
        }

        // ================================================================== ギミック

        /// <summary>
        /// 隠し壁 (攻撃で壊れる、BreakableBox + flagId で永続化)。見た目はひび割れタイル。
        /// layer 10 の実体なので岩と同じく足場/壁になる。[x0,x1)×[y0,y1) を塞ぐ。
        /// **1 セル = 1 個の BreakableBox** (子 "Cell_x_y", hp = セルごと)。攻撃の当たり判定に入ったセルだけが壊れる
        /// (2026-10-03 PendingChanges 17 でセルに分けた。2026-10-04 PendingChanges 29: 当たり判定内のセルは同時に壊れてよい)。全セルが同じ flagId を共有し、
        /// 1 セル壊れた時点でフラグが立つ (隠し部屋の幕が開く)。再訪では全セルが消えている。
        /// ※ 岩の Terrain タイルとは別物なので、その範囲の Terrain は Clear しておくこと (このメソッドが消す)。
        /// </summary>
        public static GameObject HiddenWall(int x0, int y0, int x1, int y1, string flagId, int hp = 1)
        {
            FillRect(Map("Terrain"), x0, y0, x1, y1, null);
            FillRect(Map("Backfill"), x0, y0, x1, y1, null); // 壊した後に岩肌が残らないように
            var size = new Vector2(x1 - x0, y1 - y0);
            var go = NewObject("HiddenWall_" + flagId, new Vector2(x0, y0) + size * 0.5f, LayerGround, "Terrain Objects");
            var art = Art("tiles/tile_crack_wall.png");
            foreach (var cell in Cells(go, x0, y0, x1, y1))
            {
                AddSprite(cell, art, OrderTerrain);
                Box(cell, Vector2.one, false);
                AddComp<BreakableBox>(cell, ("_maxHp", hp), ("_flagId", flagId));
            }
            return go;
        }

        /// <summary>[x0,x1)×[y0,y1) の 1 セルずつの子 (名前 Cell_x_y, レイヤーは親と同じ) を作る。</summary>
        private static List<GameObject> Cells(GameObject parent, int x0, int y0, int x1, int y1)
        {
            var list = new List<GameObject>();
            for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
            {
                var cell = new GameObject($"Cell_{x}_{y}") { layer = parent.layer };
                cell.transform.SetParent(parent.transform, false);
                cell.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                list.Add(cell);
            }
            return list;
        }

        /// <summary>移動スキルでのみ壊れるブロック (SkillBreakable)。見た目は各スキル色のタイル。</summary>
        public static GameObject SkillBlock(int x0, int y0, int x1, int y1, PlayerSkill skill, string flagId)
        {
            FillRect(Map("Terrain"), x0, y0, x1, y1, null);
            FillRect(Map("Backfill"), x0, y0, x1, y1, null);
            var size = new Vector2(x1 - x0, y1 - y0);
            var art = skill == PlayerSkill.GroundSlam ? "tiles/tile_slam_block.png"
                : skill == PlayerSkill.ChargeRush ? "tiles/tile_rush_block.png" : "tiles/tile_super_block.png";
            var go = NewObject("SkillBlock_" + flagId, new Vector2(x0, y0) + size * 0.5f, LayerGround, "Terrain Objects");
            // 1 セル = 1 個 (HiddenWall と同じ。スキルは当たったセルだけ砕く = 体の通る穴が開く)。全セルが flagId を共有
            var sprite = Art(art);
            foreach (var cell in Cells(go, x0, y0, x1, y1))
            {
                AddSprite(cell, sprite, OrderTerrain);
                Box(cell, Vector2.one, false);
                AddComp<SkillBreakable>(cell, ("_requiredSkill", (int)skill), ("_flagId", flagId));
            }
            return go;
        }

        /// <summary>トゲ床 [x0,x1) を床面 y の上に (HazardZone, 安全地点へ戻す)。</summary>
        public static GameObject Spikes(int x0, int x1, int y, int damage = 1, bool returnToSafeGround = true)
        {
            var w = x1 - x0;
            var go = NewObject("Spikes", new Vector2(x0 + w * 0.5f, y), LayerIgnoreRaycast, "Gimmicks");
            AddSprite(go, Art("tiles/tile_spikes.png"), OrderGimmick, new Vector2(w, 1f)); // pivot bottom
            Box(go, new Vector2(w - 0.1f, 0.5f), true, new Vector2(0, 0.25f));
            AddComp<HazardZone>(go, ("_damage", damage), ("_returnToSafeGround", returnToSafeGround));
            return go;
        }

        /// <summary>
        /// 梯子 (x = 左端の整数、幅 1、[y0,y1))。見た目は ladder タイル、上り下りは <see cref="ClimbableLadder"/>
        /// (範囲内で上 + ジャンプで掴まる、上端で下 + ジャンプで掴まって降りる)。
        /// 当たりは上端 y1 のすり抜け床 1 枚だけ (登り切ってそこに立つ。横木を跳んで登る形はやめた: PendingChanges 44)。
        /// </summary>
        public static GameObject Ladder(int x, int y0, int y1)
        {
            var go = NewObject("Ladder", new Vector2(x + 0.5f, (y0 + y1) * 0.5f), LayerDefault, "Gimmicks");
            // 見た目は 1 マス幅ちょうど (横木の間隔 = 1/3 マス。元の ladder タイルは絵が 0.27 幅しか無かったので
            // 中身を切り出して 1 マスに合わせた tile_ladder_1cell。PendingChanges 50)
            // 描画順はプレイヤー (0) の後ろ (-1): 掴まっている体の手前に横木が掛からないように
            AddSprite(go, Art("tiles/tile_ladder_1cell.png"), -1, new Vector2(1f, y1 - y0));
            var top = new GameObject("Top") { layer = LayerGround };
            top.transform.SetParent(go.transform, false);
            top.transform.position = new Vector3(x + 0.5f, y1 - 0.125f, 0);
            var c = Box(top, new Vector2(1f, 0.25f), false); // 梯子と同じ 1 マス幅
            c.usedByEffector = true;
            var e = top.AddComponent<PlatformEffector2D>();
            e.useOneWay = true;
            e.surfaceArc = 180f;
            AddComp<ClimbableLadder>(go, ("_halfHeight", (y1 - y0) * 0.5f), ("_topPlatform", c));
            return go;
        }

        /// <summary>ベルトコンベア (上面 top、[x0,x1))。speed 正=右。</summary>
        public static GameObject ConveyorBelt(int x0, int x1, int top, float speed, string reverseFlag = "")
        {
            var w = x1 - x0;
            var go = NewObject("Conveyor", new Vector2(x0 + w * 0.5f, top - 0.25f), LayerGround, "Gimmicks");
            AddSprite(go, Art("tiles/tile_conveyor.png"), OrderGimmick, new Vector2(w, 1f / 3f));
            Box(go, new Vector2(w, 0.5f), false);
            AddComp<Conveyor>(go, ("_speed", speed), ("_reverseFlag", reverseFlag));
            return go;
        }

        /// <summary>往復リフト (lift_platform 3x1)。pos = 下端位置の中心 (上面 = pos.y + 0.5)。offsets は配置位置からの経由点。</summary>
        public static GameObject Lift(Vector2 pos, Vector2[] offsets, float speed = 2f, float wait = 1.5f, string requiredFlag = "")
        {
            var go = NewObject("Lift", pos, LayerGround, "Gimmicks");
            AddSprite(go, Art("platforms/lift_platform.png"), OrderGimmick);
            Box(go, new Vector2(3f, 0.5f), false, new Vector2(0, 0.25f));
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            AddComp<MovingPlatform>(go, ("_mode", 0), ("_waypoints", offsets), ("_speed", speed), ("_waitTime", wait), ("_requiredFlag", requiredFlag));
            return go;
        }

        /// <summary>敵 (Enemy / FlyingEnemy プレハブ)。仮ボス = scale 2 + heavy。</summary>
        public static GameObject Enemy(float x, float y, bool flying = false, float scale = 1f, bool heavy = false, string name = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(flying ? "Assets/Prefabs/FlyingEnemy.prefab" : "Assets/Prefabs/Enemy.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ActiveScene);
            go.transform.SetParent(Group("Enemies"), true);
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = Vector3.one * scale;
            if (name != null)
                go.name = name;
            if (heavy)
                Set(go.GetComponent<EnemyController>(), "_consts",
                    AssetDatabase.LoadAssetAtPath<EnemyConsts>("Assets/Scripts/Enemy/EnemyConsts_Heavy.asset"));
            return go;
        }

        /// <summary>操作ヒントのゾーン [x0,x1)×[y0,y1)。</summary>
        public static GameObject Hint(float x0, float y0, float x1, float y1, string keyboard, string gamepad = null, string name = "Hint")
        {
            var go = NewObject(name, new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), LayerIgnoreRaycast, "TutorialHints");
            Box(go, new Vector2(x1 - x0, y1 - y0), true);
            AddComp<TutorialHintZone>(go, ("_keyboardText", keyboard), ("_gamepadText", gamepad ?? ""));
            return go;
        }

        private static GameObject PickupBase(string name, string sprite, float x, float y, float scale)
        {
            var go = NewObject(name, new Vector2(x, y), LayerInteractable, "Pickups");
            go.transform.localScale = Vector3.one * scale;
            AddSprite(go, Art(sprite), OrderGimmick);
            Box(go, new Vector2(1.4f, 1.4f) / scale, true);
            return go;
        }

        /// <summary>
        /// 糸のピックアップ (糸巻きの絵 = Props/thread_spool.png, 2026-10-03 PendingChanges 14)。pickupId は A1_Pickup_&lt;部屋&gt;_&lt;n&gt;。
        /// 糸の大束 (100 以上) は少し大きく (×1.25) 描く。
        /// </summary>
        public static GameObject ThreadPickupAt(float x, float y, int amount, string pickupId)
        {
            var go = PickupBase("Thread_" + pickupId, PropsRoot + "/thread_spool.png", x, y, amount >= 100 ? 1.25f : 1f);
            AddComp<ThreadPickup>(go, ("_amount", amount), ("_pickupId", pickupId));
            return go;
        }

        /// <summary>
        /// 読み物/キーアイテム (KeyItemPickup: 通知「〜を手に入れた!」+ フラグ)。日誌などのロア用。
        /// sprite 既定は宝箱 (pivot bottom → y は床面)。
        /// </summary>
        public static GameObject KeyItemAt(float x, float y, string itemName, string flagId, string pickupId,
            string sprite = "props/treasure_chest.png", string prompt = "調べる")
        {
            var go = PickupBase("KeyItem_" + pickupId, sprite, x, y, 1f);
            go.GetComponent<BoxCollider2D>().offset = new Vector2(0, 0.5f);
            AddComp<KeyItemPickup>(go, ("_itemName", itemName), ("_flagId", flagId), ("_pickupId", pickupId),
                ("_promptText", prompt), ("_promptOffset", new Vector3(0, 1.6f, 0)));
            return go;
        }

        /// <summary>消耗品のピックアップ (ConsumableItemPickup)。item は ItemDefinition アセットのパス。</summary>
        public static GameObject ConsumablePickupAt(float x, float y, string itemDefinitionPath, int count, string pickupId,
            string sprite = "props/treasure_chest.png")
        {
            var go = PickupBase("Item_" + pickupId, sprite, x, y, 1f);
            AddComp<ConsumableItemPickup>(go, ("_item", AssetDatabase.LoadMainAssetAtPath(itemDefinitionPath)),
                ("_count", count), ("_pickupId", pickupId));
            return go;
        }

        /// <summary>
        /// 壊せる木箱 (BreakableBox, hp 1, 1×1, 絵 = cargo_crate)。(x, floorY) = 箱の下の床面の中心 (箱の中心は floorY + 0.5)。
        /// Rigidbody2D (動的, 横と回転は固定) なので積むと重なって乗り、下の箱を壊すと上の箱が落ちる。
        /// coinId を渡すとコインの箱 (A1_Coin_&lt;部屋&gt;_&lt;n&gt;): 壊すとコインが飛び出し、取った後は箱ごと出ない (2026-10-03 PendingChanges 16)。
        /// 壊した箱は拠点 (SavePoint) で休むまで壊れたまま (BrokenCrateRegistry, PendingChanges 31)。当たり判定内の箱は同時に壊れる (PendingChanges 29)。
        /// コインの箱は見分けがつかないようにする (PendingChanges 20): 絵・色・大きさ・hp・物理・描画順はふつうの木箱と完全に同じ
        /// (色は <see cref="CrateColor"/> 固定で、個別の色付けはできない)。違うのは GameObject 名 (CoinCrate_…, 画面には出ない) だけ。
        /// </summary>
        public static GameObject Crate(float x, float floorY, string coinId = null)
        {
            var go = NewObject(string.IsNullOrEmpty(coinId) ? "Crate" : "CoinCrate_" + coinId,
                new Vector2(x, floorY + 0.5f), LayerGround, "Gimmicks");
            AddSprite(go, Art("props/cargo_crate.png"), OrderGimmick, null, CrateColor);
            Box(go, new Vector2(0.96f, 1f), false);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.mass = 5f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var coinPrefab = string.IsNullOrEmpty(coinId) ? null
                : AssetDatabase.LoadAssetAtPath<CoinPiece>(CoinPrefabPath)
                  ?? throw new InvalidOperationException(CoinPrefabPath + " がありません (SceneScripts/prep_props.cs)");
            AddComp<BreakableBox>(go, ("_maxHp", 1), ("_coinId", coinId ?? ""), ("_coinPrefab", coinPrefab));
            return go;
        }

        /// <summary>壊せる木箱の色 (コインの有無に関係なく全部この色)。</summary>
        public static readonly Color CrateColor = new Color(0.92f, 0.84f, 0.74f);

        /// <summary>
        /// 積んだ木箱 (下から順に Crate を重ねる)。coinIndex 番目 (0 = 一番下) の箱にコイン (coinId)。coinId が null ならコイン無し。
        /// </summary>
        public static GameObject[] CrateStack(float x, float floorY, int count, string coinId = null, int coinIndex = 0)
        {
            var list = new List<GameObject>();
            for (var i = 0; i < count; i++)
                list.Add(Crate(x, floorY + i, i == coinIndex ? coinId : null));
            return list.ToArray();
        }

        /// <summary>セーブポイント (SavePoint.prefab にベンチの絵を差す)。y = 床面。</summary>
        public static GameObject SavePointAt(float x, float y)
        {
            var go = NewObject("SavePoint", new Vector2(x, y), LayerInteractable, "Gimmicks");
            AddSprite(go, Art("props/bench_save.png"), OrderProp); // キャラクターより奥 (プレイヤーは 0)
            Box(go, new Vector2(3f, 1.6f), true, new Vector2(0, 0.8f));
            AddComp<SavePoint>(go, ("_promptOffset", new Vector3(0, 2f, 0)));
            return go;
        }

        /// <summary>
        /// 壊せるスピーカー。y = 床面。design = Drive「speaker」の絵 1〜3 (gimmick/speaker/speaker_0N_a/_b を交互に = 鳴っている、
        /// 2026-10-08 PendingChanges 42)。当たりは絵の大きさから (幅 0.75・高さ 0.9)。壊れた絵は <see cref="SpeakerBrokenArt"/>。
        /// </summary>
        public static GameObject SpeakerAt(float x, float y, string flagId, int design = 1)
        {
            var go = NewObject("Speaker_" + flagId, new Vector2(x, y), LayerGround, "Gimmicks");
            var a = Art(SpeakerArt(design, "a"));
            var sr = AddSprite(go, a, OrderGimmick);
            var size = sr.sprite.bounds.size;
            Box(go, new Vector2(size.x * 0.75f, size.y * 0.9f), false, new Vector2(sr.sprite.bounds.center.x, size.y * 0.45f));
            AddComp<SpriteFlipbook>(go, ("_frames", new Object[] { a, Art(SpeakerArt(design, "b")) }), ("_fps", 3f), ("_phase", x * 0.37f));
            AddComp<BreakableSpeaker>(go, ("_flagId", flagId));
            return go;
        }

        /// <summary>スピーカーの絵のパス (design 1〜3, state = a / b / broken)。</summary>
        public static string SpeakerArt(int design, string state) => $"{GimmickRoot}/speaker/speaker_0{design}_{state}.png";

        /// <summary>壊れたスピーカーの絵 (Prop に渡す。pivot = 床の中心)。</summary>
        public static string SpeakerBrokenArt(int design) => SpeakerArt(design, "broken");

        /// <summary>
        /// ボス部屋: arena = [x0,x1)×[y0,y1) のトリガー + BossArena、ボス (仮ボス: 2 倍 + Heavy)、
        /// gates = 閉じる壁の矩形 (layer 10, 非アクティブ, fence_gate の絵)。
        /// threadDrop = ボスが落とす糸 (EnemyController._threadDropOverride。0 なら定数アセットのまま)。
        /// Heavy は普通の重装甲の敵にも使うので、ボスの糸はここで上書きする (2026-10-03 PendingChanges 18: 20〜30 本 → 12 粒)。
        /// groundBossArt = 地上ボスの絵 (Sprite のパス。子 Visual の野良ウマの代わりに使い、アニメは野良ウマと同じ)。
        /// 空なら仮の四角のまま (<see cref="KeepPlaceholderLook"/>)。
        /// </summary>
        public static GameObject BossRoom(RectInt arena, string flagId, Vector2 bossPos, bool flying, RectInt[] gates, string defeatMessage,
            string followUpMessage = "", bool followUpUnlessRed = false, int threadDrop = 20, string groundBossArt = null)
        {
            var go = NewObject("BossArena_" + flagId, arena.center, LayerIgnoreRaycast, "Gimmicks");
            Box(go, arena.size, true);
            var boss = Enemy(bossPos.x, bossPos.y, flying, 2f, true, "Boss_" + flagId);
            if (!flying && string.IsNullOrEmpty(groundBossArt))
                KeepPlaceholderLook(boss);
            else if (!flying)
                UseGroundBossArt(boss, groundBossArt);
            if (threadDrop > 0)
                Set(boss.GetComponent<EnemyController>(), "_threadDropOverride", threadDrop);
            var gateObjs = new List<GameObject>();
            foreach (var g in gates)
            {
                var gate = GateObject("Gate", g, OrderGimmick);
                gate.SetActive(false);
                gateObjs.Add(gate);
            }
            AddComp<BossArena>(go, ("_flagId", flagId), ("_boss", boss.GetComponent<EnemyController>()),
                ("_gates", gateObjs.ToArray()), ("_defeatMessage", defeatMessage),
                ("_followUpMessage", followUpMessage ?? ""), ("_followUpUnlessUpgrade", followUpUnlessRed),
                ("_followUpUpgrade", ScissorUpgrade.Red));
            return go;
        }

        /// <summary>
        /// 地上の仮ボスは雑魚 (Enemy.prefab = 野良ウマ, 2026-10-08 PendingChanges 42) と同じ見た目にしない: 子 Visual を隠し、
        /// ルートの仮の四角 (SpriteRenderer) を有効に戻してそれに色・向き・フラッシュをかける。ボスの絵が決まるまでの措置。
        /// </summary>
        public static void KeepPlaceholderLook(GameObject enemy)
        {
            var ec = enemy.GetComponent<EnemyController>();
            var root = enemy.GetComponent<SpriteRenderer>();
            root.enabled = true;
            Set(ec, "_visual", root);
            Set(ec, "_visualFacesLeft", false);
            Set(ec, "_hitFlashColor", Color.white);
            var vis = enemy.transform.Find("Visual");
            if (vis != null)
                vis.gameObject.SetActive(false);
            var anim = enemy.GetComponent<EnemyAnimator>();
            if (anim != null)
                Object.DestroyImmediate(anim);
        }

        /// <summary>
        /// 地上ボスの絵: 子 Visual (野良ウマと同じ Animator = 弾む・駆ける・被弾・撃破) の絵だけを差し替える。
        /// 大きさとピボットは絵の取り込み設定で当たり (Enemy 1x1, ボスは ×2) に合わせておく
        /// (歯車の重馬 = bigHorse, Docs/Area1/SceneScripts/prep_newart.cs, PendingChanges 43)。絵は左向き。
        /// </summary>
        public static void UseGroundBossArt(GameObject enemy, string spritePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath)
                         ?? throw new ArgumentException("sprite not found: " + spritePath);
            var vis = enemy.transform.Find("Visual") ?? throw new ArgumentException("Visual が無い: " + enemy.name);
            var sr = vis.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            EditorUtility.SetDirty(sr);
            var ec = enemy.GetComponent<EnemyController>();
            Set(ec, "_visual", sr);
            Set(ec, "_visualFacesLeft", true);
        }

        /// <summary>
        /// 開き戸 (Drive「door」, gimmick/door/door_01..04 = 閉 → 開。2026-10-08 PendingChanges 42) で通路 r を塞ぐ FlagDoor。
        /// 扉は元絵の縦横比のまま高さ doorHeight (既定 4 = 元絵 3.48 の ×1.15) で、r の下端の中央に立てる。
        /// 当たり = 幅 r.width × 高さ doorHeight (layer 10)。扉より上 (r.yMin + doorHeight 〜 r.yMax) は岩 (Solid) で塞いで、
        /// 通路の高さを扉に合わせる (以前は扉の絵を通路の高さいっぱいに引き伸ばしていた)。
        /// フラグが立つと上へスライドせず、その場でコマ送りして開き、開いた扉の絵が残る (当たりは消える)。
        /// flip = 扉が左へ開く (蝶番が右)。order は既定で岩 (0) の裏。
        /// </summary>
        public static GameObject FlagSwingDoor(string name, RectInt r, string flagId, int order = -1, bool flip = false,
            float duration = 0.6f, int doorHeight = 4, string group = "Gimmicks")
        {
            var h = Mathf.Min(doorHeight, r.height);
            if (h < r.height)
                Solid(r.xMin, r.yMin + h, r.xMax, r.yMax);
            var door = NewObject(name, new Vector2(r.center.x, r.yMin), LayerGround, group);
            Box(door, new Vector2(r.width, h), false, new Vector2(0, h * 0.5f));
            var frames = new[] { "01", "02", "03", "04" }.Select(n => (Object)Art($"{GimmickRoot}/door/door_{n}.png")).ToArray();
            var sp = new GameObject("Sprite");
            sp.transform.SetParent(door.transform, false);
            var first = (Sprite)frames[0];
            var k = h / first.bounds.size.y;
            sp.transform.localScale = new Vector3(k, k, 1);
            var sr = AddSprite(sp, first, order);
            sr.flipX = flip;
            AddComp<FlagDoor>(door, ("_flagId", flagId), ("_swingRenderer", sr), ("_swingFrames", frames), ("_slideDuration", duration));
            return door;
        }

        /// <summary>
        /// レバー (Drive「lever」: 台座 lever_02 + 傾く柄 lever_01, 2026-10-08 PendingChanges 42)。(x, floorY) = 床の上の中心。
        /// ルート = 柄の回転の中心 (台座の半円の中心, 床 + 0.11×scale)。ルートの SpriteRenderer = 台座、子 "Handle" = 柄 (台座の後ろ)。
        /// 高さ ≈ 3.42×scale (既定 0.6 → 2.05)。handle = 柄の絵のファイル名 (lever_01 / lever_01_red / lever_01_teal)。
        /// LeverSwitch は呼び出し側で付ける (_handle は付けてから SetLeverHandle で渡す)。
        /// </summary>
        public static GameObject LeverAt(string name, float x, float floorY, float scale = 0.6f, string handle = "lever_01.png")
        {
            var lever = NewObject(name, new Vector2(x, floorY + 60f / 546f * scale), LayerInteractable, "Gimmicks");
            lever.transform.localScale = new Vector3(scale, scale, 1);
            AddSprite(lever, Art($"{GimmickRoot}/lever/lever_02.png"), OrderGimmick);
            var h = new GameObject("Handle");
            h.transform.SetParent(lever.transform, false);
            h.transform.localRotation = Quaternion.Euler(0, 0, 30f); // LeverSwitch._handleOffAngle と同じ (作動前は左へ倒れている)
            AddSprite(h, Art($"{GimmickRoot}/lever/{handle}"), OrderGimmick - 1);
            // 当たり (トリガー) は台座と柄を覆う (ワールドで 1.44×2.04 相当 at 0.6)
            Box(lever, new Vector2(2.4f, 3.4f), true, new Vector2(0, 1.6f));
            return lever;
        }

        /// <summary>LeverAt の柄を LeverSwitch に渡す (作動で柄だけが傾く)。</summary>
        public static LeverSwitch SetLeverHandle(GameObject lever)
        {
            var sw = lever.GetComponent<LeverSwitch>();
            Set(sw, "_handle", lever.transform.Find("Handle"));
            return sw;
        }

        /// <summary>
        /// 柵の門 (fence_gate) で矩形 r を塞ぐ実体 (layer 10, BoxCollider = r)。絵は子 "Sprite" で、
        /// 画像上部の透明な余白 (高さの約 22%) を見込んで縦を伸ばし、矩形いっぱいに見せる。
        /// ルートは (r.center.x, r.yMin)。BossRoom のゲートや FlagDoor の扉に使う。
        /// </summary>
        public static GameObject GateObject(string name, RectInt r, int order = OrderGimmick, string group = "Gimmicks")
        {
            var gate = NewObject(name, new Vector2(r.center.x, r.yMin), LayerGround, group);
            Box(gate, new Vector2(r.width, r.height), false, new Vector2(0, r.height * 0.5f));
            var sp = new GameObject("Sprite");
            sp.transform.SetParent(gate.transform, false);
            sp.transform.localScale = new Vector3(r.width / 2f, r.height / (4f * 0.78f), 1);
            AddSprite(sp, Art("props/fence_gate.png"), order); // 2x4, pivot bottom
            return gate;
        }

        /// <summary>
        /// 会話 NPC (NpcTalk, layer 11 + トリガー)。y = 床面 (npc/ の絵は pivot bottom)。
        /// talkedFlag + rewardThread で一度きりの糸のご褒美。
        /// </summary>
        public static GameObject NpcAt(float x, float y, string sprite, string speakerName, string[] lines,
            string[] repeatLines = null, string talkedFlag = "", int rewardThread = 0, bool flipX = false)
        {
            var go = NewObject("Npc_" + speakerName, new Vector2(x, y), LayerInteractable, "Gimmicks");
            var sr = AddSprite(go, Art(sprite), OrderGimmick);
            sr.flipX = flipX;
            var h = sr.sprite.bounds.size.y;
            Box(go, new Vector2(1.6f, Mathf.Max(2f, h)), true, new Vector2(0, Mathf.Max(2f, h) * 0.5f));
            AddComp<NpcTalk>(go, ("_speakerName", speakerName), ("_lines", lines), ("_repeatLines", repeatLines ?? new string[0]),
                ("_talkedFlag", talkedFlag), ("_rewardThread", rewardThread), ("_promptOffset", new Vector3(0, h + 0.4f, 0)));
            return go;
        }

        /// <summary>
        /// Utage の会話 NPC (NpcTalkPoint, layer 11 + トリガー)。y = 床面 (npc/ の絵は pivot bottom)。
        /// セリフは Assets/Dialogue/Scenarios/Dialogue.xls のラベル (label = 初回, repeatLabel = 2回目以降)。
        /// talkedFlag + rewardThread で一度きりの糸のご褒美。name は GameObject 名 (Npc_<name>)。
        /// </summary>
        public static GameObject NpcDialogueAt(float x, float y, string sprite, string name, string label,
            string repeatLabel = "", string talkedFlag = "", int rewardThread = 0, bool flipX = false)
        {
            var go = NewObject("Npc_" + name, new Vector2(x, y), LayerInteractable, "Gimmicks");
            var sr = AddSprite(go, Art(sprite), OrderGimmick);
            sr.flipX = flipX;
            var h = sr.sprite.bounds.size.y;
            Box(go, new Vector2(1.6f, Mathf.Max(2f, h)), true, new Vector2(0, Mathf.Max(2f, h) * 0.5f));
            AddComp<NpcTalkPoint>(go, ("_scenarioLabel", label), ("_repeatLabel", repeatLabel ?? ""),
                ("_talkedFlag", talkedFlag), ("_rewardThread", rewardThread), ("_promptOffset", new Vector3(0, h + 0.4f, 0)));
            return go;
        }

        /// <summary>
        /// 移動装置 (TransportInteractable, layer 11 + トリガー size)。pos = トリガー中心。
        /// レイアウトでシーンを持たない部屋 (E1 搬出エレベーター / WL 非常用リフト) の両端に置く。
        /// </summary>
        public static GameObject TransportAt(Vector2 pos, Vector2 size, string sceneName, string entranceId,
            string[] requiredFlags = null, string prompt = "乗る", string lockedMessage = "動かない…",
            string activateFlag = "", string activateMessage = "", string name = "Transport")
        {
            var go = NewObject(name, pos, LayerInteractable, "Gimmicks");
            Box(go, size, true);
            AddComp<TransportInteractable>(go, ("_sceneName", sceneName), ("_entranceId", entranceId),
                ("_requiredFlags", requiredFlags ?? new string[0]), ("_promptText", prompt), ("_lockedMessage", lockedMessage),
                ("_activateFlag", activateFlag), ("_activateMessage", activateMessage), ("_promptOffset", new Vector3(0, size.y * 0.5f + 0.6f, 0)));
            return go;
        }

        // ================================================================== 背景

        /// <summary>
        /// 背景: 全室屋内 → 地下の背景 (bg_underground, カメラ追従の視差)。屋外の部屋がある →
        /// 夜空 (カメラ追従) + 園内のシルエット (地面 y=0、横に視差) + 屋内の部屋ごとに地下の背景板。
        /// </summary>
        public static string Background(string sceneId)
        {
            var layout = Area1Layout.Load();
            var rooms = layout.RoomsInScene(sceneId).ToList();
            var group = Group("Background");
            for (var i = group.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(group.GetChild(i).gameObject);
            var b = Bounds(sceneId);
            var center = b.center;
            var anyOutdoor = rooms.Any(r => r.Outdoor);

            if (!anyOutdoor)
            {
                // 縦横とも Tiled で敷き、カメラに 0.85 で追従 (段違いの機械室が奥に続いて見える)
                AddParallax(group, "Underground", Art("bg/bg_underground.png"), center, new Vector2(0.85f, 0.85f), 3.2f,
                    b.width + 60f, OrderSky, b.height + 60f);
                ExitViewFades(group, sceneId);
                return "[Background] underground";
            }

            AddParallax(group, "NightSky", Art("bg/bg_night_sky.png"), center, new Vector2(1f, 1f), 3.2f, 0f, OrderSky);
            var sil = AddParallax(group, "ParkSilhouette", Art("bg/bg_park_silhouette.png"), new Vector2(center.x, 0f),
                new Vector2(0.6f, 0f), 3f, b.width * 0.4f + 80f, OrderFarBg);
            foreach (var room in rooms.Where(r => !r.Outdoor))
            foreach (var r in room.Rects)
            {
                IndoorBackdrop(group, "Backdrop_" + room.Id, r);
            }
            // 屋内の部屋どうしをつなぐ通路 (同一シーン内 / 他シーンへの掘削) にも背景板 (空が覗かないように)
            bool Indoor(string roomId) { var rm = layout.GetRoom(roomId); return rm != null && !rm.Outdoor; }
            var passageRects = new List<(string name, RectInt rect)>();
            foreach (var l in LinksOf(sceneId))
                if (l.HasPassage && layout.SceneOfRoom(l.A) == layout.SceneOfRoom(l.B) && (Indoor(l.A) || Indoor(l.B)))
                    passageRects.Add(($"Backdrop_{l.A}-{l.B}", l.Passage));
            foreach (var e in BoundaryExits(sceneId))
                if (Indoor(e.OurRoom) && Indoor(e.OtherRoom))
                    passageRects.Add(($"Backdrop_{e.OurRoom}-{e.OtherRoom}", e.Carved));
            // 出口の先の見通し: 屋内の通路は背景板も先まで続ける
            foreach (var v in ExitViewsOf(sceneId))
                if (!v.Sky && (Indoor(v.Exit.OurRoom) || Indoor(v.Exit.OtherRoom)))
                    passageRects.Add(($"Backdrop_ExitView_{v.Exit.OurRoom}-{v.Exit.OtherRoom}", v.View));
            foreach (var (name, r) in passageRects)
            {
                IndoorBackdrop(group, name, r);
            }
            ExitViewFades(group, sceneId);
            return "[Background] outdoor sky+silhouette" + (sil != null ? "" : "");
        }

        /// <summary>
        /// 屋内の出口の見通しの奥半分を暗く沈める (通路が闇の奥へ続いて見える。床・天井の縁も一緒に沈める order 1)。
        /// 掘削の端から 4 セルは素の明るさ (プレイヤーが立てる所)。
        /// </summary>
        private static void ExitViewFades(Transform group, string sceneId)
        {
            foreach (var v in ExitViewsOf(sceneId))
            {
                if (!v.Dark || v.Length < 6)
                    continue;
                var r = v.View;
                var keep = 3;                 // 手前の明るいまま残す長さ
                var len = v.Length - keep;
                var horizontal = v.Exit.Dir == ExitDir.Left || v.Exit.Dir == ExitDir.Right;
                Vector2 c; float across; float rot;
                switch (v.Exit.Dir)
                {
                    case ExitDir.Right: c = new Vector2(r.xMax - len * 0.5f, r.center.y); rot = 90f; break;
                    case ExitDir.Left: c = new Vector2(r.xMin + len * 0.5f, r.center.y); rot = -90f; break;
                    case ExitDir.Up: c = new Vector2(r.center.x, r.yMax - len * 0.5f); rot = 180f; break;
                    default: c = new Vector2(r.center.x, r.yMin + len * 0.5f); rot = 0f; break;
                }
                across = (horizontal ? r.height : r.width) + 4f; // 両側の壁の縁 (2 セル) まで
                var go = new GameObject($"ExitFade_{v.Exit.OtherScene ?? v.Exit.OtherRoom}");
                go.transform.SetParent(group, false);
                go.transform.position = c;
                go.transform.rotation = Quaternion.Euler(0, 0, rot);
                var sw = 32f / Area1AssetPrep.PixelsPerUnit;
                go.transform.localScale = new Vector3(across / sw, len, 1f);
                AddSprite(go, Art("fx/shade.png"), OrderTerrain + 1, null, new Color(1f, 1f, 1f, 0.8f));
            }
        }

        /// <summary>
        /// 屋外シーンの屋内の部屋・通路の背景板。polish B: 細かい格子に見えた machine_room_wall (1 セル周期) をやめ、
        /// 地下シーンと同じ機械室の絵 (bg/bg_underground, 倍率 3.2) を矩形に Tiled で敷く (視差なし、少し暗く)。
        /// </summary>
        private static GameObject IndoorBackdrop(Transform group, string name, RectInt r)
        {
            const float k = 3.2f;
            var go = new GameObject(name);
            go.transform.SetParent(group, false);
            go.transform.position = r.center;
            go.transform.localScale = new Vector3(k, k, 1f);
            AddSprite(go, Art("bg/bg_underground.png"), OrderRoomBackdrop, (Vector2)r.size / k, new Color(0.72f, 0.68f, 0.8f));
            return go;
        }

        private static GameObject AddParallax(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 factor,
            float scale, float tiledWidthWorld, int order, float tiledHeightWorld = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(anchor.x, anchor.y, 0);
            go.transform.localScale = new Vector3(scale, scale, 1);
            var size = sprite.bounds.size;
            AddSprite(go, sprite, order, tiledWidthWorld > 0
                ? new Vector2(tiledWidthWorld / scale, tiledHeightWorld > 0 ? tiledHeightWorld / scale : size.y)
                : (Vector2?)null);
            AddComp<ParallaxLayer>(go, ("_factor", factor), ("_anchor", anchor));
            return go;
        }

        /// <summary>
        /// 大観覧車 (共通プレハブ) をワールド (54,252) に置く。W*/U4 シーンで使う。
        /// tint を渡すと全部品の色に乗算する (支柱の中など、奥の観覧車を暗く沈めて手前の足場と見分けやすくする)。
        /// </summary>
        public static GameObject PlaceFerrisWheel(bool decorGondolas = true, bool baseStation = true, Color? tint = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Area1AssetPrep.FerrisPrefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ActiveScene);
            go.transform.SetParent(Group("Background"), true);
            go.transform.position = Area1AssetPrep.FerrisCenter;
            var dg = go.transform.Find("DecorGondolas");
            if (dg != null) dg.gameObject.SetActive(decorGondolas);
            var bs = go.transform.Find("BaseStation");
            if (bs != null) bs.gameObject.SetActive(baseStation);
            if (tint.HasValue)
                foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.color *= tint.Value;
            return go;
        }

        /// <summary>
        /// 視差つきの背景の絵 (Background グループ, ParallaxLayer)。pos = カメラが anchor にある時の表示位置。
        /// factor 1 = カメラに張り付く (無限遠) / 0 = ワールド固定。遠景の観覧車 (屋外シーン) などに使う。
        /// tiledSize を渡すと Tiled 描画 (ワールド単位ではなくスプライトのローカル単位)。
        /// SceneView では pos に表示される (ゲーム中はカメラに合わせて動く)。
        /// </summary>
        public static GameObject ParallaxProp(string relPath, Vector2 pos, Vector2 factor, Vector2 anchor, float scale = 1f,
            int order = OrderFarBg - 1, Color? color = null, Vector2? tiledSize = null)
        {
            var go = new GameObject(Path.GetFileNameWithoutExtension(relPath));
            go.transform.SetParent(Group("Background"), false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1);
            AddSprite(go, Art(relPath), order, tiledSize, color);
            AddComp<ParallaxLayer>(go, ("_factor", factor), ("_anchor", anchor));
            return go;
        }

        // ---- 中景の観覧車 (W1〜W4 共通)。全シーン同じ値なので、遷移しても同じカメラ位置では同じ見え方になる
        public static readonly Vector2 MidWheelPos = new Vector2(40f, 40f);      // カメラが MidWheelAnchor にある時の輪の中心
        public static readonly Vector2 MidWheelAnchor = new Vector2(40f, 0f);
        public static readonly Vector2 MidWheelFactor = new Vector2(0.85f, 0.82f); // 1 に近いほど遠い (カメラ 1 上がると輪は 0.18 下がって見える)
        public const float MidWheelRadius = 36f;

        /// <summary>
        /// 中景の観覧車 (飾り, 当たり無し, Background グループ, order -93..-91)。地上〜外周 (W1〜W4) のどこからでも
        /// 「巨大な輪の中にいる」ように見せる: 本物 (54,252 半径 90) は地上近くでは画面に入らないので、
        /// 縮めた輪 (半径 36, 部品 0.9 倍) を視差 (MidWheelFactor) で重ね、ゆっくり回す (WheelSpin)。
        /// 外周のトラス + 電球 (3 組で順に明滅) + スポーク 16 本 (4 本おきに電球の連なり) + ハブ + 吊られたゴンドラ 12 台 (常に直立)。
        /// 地上 (カメラ y≈8) では外周の下端とゴンドラが画面の上 1/3 を通り過ぎ、登るほどスポークがハブへ集まって見える。
        /// tint で全体を沈める (支柱の中など)。足場と見分けがつくよう暗い紫のシルエット + 暖色の電球。
        /// polish B: カメラが fadeStartY→fadeEndY (既定 135→152) へ上がると薄れて消える (FadeByCameraHeight)。W3 の上端から先は本物の外周・スポークだけ。
        /// </summary>
        public static GameObject FerrisMidground(float spinDegPerSec = 1.2f, Color? tint = null, float alpha = 0.9f,
            float fadeStartY = 135f, float fadeEndY = 152f)
        {
            const float k = 0.9f;
            var R = MidWheelRadius;
            var t = tint ?? Color.white;
            var steel = new Color(0.36f * t.r, 0.3f * t.g, 0.52f * t.b, alpha);
            var root = NewObject("FerrisMidground", MidWheelPos, LayerDefault, "Background");
            AddComp<ParallaxLayer>(root, ("_factor", MidWheelFactor), ("_anchor", MidWheelAnchor));
            // polish B: 本物の外周 (y≈162) が画面に入る高さ (カメラ y 135→152) で消す = 縮めた輪と本物の輪が同時に見えない
            AddComp<FadeByCameraHeight>(root, ("_fadeStartY", fadeStartY), ("_fadeEndY", fadeEndY));
            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform, false);

            GameObject Part(string name, Transform parent, string art, Vector2 pos, float ang, Vector2 scale, int order, Color col,
                Vector2? tiled = null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0, 0, ang);
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                AddSprite(go, Art(art), order, tiled, col);
                return go;
            }

            // スポーク
            for (var i = 0; i < 16; i++)
            {
                var a = i * 22.5f;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                var len = R - 0.6f;
                Part($"Spoke{i:00}", ring.transform, "ferris/spoke_beam.png", dir * (len * 0.5f), a, new Vector2(k, k), -93,
                    steel, new Vector2(len / k, 0.5f));
            }
            // 外周のトラス
            var segLen = 6f * k;
            var n = Mathf.RoundToInt(2f * Mathf.PI * R / segLen);
            var chord = 2f * R * Mathf.Sin(Mathf.PI / n);
            for (var i = 0; i < n; i++)
            {
                var a = (i + 0.5f) * 360f / n;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Part($"Truss{i:00}", ring.transform, "ferris/rim_truss.png", dir * R * Mathf.Cos(Mathf.PI / n), a - 90f,
                    new Vector2(chord / 6f * 1.01f, k), -92, steel);
            }
            // 電球: 外周 (3 組で順に明滅 = 流れる電飾) + 4 本おきのスポークに沿った連なり
            var bulbCol = new Color(1f, 0.86f, 0.62f, 0.85f * alpha);
            var groups = new Transform[3];
            for (var g = 0; g < 3; g++)
            {
                groups[g] = new GameObject("Bulbs" + g).transform;
                groups[g].SetParent(ring.transform, false);
                AddComp<BlinkingLight>(groups[g].gameObject, ("_period", 1.8f), ("_phase", g / 3f), ("_minAlpha", 0.35f), ("_onRatio", 0.6f));
            }
            var bn = n * 2;
            for (var i = 0; i < bn; i++)
            {
                var a = i * 360f / bn;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Part($"Bulb{i:000}", groups[i % 3], "ferris/rim_bulb.png", dir * (R + 0.75f), 0, Vector2.one * 0.75f, -91, bulbCol);
            }
            for (var i = 0; i < 16; i += 4)
            {
                var a = i * 22.5f;   // 4 本おきのスポークに沿わせる
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                for (var d = 6f; d < R - 2f; d += 2.4f)
                    Part($"SpokeBulb{i:00}_{d:00}", groups[Mathf.RoundToInt(d / 2.4f) % 3], "ferris/rim_bulb.png", dir * d, 0,
                        Vector2.one * 0.5f, -91, bulbCol);
            }
            // ハブ
            Part("Hub", ring.transform, "ferris/hub_axle.png", Vector2.zero, 0, new Vector2(k, k), -91, steel);
            // ゴンドラ 12 台 (吊り金具の上端を外周に、常に直立)
            var upright = new List<Object>();
            for (var i = 0; i < 12; i++)
            {
                var a = (i + 0.5f) * 30f;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                var g = new GameObject($"Gondola{i:00}").transform;
                g.SetParent(ring.transform, false);
                g.localPosition = dir * R;
                Part("Hanger", g, "ferris/gondola_hanger.png", Vector2.zero, 0, Vector2.one * 0.7f, -92, steel);
                // 乗れるゴンドラ (W5) と見間違えないよう、暗く小さめのシルエットにする
                Part("Cabin", g, "platforms/gondola_platform.png", new Vector2(0, -1.95f), 0, Vector2.one * 0.62f, -91,
                    new Color(0.44f * t.r, 0.38f * t.g, 0.58f * t.b, 0.85f * alpha));
                upright.Add(g);
            }
            AddComp<WheelSpin>(ring, ("_speed", spinDegPerSec), ("_upright", upright.ToArray()));
            return root;
        }

        /// <summary>
        /// 屋外の街灯 (飾り, 当たり無し, Props)。支柱 = 観覧車の脚の絵を細く Tiled、灯 = boss_door_lamp_on。
        /// (x, floorY) = 支柱の根元。height = 灯の高さ。
        /// </summary>
        public static GameObject LampPost(float x, float floorY, float height = 5f, Color? lamp = null, int order = -6)
        {
            var go = NewObject("LampPost", new Vector2(x, floorY), LayerDefault, "Props");
            var pole = new GameObject("Pole");
            pole.transform.SetParent(go.transform, false);
            pole.transform.localPosition = new Vector3(0, height * 0.5f, 0);
            pole.transform.localScale = new Vector3(0.3f, 1f, 1f);
            AddSprite(pole, Art("ferris/support_leg.png"), order, new Vector2(2f, height), new Color(0.42f, 0.36f, 0.52f));
            var head = new GameObject("Lamp");
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, height + 0.25f, 0);
            head.transform.localScale = Vector3.one * 0.75f;
            AddSprite(head, Art("props/boss_door_lamp_on.png"), order + 1, null, lamp ?? new Color(1f, 0.85f, 0.6f));
            return go;
        }

        // ================================================================== 観覧車の構造 (W 区画)

        /// <summary>
        /// 鉄骨の足場 (すり抜け、layer 10 + PlatformEffector2D)。見た目は ferris/leg_crossbrace (筋交いの梁) の Tiled。
        /// 上面 = top (絵の梁の上端も top に揃える)、[x0,x1)。OneWay と同じく ↓+ジャンプで降りられる。
        /// 観覧車の支柱の中・外周の点検路など「機械を登っている」所で floor_08 の代わりに使う。
        /// </summary>
        public static GameObject Girder(int x0, int x1, int top, Color? color = null, string name = "Girder")
        {
            var w = x1 - x0;
            var go = NewObject(name, new Vector2(x0 + w * 0.5f, top), LayerGround, "Terrain Objects");
            var art = new GameObject("Beam");
            art.transform.SetParent(go.transform, false);
            // 絵の梁は 1 セルの高さの中央 55% (上端 = 中心 +0.266)
            art.transform.localPosition = new Vector3(0, -0.266f, 0);
            AddSprite(art, Art("ferris/leg_crossbrace.png"), OrderPlatforms, new Vector2(w, 1f), color);
            var c = Box(go, new Vector2(w, 0.6f), false, new Vector2(0, -0.3f));
            c.usedByEffector = true;
            var e = go.AddComponent<PlatformEffector2D>();
            e.useOneWay = true;
            e.surfaceArc = 180f;
            return go;
        }

        /// <summary>
        /// 背景の飾りの柱 (ferris/support_leg を縦に Tiled、当たり無し)。(x, y0..y1) の中心線、scale で太さ
        /// (1 で約 0.55 幅、2.5 で約 1.4 幅)。group は既定 Props (飾りの散布はこの周りを避ける) / "Background" も可。
        /// </summary>
        public static GameObject Truss(float x, float y0, float y1, float scale = 1.5f, int order = -45, Color? color = null,
            float angleDeg = 0f, string group = "Props")
        {
            var go = NewObject("Truss", new Vector2(x, (y0 + y1) * 0.5f), LayerDefault, group);
            go.transform.localScale = new Vector3(scale, 1f, 1f);
            go.transform.rotation = Quaternion.Euler(0, 0, angleDeg);
            AddSprite(go, Art("ferris/support_leg.png"), order, new Vector2(2f, y1 - y0), color ?? new Color(0.62f, 0.52f, 0.72f));
            return go;
        }

        /// <summary>背景の飾りの梁 (ferris/leg_crossbrace を横に Tiled、当たり無し)。y = 梁の中心、scale で太さ。</summary>
        public static GameObject Brace(float x0, float x1, float y, float scale = 1f, int order = -44, Color? color = null,
            string group = "Props")
        {
            var go = NewObject("Brace", new Vector2((x0 + x1) * 0.5f, y), LayerDefault, group);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            AddSprite(go, Art("ferris/leg_crossbrace.png"), order, new Vector2((x1 - x0) / scale, 1f), color ?? new Color(0.62f, 0.52f, 0.72f));
            return go;
        }

        /// <summary>
        /// 屋内だけのシーンの背景 (地下の機械室) を夜空に差し替える。観覧車の支柱の中など、
        /// 鉄骨の隙間から空と観覧車 (PlaceFerrisWheel) が見える場所に使う。BuildBase の後に呼ぶ。
        /// </summary>
        public static GameObject SkyBackdrop()
        {
            var group = Group("Background");
            var under = group.Find("Underground");
            if (under != null)
                Object.DestroyImmediate(under.gameObject);
            var center = Bounds(CurrentId).center;
            return AddParallax(group, "NightSky", Art("bg/bg_night_sky.png"), center, new Vector2(1f, 1f), 3.2f, 0f, OrderSky);
        }

        /// <summary>
        /// 横風 (WindArea, layer 2) を [x0,x1)×[y0,y1) に置く。gust=true で吹く/止むを繰り返す (on/off 秒)。
        /// 吹いている間だけ見える風の線 (ParticleSystem, 子 "WindLines") を _activeVisual に付ける。
        /// velocity.x 負 = 左へ押す。歩き速度は 8 (それ未満なら逆らって進める)。
        /// </summary>
        public static GameObject Wind(int x0, int y0, int x1, int y1, Vector2 velocity, bool gust = true, float on = 2.5f,
            float off = 2f, float phase = 0f, float groundedMultiplier = 0.7f, string name = "Wind")
        {
            var size = new Vector2(x1 - x0, y1 - y0);
            var go = NewObject(name, new Vector2(x0, y0) + size * 0.5f, LayerIgnoreRaycast, "Gimmicks");
            Box(go, size, true);

            var lines = new GameObject("WindLines");
            lines.transform.SetParent(go.transform, false);
            var ps = lines.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var vis = velocity * 4f;   // 見た目は速く流す
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = Mathf.Max(0.5f, size.x / Mathf.Max(1f, vis.magnitude) * 1.1f);
            main.startSpeed = 0f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.92f, 0.9f, 1f, 0.6f);
            main.maxParticles = 400;
            var em = ps.emission;
            em.rateOverTime = Mathf.Clamp(size.x * size.y / 8f, 10f, 160f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x, size.y, 0.1f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(vis.x * 0.8f, vis.x * 1.2f);
            vel.y = new ParticleSystem.MinMaxCurve(vis.y * 0.8f - 0.3f, vis.y * 1.2f + 0.3f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var pr = lines.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.06f;
            pr.lengthScale = 3f;
            pr.sortingOrder = OrderGimmick + 1;
            pr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");

            AddComp<WindArea>(go, ("_velocity", velocity), ("_groundedMultiplier", groundedMultiplier),
                ("_pattern", gust ? 1 : 0), ("_gustOnTime", on), ("_gustOffTime", off), ("_phaseOffset", phase),
                ("_activeVisual", lines));
            return go;
        }

        /// <summary>
        /// 隠し部屋の目隠し: 壁を壊すまで部屋の中が見えないよう、rects のセルを周りの壁と同じ質感 (岩肌 / PlateWalls の範囲は鉄板)
        /// の前面タイルマップ (Grid/Cover_&lt;flag&gt;, order 5, 当たり無し) で覆い、flagId が立つと消す (FlagActivator)。
        /// 部屋の内側の縁タイルも隠すよう、rects は部屋より 1 セル広めに。ひび壁 (HiddenWall) のセルには掛けないこと。
        /// PlateWalls は先に呼んでおく。
        /// </summary>
        public static GameObject HiddenRoomCover(string flagId, float shade, params RectInt[] rects)
        {
            var grid = ActiveScene.GetRootGameObjects().First(g => g.name == "Grid");
            var tm = NewTilemap(grid.transform, "Cover_" + flagId, OrderGimmick + 2, LayerDefault);
            var pn = Area1AssetPrep.PlateTexPeriod;
            foreach (var rect in rects)
                for (var x = rect.xMin; x < rect.xMax; x++)
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    var p = new Vector3Int(x, y, 0);
                    if (IsPlate(x, y))
                    {
                        tm.SetTile(p, Tile(Area1AssetPrep.PlateTexTileName(Mod(-y, pn), Mod(x, pn))));
                        tm.SetColor(p, new Color(shade, shade, shade, 1f));
                    }
                    else
                    {
                        // 周りの岩肌 (PaintRock) と同じ反転・色むら
                        var (r, c, m) = RockCell(x, y);
                        tm.SetTile(p, Tile(Area1AssetPrep.RockTexTileName(r, c)));
                        tm.SetTransformMatrix(p, m);
                        var nz = RockNoise(x, y);
                        tm.SetColor(p, new Color(shade * nz.r, shade * nz.g, shade * nz.b, 1f));
                    }
                }
            var act = NewObject("CoverFlag_" + flagId, rects[0].center, LayerDefault, "Gimmicks");
            AddComp<FlagActivator>(act, ("_flags", new[] { flagId }), ("_hideWhenSet", new Object[] { tm.gameObject }));
            return tm.gameObject;
        }

        // ================================================================== 地下の機械 (D 区画)

        /// <summary>
        /// 配管の足場 (すり抜け、layer 10 + PlatformEffector2D)。見た目は rock/decal_pipe_h (錆びた配管) を 0.8 倍で横に Tiled。
        /// 上面 = top (配管の胴の上端も top に揃える。フランジが少しはみ出る)、[x0,x1)。↓+ジャンプで降りられる。
        /// D 区画 (地下の機械室) で floor_08 の代わりに使う。
        /// </summary>
        public static GameObject PipePlatform(int x0, int x1, int top, Color? color = null, string name = "Pipe")
        {
            var w = x1 - x0;
            var go = NewObject(name, new Vector2(x0 + w * 0.5f, top), LayerGround, "Terrain Objects");
            var art = new GameObject("PipeArt");
            art.transform.SetParent(go.transform, false);
            // 絵の胴は高さの 75% (上端 = 中心 +0.372)。0.8 倍で上端 = 中心 +0.30
            art.transform.localPosition = new Vector3(0, -0.3f, 0);
            art.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            AddSprite(art, Art("rock/decal_pipe_h.png"), OrderPlatforms, new Vector2(w / 0.8f, 1f), color);
            var c = Box(go, new Vector2(w, 0.6f), false, new Vector2(0, -0.3f));
            c.usedByEffector = true;
            var e = go.AddComponent<PlatformEffector2D>();
            e.useOneWay = true;
            e.surfaceArc = 180f;
            return go;
        }

        /// <summary>
        /// 飾りの配管 (当たり無し)。(x0,y0)→(x1,y1) の水平 (y0==y1) か垂直 (x0==x1) の線。thickness = 太さ (ワールド)。
        /// 既定は奥の壁に這わせる暗め (order -6)。group は既定 Props (岩の飾りの散布はこの周りを避ける)。
        /// </summary>
        public static GameObject PipeRun(float x0, float y0, float x1, float y1, float thickness = 0.7f, int order = -6,
            Color? color = null, string group = "Props")
        {
            var vertical = Mathf.Approximately(x0, x1);
            var len = vertical ? Mathf.Abs(y1 - y0) : Mathf.Abs(x1 - x0);
            var go = NewObject(vertical ? "PipeV" : "PipeH", new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), LayerDefault, group);
            go.transform.localScale = new Vector3(thickness, thickness, 1f);
            AddSprite(go, Art(vertical ? "rock/decal_pipe_v.png" : "rock/decal_pipe_h.png"), order,
                vertical ? new Vector2(1f, len / thickness) : new Vector2(len / thickness, 1f), color ?? new Color(0.62f, 0.56f, 0.7f));
            return go;
        }

        /// <summary>放射状の光暈 (fx/glow, 当たり無し)。灯の周りに。radius = 半径 (ワールド)。</summary>
        public static GameObject Glow(float x, float y, float radius, Color color, int order = -6, string group = "Props")
        {
            var go = NewObject("Glow", new Vector2(x, y), LayerDefault, group);
            var s = radius * 2f / (256f / Area1AssetPrep.PixelsPerUnit);
            go.transform.localScale = new Vector3(s, s, 1f);
            AddSprite(go, Art("fx/glow.png"), order, null, color);
            return go;
        }

        /// <summary>
        /// 点滅する灯 (BlinkingLight)。boss_door_lamp_on の小さな灯 + 光暈。本道の目印に並べ、phase を少しずつずらすと進む向きへ流れて見える。
        /// hanging = 天井から吊る (y = 天井)、false = 壁に付けた灯 (y = 灯の中心)。
        /// </summary>
        public static GameObject GuideLamp(float x, float y, float phase, Color color, bool hanging = true, float period = 1.4f,
            string name = "GuideLamp")
        {
            var go = NewObject(name, new Vector2(x, y), LayerDefault, "Props");
            var lampY = hanging ? -0.55f : 0f;
            if (hanging)
            {
                var wire = new GameObject("Wire");
                wire.transform.SetParent(go.transform, false);
                wire.transform.localPosition = new Vector3(0, -0.2f, 0);
                AddSprite(wire, Art("rock/decal_chain_hanging.png"), OrderProp - 1, null, new Color(0.5f, 0.45f, 0.58f));
                wire.transform.localScale = new Vector3(0.22f, 0.15f, 1f);
            }
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(go.transform, false);
            lamp.transform.localPosition = new Vector3(0, lampY, 0);
            lamp.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            if (hanging)
                lamp.transform.localRotation = Quaternion.Euler(0, 0, 180f);
            AddSprite(lamp, Art("props/boss_door_lamp_on.png"), OrderProp, null, color);
            var glow = new GameObject("Halo");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0, lampY, 0);
            var gs = 1.9f / (256f / Area1AssetPrep.PixelsPerUnit);
            glow.transform.localScale = new Vector3(gs, gs, 1f);
            AddSprite(glow, Art("fx/glow.png"), OrderProp - 1, null, new Color(color.r, color.g, color.b, 0.5f));
            AddComp<BlinkingLight>(go, ("_period", period), ("_phase", phase), ("_minAlpha", 0.12f), ("_onRatio", 0.45f));
            return go;
        }

        /// <summary>
        /// 蒸気の噴き出し口 (HazardZone の周期 ON/OFF, layer 2)。(x, y) = 噴き出し口の根元 (床置きなら床面 y、天井なら天井面 y)。
        /// down=true で天井から下へ噴く。length = 蒸気の柱の長さ (根元から)。on/off = 噴く/止む秒、phase = 周期のずれ (秒)。
        /// 見た目: 口 (platforms/steam_vent) + 噴いている間だけ蒸気の柱 (steam_puff, 揺らぎ) + 噴く 0.6 秒前の予兆 (小さな湯気と赤い光)。
        /// 当たりは柱の幅 0.9 × (根元 +0.5 〜 先端)。ダメージ後はノックバックのみ (安全地点へは戻さない)。
        /// </summary>
        public static GameObject SteamVent(float x, float y, float length, float on = 1.2f, float off = 2f, float phase = 0f,
            bool down = false, int damage = 1, string name = "Steam")
        {
            var dir = down ? -1f : 1f;
            var go = NewObject(name, new Vector2(x, y), LayerIgnoreRaycast, "Gimmicks");
            var nozzle = new GameObject("Nozzle");
            nozzle.transform.SetParent(go.transform, false);
            nozzle.transform.localScale = new Vector3(0.75f, 0.75f * dir, 1f);
            nozzle.transform.localPosition = new Vector3(0, -0.12f * dir, 0);   // 少し床/天井に埋める
            AddSprite(nozzle, Art("platforms/steam_vent.png"), OrderGimmick - 4);

            var start = 0.5f;
            var colLen = Mathf.Max(0.5f, length - start);
            Box(go, new Vector2(0.9f, colLen), true, new Vector2(0, dir * (start + colLen * 0.5f)));

            var fx = new GameObject("SteamFx");
            fx.transform.SetParent(go.transform, false);
            fx.transform.localPosition = new Vector3(0, dir * 0.45f, 0);
            // polish B: 蒸気の柱を部品で組む = 口のふき出し (steam_base_burst) + 縦に Tiled した柱 (steam_column_seg, 口から離れる向きへ流れる
            // A1_SteamRise) + 先端の綿雲 (steam_top_puff)。どれも縦横比そのまま (潰さない)。下向きは "Flip" (上下反転) の中に組むので
            // 流れる向きも反転して合う。下向きの先端 (床・水面) には上向きのふき出しを置いて「当たって広がる」湯気にする。
            var L = Mathf.Max(0.6f, length - 0.45f);       // fx の原点 (口 +0.45) から先端まで
            const float burstK = 1.4f, colK = 2.2f, puffK = 1.5f;
            var flip = new GameObject("Flip");
            flip.transform.SetParent(fx.transform, false);
            flip.transform.localScale = new Vector3(1f, dir, 1f);
            var steamCol = new Color(1f, 1f, 1f, 0.92f);
            var mouthK = down ? burstK * 0.7f : burstK;
            var burstH = 1f * mouthK;
            var burst = PartSprite(flip.transform, "Burst", "polish/steam_base_burst.png", new Vector2(0, -0.35f), mouthK,
                OrderGimmick + 1, steamCol);
            AddComp<SpriteWobble>(burst.gameObject, ("_scaleAmplitude", new Vector2(0.06f, 0.08f)), ("_speed", 13f), ("_phase", x));
            var tipH = down ? 1f * burstK : 1.5f * puffK;  // 先端の部品の高さ (下向き = ふき出し / 上向き = 綿雲)
            var colStart = burstH * 0.45f - 0.35f;
            var colEnd = L - tipH * (down ? 0.55f : 0.6f);
            if (colEnd > colStart + 0.1f)
            {
                // 柱 (pivot bottom)。Tiled の高さは絵の単位 (= ワールド / colK)
                PartSprite(flip.transform, "Column", "polish/steam_column_seg.png", new Vector2(0, colStart), colK, OrderGimmick + 1,
                    steamCol, new Vector2(1f, (colEnd - colStart) / colK), "A1_SteamRise");
            }
            if (!down)
            {
                var puffY = Mathf.Max(colStart, L - tipH);
                var puff = PartSprite(flip.transform, "TopPuff", "polish/steam_top_puff.png", new Vector2(0, puffY),
                    L < tipH + 0.6f ? puffK * Mathf.Clamp((L + 0.35f) / (tipH + 0.6f), 0.6f, 1f) : puffK, OrderGimmick + 2, steamCol);
                AddComp<SpriteWobble>(puff.gameObject, ("_scaleAmplitude", new Vector2(0.07f, 0.05f)), ("_speed", 9f), ("_phase", x + 1.3f));
            }
            else
            {
                // 下向きの先端 (床・水面) = 上向きのふき出し (当たって左右へ広がる湯気) + 白いにじみ
                var tipY = -L - 0.12f;
                var tip = PartSprite(fx.transform, "TipBurst", "polish/steam_base_burst.png", new Vector2(0, tipY), burstK, OrderGimmick + 2, steamCol);
                AddComp<SpriteWobble>(tip.gameObject, ("_scaleAmplitude", new Vector2(0.1f, 0.12f)), ("_speed", 11f), ("_phase", x + 0.7f));
                var core = new GameObject("SplashCore");
                core.transform.SetParent(fx.transform, false);
                core.transform.localPosition = new Vector3(0, tipY + 0.3f, 0);
                var cs = 1.6f / (256f / Area1AssetPrep.PixelsPerUnit);
                core.transform.localScale = new Vector3(cs, cs * 0.6f, 1f);
                AddSprite(core, Art("fx/glow.png"), OrderGimmick + 1, null, new Color(1f, 1f, 1f, 0.45f));
            }
            fx.SetActive(false);

            var warn = new GameObject("SteamHiss");
            warn.transform.SetParent(go.transform, false);
            warn.transform.localPosition = new Vector3(0, dir * 0.45f, 0);
            // 予兆の湯気: 口の先に小さな白いもやが 3 つ、細かく震える (シューッ)
            HissBlobs(warn.transform, Vector3.zero, dir);
            var hg = new GameObject("WarnGlow");
            hg.transform.SetParent(warn.transform, false);
            var gs = 1.6f / (256f / Area1AssetPrep.PixelsPerUnit);
            hg.transform.localScale = new Vector3(gs, gs, 1f);
            AddSprite(hg, Art("fx/glow.png"), OrderGimmick - 3, null, new Color(1f, 0.35f, 0.3f, 0.8f));
            if (down)
            {
                // 先端にも予兆 (細い湯気が落ちてくる + 着地点がぼんやり赤い)
                HissBlobs(warn.transform, new Vector3(0, -(length - 0.45f), 0), 1f, 0.5f);
                var tg = new GameObject("TipGlow");
                tg.transform.SetParent(warn.transform, false);
                tg.transform.localPosition = new Vector3(0, -(length - 0.45f), 0);
                var ts = 2.2f / (256f / Area1AssetPrep.PixelsPerUnit);
                tg.transform.localScale = new Vector3(ts, ts * 0.45f, 1f);
                AddSprite(tg, Art("fx/glow.png"), OrderGimmick - 3, null, new Color(1f, 0.35f, 0.3f, 0.55f));
            }
            warn.SetActive(false);

            AddComp<HazardZone>(go, ("_damage", damage), ("_returnToSafeGround", false), ("_cycle", true),
                ("_onTime", on), ("_offTime", off), ("_phaseOffset", phase), ("_warningTime", 0.6f),
                ("_activeVisual", fx), ("_warningVisual", warn));
            return go;
        }

        // 蒸気の予兆のもや (fx/glow の白い小さなにじみ 3 つ + 震え)。dir = 噴く向き (+1 上 / -1 下)
        private static void HissBlobs(Transform parent, Vector3 at, float dir, float alpha = 0.7f)
        {
            var offs = new[] { new Vector2(0f, 0.25f), new Vector2(-0.22f, 0.55f), new Vector2(0.2f, 0.8f) };
            for (var i = 0; i < offs.Length; i++)
            {
                var b = new GameObject("Hiss" + i);
                b.transform.SetParent(parent, false);
                b.transform.localPosition = at + new Vector3(offs[i].x, offs[i].y * dir, 0);
                var k = (0.55f - i * 0.1f) / (256f / Area1AssetPrep.PixelsPerUnit);
                b.transform.localScale = new Vector3(k, k, 1f);
                AddSprite(b, Art("fx/glow.png"), OrderGimmick + 1, null, new Color(1f, 1f, 1f, alpha - i * 0.15f));
                AddComp<SpriteWobble>(b, ("_scaleAmplitude", new Vector2(0.3f, 0.3f)), ("_moveAmplitude", new Vector2(0.05f, 0.08f)),
                    ("_speed", 24f + i * 5f), ("_phase", i * 2.1f));
            }
        }

        /// <summary>
        /// 水 (WaterZone, layer 2: 中では移動 0.6 倍・ジャンプ 0.9 倍の高さ・ゆっくり落ちる)。[x0,x1)×[y0,surfaceY)。
        /// 見た目 (polish B): `WaterVisual` = 半透明の水の模様 (奥, order -6, ゆっくり流れる) + 同じ模様をごく薄く手前 (order 5, プレイヤーが透ける)
        /// + 水面の帯 (横へ流れる + 上下の揺らぎ)。look = Oil で油 (D4s)。tint は模様の色に乗算 (省略 = 地下の水の明るさ)。
        /// </summary>
        public static GameObject WaterBody(int x0, int y0, int x1, int surfaceY, Color? tint = null, string name = "Water",
            WaterLook look = WaterLook.Water)
        {
            var w = x1 - x0;
            var h = surfaceY - y0;
            var go = NewObject(name, new Vector2(x0 + w * 0.5f, y0 + h * 0.5f), LayerIgnoreRaycast, "Gimmicks");
            Box(go, new Vector2(w, h), true);
            AddComp<WaterZone>(go);
            var vis = WaterVisual(x0, y0, x1, surfaceY, look, tint ?? new Color(0.62f, 0.76f, 0.86f), OrderProp - 1, OrderGimmick + 2,
                "Visual", "Gimmicks");
            vis.transform.SetParent(go.transform, true);
            return go;
        }

        /// <summary>
        /// ベルトコンベアを床に埋めて置く (ConveyorBelt の見た目違い)。床の上面 top の [x0,x1) の最上段 1 セルを掘り、
        /// 当たり (Conveyor, layer 10) を top-1..top に。見た目 = 矢印の帯 (流れに合わせて動く・向きが変わる, ConveyorBeltVisual) +
        /// 暗い機械の胴 + 両端の回るローラー。speed 正=右、reverseFlag が立つと逆。
        /// </summary>
        public static GameObject Belt(int x0, int x1, int top, float speed, string reverseFlag = "", string name = "Belt")
        {
            Clear(x0, top - 1, x1, top);
            var go = ConveyorBelt(x0, x1, top, speed, reverseFlag);
            go.name = name;
            var w = x1 - x0;
            Object.DestroyImmediate(go.GetComponent<SpriteRenderer>());
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(w, 1f);
            box.offset = new Vector2(0, -0.25f);      // ルートは top-0.25 → 当たり top-1..top

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, -0.915f, 0);   // 当たりの下端より 1 セル下まで (掘った段の下の縁タイルを隠す)
            AddSprite(body, Art("ferris/leg_crossbrace.png"), OrderGimmick - 1, new Vector2(w, 1.67f), new Color(0.42f, 0.36f, 0.5f));
            // 筋交いの絵は隙間が透けるので、後ろに暗い鉄板 (不透明) を敷く
            var backing = new GameObject("Backing");
            backing.transform.SetParent(go.transform, false);
            backing.transform.localPosition = new Vector3(0, -0.915f, 0);
            AddSprite(backing, Art("ferris/plate/plate_tex_r0_c0.png"), OrderGimmick - 2, new Vector2(w, 1.67f), new Color(0.24f, 0.2f, 0.28f));

            const float period = 1f / 3f;
            var belt = new GameObject("BeltArt");
            belt.transform.SetParent(go.transform, false);
            belt.transform.localPosition = new Vector3(0, 0.25f - period * 0.5f, 0);
            AddSprite(belt, Art("tiles/tile_conveyor.png"), OrderGimmick, new Vector2(w + period, period));

            var rollers = new List<Transform>();
            foreach (var rx in new[] { -w * 0.5f, w * 0.5f })
            {
                var r = new GameObject("Roller");
                r.transform.SetParent(go.transform, false);
                r.transform.localPosition = new Vector3(rx, -0.25f, 0);
                r.transform.localScale = new Vector3(0.27f, 0.27f, 1f);
                AddSprite(r, Art("platforms/gear_platform.png"), OrderGimmick + 1, null, new Color(0.75f, 0.68f, 0.82f));
                rollers.Add(r.transform);
            }
            AddComp<ConveyorBeltVisual>(go, ("_conveyor", go.GetComponent<Conveyor>()), ("_belt", belt.transform), ("_period", period),
                ("_rollers", rollers.Select(t => (Object)t).ToArray()), ("_rollerRadius", 0.5f));
            return go;
        }

        /// <summary>
        /// 暗がりの帯 (fx/shade の縦グラデーション, 当たり無し)。[x0,x1)×[y0,y1) で下ほど濃い (下端 alphaBottom → 上端 0)。
        /// 既定 order -9 = 岩肌 (Backfill) と部屋の背景の前・岩の飾りや足場の後ろ (遊びの見やすさは変えずに奥だけ沈める)。
        /// flip=true で上ほど濃い。
        /// </summary>
        public static GameObject Shade(float x0, float y0, float x1, float y1, float alphaBottom = 0.6f, bool flip = false,
            int order = OrderBackfill + 1, string group = "Props")
        {
            var go = NewObject("Shade", new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), LayerDefault, group);
            var sw = 32f / Area1AssetPrep.PixelsPerUnit;
            go.transform.localScale = new Vector3((x1 - x0) / sw, (y1 - y0) * (flip ? -1f : 1f), 1f);
            AddSprite(go, Art("fx/shade.png"), order, null, new Color(1f, 1f, 1f, alphaBottom));
            return go;
        }

        /// <summary>
        /// 岩肌の色を縦のグラデーションで沈める (TintRock を 1 行ずつ)。y0 の行に bottom、y1 の行に top を掛け、間は線形。
        /// 縦穴で「下ほど暗い」を出す時に。
        /// </summary>
        public static void TintRockGradient(int x0, int y0, int x1, int y1, Color bottom, Color top)
        {
            for (var y = y0; y < y1; y++)
            {
                var t = y1 - 1 == y0 ? 0f : (y - y0) / (float)(y1 - 1 - y0);
                TintRock(x0, y, x1, y + 1, Color.Lerp(bottom, top, t));
            }
        }

        // 岩肌の色づけ (Finish の PaintRock の後に掛ける)
        private static readonly List<(RectInt rect, Color tint)> RockTints = new List<(RectInt, Color)>();

        /// <summary>
        /// [x0,x1)×[y0,y1) の岩肌 (Backfill) の色に tint を乗算する (陰影は残る)。Finish が PaintRock の後に適用する。
        /// 空に浮いた売店の外壁を砂糖菓子色にする等、岩以外の素材に見せたい時に使う。BuildBase で消える。
        /// </summary>
        public static void TintRock(int x0, int y0, int x1, int y1, Color tint) =>
            RockTints.Add((new RectInt(x0, y0, x1 - x0, y1 - y0), tint));

        private static string ApplyRockTints()
        {
            if (RockTints.Count == 0)
                return "";
            var backfill = Map("Backfill");
            var n = 0;
            foreach (var (r, tint) in RockTints)
                for (var x = r.xMin; x < r.xMax; x++)
                for (var y = r.yMin; y < r.yMax; y++)
                {
                    var p = new Vector3Int(x, y, 0);
                    if (!backfill.HasTile(p)) continue;
                    backfill.SetColor(p, backfill.GetColor(p) * tint);
                    n++;
                }
            return $"[TintRock] {n} cells\n";
        }

        // ================================================================== 岩の見た目 (質感 + 飾り)

        /// <summary>Finish が使う飾りの密度 (BuildBase で 1 に戻る)。0 = 飾りなし。</summary>
        public static float DecorDensity = 1f;

        /// <summary>
        /// 岩の飾りの密度を決める (Finish が地形の完成後に <see cref="ScatterRockDecor"/> を流す)。
        /// 1 = 標準、0.5 = 控えめ、0 = なし。BuildBase の後ならどこで呼んでもよい。
        /// </summary>
        public static void RockDecor(float density = 1f) => DecorDensity = Mathf.Max(0f, density);

        // 鉄板の壁にする範囲 (PaintRock が岩肌の代わりに a1_plate_tex を敷く)。BuildBase で消える
        private static readonly List<RectInt> PlateRects = new List<RectInt>();

        /// <summary>
        /// [x0,x1)×[y0,y1) の岩の見た目を「鉄板の壁」(ferris/machine_room_wall を明るくした 2x2 周期) にする。
        /// 当たり・縁タイルはそのまま。観覧車の支柱の中・整備用の縦穴・外周の物置など、岩ではおかしい所に使う。
        /// 岩の飾りもこの範囲では機械系 (歯車・配管・リベット板) だけになる。BuildBase で消える。
        /// 素材が無ければ Area1AssetPrep.PreparePlate() を先に流す。
        /// </summary>
        public static void PlateWalls(int x0, int y0, int x1, int y1) =>
            PlateRects.Add(new RectInt(x0, y0, x1 - x0, y1 - y0));

        /// <summary>シーン全体の岩を鉄板の壁にする (PlateWalls の全域版)。</summary>
        public static void PlateWallsAll() => PlateWalls(-100000, -100000, 100000, 100000);

        private static bool IsPlate(int x, int y)
        {
            foreach (var r in PlateRects)
                if (x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax)
                    return true;
            return false;
        }

        /// <summary>岩肌の陰影: 部屋の縁から RockShadeDepth セルで明るさ RockShadeMin まで暗くなる。</summary>
        public const int RockShadeDepth = 8;
        public const float RockShadeMin = 0.5f;

        private static int Mod(int a, int m) => ((a % m) + m) % m;

        private static int FloorDiv(int a, int b) => (int)Mathf.Floor(a / (float)b);

        // ワールドのセル座標から決まる整数ハッシュ (シーンに依らない = 隣のシーンと同じ場所は同じ見た目)
        private static uint CellHash(int x, int y, uint salt)
        {
            unchecked
            {
                var h = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u ^ salt * 0xcb1ab31fu;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return h;
            }
        }

        /// <summary>
        /// 岩肌のセル (x,y) に敷くスライス (行 r・列 c) と反転。4x4 のブロックごとに左右・上下の反転をハッシュで決める
        /// (ブロック内のスライスの並びも鏡に合わせて逆にするので、ブロックの境も含めて継ぎ目は出ない)。
        /// </summary>
        public static (int r, int c, Matrix4x4 m) RockCell(int x, int y)
        {
            var n = Area1AssetPrep.RockTexPeriod;
            var r = Mod(-y, n);
            var c = Mod(x, n);
            var h = CellHash(FloorDiv(x, n), FloorDiv(-y, n), 17u);
            var fx = (h & 1u) != 0;
            var fy = (h & 2u) != 0;
            if (fx) c = n - 1 - c;
            if (fy) r = n - 1 - r;
            var m = fx || fy ? Matrix4x4.Scale(new Vector3(fx ? -1f : 1f, fy ? -1f : 1f, 1f)) : Matrix4x4.identity;
            return (r, c, m);
        }

        /// <summary>岩肌の色むらの強さ (明るさ ±)。0 で無効</summary>
        public const float RockNoiseAmount = 0.12f;

        // なめらかな値ノイズ (格子 span セル、smoothstep 補間) 0..1
        private static float ValueNoise(int x, int y, int span, uint salt)
        {
            var gx = FloorDiv(x, span); var gy = FloorDiv(y, span);
            var tx = (x - gx * span + 0.5f) / span; var ty = (y - gy * span + 0.5f) / span;
            tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
            float V(int ix, int iy) => (CellHash(ix, iy, salt) & 0xffff) / 65535f;
            var a = Mathf.Lerp(V(gx, gy), V(gx + 1, gy), tx);
            var b = Mathf.Lerp(V(gx, gy + 1), V(gx + 1, gy + 1), tx);
            return Mathf.Lerp(a, b, ty);
        }

        /// <summary>岩肌のセル (x,y) の色むら (乗算色)。7 セル周期の明暗 (+3 セル周期の弱い明暗) + 13 セル周期のわずかな赤み/青み。</summary>
        public static Color RockNoise(int x, int y)
        {
            var v = (ValueNoise(x, y, 7, 3u) - 0.5f) * 2f * RockNoiseAmount + (ValueNoise(x, y, 3, 11u) - 0.5f) * 2f * RockNoiseAmount * 0.4f;
            var hue = (ValueNoise(x, y, 13, 5u) - 0.5f) * 2f * RockNoiseAmount * 0.6f;
            return new Color(1f + v + hue, 1f + v, 1f + v - hue, 1f);
        }

        /// <summary>
        /// 岩肌を敷く: Terrain の岩 (A1TerrainRule / 外周の a1_rock_seal) と Backfill の全セルに、
        /// Backfill タイルマップへ rock_tex のスライスを位置で敷く (セル (x,y) = 行 (-y) mod 4・列 x mod 4 → 4x4 で継ぎ目なし)。
        /// Terrain の内部は絵なし・縁は切り抜き版なので、内側はすべてこの岩肌が見える。当たりは一切変えない。Finish が呼ぶ。
        /// </summary>
        public static string PaintRock()
        {
            var terrain = Map("Terrain");
            var backfill = Map("Backfill");
            var rule = RuleTile;
            var seal = Tile(Area1AssetPrep.RockSealTileName);
            var n = Area1AssetPrep.RockTexPeriod;
            var tex = new TileBase[n, n];
            for (var r = 0; r < n; r++)
            for (var c = 0; c < n; c++)
                tex[r, c] = Tile(Area1AssetPrep.RockTexTileName(r, c));

            terrain.CompressBounds();
            backfill.CompressBounds();
            var cells = new HashSet<Vector3Int>();
            foreach (var p in terrain.cellBounds.allPositionsWithin)
            {
                var t = terrain.GetTile(p);
                if (t == rule || t == seal)
                    cells.Add(p);
            }
            foreach (var p in backfill.cellBounds.allPositionsWithin)
                if (backfill.HasTile(p))
                    cells.Add(p);

            // 鉄板の壁 (PlateWalls で指定した範囲): 観覧車の支柱・機械室の内部
            TileBase[,] plate = null;
            var pn = Area1AssetPrep.PlateTexPeriod;
            if (PlateRects.Count > 0)
            {
                plate = new TileBase[pn, pn];
                for (var r = 0; r < pn; r++)
                for (var c = 0; c < pn; c++)
                    plate[r, c] = Tile(Area1AssetPrep.PlateTexTileName(r, c));
            }

            var pos = cells.ToArray();
            var isPlate = pos.Select(p => plate != null && IsPlate(p.x, p.y)).ToArray();
            var tiles = new TileBase[pos.Length];
            var mats = new Matrix4x4[pos.Length];
            for (var i = 0; i < pos.Length; i++)
            {
                var p = pos[i];
                if (isPlate[i]) { tiles[i] = plate[Mod(-p.y, pn), Mod(p.x, pn)]; mats[i] = Matrix4x4.identity; continue; }
                var (r, c, m) = RockCell(p.x, p.y);
                tiles[i] = tex[r, c];
                mats[i] = m;
            }
            backfill.SetTiles(pos, tiles);
            for (var i = 0; i < pos.Length; i++)
                if (mats[i] != Matrix4x4.identity)
                    backfill.SetTransformMatrix(pos[i], mats[i]);

            // 奥行き: 部屋 (空き) から離れるほど暗くする (縁の近くは素の色 → 足場の帯と部屋が浮き立つ)
            var dist = new Dictionary<Vector3Int, int>();
            var q = new Queue<Vector3Int>();
            foreach (var p in cells)
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var nb = new Vector3Int(p.x + dx, p.y + dy, 0);
                    if (!cells.Contains(nb) && !dist.ContainsKey(p)) { dist[p] = 1; q.Enqueue(p); }
                }
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                var d = dist[c];
                if (d >= RockShadeDepth) continue;
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var nb = new Vector3Int(c.x + dx, c.y + dy, 0);
                    if (!cells.Contains(nb) || dist.ContainsKey(nb)) continue;
                    dist[nb] = d + 1;
                    q.Enqueue(nb);
                }
            }
            for (var i = 0; i < pos.Length; i++)
            {
                var p = pos[i];
                var d = dist.TryGetValue(p, out var dd) ? dd : RockShadeDepth;
                var k = Mathf.Lerp(1f, RockShadeMin, Mathf.Clamp01((d - 1f) / (RockShadeDepth - 1f)));
                // 岩肌だけ大きな色むら (数セル周期のなめらかな明暗 + わずかな色相) を掛けて、4x4 の繰り返しを目立たなくする
                var nz = isPlate[i] ? Color.white : RockNoise(p.x, p.y);
                backfill.SetColor(p, new Color(k * nz.r, k * nz.g, k * nz.b, 1f));
            }
            return $"[PaintRock] {pos.Length} cells";
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                var h = (int)2166136261;
                foreach (var ch in s)
                    h = (h ^ ch) * 16777619;
                return h & 0x7fffffff;
            }
        }

        private class DecorSpec
        {
            public string Path; public int W, H; public float Weight;
            public DecorSpec(string path, int w, int h, float weight) { Path = path; W = w; H = h; Weight = weight; }
        }

        // 岩の中 (部屋の縁から 2〜5 セル奥) に埋める飾り
        private static readonly DecorSpec[] RockMotifs =
        {
            new DecorSpec("rock/rock_motif_gear.png", 1, 1, 3f),
            new DecorSpec("rock/rock_motif_stones.png", 1, 1, 3f),
            new DecorSpec("rock/rock_motif_cracks.png", 1, 1, 2f),
            new DecorSpec("rock/decal_crack_01.png", 1, 1, 2f),
            new DecorSpec("rock/decal_crack_02.png", 1, 1, 2f),
            new DecorSpec("rock/rock_motif_plate.png", 1, 1, 1.5f),
            new DecorSpec("rock/rock_motif_fossil.png", 1, 1, 1f),
            new DecorSpec("rock/rock_motif_pipestub.png", 1, 1, 1f),
            new DecorSpec("rock/decal_moss_glow.png", 1, 1, 0.35f),   // 光る苔は目立つので控えめ (MossMax 個まで、暗く)
            new DecorSpec("rock/rock_motif_gears_big.png", 2, 2, 1f),
            new DecorSpec("rock/rock_motif_boulders_big.png", 2, 2, 1f),
            new DecorSpec("rock/decal_pipe_h.png", 2, 1, 0.7f),
            new DecorSpec("rock/decal_pipe_v.png", 1, 2, 0.7f),
        };

        /// <summary>岩の飾りの光る苔の上限 (シーンごと)</summary>
        public const int MossMax = 2;

        // 奥の大きな岩の面に埋める飾り (Deep)。暗く・疎らに
        private static readonly DecorSpec[] DeepMotifs =
        {
            new DecorSpec("rock/rock_motif_stones.png", 1, 1, 1f),
            new DecorSpec("rock/rock_motif_cracks.png", 1, 1, 1f),
            new DecorSpec("rock/decal_crack_01.png", 1, 1, 1f),
            new DecorSpec("rock/decal_crack_02.png", 1, 1, 1f),
            new DecorSpec("rock/rock_motif_boulders_big.png", 2, 2, 1f),
            new DecorSpec("rock/rock_motif_fossil.png", 1, 1, 1f),
        };

        private static bool PlateMotif(string path) =>
            path.Contains("gear") || path.Contains("plate") || path.Contains("pipe");

        /// <summary>
        /// 岩と部屋の飾りを決定的に (シーン名をシードに) 散らす。ルート "RockDecor" を作り直す。
        /// - Rock: 岩の中、部屋の縁から 2〜5 セル奥 (縁の帯に掛からない) に歯車・石・化石・ひび・配管など (order -8)。
        /// - Wall: 屋内の部屋の奥の壁 (背景の前、ゲームの物の後ろ, order -7) に、床から 3 上のランプ・床から 2 のポスター、
        ///   天井から吊る旗飾り・鎖。すり抜け床・ギミック・ピックアップ・出口・スポーンの周りは避ける。
        /// density 1 = 標準。Finish が DecorDensity で呼ぶ (単体でも可)。
        /// </summary>
        public static string ScatterRockDecor(float density = 1f)
        {
            foreach (var old in ActiveScene.GetRootGameObjects().Where(g => g.name == "RockDecor").ToList())
                Object.DestroyImmediate(old);
            var root = new GameObject("RockDecor").transform;
            if (density <= 0f)
                return "[RockDecor] density 0";
            var rockGroup = new GameObject("Rock").transform;
            rockGroup.SetParent(root, false);
            var wallGroup = new GameObject("Wall").transform;
            wallGroup.SetParent(root, false);

            var id = CurrentId;
            var layout = Area1Layout.Load();
            var rnd = new System.Random(StableHash(id));
            var backfill = Map("Backfill");
            var platforms = Map("Platforms");
            bool Rock(int x, int y) => backfill.HasTile(new Vector3Int(x, y, 0));

            var b = Expand(Bounds(id), RockMargin + 2);
            var roomCells = new HashSet<Vector2Int>();
            foreach (var r in OpenRects(id))
                AddCells(roomCells, r);

            // 塞がっている所 (ギミック・ピックアップ・飾り・敵・すり抜け床・出口・スポーン) + 余白
            var blocked = new HashSet<Vector2Int>();
            void Block(UnityEngine.Bounds bb, int margin)
            {
                for (var x = Mathf.FloorToInt(bb.min.x) - margin; x <= Mathf.FloorToInt(bb.max.x) + margin; x++)
                for (var y = Mathf.FloorToInt(bb.min.y) - margin; y <= Mathf.FloorToInt(bb.max.y) + margin; y++)
                    blocked.Add(new Vector2Int(x, y));
            }
            foreach (var g in new[] { "Props", "Gimmicks", "Pickups", "Terrain Objects", "Enemies" })
            {
                var grp = ActiveScene.GetRootGameObjects().FirstOrDefault(o => o.name == g);
                if (grp == null) continue;
                foreach (var rend in grp.GetComponentsInChildren<Renderer>(true))
                    Block(rend.bounds, 1);
            }
            foreach (var sp in Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Block(new UnityEngine.Bounds(sp.transform.position, new Vector3(1, 2, 0)), 1);
            foreach (var e in BoundaryExits(id))
                Block(new UnityEngine.Bounds(e.Carved.center, new Vector3(e.Carved.width, e.Carved.height, 0)), 1);
            platforms.CompressBounds();
            foreach (var p in platforms.cellBounds.allPositionsWithin)
                if (platforms.HasTile(p))
                    Block(new UnityEngine.Bounds(new Vector3(p.x + 0.5f, p.y + 0.5f), Vector3.one * 0.9f), 1);

            // 岩の中の「部屋 (空き) からの距離」(8 近傍、最大 6)
            var dist = new Dictionary<Vector2Int, int>();
            var q = new Queue<Vector2Int>();
            for (var x = b.xMin; x < b.xMax; x++)
            for (var y = b.yMin; y < b.yMax; y++)
                if (!Rock(x, y)) { var c = new Vector2Int(x, y); dist[c] = 0; q.Enqueue(c); }
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                var d = dist[c];
                if (d >= 6) continue;
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var nb = new Vector2Int(c.x + dx, c.y + dy);
                    if (!b.Contains(nb) || dist.ContainsKey(nb) || !Rock(nb.x, nb.y)) continue;
                    dist[nb] = d + 1;
                    q.Enqueue(nb);
                }
            }
            int Dist(int x, int y) => dist.TryGetValue(new Vector2Int(x, y), out var dd) ? dd : (Rock(x, y) ? 99 : 0);

            List<T> Shuffle<T>(IEnumerable<T> src)
            {
                var list = src.ToList();
                for (var i = list.Count - 1; i > 0; i--)
                {
                    var j = rnd.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
                return list;
            }

            GameObject Place(Transform parent, string path, Vector2 center, int order, bool flipX = false, Color? color = null)
            {
                var go = new GameObject(Path.GetFileNameWithoutExtension(path));
                go.transform.SetParent(parent, false);
                go.transform.position = center;
                var sr = AddSprite(go, Art(path), order, null, color);
                sr.flipX = flipX;
                return go;
            }

            // ---- Rock: 岩の中の飾り
            var bandCells = new List<Vector2Int>();
            for (var x = b.xMin; x < b.xMax; x++)
            for (var y = b.yMin; y < b.yMax; y++)
            {
                var d = Dist(x, y);
                if (d >= 2 && d <= 4) bandCells.Add(new Vector2Int(x, y));
            }
            var target = Mathf.RoundToInt(density * bandCells.Count / 40f);
            var spacing = 5f / Mathf.Sqrt(density);
            var placed = new List<Vector2>();
            var totalW = RockMotifs.Sum(m => m.Weight);
            var rockCount = 0;
            var mossCount = 0;
            foreach (var c in Shuffle(bandCells))
            {
                if (rockCount >= target) break;
                var roll = (float)rnd.NextDouble() * totalW;
                var spec = RockMotifs.Last();
                foreach (var m in RockMotifs)
                {
                    roll -= m.Weight;
                    if (roll <= 0f) { spec = m; break; }
                }
                // 鉄板の壁では岩らしい飾り (石・化石・苔・ひび・岩塊) を機械の飾りに差し替える
                if (PlateRects.Count > 0 && IsPlate(c.x, c.y) && !PlateMotif(spec.Path))
                {
                    var mech = RockMotifs.Where(m => PlateMotif(m.Path)).ToArray();
                    spec = mech[rnd.Next(mech.Length)];
                }
                var isMoss = spec.Path.Contains("moss");
                if (isMoss && mossCount >= MossMax) continue;
                var ok = true;
                for (var x = c.x; x < c.x + spec.W && ok; x++)
                for (var y = c.y; y < c.y + spec.H && ok; y++)
                {
                    var d = Dist(x, y);
                    ok = d >= 2 && d <= 5;
                }
                if (!ok) continue;
                var center = new Vector2(c.x + spec.W * 0.5f, c.y + spec.H * 0.5f);
                if (placed.Any(p => (p - center).sqrMagnitude < spacing * spacing)) continue;
                var flip = rnd.Next(2) == 0;
                if (spec.Path.Contains("pipestub"))
                {
                    // 管の口を部屋に向ける (右 = 既定)。左右とも 3 以内に部屋が無ければ置かない
                    bool AirWithin(int dir) => Enumerable.Range(1, 3).Any(k => !Rock(c.x + dir * k, c.y));
                    if (AirWithin(1)) flip = false;
                    else if (AirWithin(-1)) flip = true;
                    else continue;
                }
                if (spec.Path.Contains("fossil") || spec.Path.Contains("pipe_") || spec.Path.Contains("plate"))
                    flip = false;
                // 岩肌の陰影 (PaintRock) と同じ明るさに沈め、小物は少しだけ大きさを散らす
                var shade = Mathf.Lerp(1f, RockShadeMin, Mathf.Clamp01((Dist(c.x, c.y) - 1f) / (RockShadeDepth - 1f)));
                // 光る苔は岩肌に沈めて色を抜く (明るい水色が岩の上で浮かないように)
                var col = isMoss ? new Color(shade * 0.55f, shade * 0.62f, shade * 0.68f, 0.85f) : new Color(shade, shade, shade, 1f);
                var go = Place(rockGroup, spec.Path, center, OrderRockDecor, flip, col);
                if (spec.W == 1 && spec.H == 1)
                    go.transform.localScale = Vector3.one * (isMoss ? 0.8f : 1f + 0.3f * (float)rnd.NextDouble());
                placed.Add(center);
                rockCount++;
                if (isMoss) mossCount++;
            }

            // ---- Deep: 部屋から遠い大きな岩の面 (縁から 6 セル以上奥) にも、暗い石・ひび・岩塊を疎らに埋める
            //      (4x4 の岩肌の繰り返しを崩す。カメラが部屋の外を映した時の単調さ対策)
            var deepCells = new List<Vector2Int>();
            for (var x = b.xMin; x < b.xMax; x++)
            for (var y = b.yMin; y < b.yMax; y++)
                if (Dist(x, y) >= 6 && Rock(x, y)) deepCells.Add(new Vector2Int(x, y));
            var deepTarget = Mathf.RoundToInt(density * deepCells.Count / 70f);
            var deepSpacing = 6.5f / Mathf.Sqrt(Mathf.Max(0.25f, density));
            var deepCount = 0;
            foreach (var c in Shuffle(deepCells))
            {
                if (deepCount >= deepTarget) break;
                var spec = DeepMotifs[rnd.Next(DeepMotifs.Length)];
                var ok = true;
                for (var x = c.x; x < c.x + spec.W && ok; x++)
                for (var y = c.y; y < c.y + spec.H && ok; y++)
                    ok = Dist(x, y) >= 6 && Rock(x, y);
                if (!ok) continue;
                var center = new Vector2(c.x + spec.W * 0.5f, c.y + spec.H * 0.5f);
                if (placed.Any(p => (p - center).sqrMagnitude < deepSpacing * deepSpacing)) continue;
                if (PlateRects.Count > 0 && IsPlate(c.x, c.y)) continue;   // 鉄板の壁には置かない
                var k = RockShadeMin * (1.1f + 0.2f * (float)rnd.NextDouble());
                var go = Place(rockGroup, spec.Path, center, OrderRockDecor, rnd.Next(2) == 0, new Color(k, k, k, 0.9f));
                go.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(4) * 90f * (spec.Path.Contains("crack") ? 1f : 0f));
                go.transform.localScale = Vector3.one * (spec.W > 1 ? 1f + 0.4f * (float)rnd.NextDouble() : 1.2f + 0.6f * (float)rnd.NextDouble());
                placed.Add(center);
                deepCount++;
            }

            // ---- Wall: 屋内の部屋の奥の壁
            var wallCount = 0;
            var wallPlaced = new List<Vector2>();
            bool Air(int x, int y) => !Rock(x, y) && roomCells.Contains(new Vector2Int(x, y));
            bool Free(int x0, int y0, int w, int h)
            {
                for (var x = x0; x < x0 + w; x++)
                for (var y = y0; y < y0 + h; y++)
                    if (!Air(x, y) || blocked.Contains(new Vector2Int(x, y))) return false;
                return true;
            }
            void Occupy(int x0, int y0, int w, int h)
            {
                for (var x = x0 - 1; x < x0 + w + 1; x++)
                for (var y = y0 - 1; y < y0 + h + 1; y++)
                    blocked.Add(new Vector2Int(x, y));
            }
            bool FarFrom(Vector2 p, float min) => wallPlaced.All(o => (o - p).sqrMagnitude >= min * min);

            foreach (var room in layout.RoomsInScene(id).Where(r => !r.Outdoor))
            foreach (var r in room.Rects)
            {
                var floors = new List<Vector2Int>();   // 床の上の最初の空きセル
                var ceilings = new List<Vector2Int>(); // 天井の下の最初の空きセル
                for (var x = r.xMin; x < r.xMax; x++)
                for (var y = r.yMin; y < r.yMax; y++)
                {
                    if (!Air(x, y)) continue;
                    // 床は左右にも続いている所だけ (段の角や穴の縁の上に浮いて見えないように)
                    if (Rock(x, y - 1) && Rock(x - 1, y - 1) && Rock(x + 1, y - 1)) floors.Add(new Vector2Int(x, y));
                    if (Rock(x, y + 1)) ceilings.Add(new Vector2Int(x, y));
                }

                // 数は部屋の幅で決める (端数は確率で丸める)
                int Quota(float per)
                {
                    var f = density * r.width / per;
                    var k = Mathf.FloorToInt(f);
                    return k + (rnd.NextDouble() < f - k ? 1 : 0);
                }

                var lamps = Quota(14f);
                foreach (var f in Shuffle(floors))
                {
                    if (lamps <= 0) break;
                    if (!Free(f.x, f.y, 1, 5)) continue;
                    var c = new Vector2(f.x + 0.5f, f.y + 3.5f);
                    if (!FarFrom(c, 6f)) continue;
                    Place(wallGroup, "rock/decal_lamp_wall.png", c, OrderWallDecor, rnd.Next(2) == 0);
                    Occupy(f.x, f.y + 3, 1, 1);
                    wallPlaced.Add(c); lamps--; wallCount++;
                }

                var posters = Quota(30f);
                foreach (var f in Shuffle(floors))
                {
                    if (posters <= 0) break;
                    if (!Free(f.x, f.y, 1, 4)) continue;
                    var c = new Vector2(f.x + 0.5f, f.y + 2.1f);
                    if (!FarFrom(c, 5f)) continue;
                    Place(wallGroup, "rock/decal_poster.png", c, OrderWallDecor);
                    Occupy(f.x, f.y + 1, 1, 2);
                    wallPlaced.Add(c); posters--; wallCount++;
                }

                var buntings = Quota(26f);
                foreach (var c0 in Shuffle(ceilings))
                {
                    if (buntings <= 0) break;
                    if (!Free(c0.x, c0.y - 4, 3, 5) || !Rock(c0.x + 1, c0.y + 1) || !Rock(c0.x + 2, c0.y + 1)) continue;
                    var c = new Vector2(c0.x + 1.5f, c0.y + 0.62f);
                    if (!FarFrom(c, 6f)) continue;
                    Place(wallGroup, "rock/decal_bunting.png", c, OrderWallDecor);
                    Occupy(c0.x, c0.y, 3, 1);
                    wallPlaced.Add(c); buntings--; wallCount++;
                }

                var chains = Quota(22f);
                foreach (var c0 in Shuffle(ceilings))
                {
                    if (chains <= 0) break;
                    if (!Free(c0.x, c0.y - 6, 1, 7)) continue;
                    var c = new Vector2(c0.x + 0.5f, c0.y + 1.15f - 1.5f); // 上端 (フック) を天井の帯の裏へ
                    if (!FarFrom(c, 5f)) continue;
                    Place(wallGroup, "rock/decal_chain_hanging.png", c, OrderWallDecor, false, new Color(0.85f, 0.82f, 0.9f));
                    Occupy(c0.x, c0.y - 2, 1, 3);
                    wallPlaced.Add(c); chains--; wallCount++;
                }
            }

            return $"[RockDecor] seed={StableHash(id)} density={density:0.##} rock={rockCount}/{target} deep={deepCount}/{deepTarget} moss={mossCount} wall={wallCount}";
        }

        // ================================================================== 仕上げ・検証

        /// <summary>タイル再評価 → コライダー再生成 → 検証 → 保存。最後に必ず呼ぶ。</summary>
        public static string Finish(bool save = true)
        {
            Refresh();
            PruneExitViewDecor();
            // 見た目: 岩肌 (Backfill) と飾り。地形が出来上がった後に毎回作り直す (当たりには触れない)
            var look = PaintRock() + "\n" + ApplyRockTints() + ScatterRockDecor(DecorDensity) + "\n";
            var report = look + Validate();
            if (save)
                Save();
            return report;
        }

        public static void Refresh()
        {
            foreach (var n in new[] { "Terrain", "Platforms", "Backfill", "Deco" })
                Map(n).RefreshAllTiles();
            // 新規に付けた TilemapCollider2D→Composite はエディタ上で形状が 0 のままになることがある
            // (実測: shapes=0)。合成を一度外して付け直すと再生成される
            var terrainGo = Map("Terrain").gameObject;
            var tc = terrainGo.GetComponent<TilemapCollider2D>();
            var comp = terrainGo.GetComponent<CompositeCollider2D>();
            if (tc != null && comp != null)
            {
                tc.compositeOperation = Collider2D.CompositeOperation.None;
                tc.ProcessTilemapChanges();
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
                tc.ProcessTilemapChanges();
                comp.GenerateGeometry();
            }
            var pc = Map("Platforms").GetComponent<TilemapCollider2D>();
            if (pc != null)
                pc.ProcessTilemapChanges();
            var grid = ActiveScene.GetRootGameObjects().FirstOrDefault(g => g.name == "Grid");
            var stop = grid != null ? grid.transform.Find("ExitStop") : null;
            if (stop != null && stop.TryGetComponent<TilemapCollider2D>(out var sc))
                sc.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
        }

        public static void Save()
        {
            var scene = ActiveScene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void EnsureInBuildSettings(string scenePath)
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == scenePath))
                return;
            list.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        /// <summary>
        /// 開いているシーンの検証:
        /// - 遷移ゾーンの先シーンが Build Settings にあり、先シーンに Id=From&lt;自&gt; のスポーンがある
        /// - 横の出口ゾーンの全幅の下に床がある / 部屋側の出口手前にも床がある
        /// - 各スポーンの真下 4 単位以内に床がある、スポーンが岩に埋まっていない
        /// 行頭 OK / WARN / ERR。
        /// </summary>
        public static string Validate()
        {
            Refresh();
            var sb = new StringBuilder();
            var id = CurrentId;
            sb.AppendLine($"== Validate {id}");
            int err = 0, warn = 0;
            void Err(string s) { sb.AppendLine("ERR  " + s); err++; }
            void Warn(string s) { sb.AppendLine("WARN " + s); warn++; }
            void Ok(string s) => sb.AppendLine("OK   " + s);

            var terrain = Map("Terrain");
            var platforms = Map("Platforms");
            bool SolidAt(float x, float y)
            {
                var c = new Vector3Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y), 0);
                if (terrain.HasTile(c) || platforms.HasTile(c))
                    return true;
                foreach (var col in Physics2D.OverlapPointAll(new Vector2(x, y)))
                    if (!col.isTrigger && (col.gameObject.layer == LayerGround || col.gameObject.layer == LayerDefault))
                        return true;
                return false;
            }

            var cc = terrain.GetComponent<CompositeCollider2D>();
            if (cc == null || cc.pathCount == 0) Err("Terrain の当たり (CompositeCollider2D) が空");
            else Ok($"Terrain の当たり: composite paths={cc.pathCount}");

            var buildNames = EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToList();
            if (!buildNames.Contains(id))
                Err($"{id} が Build Settings にありません");

            // UI はステージに置かない (UI シーン StageUI / ShopUI などに分離)。ステージ側はデータ (StageInfo) だけ
            var infos = Object.FindObjectsByType<StageInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (infos.Length != 1)
                Err($"StageInfo が {infos.Length} 個 (1 個必要)");
            else if (string.IsNullOrEmpty(new SerializedObject(infos[0]).FindProperty("_areaName").stringValue))
                Warn("StageInfo の _areaName が空 (タイトルが出ない)");
            else
                Ok($"StageInfo: {new SerializedObject(infos[0]).FindProperty("_areaName").stringValue}");
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                    Err($"ステージに画面 UI の Canvas '{canvas.name}' がある (UI は UI シーンへ)");

            var exits = BoundaryExits(id);
            foreach (var zone in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(zone);
                var target = so.FindProperty("_sceneName").stringValue;
                var entrance = so.FindProperty("_entranceId").stringValue;
                var name = zone.name;
                var targetPath = ScenePath(target);
                if (!buildNames.Contains(target))
                {
                    if (File.Exists(targetPath)) Err($"{name}: {target} が Build Settings に無い");
                    else Warn($"{name}: 先シーン {target} は未作成");
                }
                else if (!File.Exists(targetPath))
                {
                    Warn($"{name}: {target} は Area1 フォルダ外 (Id 未確認)");
                }
                else
                {
                    var text = File.ReadAllText(targetPath);
                    if (Regex.IsMatch(text, @"_id: " + Regex.Escape(entrance) + @"\s*\r?\n"))
                        Ok($"{name}: {target} に Spawn Id={entrance} あり");
                    else
                        Err($"{name}: {target} に Spawn Id={entrance} が無い");
                }

                var col = zone.GetComponent<BoxCollider2D>();
                var bounds = col.bounds;
                var exit = exits.FirstOrDefault(e => "ExitTo" + e.OtherScene == name);
                if (exit == null || exit.Dir == ExitDir.Left || exit.Dir == ExitDir.Right)
                {
                    var missing = new List<int>();
                    for (var x = Mathf.FloorToInt(bounds.min.x); x < Mathf.CeilToInt(bounds.max.x); x++)
                        if (!SolidAt(x + 0.5f, bounds.min.y - 0.5f))
                            missing.Add(x);
                    if (missing.Count > 0) Err($"{name}: 出口ゾーンの下に床が無い x={string.Join(",", missing)}");
                    else Ok($"{name}: 出口ゾーン全幅の下に床あり");

                    if (exit != null)
                    {
                        var p = exit.Passage;
                        var inside = exit.Dir == ExitDir.Right ? Enumerable.Range(p.xMin - 3, 3) : Enumerable.Range(p.xMax, 3);
                        var gaps = inside.Where(x => !SolidAt(x + 0.5f, p.yMin - 0.5f)).ToList();
                        if (gaps.Count > 0) Warn($"{name}: 部屋側の出口手前 (y={p.yMin}) に床が無い x={string.Join(",", gaps)}");
                        var blocked = new List<string>();
                        for (var x = p.xMin; x < p.xMax; x++)
                        for (var y = p.yMin; y < p.yMin + 2; y++)
                            if (terrain.HasTile(new Vector3Int(x, y, 0))) blocked.Add($"({x},{y})");
                        if (blocked.Count > 0) Err($"{name}: 通路が岩で塞がれている {string.Join(" ", blocked)}");
                    }
                }
            }

            // 出口の見通し: 掘削の端の先が見た目で開いているか、止めの当たりがあるか
            var views = ExitViewsOf(id);
            if (views.Count == 0)
                Warn("出口の見通しの情報が無い (BuildBase と同じ呼び出しで Finish していない?)");
            var backfillMap = Map("Backfill");
            foreach (var v in views)
            {
                var tag = $"見通し {v.Exit.Dir}→{v.Exit.OtherScene ?? v.Exit.OtherRoom}";
                var closed = 0;
                for (var x = v.View.xMin; x < v.View.xMax; x++)
                for (var y = v.View.yMin; y < Mathf.Min(v.View.yMax, v.View.yMin + 12); y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (terrain.HasTile(c) || backfillMap.HasTile(c)) closed++;
                }
                var stopOk = true;
                for (var x = v.Stop.xMin; x < v.Stop.xMax && stopOk; x++)
                for (var y = v.Stop.yMin; y < Mathf.Min(v.Stop.yMax, v.Stop.yMin + 12) && stopOk; y++)
                    stopOk = SolidAt(x + 0.5f, y + 0.5f);
                if (!stopOk) Err($"{tag}: 止めの当たりが無い (場外へ出られる)");
                if (closed > 0) Warn($"{tag}: 開けた先の {closed} セルがレシピで塞がれている");
                else Ok($"{tag}: 先 {v.Length} セルまで開いている{(v.Sky ? " (空)" : "")}");
            }

            // 一方通行の受け側・双方向の入口ごとにスポーンがあるか
            var spawns = Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ladders = Object.FindObjectsByType<ClimbableLadder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var e in exits.Where(e => e.CanArrive))
                if (spawns.All(s => s.Id != "From" + e.OtherScene))
                    Err($"入口 From{e.OtherScene} のスポーンが無い");
            if (spawns.All(s => !string.IsNullOrEmpty(s.Id)))
                Warn("SpawnDefault (Id 空) が無い");

            foreach (var s in spawns)
            {
                var p = (Vector2)s.transform.position;
                if (SolidAt(p.x, p.y) || SolidAt(p.x, p.y + 0.6f) || SolidAt(p.x, p.y - 0.5f))
                {
                    Err($"{s.name} {p}: 岩/床に埋まっている");
                    continue;
                }
                // 梯子の途中の入り口: 着くと梯子に掴まる (PlayerController.AttachToLadderOnArrival, PendingChanges 44)
                var onLadder = ladders.FirstOrDefault(l => l.CanGrabAt(p.x, p.y - 0.9f, 0.5f));
                if (onLadder != null)
                {
                    Ok($"{s.name} {p}: 梯子の途中 (着くと掴まる。梯子 y{onLadder.Bottom:F0}..{onLadder.Top:F0})");
                    continue;
                }
                var ground = float.NaN;
                for (var d = 0.5f; d <= 4f; d += 0.25f)
                    if (SolidAt(p.x, p.y - d)) { ground = p.y - d; break; }
                var exit = exits.FirstOrDefault(e => "From" + e.OtherScene == s.Id);
                if (float.IsNaN(ground))
                {
                    if (exit != null && exit.Dir == ExitDir.Up) Warn($"{s.name} {p}: 上からの到着 (落下で着地)。真下 4 以内に床なし");
                    else Err($"{s.name} {p}: 真下 4 以内に床が無い");
                }
                else Ok($"{s.name} {p}: 床 y≈{Mathf.Ceil(ground)}");
            }

            // 移動装置 (TransportInteractable) の先シーン・スポーン
            foreach (var tr in Object.FindObjectsByType<TransportInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(tr);
                var target = so.FindProperty("_sceneName").stringValue;
                var entrance = so.FindProperty("_entranceId").stringValue;
                var targetPath = ScenePath(target);
                if (string.IsNullOrEmpty(target)) Err($"{tr.name}: _sceneName が空");
                else if (!File.Exists(targetPath)) Warn($"{tr.name}: 先シーン {target} は未作成");
                else if (!buildNames.Contains(target)) Err($"{tr.name}: {target} が Build Settings に無い");
                else if (Regex.IsMatch(File.ReadAllText(targetPath), @"_id: " + Regex.Escape(entrance) + @"\s*\r?\n"))
                    Ok($"{tr.name}: {target} に Spawn Id={entrance} あり");
                else Warn($"{tr.name}: {target} に Spawn Id={entrance} が無い (先シーンの担当が置く)");
            }

            // ピックアップ Id の重複・命名
            foreach (var pk in Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var pid = new SerializedObject(pk).FindProperty("_pickupId").stringValue;
                if (string.IsNullOrEmpty(pid)) Warn($"{pk.name}: _pickupId が空 (再訪で復活する)");
            }

            sb.AppendLine($"== {err} error(s), {warn} warning(s)");
            return sb.ToString();
        }

        // ================================================================== 点検用

        /// <summary>SceneView を (cx,cy) に寄せて PNG を保存 (Assets/Screenshots/Area1/&lt;name&gt;.png)。halfHeight = 縦の半分。</summary>
        public static string FrameSceneView(float cx, float cy, float halfHeight)
        {
            var sv = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            sv.in2DMode = true;
            sv.drawGizmos = true;
            sv.LookAt(new Vector3(cx, cy, 0), Quaternion.identity, halfHeight, true, true);
            sv.Repaint();
            return $"framed ({cx},{cy}) h={halfHeight}";
        }

        /// <summary>部屋 roomId を SceneView に収める (余白 pad)。</summary>
        public static string FrameRoom(string roomId, float pad = 4f)
        {
            var room = Area1Layout.Load().GetRoom(roomId);
            var r = room.Rects[0];
            foreach (var x in room.Rects.Skip(1))
                r = new RectInt(Mathf.Min(r.xMin, x.xMin), Mathf.Min(r.yMin, x.yMin),
                    Mathf.Max(r.xMax, x.xMax) - Mathf.Min(r.xMin, x.xMin), Mathf.Max(r.yMax, x.yMax) - Mathf.Min(r.yMin, x.yMin));
            var half = Mathf.Max(r.height * 0.5f, r.width * 0.5f * 9f / 16f) + pad;
            return FrameSceneView(r.center.x, r.center.y, half);
        }

        /// <summary>レイアウト上の部屋・通路・意図を一覧 (設計の確認用)。</summary>
        public static string Describe(string sceneId)
        {
            var layout = Area1Layout.Load();
            var sb = new StringBuilder($"{sceneId} 「{layout.SceneTitle(sceneId)}」 bounds={Bounds(sceneId)}\n");
            foreach (var r in layout.RoomsInScene(sceneId))
            {
                sb.AppendLine($"- {r.Id} {r.Name} outdoor={r.Outdoor} boss={r.Boss} speaker={r.Speaker}");
                foreach (var rc in r.Rects) sb.AppendLine($"    rect x{rc.xMin}..{rc.xMax} y{rc.yMin}..{rc.yMax}");
                sb.AppendLine($"    intent: {r.Intent}");
                sb.AppendLine($"    features: {string.Join(" / ", r.Features)}");
                foreach (var w in layout.Rewards.Where(w => w.Room == r.Id))
                    sb.AppendLine($"    reward: {w.Kind} {w.Name} req={w.Req}");
            }
            foreach (var l in LinksOf(sceneId))
                sb.AppendLine($"  link {l.A}-{l.B} {l.Kind} gate={l.Gate} oneWay={l.OneWay} zip={l.Zipline} passage={(l.HasPassage ? $"x{l.Passage.xMin}..{l.Passage.xMax} y{l.Passage.yMin}..{l.Passage.yMax}" : "-")}");
            foreach (var e in BoundaryExits(sceneId))
                sb.AppendLine($"  exit {e.Dir} -> {(e.Transport ? "(移動装置 " + e.OtherRoom + ")" : e.OtherScene)} canExit={e.CanExit} canArrive={e.CanArrive}");
            return sb.ToString();
        }
    }
}
