using System.Text;
using UnityEditor;
using UnityEngine;

// Docs/EnemySpec.md の雑魚 7 種 + ボス「グラミネ第1のボス」の定数アセットとプレハブを作る/更新する (何度流してもよい)。
//   定数:     Assets/Prefabs/Enemies/EnemyConsts_<名前>.asset (既存 EnemyConsts.asset を複製して値を上書き)
//   プレハブ: Assets/Prefabs/Enemies/<名前>.prefab
//     地上 1〜4・7 = Enemy.prefab (野良ウマの絵) の Variant / 飛行 5・6 = FlyingEnemy.prefab の Variant /
//     ボス = Enemy.prefab の Variant ×2 (地上の仮ボスと同じ仮の四角 = BossRoom の KeepPlaceholderLook と同じ処置)
//   見た目 (絵・色) は変えない。部品の並び順 = CompositeEnemyBehaviour の優先度。
// run_script --file Docs/Area1/SceneScripts/prep_enemy_spec.cs --entry PrepEnemySpec.Run
//   (7 ポップコーン投げだけ作り直す: --entry PrepEnemySpec.RunPopcorn)
public static class PrepEnemySpec
{
    const string Dir = "Assets/Prefabs/Enemies";
    static readonly StringBuilder Log = new();

    public static string Run()
    {
        Log.Clear();
        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        //                       MaxHp Guard Move Chase Range Contact
        var chaser = Consts("Chaser", 10, 2, 2f, 4.5f, 7f, 1);
        var bundled = Consts("Bundled", 20, 14, 1.2f, 1.2f, 6f, 2);
        var charger = Consts("Charger", 15, 4, 2f, 0f, 0f, 1);
        var stomper = Consts("Stomper", 15, 4, 2f, 0f, 0f, 1);
        var dasher = Consts("FlyingDasher", 10, 2, 1.5f, 2.5f, 8f, 1);
        var shooter = Consts("FlyingShooter", 8, 1, 1.5f, 3f, 9f, 1);
        // 雑魚の HP: 2026-10-10 ユーザー決定で引き上げ (プレイヤーの近接 HP ダメージ 5 → Chaser 2 発 / Bundled 4 発 など)
        // ボス: 2026-10-10 ユーザー指示「弱すぎる、最低でも HP500」→ HP 500。防御値は 40 → 20 (同日ユーザー決定)
        var boss = Consts("BossGramine", 500, 20, 2f, 0f, 0f, 1, breakDuration: 3f);

        // ---- 1 Chaser: 発見したら追う (振り向き 0.2 秒・端で止まる)、見つける前は巡回 (端で引き返す)
        Variant("Enemy_Chaser", false, chaser, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<EnemyPerception>(), ("_detectRange", 0f), ("_loseRange", 10f));
            Set(go.AddComponent<ChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly), ("_turnDelay", 0.2f), ("_stopAtLedge", true));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly), ("_turnAtLedge", true));
        });

        // ---- 2 Bundled: 防御値が大きいだけ。発見したらプレイヤーの方へ歩く
        Variant("Enemy_Bundled", false, bundled, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<ChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly), ("_stopAtLedge", true));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly), ("_turnAtLedge", true));
            go.AddComponent<EnemyPerception>();
        });

        // ---- 3 Charger: 突進
        Variant("Enemy_Charger", false, charger, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<ChargeMotion>(), ("_phase", (int)EnemyPhaseFilter.Always));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.Always), ("_turnAtLedge", true));
        });

        // ---- 4 Stomper: ジャンプ踏みつけ
        Variant("Enemy_Stomper", false, stomper, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<StompJumpMotion>(), ("_phase", (int)EnemyPhaseFilter.Always));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.Always), ("_turnAtLedge", true));
        });

        // ---- 5 FlyingDasher: 親の FloatingChase/FloatingPatrol を外して、突進を一番上に付け直す (並び順 = 優先度)
        Variant("Enemy_FlyingDasher", true, dasher, go =>
        {
            Object.DestroyImmediate(go.GetComponent<FloatingChaseMotion>(), true);
            Object.DestroyImmediate(go.GetComponent<FloatingPatrolMotion>(), true);
            Set(go.AddComponent<EnemyPerception>(), ("_detectRange", 8f), ("_loseRange", 12f));
            Set(go.AddComponent<FloatingDashMotion>(), ("_phase", (int)EnemyPhaseFilter.Always));
            Set(go.AddComponent<FloatingChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly), ("_keepDistance", 5f), ("_keepHeight", 2.5f));
            Set(go.AddComponent<FloatingPatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly));
        });

        // ---- 6 FlyingShooter: 斜め上に陣取って撃つ (親の FloatingChase/FloatingPatrol をそのまま使う)
        Variant("Enemy_FlyingShooter", true, shooter, go =>
        {
            Set(go.AddComponent<EnemyPerception>(), ("_detectRange", 9f), ("_loseRange", 13f), ("_requireLineOfSight", true));
            Set(go.GetComponent<FloatingChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly),
                ("_standOffset", new Vector2(6f, 3f)), ("_retreatDistance", 4f));
            Set(go.GetComponent<FloatingPatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly));
            Set(go.AddComponent<EnemyShootAction>(), ("_phase", (int)EnemyPhaseFilter.Always));
        });

        // ---- ボス: 地上の仮ボスと同じ仮の四角 ×2。うごきは GramineBossBehaviour だけ (EnemyController の自動付与を止める)
        Variant("Boss_Gramine", false, boss, go =>
        {
            go.transform.localScale = Vector3.one * 2f;
            var ec = go.GetComponent<EnemyController>();
            var root = go.GetComponent<SpriteRenderer>();
            root.enabled = true;
            Set(ec, ("_visual", root), ("_visualFacesLeft", false), ("_hitFlashColor", Color.white), ("_superArmor", true));
            var vis = go.transform.Find("Visual");
            if (vis != null)
                vis.gameObject.SetActive(false);
            var anim = go.GetComponent<EnemyAnimator>();
            if (anim != null)
                Object.DestroyImmediate(anim, true);
            go.AddComponent<GramineBossBehaviour>();
        });

        Popcorn();

        AssetDatabase.SaveAssets();
        return Log.ToString();
    }

    /// <summary>7 ポップコーン投げ (PopcornThrower) だけ作る/更新する。</summary>
    public static string RunPopcorn()
    {
        Log.Clear();
        Popcorn();
        AssetDatabase.SaveAssets();
        return Log.ToString();
    }

    // ---- 7 PopcornThrower: 地上。距離 6 を保ち (3 より近いと後ずさり・端で止まる)、ポップコーンを放物線で足元へ投げる
    static void Popcorn()
    {
        //                         MaxHp Guard Move Chase Range Contact
        var popcorn = Consts("PopcornThrower", 10, 3, 1.5f, 1.5f, 9f, 1);
        // 爆発パーティクルの素材 = URP 既定の ParticlesUnlit (スピーカーの爆発と同じ。新しい絵は使わない)
        var particles = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        Variant("Enemy_PopcornThrower", false, popcorn, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<EnemyPerception>(), ("_detectRange", 9f), ("_loseRange", 12f), ("_requireLineOfSight", true));
            Set(go.AddComponent<ChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly), ("_stopAtLedge", true),
                ("_keepDistance", 6f), ("_retreatDistance", 3f), ("_retreatSpeed", 2f));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly), ("_turnAtLedge", true));
            Set(go.AddComponent<EnemyLobAction>(), ("_phase", (int)EnemyPhaseFilter.Always), ("_explosionMaterial", particles));
        });
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

    static void Variant(string name, bool flying, EnemyConsts consts, System.Action<GameObject> setup)
    {
        var basePath = flying ? "Assets/Prefabs/FlyingEnemy.prefab" : "Assets/Prefabs/Enemy.prefab";
        var path = $"{Dir}/{name}.prefab";
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(basePath));
        try
        {
            go.name = name;
            Set(go.GetComponent<EnemyController>(), ("_consts", consts));
            setup(go);
            PrefabUtility.SaveAsPrefabAsset(go, path, out var ok);
            var comps = new StringBuilder();
            foreach (var c in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponents<Component>())
                comps.Append(c.GetType().Name).Append(' ');
            Log.AppendLine($"prefab {path} ok={ok}: {comps}");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
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
                case Object o: p.objectReferenceValue = o; break;
                default: throw new System.ArgumentException(field);
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
