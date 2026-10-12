using System.Text;
using UnityEditor;
using UnityEngine;

// Docs/EnemySpec.md の雑魚 7 種 + ボス「グラミネ第1のボス」の定数アセットとプレハブを作る/更新する (何度流してもよい)。
//   定数:     Assets/Prefabs/Enemies/EnemyConsts_<名前>.asset (既存 EnemyConsts.asset を複製して値を上書き)
//   プレハブ: Assets/Prefabs/Enemies/<名前>.prefab
//     地上 1〜4・7 = Enemy.prefab (野良ウマの絵) の Variant / 飛行 5・6 = FlyingEnemy.prefab の Variant /
//     ボス = Enemy.prefab の Variant ×2 (地上の仮ボスと同じ仮の四角 = BossRoom の KeepPlaceholderLook と同じ処置)
//   見た目 (絵・色) は変えない (専用の絵がある 2 厚着の敵 = armadillo、7 ポップコーン投げ = popduck + ポップコーンの爆弾 PopcornBomb.prefab だけ替える)。部品の並び順 = CompositeEnemyBehaviour の優先度。
// run_script --file Docs/Area1/SceneScripts/prep_enemy_spec.cs --entry PrepEnemySpec.Run
//   (2 厚着の敵だけ作り直す: --entry PrepEnemySpec.RunBundled / 7 ポップコーン投げだけ: --entry PrepEnemySpec.RunPopcorn /
//    ネズミの巣のネズミ (Enemy_Rat): --entry PrepEnemySpec.RunRat。先に SceneScripts/prep_consts_D.cs で定数を作っておく)
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

        Bundled(bundled);

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
        //      見た目の Spine は prep_gramine_spine.cs が付ける (ここで作り直したら続けて流す)
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

    /// <summary>2 厚着の敵 (Bundled) だけ作る/更新する。</summary>
    public static string RunBundled()
    {
        Log.Clear();
        Bundled(Consts("Bundled", 20, 14, 1.2f, 1.2f, 6f, 2));
        AssetDatabase.SaveAssets();
        return Log.ToString();
    }

    // ---- 2 Bundled: 防御値が大きいだけ。発見したらプレイヤーの方へ歩く。絵 = リボンで包まれたアルマジロ
    static void Bundled(EnemyConsts bundled)
    {
        var armadillo = PopcornSprite(ArmadilloArt, ArmadilloPpu, ArmadilloPivot);
        Variant("Enemy_Bundled", false, bundled, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<ChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly), ("_stopAtLedge", true));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly), ("_turnAtLedge", true));
            go.AddComponent<EnemyPerception>();
            // 当たり判定: 甲羅に合わせて横長 (頭は少しはみ出す)
            UseSingleArt(go, armadillo, ArmadilloArt, new Vector2(1.4f, 1.1f));
        });
    }

    /// <summary>
    /// ネズミ (A1_D3 のネズミの巣 = WaveArena A1_RatNest で湧く小ネズミ・大ネズミ) のプレハブを作る/更新する。
    /// うごきは Enemy.prefab のまま (部品を足さない)。強さは湧かせる側 (build_A1_D3.cs の Rat()) が定数で上書きし、大きさも Scale で変える
    /// (小ネズミ ×0.6 = 高さ約 0.8 / 大ネズミ ×1.1 = 高さ約 1.46)。
    /// </summary>
    public static string RunRat()
    {
        Log.Clear();
        var rat = PopcornSprite(RatArt, RatPpu, RatPivot);
        var consts = AssetDatabase.LoadAssetAtPath<EnemyConsts>("Assets/Prefabs/Area1/EnemyConsts_A1Rat.asset");
        // 当たり判定: 胴体に合わせて横長 (鼻先・しっぽ・ひげははみ出す)
        Variant("Enemy_Rat", false, consts, go => UseSingleArt(go, rat, RatArt, new Vector2(1.0f, 0.9f)));
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
        var bomb = PopcornBomb();
        var duck = PopcornSprite(DuckArt, DuckPpu, DuckPivot);
        Variant("Enemy_PopcornThrower", false, popcorn, go =>
        {
            go.AddComponent<CompositeEnemyBehaviour>();
            Set(go.AddComponent<EnemyPerception>(), ("_detectRange", 9f), ("_loseRange", 12f), ("_requireLineOfSight", true));
            Set(go.AddComponent<ChaseMotion>(), ("_phase", (int)EnemyPhaseFilter.AlertOnly), ("_stopAtLedge", true),
                ("_keepDistance", 6f), ("_retreatDistance", 3f), ("_retreatSpeed", 2f));
            Set(go.AddComponent<PatrolMotion>(), ("_phase", (int)EnemyPhaseFilter.SearchOnly), ("_turnAtLedge", true));
            var lob = go.AddComponent<EnemyLobAction>();
            Set(lob, ("_phase", (int)EnemyPhaseFilter.Always), ("_explosionMaterial", particles));

            // 見た目: 野良ウマの絵+アニメ → ポップコーン頭のアヒルの 1 枚絵 (左向き。アニメ無し)
            // 当たり判定: 胴体に合わせて細く高く
            var vis = UseSingleArt(go, duck, DuckArt, new Vector2(0.6f, 1.1f));
            if (vis == null)
                return;
            Set(lob, ("_squashTarget", vis), ("_muzzleOffset", new Vector2(0.25f, 0.2f)));
            if (bomb != null)
                Set(lob, ("_bombPrefab", bomb));
        });
    }

    // ---- ポップコーン投げの絵 (Assets/LocalData/Character/)
    const string DuckArt = "Assets/LocalData/Character/popduck.png";
    // 1 枚絵 2894x4093 の見えている部分の高さ 3082px → 1.23 units (プレイヤー (当たり判定 1.2) と同じくらい。野良ウマは 1.66)
    const float DuckPpu = 2500f;
    // 足元の中央 (足の下端 y=3525px、胴体の中央 x≈1560px)
    static readonly Vector2 DuckPivot = new(0.539f, 0.139f);
    const string BombArtDir = "Assets/LocalData/Character/popbomb";
    const string BombPrefabPath = Dir + "/PopcornBomb.prefab";

    // ---- 厚着の敵 (Bundled) の絵 = アルマジロ (2026-10-12 ユーザー支給「アルマジロ.png」→ armadillo.png、余白を切った 2916x2104)
    const string ArmadilloArt = "Assets/LocalData/Character/armadillo.png";
    // 高さ 2104px → 1.36 units (野良ウマ 1.66 より少し低く、横に広い 1.88)
    const float ArmadilloPpu = 1550f;
    // 足元の中央 (足の下端 y≈2030px、甲羅の中央 x≈1460px)
    static readonly Vector2 ArmadilloPivot = new(0.5f, 0.035f);

    // ---- ネズミの絵 (2026-10-12 ユーザー支給「ネズミ.png」→ rat.png、余白を切った 2650x1593、左向き)
    const string RatArt = "Assets/LocalData/Character/rat.png";
    // 高さ 1593px → 1.33 units (×1 の時。小ネズミ ×0.6 で約 0.8)
    const float RatPpu = 1200f;
    // 足元 = 胴体の下端の中央 (x≈1335px, y≈1577px)
    static readonly Vector2 RatPivot = new(0.504f, 0.01f);

    /// <summary>
    /// 地上の敵 (Enemy.prefab の Variant) の見た目を、野良ウマの絵+アニメから 1 枚絵 (左向き・アニメ無し) に替える。
    /// 絵の原点 (pivot) = 足元の中央を当たり判定の下端 (y = -0.5、床+0.6 に置いた時の接地面) に合わせ、
    /// 当たり判定は下端 -0.5 のまま colliderSize にする。絵か Visual が無ければ何もせず null。
    /// </summary>
    static Transform UseSingleArt(GameObject go, Sprite sprite, string artPath, Vector2 colliderSize)
    {
        var vis = go.transform.Find("Visual");
        if (sprite == null || vis == null)
        {
            Log.AppendLine($"WARN {go.name}: 絵 ({artPath}) か Visual が無いので、見た目は野良ウマのまま");
            return null;
        }

        var anim = vis.GetComponent<Animator>();
        if (anim != null)
            Object.DestroyImmediate(anim, true);
        var enemyAnim = go.GetComponent<EnemyAnimator>();
        if (enemyAnim != null)
            Object.DestroyImmediate(enemyAnim, true);

        vis.localPosition = new Vector3(0f, -0.5f, 0f);
        var sr = vis.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = false;
        Set(go.GetComponent<EnemyController>(), ("_visualFacesLeft", true));
        var box = go.GetComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.offset = new Vector2(0f, colliderSize.y * 0.5f - 0.5f);
        return vis;
    }

    /// <summary>絵を Sprite (単体・指定 PPU・指定 pivot) で取り込み直して返す (設定が同じなら何もしない)。</summary>
    static Sprite PopcornSprite(string path, float ppu, Vector2 pivot)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        var pivotChanged = settings.spriteAlignment != (int)SpriteAlignment.Custom || settings.spritePivot != pivot;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
            || !Mathf.Approximately(importer.spritePixelsPerUnit, ppu) || pivotChanged)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spritePixelsPerUnit = ppu;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            Log.AppendLine($"import {path}: PPU {ppu} pivot {pivot}");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>
    /// ポップコーン (EnemyLobBomb) のプレハブを作る/更新する。絵 popbomb_1〜5 (1 セル 546px, PPU 546, 中央 pivot):
    /// 1 = 粒 (飛行・着地) / 2 = 割れた粒 (予告の点滅で 1 と交互・斬られた時) / 3〜5 = 爆発。
    /// </summary>
    static EnemyLobBomb PopcornBomb()
    {
        var frames = new Sprite[5];
        for (var i = 0; i < frames.Length; i++)
            frames[i] = PopcornSprite($"{BombArtDir}/popbomb_{i + 1}.png", 546f, new Vector2(0.5f, 0.5f));
        if (System.Array.IndexOf(frames, null) >= 0)
        {
            Log.AppendLine($"WARN ポップコーンの絵 ({BombArtDir}/popbomb_1〜5.png) が揃っていないので、爆弾は仮表示のまま");
            return null;
        }

        var root = new GameObject("PopcornBomb");
        try
        {
            var lobBomb = root.AddComponent<EnemyLobBomb>();
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            // 素材は敵の絵 (Enemy.prefab の Visual) と同じにする (ライティングの見え方を揃える)
            var enemyVisual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab")
                .transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            if (enemyVisual != null)
                sr.sharedMaterial = enemyVisual.sharedMaterial;

            Set(lobBomb, ("_flyingSprite", frames[0]), ("_crackSprite", frames[1]));
            var so = new SerializedObject(lobBomb);
            var burst = so.FindProperty("_burstSprites");
            burst.arraySize = 3;
            for (var i = 0; i < 3; i++)
                burst.GetArrayElementAtIndex(i).objectReferenceValue = frames[i + 2];
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BombPrefabPath, out var ok);
            Log.AppendLine($"prefab {BombPrefabPath} ok={ok}");
            return prefab != null ? prefab.GetComponent<EnemyLobBomb>() : null;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
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
