// グラミネ第1のボス (Boss_Gramine.prefab) の見た目を Spine (Assets/Motion/gramine, 2026-10-12 デザイナー支給 #13) にする (何度流してもよい)。
//   子 "SpineVisual" = SkeletonRenderer + SkeletonAnimation (gramine_SkeletonData)、ルートに GramineSpineVisual。
//   仮の四角 (ルートの SpriteRenderer = EnemyController._visual) は描画だけ止まる (向き・色を読むために残す)。
//   四角の色は白 (骨格へそのまま写すため)、被弾の光は薄い赤 (白い絵に白を重ねても見えないため)。
// unity command run_script --file Docs/Area1/SceneScripts/prep_gramine_spine.cs --entry PrepGramineSpine.Run --args '[0.07, -0.5]'
//   引数 = 子の拡大率 (ルートは ×2 なので見た目はこの 2 倍)、子の y (足元 = 当たり判定の下端 -0.5)
// 新しいシーンを開いた後に絵のデータを読むこと (先に読むと NewScene の時に解放される)。ここではプレハブだけを扱うので関係ない。
using System.Text;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public static class PrepGramineSpine
{
    const string PrefabPath = "Assets/Prefabs/Enemies/Boss_Gramine.prefab";
    const string SkeletonPath = "Assets/Motion/gramine/gramine_SkeletonData.asset";
    const string ChildName = "SpineVisual";

    public static string Run(float scale, float y)
    {
        var log = new StringBuilder();
        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SkeletonPath);
        if (data == null)
            return "no " + SkeletonPath;

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var old = root.transform.Find(ChildName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var placeholder = root.GetComponent<SpriteRenderer>();
            placeholder.color = Color.white;
            var ec = root.GetComponent<EnemyController>();
            var ecSo = new SerializedObject(ec);
            ecSo.FindProperty("_hitFlashColor").colorValue = new Color(1f, 0.55f, 0.55f, 1f);
            ecSo.ApplyModifiedPropertiesWithoutUndo();

            var child = new GameObject(ChildName);
            child.layer = root.layer;
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(0f, y, 0f);
            child.transform.localScale = Vector3.one * scale;
            child.AddComponent<MeshFilter>();
            var mr = child.AddComponent<MeshRenderer>();
            mr.sortingLayerID = placeholder.sortingLayerID;
            mr.sortingOrder = placeholder.sortingOrder;
            var renderer = child.AddComponent<SkeletonRenderer>();
            var animation = child.AddComponent<SkeletonAnimation>();

            var rSo = new SerializedObject(renderer);
            rSo.FindProperty("skeletonDataAsset").objectReferenceValue = data;
            rSo.ApplyModifiedPropertiesWithoutUndo();
            var aSo = new SerializedObject(animation);
            aSo.FindProperty("animationName").stringValue = "L/guramine_idle_normal_L";
            aSo.FindProperty("loop").boolValue = true;
            var transferred = aSo.FindProperty("wasDeprecatedTransferred");
            if (transferred != null)
                transferred.boolValue = true;
            aSo.ApplyModifiedPropertiesWithoutUndo();

            var visual = root.GetComponent<GramineSpineVisual>() ?? root.AddComponent<GramineSpineVisual>();
            var vSo = new SerializedObject(visual);
            vSo.FindProperty("_skeleton").objectReferenceValue = animation;
            vSo.FindProperty("_placeholder").objectReferenceValue = placeholder;
            vSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var ok);
            log.AppendLine($"prefab {PrefabPath} ok={ok} SpineVisual scale={scale} y={y} sorting={mr.sortingLayerName}/{mr.sortingOrder}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return log.ToString();
    }
}
