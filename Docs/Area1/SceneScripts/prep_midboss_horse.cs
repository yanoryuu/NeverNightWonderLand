using System.Text;
using UnityEditor;
using UnityEngine;

// 下の中ボス「歯車の重馬」(Docs/EnemySpec.md「下の中ボス」) の定数アセットとプレハブを作る/更新する (何度流してもよい)。
//   定数:     Assets/Prefabs/Enemies/EnemyConsts_HorseBoss.asset (既存 EnemyConsts.asset を複製して値を上書き)
//             HP 100 / 防御値 30 / ブレイク 2.5 秒 / 接触 1 / 駆ける速さ 6
//   プレハブ: Assets/Prefabs/Enemies/Boss_Horse.prefab = Enemy.prefab (野良ウマの Animator) の Variant ×2。
//             子 Visual の絵を bigHorse にする (BossRoom の UseGroundBossArt と同じ処置、絵は左向き)。
//             うごきは HorseBossBehaviour だけ (EnemyController の自動付与を止める)。スーパーアーマー (通常の被弾で怯まない)。
// 部屋への配置は Docs/Area1/SceneScripts/build_A1_MB2.cs (UseHorseBoss)。
// run_script --file Docs/Area1/SceneScripts/prep_midboss_horse.cs --entry PrepMidbossHorse.Run
public static class PrepMidbossHorse
{
    const string Dir = "Assets/Prefabs/Enemies";
    const string ConstsPath = Dir + "/EnemyConsts_HorseBoss.asset";
    public const string PrefabPath = Dir + "/Boss_Horse.prefab";
    const string ArtPath = "Assets/LocalData/Character/bigHorse.png";

    public static string Run()
    {
        var log = new StringBuilder();
        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        // ---- 定数
        var consts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(ConstsPath);
        if (consts == null)
        {
            AssetDatabase.CopyAsset("Assets/Scripts/Enemy/EnemyConsts.asset", ConstsPath);
            consts = AssetDatabase.LoadAssetAtPath<EnemyConsts>(ConstsPath);
            log.AppendLine("create " + ConstsPath);
        }
        Set(consts, ("_maxHp", 100), ("_maxGuard", 30), ("_breakDuration", 2.5f), ("_contactDamage", 1),
            ("_moveSpeed", 6f), ("_chaseSpeed", 0f), ("_chaseRange", 0f));
        log.AppendLine($"consts {ConstsPath}: HP100 G30 break2.5 contact1 move6");

        // ---- プレハブ
        var art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (art == null)
            log.AppendLine($"WARN {ArtPath} の Sprite が無いので、見た目は野良ウマのまま");

        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab"));
        try
        {
            go.name = "Boss_Horse";
            go.transform.localScale = Vector3.one * 2f;
            var ec = go.GetComponent<EnemyController>();
            Set(ec, ("_consts", consts), ("_superArmor", true));

            var vis = go.transform.Find("Visual");
            if (vis != null && art != null)
            {
                var sr = vis.GetComponent<SpriteRenderer>();
                sr.sprite = art;
                Set(ec, ("_visual", sr), ("_visualFacesLeft", true));
            }

            foreach (var old in go.GetComponents<EnemyBehaviour>())
                if (!(old is HorseBossBehaviour))
                    Object.DestroyImmediate(old, true);
            if (go.GetComponent<HorseBossBehaviour>() == null)
                go.AddComponent<HorseBossBehaviour>();
            if (vis != null)
                Set(go.GetComponent<HorseBossBehaviour>(), ("_poseTarget", vis));

            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out var ok);
            var comps = new StringBuilder();
            foreach (var c in AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponents<Component>())
                comps.Append(c.GetType().Name).Append(' ');
            log.AppendLine($"prefab {PrefabPath} ok={ok}: {comps}");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }

        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    static void Set(Object target, params (string field, object value)[] values)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in values)
        {
            var p = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name}.{field} が無い");
            switch (value)
            {
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case Object o: p.objectReferenceValue = o; break;
                default: throw new System.ArgumentException(field);
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
