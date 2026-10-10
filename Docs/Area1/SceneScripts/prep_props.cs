using System.IO;
using UnityEditor;
using UnityEngine;

// プロップス素材 (2026-10-03 PendingChanges 14〜16) の準備。何度流してもよい。
// 1. Assets/LocalData/StageAsset/Props/*.png のインポート設定 (Sprite / 中心ピボット / PPU = 幅 → 1 単位 / ミップマップなし / 最大 512)
//    元画像 (Drive「プロップス素材」を 1024 の正方形に切り抜いた物): coin / thread_spool / mishin_needle / machi_needle / cloth_cutter
//    小さい粒用の縮小版 (128px): thread_piece / coin_piece (py で Lanczos 縮小。ミップマップなしで小さく描くとざらつくため)
// 2. 散らばる糸の粒 ThreadPiece.prefab / 箱のコイン CoinPiece.prefab (Rigidbody2D + 円の当たり + 跳ねる物理マテリアル)
// 3. Player.prefab に ThreadDropSpawner (粒のプレハブ) を付ける
// unity command run_script --file Docs/Area1/SceneScripts/prep_props.cs --entry PrepProps.Run
public static class PrepProps
{
    const string PropsDir = "Assets/LocalData/StageAsset/Props";
    const string ThreadPiecePath = "Assets/Prefabs/ThreadPiece.prefab";
    const string CoinPiecePath = "Assets/Prefabs/CoinPiece.prefab";
    const string MaterialPath = "Assets/Prefabs/LootBounce.physicsMaterial2D";
    const string PlayerPath = "Assets/Prefabs/Player.prefab";

    public static string Run()
    {
        AssetDatabase.Refresh();
        var log = new System.Text.StringBuilder();

        foreach (var path in Directory.GetFiles(PropsDir, "*.png"))
        {
            var p = path.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(p);
            importer.GetSourceTextureWidthAndHeight(out var w, out var h);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = w;          // 正方形 1 枚 = 1 単位 (大きさは transform の scale で決める)
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Min(512, Mathf.NextPowerOfTwo(Mathf.Max(w, h)));
            importer.textureCompression = TextureImporterCompression.Uncompressed; // Area1Gen と同じ
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            log.AppendLine($"sprite {p} {w}x{h} ppu={w} max={importer.maxTextureSize}");
        }

        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("LootBounce");
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        mat.bounciness = 0.35f;
        mat.friction = 0.8f;
        EditorUtility.SetDirty(mat);

        var thread = MakePiece<ThreadPiece>(ThreadPiecePath, "ThreadPiece", PropsDir + "/thread_piece.png", 0.4f, mat);
        var coin = MakePiece<CoinPiece>(CoinPiecePath, "CoinPiece", PropsDir + "/coin_piece.png", 0.42f, mat);
        log.AppendLine("prefabs: " + ThreadPiecePath + ", " + CoinPiecePath);

        // コインは 1 枚で目立つように少し大きく (0.55 単位)
        var coinRoot = PrefabUtility.LoadPrefabContents(CoinPiecePath);
        coinRoot.transform.localScale = Vector3.one * 0.55f;
        PrefabUtility.SaveAsPrefabAsset(coinRoot, CoinPiecePath);
        PrefabUtility.UnloadPrefabContents(coinRoot);

        var player = PrefabUtility.LoadPrefabContents(PlayerPath);
        var spawner = player.GetComponent<ThreadDropSpawner>() ?? player.AddComponent<ThreadDropSpawner>();
        var so = new SerializedObject(spawner);
        so.FindProperty("_piecePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ThreadPiece>(ThreadPiecePath);
        so.FindProperty("_pieceScale").floatValue = 0.38f; // 値 1 の粒 = 0.38 単位 (画面で約 37px)
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
        PrefabUtility.UnloadPrefabContents(player);
        log.AppendLine("Player.prefab: ThreadDropSpawner -> ThreadPiece.prefab");

        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    /// <summary>粒のプレハブ: SpriteRenderer + Rigidbody2D (動的) + CircleCollider2D (半径 = 絵の radius 倍) + T。</summary>
    static GameObject MakePiece<T>(string path, string name, string sprite, float radius, PhysicsMaterial2D mat) where T : LootPiece
    {
        var go = new GameObject(name) { layer = 2 }; // Ignore Raycast: 接地判定・攻撃判定に掛からない
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sprite) ?? throw new System.Exception("missing " + sprite);
        sr.sortingOrder = 4; // ギミック (3) の手前
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.6f;
        rb.linearDamping = 0.2f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 転がり続けないように (向きは Launch でばらす)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = mat;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.sharedMaterial = mat;
        go.AddComponent<T>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>
    /// 投げる針の弾 (MachiNeedle.prefab / MishinNeedle.prefab) の絵を新しい素材に差し替える (2026-10-03 PendingChanges 23)。何度流してもよい。
    /// unity command run_script --file Docs/Area1/SceneScripts/prep_props.cs --entry PrepProps.Needles
    /// ルート = 当たり (BoxCollider2D, 大きさは以前の見た目の矩形と同じ) と向き (Projectile が transform.right を速度に向ける) だけで scale 1。
    /// 絵は子 "Art" (SpriteRenderer): 素材は針先が左下を向いた斜めの絵なので、針先が +X を向くように回し、
    /// 針の軸 (針先〜頭の端) の中点がルートの原点に来るように置く (刺さると半分が壁に入り、頭が外に出る)。
    /// 角度・位置は素材の不透明部分から求めた値 (針先と軸の直線の当てはめ, py)。
    /// </summary>
    public static string Needles()
    {
        var log = new System.Text.StringBuilder();
        // (プレハブ, 絵, 当たりの大きさ, 絵の回転, 絵の scale (= 軸の長さ machi 0.8 / mishin 0.85 単位。ミシン針は線が細いので少し大きく), 絵の位置)
        log.AppendLine(Needle("Assets/Prefabs/MachiNeedle.prefab", PropsDir + "/machi_needle.png",
            new Vector2(0.6f, 0.1f), 126.34f, 0.7087f, new Vector2(-0.0214f, 0.0120f)));
        log.AppendLine(Needle("Assets/Prefabs/MishinNeedle.prefab", PropsDir + "/mishin_needle.png",
            new Vector2(0.4f, 0.14f), 119.72f, 0.7321f, new Vector2(-0.0161f, 0.0691f)));
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    static string Needle(string prefabPath, string spritePath, Vector2 colliderSize, float artRotation, float artScale, Vector2 artPos)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) ?? throw new System.Exception("missing " + spritePath);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var rootSr = root.GetComponent<SpriteRenderer>();
        var material = rootSr != null ? rootSr.sharedMaterial : null;
        var order = rootSr != null ? rootSr.sortingOrder : 5;
        if (rootSr != null)
            Object.DestroyImmediate(rootSr);
        root.transform.localScale = Vector3.one;
        var col = root.GetComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.offset = Vector2.zero;

        var art = root.transform.Find("Art");
        if (art == null)
        {
            art = new GameObject("Art").transform;
            art.SetParent(root.transform, false);
        }
        art.gameObject.layer = root.layer;
        art.localPosition = artPos;
        art.localRotation = Quaternion.Euler(0, 0, artRotation);
        art.localScale = Vector3.one * artScale;
        var sr = art.GetComponent<SpriteRenderer>();
        if (sr == null) sr = art.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        if (material != null)
            sr.sharedMaterial = material;
        sr.sortingOrder = order;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return $"{prefabPath}: Art = {sprite.name} rot {artRotation} scale {artScale}, collider {colliderSize}";
    }
}
