using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 破片の素材 (2026-10-04 PendingChanges 36) の準備。何度流してもよい。
// 1. Area1Gen/debris/*.png のインポート設定 (Area1Gen と同じ: Sprite / PPU 546 / 中心ピボット / FullRect / ミップマップなし / 非圧縮 / Bilinear)
//    絵は gpt-image-2 でグリーンバックに描かせ、SceneScripts/chroma_slice.py でクロマキー → 部品ごとに切り出した物
// 2. Assets/Resources/BreakDebrisCatalog.asset (BreakDebris が実行時に読む) に絵を登録
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/prep_debris.cs --entry PrepDebris.Run
public static class PrepDebris
{
    const string DebrisDir = "Assets/LocalData/StageAsset/Area1Gen/debris";
    const string CatalogPath = "Assets/Resources/BreakDebrisCatalog.asset";

    public static string Run()
    {
        AssetDatabase.Refresh();
        var log = new System.Text.StringBuilder();

        foreach (var path in Directory.GetFiles(DebrisDir, "*.png"))
        {
            var p = path.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(p);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 546;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            log.AppendLine("sprite " + p);
        }

        Sprite[] Load(string prefix) => Directory.GetFiles(DebrisDir, prefix + "_*.png").OrderBy(f => f)
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'))).Where(s => s != null).ToArray();

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        var catalog = AssetDatabase.LoadAssetAtPath<BreakDebrisCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BreakDebrisCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.Wood = Load("debris_wood");
        catalog.Rock = Load("debris_rock");
        catalog.Slam = Load("debris_slam");
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        log.AppendLine($"catalog wood={catalog.Wood.Length} rock={catalog.Rock.Length} slam={catalog.Slam.Length}");

        // 落下物 (鉄のおもり) は差し替えた PNG を読み直す (インポート設定は .meta のまま)
        AssetDatabase.ImportAsset("Assets/LocalData/StageAsset/Area1Gen/props/falling_weight.png", ImportAssetOptions.ForceUpdate);
        var w = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/LocalData/StageAsset/Area1Gen/props/falling_weight.png");
        log.AppendLine($"falling_weight rect={w.rect} ppu={w.pixelsPerUnit} pivot={w.pivot}");
        return log.ToString();
    }
}
