using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="PlayerConsts"/> 用の Editor 拡張。
/// 通常の項目編集に加えて、jumpHeight / timeToApex から逆算される
/// 重力・ジャンプ初速などの派生値をプレビュー表示する。
/// </summary>
[CustomEditor(typeof(PlayerConsts))]
public class PlayerConstsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // SerializeField を通常通り描画
        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        DrawDerivedValues();
    }

    /// <summary>
    /// 編集中の値から計算される派生パラメータを読み取り専用で表示する。
    /// </summary>
    private void DrawDerivedValues()
    {
        var consts = (PlayerConsts)target;

        EditorGUILayout.LabelField("派生値 (自動計算)", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField(
                new GUIContent("重力 (Gravity)", "2 * jumpHeight / timeToApex^2"),
                consts.Gravity);

            EditorGUILayout.FloatField(
                new GUIContent("ジャンプ初速 (Jump Velocity)", "gravity * timeToApex"),
                consts.JumpVelocity);
        }

        EditorGUILayout.HelpBox(
            "重力は jumpHeight と timeToApex から逆算されます。\n" +
            "到達高さや頂点までの時間を変えると、上の派生値も追従します。",
            MessageType.Info);
    }
}
