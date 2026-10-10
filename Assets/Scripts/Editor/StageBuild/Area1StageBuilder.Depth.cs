using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeverNight.Stage;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeverNight.StageBuild
{
    /// <summary>
    /// 奥行き (視差の層・霧・前景・光の筋)。素材は Area1Gen/atmos/ (Area1AssetPrep.PrepareAtmos)。
    /// <see cref="Depth"/> が開いているシーンの Background/Depth を作り直す (前回の物は消してから作る = 何度流してもよい)。
    /// ・地下 (背景が "Underground" のシーン): 既存の bg_underground の 1 枚絵を消し、
    ///   遠景の機械の大広間 (視差 0.93) → 霧 → 中景の配管の段 (0.6) → 霧 → (岩・部屋・地形) → 前景 (鎖・配管・歯車・鉄骨, 視差負) + 天井の口からの光の筋。
    /// ・夜空 (背景が "NightSky" のシーン): 既存の園内のシルエットを消し、
    ///   夜空 → 遠景の園のスカイライン (0.9) → 霧 → 中景の並木と柵 (0.55, 地上のシーンだけ) → 霧 → (部屋の背景板 -90・地形) → 前景 (茂み・街灯・旗飾り)。
    /// 屋内の部屋の背景板 (OrderRoomBackdrop -90) より奥 (-100..-96) に全部の遠景・中景を置くので、屋内の部屋に空や並木は映らない。
    /// 手順と値は Docs/Area1/SceneBuildGuide.md §16。
    /// </summary>
    public static partial class Area1StageBuilder
    {
        public const string DepthRootName = "Depth";

        /// <summary>奥行きの層の並び順。地下: 遠景 -100 / 霧 -99 / 中景 -98 / 霧 -97。夜空: 空 -100 / 遠景 -99 / 霧 -98 / 中景 -97 / 霧 -96。光の筋 -13 (環境パーティクル中景 -12 の奥・岩の塗り -10 の奥)。前景 25 (前景のパーティクル 30 の奥)。</summary>
        public const int OrderDepthShaft = -13, OrderDepthFore = 25;

        private static string AtmosArt(string file) => Area1AssetPrep.AtmosArtDir + "/" + file;

        /// <summary>種類ごとの色 (空気遠近法: 遠いほど暗く・霧の色に寄せる)。</summary>
        private struct DepthLook
        {
            public Color Far, Mid, Fog, Shaft, Fore, Fill;
        }

        private static DepthLook DepthLookOf(AtmosphereKind kind, bool sky)
        {
            if (sky)
                return new DepthLook
                {
                    Far = new Color(0.82f, 0.78f, 0.98f),        // スカイライン (元が紺)
                    Mid = new Color(0.56f, 0.5f, 0.7f),        // 並木・柵 (元が暗い紫。街灯の玉が明るすぎないように)
                    Fog = kind == AtmosphereKind.Boss ? new Color(0.78f, 0.6f, 0.8f) : new Color(0.7f, 0.64f, 0.94f),
                    Shaft = new Color(0.75f, 0.8f, 1f, 0.5f),
                    Fore = new Color(0.4f, 0.35f, 0.5f),
                    Fill = new Color(0.12f, 0.1f, 0.3f),
                };
            switch (kind)
            {
                case AtmosphereKind.Boss:
                    return new DepthLook
                    {
                        Far = new Color(0.46f, 0.33f, 0.4f), Mid = new Color(0.5f, 0.4f, 0.48f), Fog = new Color(0.32f, 0.2f, 0.26f),
                        Shaft = new Color(0.82f, 0.82f, 1f, 0.5f), Fore = new Color(0.75f, 0.65f, 0.72f), Fill = new Color(0.1f, 0.06f, 0.1f),
                    };
                case AtmosphereKind.Haven:
                    return new DepthLook
                    {
                        Far = new Color(0.54f, 0.42f, 0.4f), Mid = new Color(0.56f, 0.46f, 0.44f), Fog = new Color(0.42f, 0.31f, 0.28f),
                        Shaft = new Color(1f, 0.85f, 0.62f, 0.5f), Fore = new Color(0.8f, 0.72f, 0.75f), Fill = new Color(0.1f, 0.07f, 0.09f),
                    };
                default:
                    return new DepthLook
                    {
                        Far = new Color(0.48f, 0.4f, 0.5f), Mid = new Color(0.44f, 0.38f, 0.52f), Fog = new Color(0.33f, 0.27f, 0.4f),
                        Shaft = new Color(0.72f, 0.8f, 1f, 0.5f), Fore = new Color(0.8f, 0.76f, 0.88f), Fill = new Color(0.07f, 0.06f, 0.12f),
                    };
            }
        }

        /// <summary>
        /// 開いているシーンに奥行きの層を組む (Background/Depth を作り直す)。地形・当たり・ギミックには触らない。
        /// 背景の種類は Background() が置いた物で判定 ("NightSky" があれば夜空、無ければ地下)。色は <see cref="AtmosphereKindOf"/>。
        /// レシピでは Finish の前に呼ぶ。既存シーンへは depth_apply.cs (開く → 別呼び出しで適用 → 保存)。
        /// </summary>
        public static string Depth(string sceneId)
        {
            var log = new StringBuilder($"[Depth] {sceneId}:");
            var group = Group("Background");
            var old = group.Find(DepthRootName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(DepthRootName).transform;
            root.SetParent(group, false);

            var kind = AtmosphereKindOf(sceneId);
            var sky = group.Find("NightSky") != null;
            var look = DepthLookOf(kind, sky);
            var b = Bounds(sceneId);
            var rooms = Area1Layout.Load().RoomsInScene(sceneId).ToList();

            if (sky)
            {
                // 置き換え: 園内のシルエット (中景の観覧車・テントの影絵) はスカイライン + 並木に
                var sil = group.Find("ParkSilhouette");
                if (sil != null)
                    Object.DestroyImmediate(sil.gameObject);
                DepthSky(root, sceneId, look, b, rooms, kind, log);
            }
            else
            {
                var under = group.Find("Underground");
                if (under != null)
                    Object.DestroyImmediate(under.gameObject);
                DepthUnderground(root, sceneId, look, b, rooms, kind, log);
            }
            DepthExtras(root, sceneId, look, log);
            EditorSceneManager.MarkSceneDirty(ActiveScene);
            return log.ToString();
        }

        // ================================================================== 部品

        private static Transform DepthLayer(Transform parent, string name, Vector2 pos, Vector2 factor, Vector2 anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (factor != Vector2.zero)
                AddComp<ParallaxLayer>(go, ("_factor", factor), ("_anchor", anchor));
            return go.transform;
        }

        /// <summary>層の子の絵。worldTiled を渡すと Tiled (ワールド単位の大きさ)。scale は (x, y) の倍率。</summary>
        private static SpriteRenderer DepthSprite(Transform parent, string name, string file, Vector2 localPos, Vector2 scale, int order,
            Color color, Vector2? worldTiled = null, Material mat = null, bool flipX = false, float rotZ = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = AddSprite(go, Art(AtmosArt(file)), order,
                worldTiled.HasValue ? new Vector2(worldTiled.Value.x / scale.x, worldTiled.Value.y / scale.y) : (Vector2?)null, color);
            if (mat != null)
                sr.sharedMaterial = mat;
            sr.flipX = flipX;
            return sr;
        }

        /// <summary>単色の矩形 (fill_white を引き伸ばす)。center / size はワールド単位 (親の中のローカル)。</summary>
        private static SpriteRenderer DepthFill(Transform parent, string name, Vector2 center, Vector2 size, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var s = Art(AtmosArt("fill_white.png"));
            var u = s.bounds.size.x;
            go.transform.localScale = new Vector3(size.x / u, size.y / u, 1f);
            return AddSprite(go, s, order, null, color);
        }

        /// <summary>霧の帯 (fog_band, 横に継ぎ目なし、ゆっくり流れる)。y = 帯の中心、height = 帯の高さ (ワールド)。</summary>
        private static SpriteRenderer FogBand(Transform parent, string name, float y, float width, float height, int order, Color color,
            bool slow = false)
        {
            var s = Art(AtmosArt("fog_band.png"));
            var sx = 4f;
            var sy = height / s.bounds.size.y;
            var mat = Area1AssetPrep.AtmosArtMat(slow ? "A1_FogDriftSlow" : "A1_FogDrift");
            return DepthSprite(parent, name, "fog_band.png", new Vector2(0f, y), new Vector2(sx, sy), order, color,
                new Vector2(width, s.bounds.size.y * sy), mat);
        }

        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>x の位置で、上から下へ最初に当たる床 (layer Ground) の上面。見つからなければ fallback。</summary>
        private static float FloorAt(float x, float yFrom, float yTo, float fallback)
        {
            Physics2D.SyncTransforms();
            // トリガー (闘技場の範囲など) と、始点が中に入っている当たり (距離 0) は除く
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = 1 << LayerGround };
            var hits = new RaycastHit2D[8];
            var n = Physics2D.Raycast(new Vector2(x, yFrom), Vector2.down, filter, hits, yFrom - yTo);
            var best = fallback;
            var bestD = float.MaxValue;
            for (var i = 0; i < n; i++)
                if (hits[i].distance > 0.01f && hits[i].distance < bestD)
                {
                    bestD = hits[i].distance;
                    best = hits[i].point.y;
                }
            return best;
        }

        private static RectInt MainRect(Area1Layout.Room room) =>
            room.Rects.OrderByDescending(r => r.width * r.height).First();

        // ================================================================== 地下

        private static void DepthUnderground(Transform root, string sceneId, DepthLook L, RectInt b, List<Area1Layout.Room> rooms,
            AtmosphereKind kind, StringBuilder log)
        {
            var c = (Vector2)b.center;

            // ---- 遠景: 機械の大広間 (縦横に Tiled, 0.93 = ほとんどカメラに張り付く)。行の数を奇数にして、行の境 (= 床の帯) の位置を中心から決める
            const float farScale = 6f;
            var farSpr = Art(AtmosArt("ug_far_machinery.png"));
            var P = farSpr.bounds.size.y * farScale;
            var rows = Mathf.CeilToInt((b.height * 0.1f + 40f) / P);
            if (rows % 2 == 0) rows++;
            var farW = b.width * 0.1f + 70f;
            var far = DepthLayer(root, "Far", c, new Vector2(0.93f, 0.93f), c);
            DepthSprite(far, "Hall", "ug_far_machinery.png", Vector2.zero, Vector2.one * farScale, OrderSky, L.Far, new Vector2(farW, rows * P));
            // 遠いほど霞む: 大広間全体に霧の色を薄く + 各階の床の帯に霧
            DepthFill(far, "Haze", Vector2.zero, new Vector2(farW, rows * P), OrderSky + 1, A(L.Fog, 0.1f));
            for (var k = 0; k < rows; k++)
            {
                var floorY = -rows * P * 0.5f + k * P;           // 行の下端
                FogBand(far, $"FogFar{k}", floorY + P * 0.16f, farW, P * 0.34f, OrderSky + 1, A(L.Fog, 0.42f), k % 2 == 1);
            }

            // ---- 中景: 配管の段 (13 ごとに 1 段, 0.6)。暗く沈め、段の根元を霧で隠す (足場に見えないように)
            const float midScale = 4f, spacing = 11f;
            var midW = b.width * 0.42f + 50f;
            var half = (b.height * 0.42f + 24f) * 0.5f;
            var mid = DepthLayer(root, "Mid", c, new Vector2(0.6f, 0.6f), c);
            var n = Mathf.CeilToInt(half / spacing);
            for (var k = -n; k <= n; k++)
            {
                var y = k * spacing - 3f;
                var row = new GameObject($"Row{k + n}").transform;
                row.SetParent(mid, false);
                row.localPosition = new Vector3(0f, y, 0f);
                var flip = (k & 1) == 1;
                DepthSprite(row, "Pipes", "ug_mid_pipes.png", new Vector2(flip ? 3.3f : 0f, 0f), Vector2.one * midScale, OrderSky + 2,
                    L.Mid, new Vector2(midW, Art(AtmosArt("ug_mid_pipes.png")).bounds.size.y * midScale), null, flip);
                FogBand(row, "FogMid", 0.5f, midW, 2.6f, OrderSky + 3, A(L.Fog, 0.7f), !flip);
                // 段の下は奥へ沈む暗がり (段の下端が平らな線に見えないように)
                var under = new GameObject("Under");
                under.transform.SetParent(row, false);
                under.transform.localPosition = new Vector3(0f, -2f, 0f);
                under.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                under.transform.localScale = new Vector3(midW / (32f / Area1AssetPrep.PixelsPerUnit), 4f / (Art("fx/shade.png").bounds.size.y), 1f);
                AddSprite(under, Art("fx/shade.png"), OrderSky + 2, null, A(L.Fill, 0.5f));
                FogBand(row, "FogMidHigh", 3.4f, midW, 3.6f, OrderSky + 3, A(L.Fog, 0.18f), flip);
            }

            // ---- 光の筋: 天井の口 (上向きの出口) と高い縦坑
            var shafts = new GameObject("Shafts").transform;
            shafts.SetParent(root, false);
            var add = PolishMat("A1_Additive");
            var shaftSpr = Art(AtmosArt("ug_light_shaft.png"));
            var nShaft = 0;
            var upXs = new List<float>();
            foreach (var e in BoundaryExits(sceneId).Where(e => e.Dir == ExitDir.Up))
            {
                var r = e.Carved;
                var sx = Mathf.Clamp(r.width * 0.75f / shaftSpr.bounds.size.x, 1.2f, 4.5f);
                // 長さ = 口から部屋の床の少し上まで (光は下へ行くほど薄れる絵)
                var ours = rooms.FirstOrDefault(x => x.Id == e.OurRoom);
                var floorY = ours != null ? MainRect(ours).yMin : r.yMin - 12;
                var len = Mathf.Clamp((r.yMax - floorY) * 0.9f, 10f, 30f);
                DepthSprite(shafts, "Shaft_" + (e.OtherScene ?? e.OtherRoom), "ug_light_shaft.png", new Vector2(r.center.x, r.yMax),
                    new Vector2(sx, len / shaftSpr.bounds.size.y), OrderDepthShaft, L.Shaft, null, add);
                upXs.Add(r.center.x);
                nShaft++;
            }
            foreach (var room in rooms)
            {
                var r = MainRect(room);
                if (r.height < 24 || upXs.Any(x => x >= r.xMin && x <= r.xMax))
                    continue;
                DepthSprite(shafts, "Shaft_" + room.Id, "ug_light_shaft.png", new Vector2(r.center.x, r.yMax + 1f),
                    new Vector2(Mathf.Clamp(r.width * 0.35f / shaftSpr.bounds.size.x, 1.5f, 3.5f), Mathf.Clamp(r.height * 0.8f, 12f, 30f) / shaftSpr.bounds.size.y), OrderDepthShaft, A(L.Shaft, L.Shaft.a * 0.8f), null, add);
                nShaft++;
            }

            // ---- 前景: 部屋ごとに 1〜2 個 (画面の端寄り)。ボス部屋には置かない
            var fore = new GameObject("Fore").transform;
            fore.SetParent(root, false);
            var nFore = 0;
            var idx = 0;
            foreach (var room in rooms)
            {
                idx++;
                if (!string.IsNullOrEmpty(room.Boss))
                    continue;
                var r = MainRect(room);
                if (r.width < 16 || r.height < 9)
                    continue;
                var camY = Mathf.Min(r.yMin + 4f, r.center.y);
                var camXL = Mathf.Min(r.xMin + 9.8f, r.center.x);
                var camXR = Mathf.Max(r.xMax - 9.8f, r.center.x);
                var fac = new Vector2(-0.3f, -0.12f);
                // 左: 鎖 (天井から吊る) / 鉄骨 (左上の角) を交互
                if (idx % 2 == 0)
                {
                    var a = new Vector2(camXL, camY);
                    var lay = DepthLayer(fore, "Chain_" + room.Id, a + new Vector2(-7.4f, 6.3f), fac, a);
                    DepthSprite(lay, "Art", "ug_fore_chain.png", Vector2.zero, Vector2.one * 1.4f, OrderDepthFore, L.Fore);
                }
                else
                {
                    var a = new Vector2(camXL, camY + Mathf.Min(4f, r.height - 11f > 0 ? 4f : 0f));
                    var lay = DepthLayer(fore, "Girder_" + room.Id, a + new Vector2(-8.6f, 5.6f), fac, a);
                    DepthSprite(lay, "Art", "ug_fore_girder.png", Vector2.zero, Vector2.one * 1.5f, OrderDepthFore, L.Fore, null, null, true);
                }
                nFore++;
                if (r.width < 26)
                    continue;
                // 右: 縦の配管 (画面の右端) / 歯車 (右上の角) を交互
                if (idx % 2 == 0)
                {
                    var a = new Vector2(camXR, camY + (r.height > 14 ? 3f : 0f));
                    var lay = DepthLayer(fore, "Gear_" + room.Id, a + new Vector2(10.4f, 6.8f), fac, a);
                    DepthSprite(lay, "Art", "ug_fore_gear.png", Vector2.zero, Vector2.one * 1.35f, OrderDepthFore, L.Fore, null, null, false, 180f);
                }
                else
                {
                    var a = new Vector2(camXR, camY);
                    var lay = DepthLayer(fore, "Pipe_" + room.Id, a + new Vector2(9.4f, 0f), fac, a);
                    var ps = Art(AtmosArt("ug_fore_pipe.png"));
                    DepthSprite(lay, "Art", "ug_fore_pipe.png", Vector2.zero, Vector2.one * 1.5f, OrderDepthFore, L.Fore,
                        new Vector2(ps.bounds.size.x * 1.5f, 40f));
                }
                nFore++;
            }
            log.Append($" underground far rows={rows} mid rows={2 * n + 1} shafts={nShaft} fore={nFore}");
        }

        // ================================================================== 夜空

        private static void DepthSky(Transform root, string sceneId, DepthLook L, RectInt b, List<Area1Layout.Room> rooms,
            AtmosphereKind kind, StringBuilder log)
        {
            var outRects = rooms.Where(r => r.Outdoor).SelectMany(r => r.Rects).ToList();
            // 床 = 屋外の部屋の一番低い所の実際の床面 (部屋の矩形の下端より上に床を盛っているシーンがある: U1/FP は y3)
            var floor = outRects.Count > 0
                ? outRects.Min(r => Enumerable.Range(1, 5).Select(i => FloorAt(r.xMin + r.width * i / 6f, r.yMin + 8f, r.yMin - 3f, r.yMin)).Min())
                : b.yMin;
            var top = outRects.Count > 0 ? outRects.Max(r => r.yMax) : b.yMax;
            var camLow = floor + 4f;   // 床に立った時のカメラの中心 (実測: 床 + 3.5〜5)
            var range = Mathf.Max(top - 5.5f - camLow, 4f);
            var ground = outRects.Count > 0 && floor < 40;   // 屋外の部屋が無い (観覧車の支柱の中など) は高い所の扱い
            var fyFar = Mathf.Clamp(1f - 3f / range, 0.82f, 0.97f);
            var anchor = new Vector2(b.center.x, camLow);

            // ---- 遠景: 園のスカイライン (テント・城・コースター) + 下の地面 + 根元の霧。地上ではカメラが床にいる時に画面の下半分、
            //      高い所ではてっぺんだけ (カメラが上がるほど少しずつ沈む: 縦の視差 fyFar = 部屋の高さで 3 単位沈む)
            const float skyScale = 5.5f;
            var skySpr = Art(AtmosArt("park_far_skyline_open.png"));
            var skyH = skySpr.bounds.size.y * skyScale;
            var bottom = ground ? -6.2f : -9.5f;
            var farW = b.width * 0.12f + 60f;
            var far = DepthLayer(root, "Far", anchor, new Vector2(0.9f, fyFar), anchor);
            // 高い所で、レシピが眼下の園内 (bg_park_silhouette を画面の下端に張り付けた遠景) を置いているシーン (W4 / MB1) は
            // スカイラインを重ねない (同じ物が 2 段に見える)。霧だけ足す
            var looksDown = !ground && Group("Background").Cast<Transform>().Any(t => t.name == "bg_park_silhouette");
            DepthSprite(far, "Skyline", "park_far_skyline_open.png", new Vector2(0f, bottom), Vector2.one * skyScale, OrderSky + 1, L.Far,
                new Vector2(farW, skyH));
            var groundCol = new Color(33f / 255f * L.Far.r, 29f / 255f * L.Far.g, 90f / 255f * L.Far.b);
            DepthFill(far, "Ground", new Vector2(0f, bottom - 30f + 0.02f), new Vector2(farW, 60f), OrderSky + 1, groundCol);
            FogBand(far, "FogSkyline", bottom + 1.6f, farW, 3.6f, OrderSky + 2, A(L.Fog, 0.5f));
            FogBand(far, "FogSkylineLow", bottom - 0.6f, farW, 3f, OrderSky + 2, A(L.Fog, 0.35f), true);
            FogBand(far, "FogSkylineMid", bottom + skyH * 0.45f, farW, skyH * 0.7f, OrderSky + 2, A(L.Fog, 0.22f), true);
            if (looksDown)
                Object.DestroyImmediate(far.gameObject);

            // ---- 中景: 並木・街灯・柵 (地上のシーンだけ)。観覧車・メリーゴーランドの影絵を除いた版 (park_mid_trees_open)
            if (ground)
            {
                const float treeScale = 6.2f;
                var treeSpr = Art(AtmosArt("park_mid_trees_open.png"));
                var treeH = treeSpr.bounds.size.y * treeScale;
                var tb = floor - 1f - camLow;   // 根元は床の 1 下 (地形の後ろ)
                var midW = b.width * 0.47f + 50f;
                var mid = DepthLayer(root, "Mid", anchor, new Vector2(0.55f, 0.5f), anchor);
                DepthSprite(mid, "Trees", "park_mid_trees_open.png", new Vector2(0f, tb), Vector2.one * treeScale, OrderSky + 3, L.Mid,
                    new Vector2(midW, treeH));
                DepthFill(mid, "Ground", new Vector2(0f, tb - 20f + 0.02f), new Vector2(midW, 40f), OrderSky + 3,
                    new Color(0.13f * L.Mid.r, 0.08f * L.Mid.g, 0.15f * L.Mid.b));
                FogBand(mid, "FogTrees", tb + 1.4f, midW, 3.4f, OrderSky + 4, A(L.Fog, 0.42f), true);
                FogBand(mid, "FogTreesHigh", tb + treeH * 0.62f, midW, 4.5f, OrderSky + 4, A(L.Fog, 0.14f));
            }

            // ---- 地平線のもや (高い所で星空だけの平らな画面にならないように、画面の下にいつも淡い霧)
            var haze = DepthLayer(root, "Haze", anchor, new Vector2(0.96f, fyFar), anchor);
            FogBand(haze, "Haze", ground ? -6f : -5f, farW, ground ? 5f : 7.5f, OrderSky + 2, A(L.Fog, ground ? 0.25f : 0.42f), true);

            // ---- 前景: 地上の屋外の部屋に茂み・街灯 (+ 広場に旗飾り)。ボス部屋には置かない
            var fore = new GameObject("Fore").transform;
            fore.SetParent(root, false);
            var nFore = 0;
            var idx = 0;
            var add = PolishMat("A1_Additive");
            foreach (var room in rooms.Where(r => r.Outdoor && string.IsNullOrEmpty(r.Boss)))
            foreach (var r in room.Rects)
            {
                if (r.width < 20 || r.yMin >= 40)
                    continue;
                // 部屋の幅 30 ごとに 1 組 (茂み = 左寄り、街灯 = 右寄り)
                var pairs = Mathf.Max(1, Mathf.RoundToInt(r.width / 30f));
                for (var p = 0; p < pairs; p++)
                {
                    idx++;
                    var cx = Mathf.Lerp(r.xMin + 9.8f, r.xMax - 9.8f, pairs == 1 ? 0.5f : p / (float)(pairs - 1));
                    var fac = new Vector2(-0.3f, 0f);
                    // 茂み
                    var bx = cx - 6.8f;
                    var bf = FloorAt(bx, r.yMin + 4.5f, r.yMin - 3f, r.yMin);
                    var a = new Vector2(cx, bf + 5.5f);
                    var bush = DepthLayer(fore, $"Bush_{room.Id}_{p}", new Vector2(bx, bf - 1.45f), fac, a);   // 茂みの上端 = 床面 (足元を隠さない)
                    DepthSprite(bush, "Art", "park_fore_bush.png", Vector2.zero, Vector2.one * 1.4f, OrderDepthFore, L.Fore, null, null, idx % 2 == 0);
                    nFore++;
                    // 街灯 (灯に光暈を加算で重ねる)
                    var lx = cx + (idx % 2 == 0 ? 7.6f : -8.6f);
                    var lf = FloorAt(lx, r.yMin + 4.5f, r.yMin - 3f, r.yMin);
                    var lamp = DepthLayer(fore, $"Lamp_{room.Id}_{p}", new Vector2(lx, lf - 1f), fac, new Vector2(cx, lf + 5.5f));
                    var ls = DepthSprite(lamp, "Art", "park_fore_lamp.png", Vector2.zero, Vector2.one * 2f, OrderDepthFore, L.Fore);
                    var globeY = (ls.sprite.rect.height - 95f) / Area1AssetPrep.PixelsPerUnit * 2f;
                    DepthSprite(lamp, "Glow", "p_glow.png", new Vector2(0f, globeY), Vector2.one * 4.2f, OrderDepthFore + 1,
                        new Color(1f, 0.82f, 0.55f, 0.55f), null, add);
                    DepthSprite(lamp, "Globe", "p_soft_dot.png", new Vector2(0f, globeY), Vector2.one * 0.9f, OrderDepthFore + 1,
                        new Color(1f, 0.9f, 0.7f, 0.9f), null, add);
                    nFore++;
                }
                // 広場 (幅 40 以上・高さ 14 以上) には画面の上に旗飾り
                if (r.width >= 40 && r.height >= 14)
                {
                    var a = new Vector2(r.center.x, r.yMin + 5.5f);
                    var bunt = DepthLayer(fore, "Bunting_" + room.Id, a + new Vector2(0f, 5.9f), new Vector2(-0.2f, -0.1f), a);
                    DepthSprite(bunt, "Art", "park_fore_bunting.png", Vector2.zero, Vector2.one * 1.6f, OrderDepthFore,
                        new Color(0.55f, 0.5f, 0.62f), new Vector2(r.width * 0.2f + 26f, Art(AtmosArt("park_fore_bunting.png")).bounds.size.y * 1.6f));
                    nFore++;
                }
            }
            log.Append($" sky ground={ground} floor={floor} camRange={range:F0} fyFar={fyFar:F2} fore={nFore}");
        }

        // ================================================================== シーン固有

        private static void DepthExtras(Transform root, string sceneId, DepthLook L, StringBuilder log)
        {
            if (sceneId == "A1_AB")
            {
                // ST の石の縦穴: 真っ黒な穴に見えた → 井戸の壁を明るく + 上 (噴水の排水口) からの月明かりの筋 (上向きの出口で自動) + 底に霧
                var well = Group("Background").Find("STWell");
                if (well != null)
                    foreach (var sr in well.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.color = STWellColor;
                var extra = new GameObject("STWellMist").transform;
                extra.SetParent(root, false);
                extra.position = new Vector3(351f, 0f, 0f);
                FogBand(extra, "MistLow", -24.5f, 18f, 4f, OrderRoomBackdrop + 2, new Color(0.55f, 0.66f, 0.9f, 0.35f));
                FogBand(extra, "MistMid", -14f, 18f, 5f, OrderRoomBackdrop + 2, new Color(0.55f, 0.66f, 0.9f, 0.08f), true);
                log.Append(" AB:STWell");
            }
        }

        /// <summary>A1_AB の ST の井戸の壁の色 (build_A1_AB.cs と同じ値)。</summary>
        public static readonly Color STWellColor = new Color(0.36f, 0.4f, 0.6f);
    }
}
