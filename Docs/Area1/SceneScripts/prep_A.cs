// A 区画 (A1_AB / A1_FP) の準備: 生成素材 (Area1Gen/hall) のインポート + エリアボスの定数アセット。何度流してもよい。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/prep_A.cs --entry PrepA.Run
using UnityEditor;
using UnityEngine;

public static class PrepA
{
    public const string ConductorPath = "Assets/Prefabs/Area1/EnemyConsts_A1Conductor.asset";

    public static string Run()
    {
        AssetDatabase.Refresh();
        var import = NeverNight.StageBuild.Area1AssetPrep.ImportArt();
        var c = AssetDatabase.LoadAssetAtPath<EnemyConsts>(ConductorPath);
        var created = false;
        if (c == null) { c = ScriptableObject.CreateInstance<EnemyConsts>(); AssetDatabase.CreateAsset(c, ConductorPath); created = true; }
        var so = new SerializedObject(c);
        // 指揮者ノクターン (浮遊型のエリアボス): Heavy (HP8/防御14) より硬い。広く舞台の上を往復し、近づくとゆっくり迫る
        so.FindProperty("_maxHp").intValue = 16;
        so.FindProperty("_maxGuard").intValue = 16;
        so.FindProperty("_breakDuration").floatValue = 3.5f;
        so.FindProperty("_moveSpeed").floatValue = 2.2f;
        so.FindProperty("_patrolHalfWidth").floatValue = 11f;
        so.FindProperty("_contactDamage").intValue = 2;
        so.FindProperty("_chaseRange").floatValue = 6f;
        so.FindProperty("_chaseSpeed").floatValue = 1.8f;
        so.FindProperty("_threadDrop").intValue = 40;
        so.FindProperty("_knockbackOnHit").floatValue = 1.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        return import + " / " + (created ? "created " : "updated ") + ConductorPath;
    }
}
