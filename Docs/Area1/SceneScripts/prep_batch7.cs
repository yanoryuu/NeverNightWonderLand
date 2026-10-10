using TMPro;
using UnityEditor;
using UnityEngine;

// 2026-10-08 PendingChanges 48 / 50 の準備。何度流してもよい。
// 1. 新しい絵のインポート設定 (Sprite / FullRect / ミップマップなし / 非圧縮 / Bilinear)
//    - UI/prompt/prompt_key_badge.png (インタラクトのボタン表示の枠, 128px, PPU 256)
//    - Area1Gen/tiles/tile_ladder_1cell.png (梯子 1 マス幅, 546px = 1 単位, 横木の間隔 1/3 マス。
//      元の tile_ladder.png の中身 x198..348 (150px 幅) を y5..149 (横木 3 本 = 3 周期) で切り出し、546x546 に Lanczos で拡大。
//      縦横比は 3.64 : 3.79 で ほぼそのまま。上下は継ぎ目なくつながる)
// 2. Player.prefab のルートに InteractButtonPrompt (枠・フォント = JapaneseUI SDF) を付ける
// unity command run_script --file Docs/Area1/SceneScripts/prep_batch7.cs --entry PrepBatch7.Run --timeout_ms 600000 --timeout 620
public static class PrepBatch7
{
    const string Badge = "Assets/LocalData/UI/prompt/prompt_key_badge.png";
    const string Ladder = "Assets/LocalData/StageAsset/Area1Gen/tiles/tile_ladder_1cell.png";
    const string Font = "Assets/Art/Fonts/JapaneseUI SDF.asset";
    const string PlayerPrefab = "Assets/Prefabs/Player.prefab";

    public static string Run()
    {
        AssetDatabase.Refresh();
        var log = Import(Badge, 256f) + "\n" + Import(Ladder, 546f) + "\n";

        var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        try
        {
            var prompt = root.GetComponent<InteractButtonPrompt>() ?? root.AddComponent<InteractButtonPrompt>();
            var so = new SerializedObject(prompt);
            so.FindProperty("_badge").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Badge);
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            log += $"Player.prefab: InteractButtonPrompt badge={so.FindProperty("_badge").objectReferenceValue} font={so.FindProperty("_font").objectReferenceValue}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return log;
    }

    static string Import(string path, float ppu)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) return "missing " + path;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return $"{path}: {(sp != null ? sp.bounds.size.ToString("F3") : "no sprite")}";
    }
}
