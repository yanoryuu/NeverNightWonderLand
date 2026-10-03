using NeverNight.Stage;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeverNight.StageBuild
{
    /// <summary>
    /// polish B (磨き込み B) で足した組み立て部品。素材は Area1Gen/polish/ (manifest.json のピボット)、
    /// 流れる水・蒸気・加算の光は Area1AssetPrep.PreparePolish() が作る A1SpriteScroll のマテリアル。
    /// 手順と例は Docs/Area1/SceneBuildGuide.md §10。
    /// </summary>
    public static partial class Area1StageBuilder
    {
        public enum WaterLook { Water, Oil }

        /// <summary>polish のマテリアル (A1_Additive / A1_WaterJet / A1_SteamRise / A1_SurfaceDrift / A1_WaterBody / A1_OilBody)。</summary>
        public static Material PolishMat(string name)
        {
            var m = Area1AssetPrep.PolishMat(name);
            if (m == null)
                throw new System.ArgumentException("material not found: " + name + " (Area1AssetPrep.PreparePolish 済み?)");
            return m;
        }

        /// <summary>子の GameObject + SpriteRenderer (relPath は Area1Gen 基準)。mat を渡すとそのマテリアルで描く。</summary>
        public static SpriteRenderer PartSprite(Transform parent, string name, string relPath, Vector2 localPos, float scale, int order,
            Color? color = null, Vector2? tiledSize = null, string mat = null, float rotZ = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = AddSprite(go, Art(relPath), order, tiledSize, color);
            if (mat != null)
                sr.sharedMaterial = PolishMat(mat);
            return sr;
        }

        // ================================================================== 噴水

        /// <summary>
        /// 部品で組んだ噴水 (飾り, 当たり無し, Props)。(x, floorY) = 受け皿の底の中心。s = 部品の倍率 (1 = manifest のセル寸法:
        /// 受け皿 6×2 / 中段の鉢 4×1.5 / 頂部 2×3 → 全体 幅 6s・高さ ≈6.1s)。
        /// 水: 中段の鉢の縁と頂部の小鉢の縁から水の幕 (water_sheet_fall, かすかに伸び縮み) + 受け皿の左右から上へ噴く水 (water_jet_column, 加算)
        /// + 落ちた所のしぶき (water_splash, 揺らぎ) + 白いもや (加算)。描画順は order (中段) 〜 order+5。
        /// A1_FP の大噴水と柵の脇の小噴水、A1_U1 の柵越しの予告 (小噴水と同じ値で呼ぶ) に使う。
        /// </summary>
        public static GameObject Fountain(float x, float floorY, float s, Color? tint = null, int order = -9, string name = "Fountain",
            bool jets = true)
        {
            var c = tint ?? Color.white;
            var root = NewObject(name, new Vector2(x, floorY), LayerDefault, "Props");
            var t = root.transform;
            const string P = "polish/";
            // 組み上げ (manifest の pivot = bottom)。段の高さは絵の実測: 受け皿の台座の上端 1.66、鉢の上の台座 1.50 (少し重ねる)
            var tierY = 1.62f * s;
            var topY = tierY + 1.46f * s;
            PartSprite(t, "Tier", P + "fountain_tier.png", new Vector2(0, tierY), s, order, c);
            PartSprite(t, "Top", P + "fountain_top.png", new Vector2(0, topY), s, order + 1, c);
            PartSprite(t, "Basin", P + "fountain_basin.png", Vector2.zero, s, order + 3, c);

            var water = new Color(0.85f * c.r + 0.15f, 0.95f * c.g + 0.05f, 1f * c.b, 0.85f);
            // 水の幕: 中段の鉢の縁 (鉢の上端から 0.2 下, 幅 2.6s) → 受け皿の縁 (1.2s)。
            var rimY = tierY + 1.5f * s * 0.80f;
            var basinRimY = 1.2f * s;
            var fallH = rimY - basinRimY;
            // 水の幕は流さない (絵の上端の縁の線と筋の先の滴が縦に繋がらないので、流すと 2 本目の縁が下りてくる)。かすかに伸び縮みさせる
            var sheet = PartSprite(t, "SheetTier", P + "water_sheet_fall.png", new Vector2(0, rimY), s, order + 2, water,
                new Vector2(2.55f, fallH / s));
            AddComp<SpriteWobble>(sheet.gameObject, ("_scaleAmplitude", new Vector2(0.01f, 0.035f)), ("_speed", 6f), ("_phase", x));
            // 頂部の小鉢の縁 (頂部の底から 0.75s, 幅 1.25s) → 中段の鉢の水面
            var topRimY = topY + 0.72f * s;
            var sheetTop = PartSprite(t, "SheetTop", P + "water_sheet_fall.png", new Vector2(0, topRimY), s, order + 2, water,
                new Vector2(1.25f, (topRimY - rimY) / s));
            AddComp<SpriteWobble>(sheetTop.gameObject, ("_scaleAmplitude", new Vector2(0.01f, 0.04f)), ("_speed", 7f), ("_phase", x + 2f));
            // 受け皿に落ちた所のしぶき (左右) と、鉢に落ちた所の小さなしぶき
            for (var i = -1; i <= 1; i += 2)
            {
                var sp = PartSprite(t, i < 0 ? "SplashL" : "SplashR", P + "water_splash.png", new Vector2(i * 1.0f * s, basinRimY - 0.32f * s),
                    0.62f * s, order + 4, new Color(water.r, water.g, water.b, 0.75f), null, "A1_Additive");
                AddComp<SpriteWobble>(sp.gameObject, ("_scaleAmplitude", new Vector2(0.08f, 0.18f)), ("_speed", 7f + i), ("_phase", x + i));
            }
            var spT = PartSprite(t, "SplashTier", P + "water_splash.png", new Vector2(0, rimY - 0.2f * s), 0.42f * s, order + 2,
                new Color(water.r, water.g, water.b, 0.6f), null, "A1_Additive");
            AddComp<SpriteWobble>(spT.gameObject, ("_scaleAmplitude", new Vector2(0.06f, 0.2f)), ("_speed", 8.5f), ("_phase", x));
            if (jets)
            {
                // 受け皿の左右の口から上へ噴く水 (加算の柱 + 先端のしぶき)
                for (var i = -1; i <= 1; i += 2)
                {
                    var jx = i * 2.25f * s;
                    var jetH = 1.35f * s;
                    var jet = PartSprite(t, i < 0 ? "JetL" : "JetR", P + "water_jet_column.png", new Vector2(jx, basinRimY + jetH), 0.36f * s,
                        order + 4, new Color(water.r, water.g, water.b, 0.42f), new Vector2(1f, jetH / (0.36f * s)), "A1_WaterJet");
                    jet.transform.localRotation = Quaternion.Euler(0, 0, -i * 3f);   // 少し外へ開く
                    var cap = PartSprite(t, i < 0 ? "JetCapL" : "JetCapR", P + "water_splash.png",
                        new Vector2(jx + i * 0.1f * s, basinRimY + jetH - 0.3f * s), 0.4f * s, order + 4,
                        new Color(water.r, water.g, water.b, 0.6f), null, "A1_Additive");
                    AddComp<SpriteWobble>(cap.gameObject, ("_scaleAmplitude", new Vector2(0.12f, 0.25f)), ("_speed", 9f), ("_phase", i * 2f));
                    AddComp<SpriteWobble>(jet.gameObject, ("_scaleAmplitude", new Vector2(0.06f, 0f)), ("_speed", 11f), ("_phase", i * 1.3f));
                }
            }
            // 白いもや (加算)
            var mist = PartSprite(t, "Mist", "fx/glow.png", new Vector2(0, basinRimY + 0.2f * s), 1f, order + 5,
                new Color(0.75f, 0.88f, 1f, 0.22f), null, "A1_Additive");
            mist.transform.localScale = new Vector3(5.2f * s / (256f / Area1AssetPrep.PixelsPerUnit) * 0.5f,
                1.6f * s / (256f / Area1AssetPrep.PixelsPerUnit) * 0.5f, 1f);
            return root;
        }

        // ================================================================== 水・油

        /// <summary>
        /// 水 / 油の見た目だけ (当たり無し)。[x0,x1) × [y0, surfaceY)。
        /// 奥 = 半透明の水の模様 (water_body_tile / oil_body_tile, ゆっくり流れる, backOrder) + 手前 = 同じ模様をごく薄く (frontOrder, プレイヤーが透ける)
        /// + 水面の帯 (water_surface_strip / oil_surface_strip, 横へ流れる + 上下の揺らぎ)。tint は模様の色に乗算 (地下は暗く)。
        /// </summary>
        public static GameObject WaterVisual(float x0, float y0, float x1, float surfaceY, WaterLook look = WaterLook.Water,
            Color? tint = null, int backOrder = OrderProp - 1, int frontOrder = OrderGimmick + 2, string name = "WaterVisual",
            string group = "Props", float backAlpha = 0.8f, float frontAlpha = 0.3f)
        {
            var c = tint ?? Color.white;
            var w = x1 - x0;
            var h = surfaceY - y0;
            var root = NewObject(name, new Vector2(x0 + w * 0.5f, y0 + h * 0.5f), LayerDefault, group);
            var t = root.transform;
            var oil = look == WaterLook.Oil;
            var body = oil ? "polish/oil_body_tile.png" : "polish/water_body_tile.png";
            var strip = oil ? "polish/oil_surface_strip.png" : "polish/water_surface_strip.png";
            var bodyMat = oil ? "A1_OilBody" : "A1_WaterBody";
            // 水面の帯の下に潜り込むよう、模様の上端は水面 -0.12
            var bodyH = h - 0.12f;
            var bodyPos = new Vector2(0, -h * 0.5f + bodyH * 0.5f);
            PartSprite(t, "Back", body, bodyPos, 1f, backOrder, new Color(c.r, c.g, c.b, backAlpha), new Vector2(w, bodyH), bodyMat);
            PartSprite(t, "Front", body, bodyPos, 1f, frontOrder, new Color(c.r, c.g, c.b, frontAlpha), new Vector2(w, bodyH), bodyMat);
            // 水面の帯 (pivot top, 高さ 0.5)。波の山 (帯の上から 0.1) が水面に来るよう、帯の上端 = 水面 +0.1
            var surf = PartSprite(t, "Surface", strip, new Vector2(0, h * 0.5f + 0.1f), 1f, frontOrder,
                new Color(Mathf.Lerp(c.r, 1f, 0.35f), Mathf.Lerp(c.g, 1f, 0.35f), Mathf.Lerp(c.b, 1f, 0.35f), 0.95f), new Vector2(w, 0.5f), "A1_SurfaceDrift");
            AddComp<SpriteWobble>(surf.gameObject, ("_scaleAmplitude", Vector2.zero), ("_moveAmplitude", new Vector2(0f, 0.035f)), ("_speed", 2.2f));
            return root;
        }

        // ================================================================== 観覧車の飾り

        /// <summary>
        /// 電球の連なり (bulb_string を横に Tiled, pivot top = 吊り元の高さ y, 4 セルで 4 つのたるみ)。当たり無し。
        /// blink = 3 本ずらした明滅 (BlinkingLight, 弱い)。scale は絵の倍率 (既定 0.75 = たるみ 3 単位ごと)。
        /// </summary>
        public static GameObject BulbString(float x0, float x1, float y, float scale = 0.75f, int order = -6, Color? color = null,
            bool blink = true, string group = "Props", string name = "BulbString")
        {
            var go = NewObject(name, new Vector2((x0 + x1) * 0.5f, y), LayerDefault, group);
            var sr = PartSprite(go.transform, "String", "polish/bulb_string.png", Vector2.zero, scale, order, color ?? Color.white,
                new Vector2((x1 - x0) / scale, 1f));
            if (blink)
                AddComp<BlinkingLight>(go, ("_period", 2.6f), ("_phase", (x0 * 0.137f) % 1f), ("_minAlpha", 0.7f), ("_onRatio", 0.7f));
            return go;
        }

        /// <summary>
        /// 中景を通り過ぎるゴンドラ (passing_gondola_far, 暗いシルエット, 視差 + ゆっくり揺れる)。pos/factor/anchor は ParallaxProp と同じ。
        /// 乗れるゴンドラ (W5) と見間違えないよう暗く・小さく (既定 0.7 倍 = 1.4 単位)。
        /// </summary>
        public static GameObject FarGondola(Vector2 pos, Vector2 factor, Vector2 anchor, float scale = 0.7f, Color? color = null,
            int order = -92, float swing = 3f)
        {
            // 視差 (ParallaxLayer が位置を上書きする) と揺れ (SpriteWobble) は別の GameObject に分ける
            var go = new GameObject("FarGondola");
            go.transform.SetParent(Group("Background"), false);
            go.transform.position = pos;
            AddComp<ParallaxLayer>(go, ("_factor", factor), ("_anchor", anchor));
            var sr = PartSprite(go.transform, "Cabin", "polish/passing_gondola_far.png", Vector2.zero, scale, order,
                color ?? new Color(0.62f, 0.56f, 0.76f, 0.9f));
            AddComp<SpriteWobble>(sr.gameObject, ("_scaleAmplitude", Vector2.zero), ("_moveAmplitude", new Vector2(0.12f, 0.06f)),
                ("_speed", 0.9f), ("_phase", pos.x * 0.31f));
            return go;
        }

        /// <summary>
        /// 手前を横切る観覧車の外周の一部 (rim_segment_near, 電球付き)。pos = 弧の中心 (カメラが anchor にある時)、angle = 弧の向き (0 = 弧が上に膨らむ)。
        /// 手前 (factor 負 = カメラより速く流れる) で暗いシルエットにして画面の縁を縁取る。order は足場より手前 (既定 20)。当たり無し。
        /// </summary>
        public static GameObject NearRim(Vector2 pos, float angle, float scale, Vector2 factor, Vector2 anchor, Color? color = null,
            int order = 20)
        {
            var go = ParallaxProp("polish/rim_segment_near.png", pos, factor, anchor, scale, order,
                color ?? new Color(0.42f, 0.34f, 0.52f, 1f));
            go.name = "NearRim";
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            return go;
        }
    }
}
