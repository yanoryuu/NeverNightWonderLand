using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// アニメ・エフェクト調整シーン Assets/Scenes/Test/AnimEffectTuningScene.unity と、
// その一覧アセット Assets/Data/AnimTuning/AnimTuningCatalog.asset / AnimEffectCueSet.asset を作る。
// 何度流してもよい。シーンは作り直す。カタログはキャラ・エフェクトを Id ごとに上書き/追加する (手で足した分は残る)。
// エフェクト設定 (CueSet) は既にあれば触らない (調整した値を消さないため)。
// run_script --file Docs/AnimEffectTuning/build_AnimEffectTuningScene.cs --entry BuildAnimEffectTuning.Run
public static class BuildAnimEffectTuning
{
    const string ScenePath = "Assets/Scenes/Test/AnimEffectTuningScene.unity";
    const string DataDir = "Assets/Data/AnimTuning";
    const string CatalogPath = DataDir + "/AnimTuningCatalog.asset";
    const string CueSetPath = DataDir + "/AnimEffectCueSet.asset";

    public static string Run()
    {
        if (!AssetDatabase.IsValidFolder(DataDir))
            AssetDatabase.CreateFolder("Assets/Data", "AnimTuning");

        var catalog = AssetDatabase.LoadAssetAtPath<AnimTuningCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AnimTuningCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var cueSet = AssetDatabase.LoadAssetAtPath<AnimEffectCueSet>(CueSetPath);
        if (cueSet == null)
        {
            cueSet = ScriptableObject.CreateInstance<AnimEffectCueSet>();
            AssetDatabase.CreateAsset(cueSet, CueSetPath);
        }

        FillCatalog(catalog);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        BuildScene(catalog, cueSet);
        return $"catalog: {catalog.Characters.Count} characters / {catalog.Effects.Count} effects, scene: {ScenePath}";
    }

    static void FillCatalog(AnimTuningCatalog catalog)
    {
        // Spine の骨格 (プレハブに付く前でも確認できるよう、骨格データ単体でも出す)
        Character(catalog, "Spine: グラミネ", c =>
        {
            c.Prefab = null;
            c.Spine = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Motion/gramine/gramine_SkeletonData.asset");
            c.Scale = 0.055f; // ボスのプレハブ (GramineSpineVisual) と同じ大きさ
            c.FacesLeft = true; // L/R どちらも素の向きは左
        });
        Character(catalog, "Spine: 慈 (プレイヤー)", c =>
        {
            c.Prefab = null;
            c.Spine = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>("Assets/Motion/Itsuku/Itshuku_SkeletonData.asset");
            c.Scale = 0.0434f; // Player.prefab と同じ大きさ (本体 4 倍 × 骨格 0.010848)
            c.FacesLeft = true;
        });

        // 敵プレハブ (Spine / Animator / SpriteFlipbook が付いていれば自動で一覧に出る)
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" })
                     .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab.GetComponent<EnemyController>() == null)
                continue;
            Character(catalog, "敵: " + prefab.name, c => c.Prefab = prefab);
        }
        Character(catalog, "敵: 野良ウマ (Animator)", c => c.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab"));

        // コードで絵を差し替えているもの: ポップコーン爆弾 (EnemyLobBomb の飛行 / ひび / 破裂の絵)
        var bomb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/PopcornBomb.prefab");
        var bombScript = bomb != null ? bomb.GetComponent<EnemyLobBomb>() : null;
        if (bombScript != null)
        {
            var so = new SerializedObject(bombScript);
            var flying = so.FindProperty("_flyingSprite").objectReferenceValue as Sprite;
            var crack = so.FindProperty("_crackSprite").objectReferenceValue as Sprite;
            var burstProp = so.FindProperty("_burstSprites");
            var burst = Enumerable.Range(0, burstProp.arraySize)
                .Select(i => burstProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite).Where(s => s != null).ToArray();

            Character(catalog, "敵: ポップコーン爆弾", c =>
            {
                c.Prefab = bomb;
                c.SpriteClips = new List<AnimTuningCatalog.SpriteClip>
                {
                    new() { Name = "飛行", Frames = new[] { flying }, Fps = 1f },
                    new() { Name = "ひび", Frames = new[] { crack }, Fps = 1f },
                    new() { Name = "破裂", Frames = burst, Fps = 12f },
                };
            });
            Effect(catalog, "ポップコーン破裂", e =>
            {
                e.Prefab = null;
                e.Frames = new AnimTuningCatalog.SpriteClip { Name = "ポップコーン破裂", Frames = burst, Fps = 12f };
            });
        }
    }

    static void Character(AnimTuningCatalog catalog, string id, System.Action<AnimTuningCatalog.CharacterEntry> fill)
    {
        var entry = catalog.Characters.Find(c => c.Id == id);
        if (entry == null)
        {
            entry = new AnimTuningCatalog.CharacterEntry { Id = id };
            catalog.Characters.Add(entry);
        }
        fill(entry);
    }

    static void Effect(AnimTuningCatalog catalog, string id, System.Action<AnimTuningCatalog.EffectEntry> fill)
    {
        var entry = catalog.Effects.Find(e => e.Id == id);
        if (entry == null)
        {
            entry = new AnimTuningCatalog.EffectEntry { Id = id };
            catalog.Effects.Add(entry);
        }
        fill(entry);
    }

    static void BuildScene(AnimTuningCatalog catalog, AnimEffectCueSet cueSet)
    {
        // 開いているシーンを壊さないよう、追加で作って保存したら閉じる
        var prevActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.17f, 0.22f);
        camGo.transform.position = new Vector3(0f, 2f, -10f);

        var stageGo = new GameObject("AnimTuningStage");
        var anchor = new GameObject("Anchor (足元)").transform;
        anchor.SetParent(stageGo.transform, false);
        var stage = stageGo.AddComponent<AnimTuningStage>();
        var so = new SerializedObject(stage);
        so.FindProperty("_catalog").objectReferenceValue = catalog;
        so.FindProperty("_cueSet").objectReferenceValue = cueSet;
        so.FindProperty("_anchor").objectReferenceValue = anchor;
        so.FindProperty("_camera").objectReferenceValue = cam;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        if (prevActive.IsValid() && prevActive != scene)
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(prevActive);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
