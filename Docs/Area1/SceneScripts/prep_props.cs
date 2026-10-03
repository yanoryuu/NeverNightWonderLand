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
}
