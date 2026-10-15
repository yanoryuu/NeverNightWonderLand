using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// アニメ・エフェクト調整シーン Assets/Scenes/Test/AnimEffectTuningScene.unity と、
// その一覧アセット Assets/Data/AnimTuning/AnimTuningCatalog.asset (キャラ) / AnimEffectCueSet.asset (エフェクトと調整した値) を作る。
// 何度流してもよい。シーンは作り直す。キャラとエフェクトは Id ごとに上書き/追加する (手で足した分は残る)。
// 調整した値 (CueSet の Entries) は触らない。
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

        FillCatalog(catalog, cueSet);
        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(cueSet);
        AssetDatabase.SaveAssets();

        BuildScene(catalog, cueSet);
        return $"catalog: {catalog.Characters.Count} characters / {cueSet.Effects.Count} effects, scene: {ScenePath}";
    }

    static void FillCatalog(AnimTuningCatalog catalog, AnimEffectCueSet cueSet)
    {
        // グラミネは Boss_Gramine のプレハブに Spine が付いたので、骨格データ単体の登録は外す
        // (ゲームで使う値はプレハブの Id に紐付くため、同じキャラが 2 つあると調整先を間違える)
        catalog.Characters.RemoveAll(c => c.Id == "Spine: グラミネ");

        // Spine の骨格 (プレハブではなく骨格データ単体で出す)
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
            Character(catalog, "敵: " + prefab.name, c => EnemyPrefab(c, prefab));
        }
        Character(catalog, "敵: 野良ウマ (Animator)", c => EnemyPrefab(c, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab")));

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
            Effect(cueSet, "ポップコーン破裂", e =>
            {
                e.Prefab = null;
                e.Frames = burst;
                e.Fps = 12f;
            });
        }
    }

    static void EnemyPrefab(AnimTuningCatalog.CharacterEntry c, GameObject prefab)
    {
        c.Prefab = prefab;
        // 素の絵が左向きの敵 (EnemyController._visualFacesLeft) は右向き基準にそろえる。
        // Spine のグラミネ (GramineSpineVisual) は骨格の素の向きが左 (向きは実行時に骨格側で反転する)
        c.FacesLeft = prefab.GetComponent<GramineSpineVisual>() != null
                      || new SerializedObject(prefab.GetComponent<EnemyController>()).FindProperty("_visualFacesLeft").boolValue;
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

    static void Effect(AnimEffectCueSet cueSet, string id, System.Action<AnimEffectCueSet.Effect> fill)
    {
        var entry = cueSet.FindEffect(id);
        if (entry == null)
        {
            entry = new AnimEffectCueSet.Effect { Id = id };
            cueSet.Effects.Add(entry);
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
