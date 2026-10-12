// D 区画 (A1_D3 / D7) の敵の定数アセットを作る (何度流してもよい)。
// unity command run_script --caller plugin --skill unity-cli --file Docs/Area1/SceneScripts/prep_consts_D.cs --entry PrepConstsD.Run
using UnityEditor;
using UnityEngine;

public static class PrepConstsD
{
    public const string RatPath = "Assets/Prefabs/Area1/EnemyConsts_A1Rat.asset";

    static string Make(string path, int hp, int guard, float speed, float patrol, int contact, float chaseRange, float chaseSpeed, int drop)
    {
        var c = AssetDatabase.LoadAssetAtPath<EnemyConsts>(path);
        var created = false;
        if (c == null) { c = ScriptableObject.CreateInstance<EnemyConsts>(); AssetDatabase.CreateAsset(c, path); created = true; }
        var so = new SerializedObject(c);
        so.FindProperty("_maxHp").intValue = hp;
        so.FindProperty("_maxGuard").intValue = guard;
        so.FindProperty("_moveSpeed").floatValue = speed;
        so.FindProperty("_patrolHalfWidth").floatValue = patrol;
        so.FindProperty("_contactDamage").intValue = contact;
        so.FindProperty("_chaseRange").floatValue = chaseRange;
        so.FindProperty("_chaseSpeed").floatValue = chaseSpeed;
        so.FindProperty("_threadDrop").intValue = drop;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(c);
        return (created ? "created " : "updated ") + path;
    }

    public static string Run()
    {
        // 小ネズミ (D7 の群れ): 打たれ弱く、すばしこく寄ってくる
        var a = Make(RatPath, 3, 1, 3f, 2.5f, 1, 5f, 4.2f, 1);
        // 壁に張り付く敵 (D3) は 2026-10-12 ユーザー指示で削除
        AssetDatabase.SaveAssets();
        return a;
    }
}
