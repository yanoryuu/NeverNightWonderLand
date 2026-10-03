using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace NeverNight.StageBuild
{
    /// <summary>
    /// エリア1 の素材準備 (再実行可能)。eval_file から呼ぶ:
    /// <code>return NeverNight.StageBuild.Area1AssetPrep.PrepareAll();</code>
    /// - Area1Gen 以下の全 PNG のインポート設定 (PPU546 / Sprite Single / FullRect / manifest のピボット /
    ///   mipmap off / Uncompressed / Bilinear / alphaIsTransparency)
    /// - タイル用スプライトの Tile アセット (Tiles/Area1/)
    /// - 大観覧車プレハブ (A1_FerrisWheel.prefab)
    /// </summary>
    public static class Area1AssetPrep
    {
        public const string ArtRoot = "Assets/LocalData/StageAsset/Area1Gen";
        public const string TileDir = "Assets/LocalData/StageAsset/Tiles/Area1";
        public const string PrefabDir = "Assets/Prefabs/Area1";
        public const string FerrisPrefabPath = PrefabDir + "/A1_FerrisWheel.prefab";

        public const float PixelsPerUnit = 546f;

        /// <summary>polish B の部品 (manifest.json あり) と、それを流す/光らせるマテリアル (mat/)。</summary>
        public const string PolishDir = ArtRoot + "/polish";
        public const string PolishMatDir = PolishDir + "/mat";
        public const string ScrollShaderName = "NeverNight/A1SpriteScroll";

        // 岩の質感 (Area1Gen/rock/)。rock_tex_r{row}_c{col} = 4x4 セルで継ぎ目なく繋がる岩肌の 1 セル分
        public const string RockDir = ArtRoot + "/rock";
        /// <summary>共有の縁タイルから「内側のベタ塗り (プラム色)」だけを透明にした複製の置き場 (MakeEdgeCutouts が生成)。</summary>
        public const string RockEdgeDir = RockDir + "/edges";
        /// <summary>エリア1 専用の地形 RuleTile (StageTerrainRule の複製。内部=透明、縁=切り抜き版)。旧シーンの共有ルールは触らない。</summary>
        public const string A1TerrainRulePath = TileDir + "/A1TerrainRule.asset";
        public const string SharedTerrainRulePath = "Assets/LocalData/StageAsset/Tiles/Rules/StageTerrainRule.asset";
        public const string RockSealTileName = "a1_rock_seal";
        public const int RockTexPeriod = 4;
        public static string RockTexTileName(int row, int col) => $"a1_rock_tex_r{row}_c{col}";

        private const string TileFloorRoot = "Assets/LocalData/StageAsset/tile_floor";

        /// <summary>StageTerrainRule が使う縁タイル → 切り抜き版のファイル名。</summary>
        private static readonly (string src, string dst)[] EdgeSprites =
        {
            ("floor_04/stagetile_floor_04_l.png", "edge_floor_04_l.png"),
            ("floor_04/stagetile_floor_04_m.png", "edge_floor_04_m.png"),
            ("floor_04/stagetile_floor_04_r.png", "edge_floor_04_r.png"),
            ("StageTile/stagetile_floor_01.png", "edge_floor_01.png"),
            ("floor_07/stagetile_floor_07_l.png", "edge_floor_07_l.png"),
            ("floor_07/stagetile_floor_07_m.png", "edge_floor_07_m.png"),
            ("floor_07/stagetile_floor_07_r.png", "edge_floor_07_r.png"),
            ("StageTile/stagetile_ceiling_01.png", "edge_ceiling_01.png"),
            ("StageTile/stagetile_wall_01.png", "edge_wall_01.png"),
            ("corner/角.png", "edge_corner.png"),
        };

        // 観覧車の共通配置 (Area1Build.md §7a)
        public static readonly Vector2 FerrisCenter = new Vector2(54f, 252f);
        public const float FerrisRadius = 90f;

        public static string PrepareAll(bool rebuildFerris = false)
        {
            var log = new StringBuilder();
            log.AppendLine(MakeEdgeCutouts());
            log.AppendLine(ImportArt());
            log.AppendLine(CreateTiles());
            log.AppendLine(CreateRockTiles());
            log.AppendLine(BuildA1TerrainRule());
            log.AppendLine(BuildFerrisWheelPrefab(rebuildFerris));
            log.AppendLine(PreparePlate());
            log.AppendLine(PrepareFx());
            log.AppendLine(PreparePolish());
            return log.ToString();
        }

        // ------------------------------------------------------------------ インポート設定

        [System.Serializable]
        private class ManifestItem
        {
            public string file;
            public string pivot;
            public bool tileable;
        }

        [System.Serializable]
        private class ManifestWrap
        {
            public ManifestItem[] items;
        }

        private static Dictionary<string, string> ReadPivots()
        {
            var result = new Dictionary<string, string>();
            void Read(string manifestPath, string prefix)
            {
                if (!File.Exists(manifestPath))
                    return;
                var json = "{\"items\":" + File.ReadAllText(manifestPath, Encoding.UTF8) + "}";
                var wrap = JsonUtility.FromJson<ManifestWrap>(json);
                foreach (var it in wrap.items)
                    result[(prefix + it.file).Replace('\\', '/')] = it.pivot;
            }

            Read(ArtRoot + "/manifest.json", ArtRoot + "/");
            Read(ArtRoot + "/ferris/manifest.json", ArtRoot + "/ferris/");
            Read(RockDir + "/manifest.json", RockDir + "/"); // pivot 欄なし = center
            Read(FxDir + "/manifest.json", FxDir + "/");     // 手続き生成の効果素材 (PrepareFx)
            Read(PolishDir + "/manifest.json", PolishDir + "/"); // polish B の部品 (噴水・水・蒸気・音楽堂・観覧車の飾り)
            return result;
        }

        /// <summary>polish/manifest.json で tileable の素材 (継ぎ目なく繋がる → wrapMode Repeat。A1SpriteScroll で流せる)。</summary>
        private static HashSet<string> ReadTileable()
        {
            var result = new HashSet<string>();
            var manifestPath = PolishDir + "/manifest.json";
            if (!File.Exists(manifestPath))
                return result;
            var wrap = JsonUtility.FromJson<ManifestWrap>("{\"items\":" + File.ReadAllText(manifestPath, Encoding.UTF8) + "}");
            foreach (var it in wrap.items)
                if (it.tileable)
                    result.Add((PolishDir + "/" + it.file).Replace('\\', '/'));
            return result;
        }

        private static SpriteAlignment Alignment(string pivot)
        {
            switch (pivot)
            {
                case "bottom": return SpriteAlignment.BottomCenter;
                case "top": return SpriteAlignment.TopCenter;
                case "left": return SpriteAlignment.LeftCenter;
                case "right": return SpriteAlignment.RightCenter;
                default: return SpriteAlignment.Center;
            }
        }

        /// <summary>Area1Gen 以下の全 PNG のインポート設定を揃える (変更が無ければ再インポートしない)。</summary>
        public static string ImportArt()
        {
            var pivots = ReadPivots();
            var tileable = ReadTileable();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            int changed = 0, total = 0;
            var missingPivot = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png"))
                        continue;
                    total++;
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    if (!pivots.TryGetValue(path, out var pivot))
                    {
                        // 縁タイルの切り抜き (MakeEdgeCutouts の生成物) は元タイルと同じ center
                        if (!path.StartsWith(RockEdgeDir + "/"))
                            missingPivot.Add(path);
                        pivot = "center";
                    }

                    // 岩の素材はタイルとして隣と接するので Clamp (Repeat だと反対側の縁がにじんで継ぎ目が出る)
                    // polish の継ぎ目なし素材は Repeat (UV をずらして流すため)
                    var wrap = path.StartsWith(RockDir + "/") ? TextureWrapMode.Clamp
                        : tileable.Contains(path) ? TextureWrapMode.Repeat : importer.wrapMode;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    var align = Alignment(pivot);

                    // 元画像の大きさから maxTextureSize を決める (4096 を超える素材もある)
                    importer.GetSourceTextureWidthAndHeight(out var w, out var h);
                    var maxSize = 2048;
                    while (maxSize < Mathf.Max(w, h) && maxSize < 8192) maxSize *= 2;

                    var dirty = importer.textureType != TextureImporterType.Sprite
                                || importer.spriteImportMode != SpriteImportMode.Single
                                || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
                                || settings.spriteMeshType != SpriteMeshType.FullRect
                                || settings.spriteAlignment != (int)align
                                || importer.mipmapEnabled
                                || importer.textureCompression != TextureImporterCompression.Uncompressed
                                || importer.filterMode != FilterMode.Bilinear
                                || !importer.alphaIsTransparency
                                || importer.maxTextureSize != maxSize
                                || importer.wrapMode != wrap;
                    if (!dirty)
                        continue;

                    importer.wrapMode = wrap;
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = PixelsPerUnit;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    importer.maxTextureSize = maxSize;
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)align;
                    settings.spriteGenerateFallbackPhysicsShape = true;
                    settings.wrapMode = wrap;   // (Sprite への切り替えと同じ回で wrapMode が Clamp に戻ることがあったので明示)
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return $"[ImportArt] png={total} changed={changed}" +
                   (missingPivot.Count > 0 ? " noManifest=" + string.Join(",", missingPivot.Select(Path.GetFileName)) : "");
        }

        // ------------------------------------------------------------------ Tile アセット

        /// <summary>
        /// タイル型スプライトの Tile アセットを作る/更新する。慣例 (StageTileRules.md / tile-grid メモ):
        /// 1x1 はオフセットなし (flags=LockColor)、オフセットが付く物は LockTransform を足す。
        /// ブロック類 (ひび壁/落下/突進/大ジャンプ) は collider=Grid (Terrain に塗って見た目だけの壁にもできる)、
        /// トゲ/梯子/コンベアは見た目専用 (collider=None。当たりは HazardZone / 足場コンポーネント側)。
        /// </summary>
        public static string CreateTiles()
        {
            EnsureFolder(TileDir);
            var specs = new[]
            {
                // name, sprite, collider, offset
                ("a1_crack_wall", "tiles/tile_crack_wall.png", Tile.ColliderType.Grid, Vector2.zero),
                ("a1_slam_block", "tiles/tile_slam_block.png", Tile.ColliderType.Grid, Vector2.zero),
                ("a1_rush_block", "tiles/tile_rush_block.png", Tile.ColliderType.Grid, Vector2.zero),
                ("a1_super_block", "tiles/tile_super_block.png", Tile.ColliderType.Grid, Vector2.zero),
                // ピボット bottom → セル下端に合わせるため -0.5
                ("a1_spikes", "tiles/tile_spikes.png", Tile.ColliderType.None, new Vector2(0f, -0.5f)),
                ("a1_ladder", "tiles/tile_ladder.png", Tile.ColliderType.None, Vector2.zero),
                // 高さ 1/3 セル。上面をセル上端に揃える (+1/3)
                ("a1_conveyor", "tiles/tile_conveyor.png", Tile.ColliderType.None, new Vector2(0f, 1f / 3f)),
            };

            var made = new List<string>();
            foreach (var (name, spritePath, collider, offset) in specs)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + spritePath);
                if (sprite == null)
                {
                    made.Add(name + "(no sprite!)");
                    continue;
                }

                var path = $"{TileDir}/{name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                var isNew = tile == null;
                if (isNew)
                    tile = ScriptableObject.CreateInstance<Tile>();

                tile.sprite = sprite;
                tile.color = Color.white;
                tile.colliderType = collider;
                tile.transform = Matrix4x4.Translate(offset);
                tile.flags = offset == Vector2.zero ? TileFlags.LockColor : TileFlags.LockColor | TileFlags.LockTransform;

                if (isNew)
                    AssetDatabase.CreateAsset(tile, path);
                else
                    EditorUtility.SetDirty(tile);
                made.Add(name);
            }

            AssetDatabase.SaveAssets();
            return "[CreateTiles] " + string.Join(", ", made);
        }

        // ------------------------------------------------------------------ 岩の質感

        /// <summary>岩の準備だけを流す (切り抜き生成 → インポート → 質感 Tile → A1 用 RuleTile)。</summary>
        public static string PrepareRock(bool forceCutouts = false)
        {
            var log = new StringBuilder();
            log.AppendLine(MakeEdgeCutouts(forceCutouts));
            log.AppendLine(ImportArt());
            log.AppendLine(CreateRockTiles());
            log.AppendLine(BuildA1TerrainRule());
            return log.ToString();
        }

        /// <summary>
        /// 縁タイル (床/壁/天井/角/浮き足場) の複製を作り、内側のベタ塗り (プラム色 ≈ (80,0,54)) を透明にする。
        /// 画像の外周に接するプラム色の画素から 4 近傍で塗りつぶし (縁取り・レンガ・帯は外周のプラムと
        /// 繋がっていないので残る)。境界の 1px は色の近さで半透明にして下の岩肌となじませる。
        /// 生成物は rock/edges/ (無ければ作る。force で作り直し)。
        /// </summary>
        public static string MakeEdgeCutouts(bool force = false)
        {
            EnsureFolder(RockEdgeDir);
            var made = 0;
            foreach (var (src, dst) in EdgeSprites)
            {
                var srcPath = $"{TileFloorRoot}/{src}";
                var dstPath = $"{RockEdgeDir}/{dst}";
                if (!force && File.Exists(dstPath))
                    continue;
                if (!File.Exists(srcPath))
                    return "[EdgeCutouts] source missing: " + srcPath;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(srcPath));
                var px = tex.GetPixels32();
                int w = tex.width, h = tex.height;
                const int tol = 12, soft = 40;
                int Dist(Color32 c) => Mathf.Max(Mathf.Abs(c.r - 80), Mathf.Max(c.g, Mathf.Abs(c.b - 54)));
                bool Plum(int i) => px[i].a > 250 && Dist(px[i]) <= tol;

                var cut = new bool[px.Length];
                var q = new Queue<int>();
                void Seed(int i)
                {
                    if (!cut[i] && Plum(i)) { cut[i] = true; q.Enqueue(i); }
                }
                if (dst.StartsWith("edge_floor_07"))
                {
                    // 浮き足場は下面にも暗いレンガ帯がある (プラムに近い色で外周に接する)。
                    // 内側の帯 (上から 42〜75%) の左右の縁からだけ塗りつぶす
                    for (var y = (int)(h * 0.3f); y < (int)(h * 0.55f); y++) { Seed(y * w); Seed(y * w + w - 1); }
                }
                else
                {
                    for (var x = 0; x < w; x++) { Seed(x); Seed((h - 1) * w + x); }
                    for (var y = 0; y < h; y++) { Seed(y * w); Seed(y * w + w - 1); }
                }
                while (q.Count > 0)
                {
                    var i = q.Dequeue();
                    int x = i % w, y = i / w;
                    if (x > 0) Seed(i - 1);
                    if (x < w - 1) Seed(i + 1);
                    if (y > 0) Seed(i - w);
                    if (y < h - 1) Seed(i + w);
                }

                var outPx = (Color32[])px.Clone();
                for (var i = 0; i < px.Length; i++)
                {
                    if (cut[i])
                    {
                        outPx[i] = new Color32(80, 0, 54, 0);
                        continue;
                    }
                    int x = i % w, y = i / w;
                    var nearCut = (x > 0 && cut[i - 1]) || (x < w - 1 && cut[i + 1]) || (y > 0 && cut[i - w]) || (y < h - 1 && cut[i + w]);
                    var d = Dist(px[i]);
                    if (nearCut && d < soft)
                        outPx[i].a = (byte)(px[i].a * Mathf.Clamp01((float)(d - tol) / (soft - tol)));
                }
                tex.SetPixels32(outPx);
                tex.Apply();
                File.WriteAllBytes(dstPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                made++;
            }
            if (made > 0)
                AssetDatabase.Refresh();
            return $"[EdgeCutouts] generated {made}/{EdgeSprites.Length}";
        }

        /// <summary>岩肌の Tile 16 枚 (a1_rock_tex_r{row}_c{col}, 当たり無し) と外周用の透明な当たりタイル (a1_rock_seal)。</summary>
        public static string CreateRockTiles()
        {
            EnsureFolder(TileDir);
            var made = 0;
            Tile Upsert(string name)
            {
                var path = $"{TileDir}/{name}.asset";
                var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (t == null)
                {
                    t = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(t, path);
                }
                made++;
                return t;
            }

            for (var r = 0; r < RockTexPeriod; r++)
            for (var c = 0; c < RockTexPeriod; c++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{RockDir}/rock_tex_r{r}_c{c}.png");
                if (sprite == null)
                    return $"[RockTiles] sprite missing rock_tex_r{r}_c{c} (ImportArt 済み?)";
                var t = Upsert(RockTexTileName(r, c));
                t.sprite = sprite;
                t.color = Color.white;
                t.colliderType = Tile.ColliderType.None;
                t.transform = Matrix4x4.identity;
                t.flags = TileFlags.None; // 色はセルごとに変える (部屋から奥ほど暗く: Area1StageBuilder.PaintRock)
                EditorUtility.SetDirty(t);
            }

            // 岩の外周 (SealRing) 用: 絵は無し (下の岩肌が見える)、当たりは Grid
            var seal = Upsert(RockSealTileName);
            seal.sprite = null;
            seal.color = Color.white;
            seal.colliderType = Tile.ColliderType.Grid;
            seal.transform = Matrix4x4.identity;
            seal.flags = TileFlags.LockColor;
            EditorUtility.SetDirty(seal);

            AssetDatabase.SaveAssets();
            return $"[RockTiles] {made} tiles";
        }

        /// <summary>
        /// エリア1 専用の地形 RuleTile を共有の StageTerrainRule から作り直す (規則・当たりは同じ)。
        /// 既定出力 (内部) は絵なし = 下の Backfill の岩肌が見える。縁の出力は切り抜き版に差し替え。
        /// 共有ルールが更新されたらこれを流し直せば追従する。
        /// </summary>
        public static string BuildA1TerrainRule()
        {
            var shared = AssetDatabase.LoadAssetAtPath<RuleTile>(SharedTerrainRulePath);
            if (shared == null)
                return "[A1TerrainRule] shared rule missing";
            var rule = AssetDatabase.LoadAssetAtPath<RuleTile>(A1TerrainRulePath);
            if (rule == null)
            {
                AssetDatabase.CopyAsset(SharedTerrainRulePath, A1TerrainRulePath);
                rule = AssetDatabase.LoadAssetAtPath<RuleTile>(A1TerrainRulePath);
            }
            else
            {
                EditorUtility.CopySerialized(shared, rule);
            }
            rule.name = Path.GetFileNameWithoutExtension(A1TerrainRulePath);

            var map = new Dictionary<string, Sprite>();
            foreach (var (src, dst) in EdgeSprites)
            {
                var cutSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{RockEdgeDir}/{dst}");
                if (cutSprite == null)
                    return $"[A1TerrainRule] cutout sprite missing {dst} (MakeEdgeCutouts → ImportArt 済み?)";
                map[$"{TileFloorRoot}/{src}"] = cutSprite;
            }

            rule.m_DefaultSprite = null;
            int replaced = 0, kept = 0;
            foreach (var tr in rule.m_TilingRules)
                for (var i = 0; i < tr.m_Sprites.Length; i++)
                {
                    var p = tr.m_Sprites[i] != null ? AssetDatabase.GetAssetPath(tr.m_Sprites[i]) : null;
                    if (p != null && map.TryGetValue(p, out var s)) { tr.m_Sprites[i] = s; replaced++; }
                    else kept++;
                }
            EditorUtility.SetDirty(rule);
            AssetDatabase.SaveAssets();
            return $"[A1TerrainRule] {A1TerrainRulePath} rules={rule.m_TilingRules.Count} replaced={replaced} kept={kept}";
        }

        // ------------------------------------------------------------------ 効果素材 (手続き生成: 光・水・陰)

        public const string FxDir = ArtRoot + "/fx";

        /// <summary>
        /// 地下 (D 区画) で使う手続き生成の素材を Area1Gen/fx/ に作る (何度流してもよい。force で作り直す)。
        /// - glow.png (256²): 白い放射状のにじみ。灯の光暈・蒸気の予兆に色を乗せて使う
        /// - water.png (546², 1 セル): 縦横に継ぎ目なく繋がる半透明の水 (ゆるい縞の揺らぎ)
        /// - water_surface.png (546×91, 1×1/6 セル): 横に繋がる波打つ水面の線
        /// - shade.png (32×546): 下ほど濃い黒の縦グラデーション (暗がりの帯)
        /// 白〜淡色で作ってあるので SpriteRenderer.color で着色する。インポートは ImportArt (fx/manifest.json のピボット)。
        /// </summary>
        public static string PrepareFx(bool force = false)
        {
            EnsureFolder(FxDir);
            var made = new List<string>();
            void Write(string file, int w, int h, System.Func<int, int, Color> f)
            {
                var path = $"{FxDir}/{file}";
                if (!force && File.Exists(path))
                    return;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color[w * h];
                for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    px[y * w + x] = f(x, y);
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                made.Add(file);
            }

            const float Tau = Mathf.PI * 2f;
            Write("glow.png", 256, 256, (x, y) =>
            {
                var dx = (x + 0.5f) / 128f - 1f;
                var dy = (y + 0.5f) / 128f - 1f;
                var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            });
            Write("water.png", 546, 546, (x, y) =>
            {
                var u = x / 546f;
                var v = y / 546f;
                // 継ぎ目なし (周期が画像の幅の約数) のゆるい縞
                var w1 = Mathf.Sin(Tau * (u * 2f + v));
                var w2 = Mathf.Sin(Tau * (u * 3f - v * 2f) + 1.3f);
                var w3 = Mathf.Sin(Tau * (v * 4f + Mathf.Sin(Tau * u * 2f) * 0.15f));
                var k = (w1 * 0.5f + w2 * 0.35f + w3 * 0.25f) * 0.5f + 0.5f;
                var bright = Mathf.Lerp(0.82f, 1f, k);
                var line = Mathf.SmoothStep(0.86f, 1f, k);
                return Color.Lerp(new Color(bright, bright, bright, 0.78f), new Color(1f, 1f, 1f, 0.95f), line * 0.6f);
            });
            Write("water_surface.png", 546, 91, (x, y) =>
            {
                var u = x / 546f;
                var waveY = 58f + 9f * Mathf.Sin(Tau * u * 2f) + 4f * Mathf.Sin(Tau * u * 5f + 0.7f);
                var d = y - waveY;  // + = 線より上
                if (d > 4f)
                    return new Color(1f, 1f, 1f, 0f);
                if (d > -4f)
                    return new Color(1f, 1f, 1f, 0.95f);                                       // 光る水面の線
                return new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 0f, Mathf.Clamp01((-d - 4f) / 40f))); // 線の下の明るみ
            });
            Write("shade.png", 32, 546, (x, y) =>
            {
                var t = 1f - y / 545f;   // 下 (y=0) が濃い
                return new Color(0f, 0f, 0f, t * t * (3f - 2f * t));
            });

            var manifest = FxDir + "/manifest.json";
            if (force || !File.Exists(manifest))
            {
                File.WriteAllText(manifest,
                    "[{\"file\":\"glow.png\",\"pivot\":\"center\"},{\"file\":\"water.png\",\"pivot\":\"center\"}," +
                    "{\"file\":\"water_surface.png\",\"pivot\":\"center\"},{\"file\":\"shade.png\",\"pivot\":\"center\"}]",
                    new UTF8Encoding(false));
                made.Add("manifest.json");
            }
            if (made.Count > 0)
                AssetDatabase.Refresh();
            return $"[Fx] made={string.Join(",", made)} " + ImportArt();
        }

        // ------------------------------------------------------------------ 鉄板の壁 (観覧車の内部)

        public const string PlateDir = ArtRoot + "/ferris/plate";
        public const int PlateTexPeriod = 2;
        public static string PlateTexTileName(int row, int col) => $"a1_plate_tex_r{row}_c{col}";

        /// <summary>
        /// 観覧車の支柱・機械室の「鉄板の壁」の質感 (岩肌の代わり。Area1StageBuilder.PlateWalls で範囲指定)。
        /// ferris/machine_room_wall.png (2x2 セル, 縦横に継ぎ目なし) を明るく持ち上げて 1 セルずつ切り出し
        /// (ferris/plate/plate_tex_r{row}_c{col}.png)、当たり無しの Tile (a1_plate_tex_r{row}_c{col}) にする。
        /// 何度流してもよい (force で画像を作り直す)。
        /// </summary>
        public static string PreparePlate(bool force = false)
        {
            EnsureFolder(PlateDir);
            var src = ArtRoot + "/ferris/machine_room_wall.png";
            var made = 0;
            if (force || !File.Exists($"{PlateDir}/plate_tex_r0_c0.png"))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(src));
                var cell = tex.width / PlateTexPeriod;
                // 背景板用の暗い絵を壁として読める明るさに (少し赤紫に寄せて支柱の色となじませる)
                var gain = new Color(2.5f, 2.15f, 2.55f, 1f);
                for (var r = 0; r < PlateTexPeriod; r++)
                for (var c = 0; c < PlateTexPeriod; c++)
                {
                    // 行 r は上から数える (PaintRock と同じ: セル y が上がるほど行が減る)
                    var px = tex.GetPixels(c * cell, tex.height - (r + 1) * cell, cell, cell);
                    for (var i = 0; i < px.Length; i++)
                    {
                        var p = px[i];
                        px[i] = new Color(Mathf.Clamp01(p.r * gain.r), Mathf.Clamp01(p.g * gain.g), Mathf.Clamp01(p.b * gain.b), 1f);
                    }
                    var o = new Texture2D(cell, cell, TextureFormat.RGBA32, false);
                    o.SetPixels(px);
                    o.Apply();
                    File.WriteAllBytes($"{PlateDir}/plate_tex_r{r}_c{c}.png", o.EncodeToPNG());
                    Object.DestroyImmediate(o);
                    made++;
                }
                Object.DestroyImmediate(tex);
                AssetDatabase.Refresh();
            }

            var tiles = 0;
            for (var r = 0; r < PlateTexPeriod; r++)
            for (var c = 0; c < PlateTexPeriod; c++)
            {
                var path = $"{PlateDir}/plate_tex_r{r}_c{c}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                    return "[Plate] importer missing " + path;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
                    || importer.wrapMode != TextureWrapMode.Clamp || importer.mipmapEnabled || settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = PixelsPerUnit;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var tpath = $"{TileDir}/{PlateTexTileName(r, c)}.asset";
                var t = AssetDatabase.LoadAssetAtPath<Tile>(tpath);
                if (t == null)
                {
                    t = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(t, tpath);
                }
                t.sprite = sprite;
                t.color = Color.white;
                t.colliderType = Tile.ColliderType.None;
                t.transform = Matrix4x4.identity;
                t.flags = TileFlags.None;
                EditorUtility.SetDirty(t);
                tiles++;
            }
            AssetDatabase.SaveAssets();
            return $"[Plate] images={made} tiles={tiles}";
        }

        // ------------------------------------------------------------------ 大観覧車プレハブ

        private static Sprite Ferris(string file) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/ferris/{file}");

        /// <summary>
        /// 大観覧車 (背景の飾り、コライダー無し) を部品から組み立ててプレハブにする。
        /// ルート = 中心 (54,252)。外周半径 90、部品は 2.5 倍で太く見せる。脚は地面 y=0 まで。
        /// どのシーンでもルートをワールド (54,252) に置けば同じ部分が見える。
        /// </summary>
        public static string BuildFerrisWheelPrefab(bool force = false)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(FerrisPrefabPath) != null)
                return "[Ferris] exists";

            EnsureFolder(PrefabDir);
            const float k = 2.5f; // 部品の拡大率
            var tint = new Color(0.78f, 0.74f, 0.92f, 1f); // 夜景に沈める

            var root = new GameObject("A1_FerrisWheel");
            root.transform.position = FerrisCenter;

            SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector2 localPos, float angleDeg,
                Vector2 scale, int order, Vector2? tiledSize = null, Color? color = null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(0, 0, angleDeg);
                go.transform.localScale = new Vector3(scale.x, scale.y, 1);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                sr.color = color ?? tint;
                if (tiledSize.HasValue)
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = tiledSize.Value;
                }
                return sr;
            }

            // --- 脚 (A フレーム) : 地面 y=0 から中心へ
            var legs = new GameObject("Legs").transform;
            legs.SetParent(root.transform, false);
            const float halfBase = 62f;
            var height = FerrisCenter.y; // 中心から地面まで
            var legLen = Mathf.Sqrt(height * height + halfBase * halfBase);
            var lean = Mathf.Atan2(halfBase, height) * Mathf.Rad2Deg;
            var leg = Ferris("support_leg.png");
            foreach (var side in new[] { -1, 1 })
            {
                // 脚の中点 = 中心と足元の中間
                var foot = new Vector2(side * halfBase, -height);
                Part(side < 0 ? "LegL" : "LegR", legs, leg, foot * 0.5f, side * lean, new Vector2(k * 1.6f, k), -58,
                    new Vector2(2f, legLen / k));
            }

            // 筋交い: 中心から 35% / 60% / 85% 下
            var brace = Ferris("leg_crossbrace.png");
            foreach (var t in new[] { 0.35f, 0.6f, 0.85f })
            {
                var y = -height * t;
                var halfW = halfBase * t;
                Part($"Brace{Mathf.RoundToInt(t * 100)}", legs, brace, new Vector2(0, y), 0, new Vector2(k, k), -57,
                    new Vector2(halfW * 2f / k, 1f));
            }

            // --- スポーク 16 本 (ハブの下から外周の内側まで)
            var spokes = new GameObject("Spokes").transform;
            spokes.SetParent(root.transform, false);
            var spoke = Ferris("spoke_beam.png");
            const int spokeCount = 16;
            for (var i = 0; i < spokeCount; i++)
            {
                var a = i * 360f / spokeCount;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                var len = FerrisRadius - 1f;
                Part($"Spoke{i:00}", spokes, spoke, dir * (len * 0.5f), a, new Vector2(k, k), -56,
                    new Vector2(len / k, 0.5f));
            }

            // --- 外周トラス: 2.5 倍の 6x1 セグメントを円周に並べる (弦長で個数を決め、x スケールで微調整)
            var rim = new GameObject("Rim").transform;
            rim.SetParent(root.transform, false);
            var truss = Ferris("rim_truss.png");
            var segLen = 6f * k;
            var n = Mathf.RoundToInt(2f * Mathf.PI * FerrisRadius / segLen);
            var chord = 2f * FerrisRadius * Mathf.Sin(Mathf.PI / n);
            for (var i = 0; i < n; i++)
            {
                var a = (i + 0.5f) * 360f / n;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                // 弦の中点は半径 R*cos(pi/n)。電球側 (絵の上側) が外を向くよう angle-90
                var mid = dir * FerrisRadius * Mathf.Cos(Mathf.PI / n);
                Part($"Truss{i:00}", rim, truss, mid, a - 90f, new Vector2(chord / 6f * 1.01f, k), -55);
            }

            // --- 電球 (外周の外側に等間隔)
            var bulbs = new GameObject("Bulbs").transform;
            bulbs.SetParent(root.transform, false);
            var bulb = Ferris("rim_bulb.png");
            var bulbCount = n * 2;
            for (var i = 0; i < bulbCount; i++)
            {
                var a = i * 360f / bulbCount;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Part($"Bulb{i:00}", bulbs, bulb, dir * (FerrisRadius + 1.6f * k * 0.5f), 0, Vector2.one * 1.6f, -54,
                    null, Color.white);
            }

            // --- ハブ (スポークの付け根を覆う)
            Part("Hub", root.transform, Ferris("hub_axle.png"), Vector2.zero, 0, new Vector2(k, k), -53);

            // --- 飾りのゴンドラ (外周 16 箇所に吊り下げ。ゲーム上の足場ではない。必要なシーンでは無効化する)
            var decor = new GameObject("DecorGondolas").transform;
            decor.SetParent(root.transform, false);
            var hanger = Ferris("gondola_hanger.png");
            var gondola = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/platforms/gondola_platform.png");
            for (var i = 0; i < 16; i++)
            {
                var a = (i + 0.5f) * 360f / 16;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                var p = dir * FerrisRadius;
                var g = new GameObject($"Gondola{i:00}").transform;
                g.SetParent(decor, false);
                g.localPosition = p;
                Part("Hanger", g, hanger, Vector2.zero, 0, Vector2.one * 1.5f, -52);
                Part("Cabin", g, gondola, new Vector2(0, -4.5f), 0, Vector2.one * 1.5f, -51);
            }

            // --- 乗り場 (地面の足元。不要なシーンでは無効化)
            var station = Part("BaseStation", root.transform, Ferris("wheel_base_station.png"),
                new Vector2(0, -height), 0, new Vector2(k, k), -50);
            station.gameObject.SetActive(true);

            PrefabUtility.SaveAsPrefabAsset(root, FerrisPrefabPath);
            Object.DestroyImmediate(root);
            return $"[Ferris] built {FerrisPrefabPath} (rim segments {n}, chord {chord:F2})";
        }

        // ------------------------------------------------------------------ polish B のマテリアル

        /// <summary>
        /// polish B: 部品のインポート (polish/manifest.json のピボット、tileable は Repeat) + A1SpriteScroll のマテリアルを作る/更新する。
        /// 何度流してもよい。マテリアル名 → (UV スクロール/秒, 加算か):
        /// A1_Additive (光暈・しぶき) / A1_WaterJet (上へ噴く水, 加算) / A1_SteamRise (口から離れる向きへ流れる蒸気。
        /// 下向きの口は親を上下反転するので同じマテリアルで向きが合う) / A1_SurfaceDrift (水面の帯を横へ) / A1_WaterBody / A1_OilBody (水・油のゆっくりした揺らぎ)。
        /// </summary>
        public static string PreparePolish()
        {
            var log = ImportArt();
            EnsureFolder(PolishMatDir);
            var shader = Shader.Find(ScrollShaderName);
            if (shader == null)
                return log + " [Polish] shader not found " + ScrollShaderName;
            var specs = new (string name, Vector2 scroll, bool additive)[]
            {
                ("A1_Additive", Vector2.zero, true),
                ("A1_WaterJet", new Vector2(0f, -1.3f), true),
                ("A1_SteamRise", new Vector2(0f, -0.45f), false),
                ("A1_SurfaceDrift", new Vector2(0.045f, 0f), false),
                ("A1_WaterBody", new Vector2(0.018f, 0.008f), false),
                ("A1_OilBody", new Vector2(0.006f, 0.003f), false),
            };
            var made = new List<string>();
            foreach (var (name, scroll, additive) in specs)
            {
                var path = $"{PolishMatDir}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, path);
                    made.Add(name);
                }
                mat.shader = shader;
                mat.SetVector("_Scroll", new Vector4(scroll.x, scroll.y, 0, 0));
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            return log + $" [Polish] mats={specs.Length} new={string.Join(",", made)}";
        }

        public static Material PolishMat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{PolishMatDir}/{name}.mat");

        // ------------------------------------------------------------------ util

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
