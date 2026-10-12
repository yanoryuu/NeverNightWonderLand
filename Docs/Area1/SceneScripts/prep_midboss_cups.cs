using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 中ボス「カップ」(Docs/EnemySpec.md「上の中ボス「カップ」」, A1_MB1) の絵の取り込み・定数アセット・プレハブを作る/更新する (何度流してもよい)。
//   絵:       Assets/LocalData/Character/cup_1〜3.png (小さなカップ 黄 / ピンク / 水色) と cups.png (合体後 = 3 つが縦に積み重なった絵)
//             単体の Sprite・中央 pivot。小さなカップは幅 約 1.5 units、合体後は高さ 約 2.4 units になる PPU で取り込む。
//   定数:     Assets/Prefabs/Enemies/EnemyConsts_Cup.asset (HP 40 / 防御値 20 / 接触 1)
//             Assets/Prefabs/Enemies/EnemyConsts_CupMerged.asset (HP 100 / 防御値 15 / 接触 1 / ブレイク 3 秒)
//   プレハブ: Assets/Prefabs/Enemies/Boss_Cups.prefab
//     Boss_Cups (ルート = 合体後のカップ: SpriteRenderer cups / Rigidbody2D 重力 0 / BoxCollider2D / EnemyController (CupMerged, スーパーアーマー)
//                / CupBossBehaviour / 子 StatusCanvas)
//       ├ Cup1 (黄)    SpriteRenderer cup_1 / Rigidbody2D / BoxCollider2D / EnemyController (Cup) / CupMinionBehaviour / 子 StatusCanvas
//       ├ Cup2 (ピンク) 〃 cup_2
//       └ Cup3 (水色)  〃 cup_3
//     ルートも子も FlyingEnemy.prefab (浮遊の雑魚: EnemyController・Rigidbody2D 重力 0・BoxCollider2D・頭上の HP 表示) を複製して作る
//     (プレハブの Variant/入れ子にはしない。うごき部品 (CompositeEnemyBehaviour・浮遊の移動) は外して、カップ専用のうごきを付ける)。
//   紅茶の滴 (CupDroplet) と予告の印は実行時に作るのでプレハブは無い。
// run_script --file Docs/Area1/SceneScripts/prep_midboss_cups.cs --entry PrepMidbossCups.Run
public static class PrepMidbossCups
{
    const string Dir = "Assets/Prefabs/Enemies";
    const string PrefabPath = Dir + "/Boss_Cups.prefab";
    const string BasePrefab = "Assets/Prefabs/FlyingEnemy.prefab";
    const string ArtDir = "Assets/LocalData/Character";

    static readonly StringBuilder Log = new();

    // 小さなカップ: 名前・絵・PPU (見えている幅 ≈ 1.5 units)・絵が左向きか・当たり判定 (大きさ, ずれ)・初期位置 (ルートから)・
    // 回る角度の初期値・揺れの位相。並び順 = 積み重ねの上から (cups.png: 黄 / ピンク / 水色)
    static readonly (string name, string art, float ppu, bool facesLeft, Vector2 box, Vector2 boxOffset, Vector2 local, float angle, float phase)[] Cups =
    {
        // cup_1 1836x1479: 注ぎ口 (こぼれる紅茶) が左・取っ手が右 → 左向き
        ("Cup1", "cup_1.png", 1224f, true, new Vector2(1.0f, 0.75f), new Vector2(0.05f, 0.05f), new Vector2(-3f, 0.5f), 150f, 0f),
        // cup_2 1793x1041: 取っ手が左・紅茶が右へこぼれる → 右向き
        ("Cup2", "cup_2.png", 1195f, false, new Vector2(1.0f, 0.7f), new Vector2(0f, 0f), new Vector2(0f, 1.5f), 90f, 2.1f),
        // cup_3 1635x974: 紅茶が左へこぼれる・取っ手が右 → 左向き
        ("Cup3", "cup_3.png", 1090f, true, new Vector2(1.0f, 0.72f), new Vector2(0f, 0f), new Vector2(3f, 0.5f), 30f, 4.2f),
    };

