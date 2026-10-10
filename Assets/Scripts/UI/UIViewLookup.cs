using UnityEngine;

/// <summary>
/// ステージの部品 (TutorialHintZone / NpcTalk / ShopKeeper など) から UI の View を探すための共通処理。
/// UI は UI シーン (名前が UI で終わるシーン、Additive) に分離されているが、
/// GameField_old の旧ステージは自前の SceneUI に同じ View を持っている。
/// 両方ロードされている時に旧ステージの見た目を変えないよう、優先順は
/// 「要求元と同じシーン (旧方式の SceneUI)」→「UI シーン」→「その他」。
/// </summary>
public static class UIViewLookup
{
    public static T Find<T>(Component requester) where T : Component
    {
        var views = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (views.Length == 0)
            return null;

        T inUIScene = null;
        foreach (var view in views)
        {
            if (requester != null && view.gameObject.scene == requester.gameObject.scene)
                return view;

            if (inUIScene == null && view.gameObject.scene.name.EndsWith("UI"))
                inUIScene = view;
        }

        return inUIScene != null ? inUIScene : views[0];
    }
}
