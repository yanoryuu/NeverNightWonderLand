using System.Linq;
using UnityEditor;
using UnityEngine;

// UI 素材 (Assets/LocalData/UI/**/*.png) のインポート設定。何度流してもよい。
// 完成形 (ui_アートボード* / 装備_* / shiryou_detail_shiryou_detail) は参照用なので触らない。
// _gen/ は gen_paper.py が作った紙目・暗さ入りの紙と背景 (先に py -3 Docs/UI/Build/gen_paper.py)。
// unity command run_script --file Docs/UI/Build/import_ui.cs --entry ImportUI.Run
public static class ImportUI
{
    public static string Run()
    {
        AssetDatabase.Refresh();
        var log = new System.Text.StringBuilder();
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/LocalData/UI" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png"))
            .ToArray();

        foreach (var path in paths)
        {
            var file = System.IO.Path.GetFileName(path);
            if (file.StartsWith("ui_") || file.StartsWith("装備_") || file == "shiryou_detail_shiryou_detail.png")
            {
                log.AppendLine("skip (mockup) " + path);
                continue;
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out var w, out var h);
            var maxSize = 32;
            while (maxSize < Mathf.Max(w, h) && maxSize < 8192)
                maxSize *= 2;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; // PC: BC7 (UI の縁にブロックノイズが出にくい)
            importer.crunchedCompression = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // UI Image なので矩形メッシュ
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            // 手続きで作った角丸 (資料のスクロールのつまみ・溝) は縦に伸ばすので 9 スライス
            importer.spriteBorder = file == "pill.png" ? new Vector4(0, 6, 0, 6) : Vector4.zero;

            importer.SaveAndReimport();
            log.AppendLine($"sprite {path} {w}x{h} max={maxSize}");
        }

        return log.ToString();
    }
}