    // 合体後 cups.png 2220x2718 → 高さ 2.4 units。3 つが積み重なった絵で、上の黄色が左へ注いでいる → 左向き
    const float MergedPpu = 1132f;

    public static string Run()
    {
        Log.Clear();
        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        //                       MaxHp Guard Move Chase Range Contact
        var cupConsts = Consts("Cup", 40, 20, 2f, 3.2f, 0f, 1);
        var mergedConsts = Consts("CupMerged", 100, 15, 2f, 0f, 0f, 1, breakDuration: 3f);

        var mergedSprite = ImportSprite($"{ArtDir}/cups.png", MergedPpu);
        var cupSprites = new Sprite[Cups.Length];
        for (var i = 0; i < Cups.Length; i++)
            cupSprites[i] = ImportSprite($"{ArtDir}/{Cups[i].art}", Cups[i].ppu);
        if (mergedSprite == null || System.Array.IndexOf(cupSprites, null) >= 0)
        {
            Log.AppendLine("ERROR カップの絵 (cup_1〜3.png / cups.png) が取り込めない");
            return Log.ToString();
        }

        // 素材は敵の絵 (Enemy.prefab の Visual) と同じにする (ライティングの見え方を揃える)
        var enemyVisual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab")
            ?.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
        var material = enemyVisual != null ? enemyVisual.sharedMaterial : null;

        var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
        if (baseAsset == null)
        {
            Log.AppendLine("ERROR " + BasePrefab + " が無い");
            return Log.ToString();
        }

        var root = CloneBase(baseAsset, "Boss_Cups");
        try
        {
            // ---- ルート = 合体後のカップ (第1段階は CupBossBehaviour が眠らせる)
            var rootSr = root.GetComponent<SpriteRenderer>();
            SetupSprite(rootSr, mergedSprite, material);
            var rootBox = root.GetComponent<BoxCollider2D>();
            rootBox.size = new Vector2(1.3f, 2.2f);
            rootBox.offset = new Vector2(0.1f, -0.05f);
            var rootEc = root.GetComponent<EnemyController>();
            Set(rootEc, ("_consts", mergedConsts), ("_visual", rootSr), ("_visualFacesLeft", true),
                ("_hitFlashColor", Color.white), ("_superArmor", true));
            var rootCanvas = SetupCanvas(root, rootEc, new Vector2(0f, 1.6f));
            var boss = root.AddComponent<CupBossBehaviour>();

            // ---- 小さなカップ 3 体 (子)
            var minions = new List<Object>();
            for (var i = 0; i < Cups.Length; i++)
            {
                var c = Cups[i];
                var cup = CloneBase(baseAsset, c.name);
                cup.transform.SetParent(root.transform, false);
                cup.transform.localPosition = c.local;
                var sr = cup.GetComponent<SpriteRenderer>();
                SetupSprite(sr, cupSprites[i], material);
                sr.sortingOrder = rootSr.sortingOrder + 1 + i;
                var box = cup.GetComponent<BoxCollider2D>();
                box.size = c.box;
                box.offset = c.boxOffset;
                var ec = cup.GetComponent<EnemyController>();
                Set(ec, ("_consts", cupConsts), ("_visual", sr), ("_visualFacesLeft", c.facesLeft),
                    ("_hitFlashColor", Color.white), ("_superArmor", false));
                var canvas = SetupCanvas(cup, ec, new Vector2(0f, 0.85f));
                var minion = cup.AddComponent<CupMinionBehaviour>();
                Set(minion, ("_circleAngle", c.angle), ("_bobPhase", c.phase), ("_statusCanvas", canvas));
                minions.Add(minion);
            }

            Set(boss, ("_minions", minions.ToArray()), ("_statusCanvas", rootCanvas));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var ok);
            Log.AppendLine($"prefab {PrefabPath} ok={ok}");
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (saved != null)
            {
                foreach (Transform t in saved.transform)
                {
                    var comps = new StringBuilder();
                    foreach (var comp in t.GetComponents<Component>())
                        comps.Append(comp.GetType().Name).Append(' ');
                    Log.AppendLine($"  {saved.name}/{t.name}: {comps}");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        return Log.ToString();
    }

    /// <summary>
    /// FlyingEnemy.prefab を複製 (プレハブとのつながりなし) して、うごき部品 (EnemyController 以外の MonoBehaviour) を外す。
    /// </summary>
    static GameObject CloneBase(GameObject baseAsset, string name)
    {
        var go = Object.Instantiate(baseAsset);
        go.name = name;
        // 後ろの部品から外す (前の部品を RequireComponent しているものがあるため)
        var behaviours = go.GetComponents<MonoBehaviour>();
        for (var i = behaviours.Length - 1; i >= 0; i--)
        {
            if (behaviours[i] is EnemyController)
                continue;
            Object.DestroyImmediate(behaviours[i]);
        }
        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        return go;
    }

    static void SetupSprite(SpriteRenderer sr, Sprite sprite, Material material)
    {
        sr.enabled = true;
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.flipX = false;
        sr.drawMode = SpriteDrawMode.Simple;
        if (material != null)
            sr.sharedMaterial = material;
    }

    /// <summary>頭上の HP 表示 (FlyingEnemy の StatusCanvas) を絵の上へずらし、参照先の敵を明示する。</summary>
    static GameObject SetupCanvas(GameObject owner, EnemyController enemy, Vector2 localPosition)
    {
        var canvas = owner.transform.Find("StatusCanvas");
        if (canvas == null)
        {
            Log.AppendLine($"WARN {owner.name} に StatusCanvas が無い");
            return null;
        }
        canvas.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        var view = canvas.GetComponent<EnemyStatusGaugeView>();
        if (view != null)
            Set(view, ("_enemy", enemy));
        return canvas.gameObject;
    }

    /// <summary>絵を Sprite (単体・指定 PPU・中央 pivot) で取り込み直して返す (設定が同じなら何もしない)。</summary>
    static Sprite ImportSprite(string path, float ppu)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            // .meta がまだ無い (Unity が未取り込み) なら取り込ませてから
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        var pivot = new Vector2(0.5f, 0.5f);
        var changed = importer.textureType != TextureImporterType.Sprite
                      || importer.spriteImportMode != SpriteImportMode.Single
                      || !Mathf.Approximately(importer.spritePixelsPerUnit, ppu)
                      || settings.spriteAlignment != (int)SpriteAlignment.Center
                      || importer.maxTextureSize < 4096
                      || importer.mipmapEnabled;
        if (changed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = pivot;
            settings.spritePixelsPerUnit = ppu;
            settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            // 2000px 超えの絵なので、既定の最大サイズ (2048) で縮まないように
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            Log.AppendLine($"import {path}: PPU {ppu} pivot center");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static EnemyConsts Consts(string name, int hp, int guard, float move, float chase, float range, int contact, float breakDuration = -1f)
    {
        var path = $"{Dir}/EnemyConsts_{name}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<EnemyConsts>(path);
        if (asset == null)
        {
            AssetDatabase.CopyAsset("Assets/Scripts/Enemy/EnemyConsts.asset", path);
            asset = AssetDatabase.LoadAssetAtPath<EnemyConsts>(path);
        }

        Set(asset, ("_maxHp", hp), ("_maxGuard", guard), ("_moveSpeed", move), ("_chaseSpeed", chase),
            ("_chaseRange", range), ("_contactDamage", contact));
        if (breakDuration > 0f)
            Set(asset, ("_breakDuration", breakDuration));
        Log.AppendLine($"consts {path}: HP{hp} G{guard} move{move} chase{chase}/{range} contact{contact}");
        return asset;
    }

    static void Set(Object target, params (string field, object value)[] values)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in values)
        {
            var p = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name}.{field} が無い");
            switch (value)
            {
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case Vector2 v: p.vector2Value = v; break;
                case Color c: p.colorValue = c; break;
                case Object[] arr:
                    p.arraySize = arr.Length;
                    for (var i = 0; i < arr.Length; i++)
                        p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                    break;
                case Object o: p.objectReferenceValue = o; break;
                case null when p.propertyType == SerializedPropertyType.ObjectReference: p.objectReferenceValue = null; break;
                default: throw new System.ArgumentException(field);
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
