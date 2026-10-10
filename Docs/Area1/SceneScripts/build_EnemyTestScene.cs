using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 敵の挙動テスト用シーン Assets/Scenes/Test/EnemyTestScene.unity を作り直す (何度流してもよい)。
// Docs/EnemySpec.md の雑魚 6 種 (Assets/Prefabs/Enemies/, prep_enemy_spec.cs) を区画ごとに 1 体ずつ並べる。
// 骨組みはエリア1 のステージと同じ (PlayerSceneBootstrap → PlayerScene / StageInfo / DamageFloaterSpawner / CameraLimit / SpawnDefault)。
// 地形はグレーボックス (Ground レイヤーの BoxCollider2D + 四角の絵)。床の上面 y = 0。
//   x  10 Chaser       : 高さ 2 の台 (x4..18) の上。下に降りたプレイヤーを追って台の端で止まる
//   x  35 Bundled      : 平らな床
//   x  60 Charger      : 右の柱 (x70, 高さ 2.5) に突っ込むとふらつく
//   x  85 Stomper      : 段差 (x92..96, 高さ 1.5)
//   x 110 FlyingDasher : 浮遊 (y 4)
//   x 135 FlyingShooter: 浮遊 (y 5)、視線を遮る壁 (x127, y 3..7) と浮き足場 (x140..144, y 3)
// run_script --file Docs/Area1/SceneScripts/build_EnemyTestScene.cs --entry BuildEnemyTest.Run
public static class BuildEnemyTest
{
    const string ScenePath = "Assets/Scenes/Test/EnemyTestScene.unity";
    const int LayerGround = 10;
    static Sprite s_square;
    static Transform s_terrain;

    public static string Run()
    {
        // 開いているシーンの未保存の変更を壊さないよう、追加で作って保存したら閉じる
        var prevActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        s_square = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab").GetComponent<SpriteRenderer>().sprite;

        // ---- 骨組み (Area1StageBuilder.BuildSkeleton と同じ)
        var boot = new GameObject("PlayerSceneBootstrap").AddComponent<UISceneBootstrap>();
        Set(boot, "_uiSceneNames", new[] { "PlayerScene" });
        Set(new GameObject("StageInfo").AddComponent<StageInfo>(), "_areaName", "敵テスト");
        var floater = new GameObject("DamageFloaterSpawner").AddComponent<DamageFloaterSpawner>();
        var floaterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DamageFloater.prefab");
        if (floaterPrefab != null)
            Set(floater, "_floaterPrefab", floaterPrefab.GetComponent<DamageFloater>());
        Set(new GameObject("CameraLimit").AddComponent<CameraLimit>(), "_minY", -2f);

        var spawn = new GameObject("SpawnDefault");
        spawn.transform.position = new Vector3(0f, 1f, 0f);
        Set(spawn.AddComponent<PlayerSpawnPoint>(), "_id", "");

        // ---- 地形
        s_terrain = new GameObject("Terrain").transform;
        Block("Floor", -8, -4, 152, 0);
        Block("WallL", -10, -4, -8, 16);
        Block("WallR", 152, -4, 154, 16);
        Block("ChaserLedge", 4, 0, 18, 2);
        Block("ChargerPillar", 70, 0, 71, 2.5f);
        Block("StomperStep", 92, 0, 96, 1.5f);
        Block("ShooterCover", 127, 3, 128, 7);
        Block("FloatPlatform", 140, 3, 144, 3.5f);

        // ---- 敵
        var enemies = new GameObject("Enemies").transform;
        Enemy(enemies, "Enemy_Chaser", 10f, 2.6f);
        Enemy(enemies, "Enemy_Bundled", 35f, 0.6f);
        Enemy(enemies, "Enemy_Charger", 60f, 0.6f);
        Enemy(enemies, "Enemy_Stomper", 85f, 0.6f);
        Enemy(enemies, "Enemy_FlyingDasher", 110f, 4f);
        Enemy(enemies, "Enemy_FlyingShooter", 135f, 5f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        if (prevActive.IsValid())
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(prevActive);
            EditorSceneManager.CloseScene(scene, true);
        }

        // Build Settings (他の Test シーンと同じく登録しておく)
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == ScenePath))
        {
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        return "saved " + ScenePath;
    }

    static void Block(string name, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name) { layer = LayerGround };
        go.transform.SetParent(s_terrain, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
        go.transform.localScale = new Vector3(x1 - x0, y1 - y0, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s_square;
        sr.color = new Color(0.36f, 0.34f, 0.42f);
        go.AddComponent<BoxCollider2D>();
    }

    static void Enemy(Transform parent, string prefabName, float x, float y)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemies/{prefabName}.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = new Vector3(x, y, 0f);
    }

    static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        switch (value)
        {
            case string s: p.stringValue = s; break;
            case float f: p.floatValue = f; break;
            case string[] arr:
                p.arraySize = arr.Length;
                for (var i = 0; i < arr.Length; i++)
                    p.GetArrayElementAtIndex(i).stringValue = arr[i];
                break;
            case Object o: p.objectReferenceValue = o; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
