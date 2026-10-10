using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 画面上部のチュートリアル案内メッセージ。TutorialManager / TutorialHintZone / NpcTalk から Show / Hide で操作する。
/// 現行は UI シーン (StageUI) に常駐する。GameField_old の旧ステージは自前の SceneUI にも持っている
/// (ステージ側からは UIViewLookup で同じシーンのものを優先して探す)。
/// CanvasGroup のフェードで表示を切り替える。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TutorialMessageView : MonoBehaviour
{
    [Tooltip("メッセージ本文のテキスト")]
    [SerializeField] private TMP_Text _label;

    [Tooltip("フェード時間 (sec)")]
    [SerializeField] private float _fadeDuration = 0.25f;

    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;

        // UI シーン (StageUI) に常駐する場合は、ステージが入れ替わったら前のステージの案内を消す
        // (旧方式の SceneUI はステージと一緒に破棄されるので不要)
        if (gameObject.scene.name.EndsWith("UI"))
            StageInfo.Entered += OnStageEntered;
    }

    private void OnDestroy()
    {
        StageInfo.Entered -= OnStageEntered;
        _group.DOKill();
    }

    private void OnStageEntered(StageInfo _)
    {
        _group.DOKill();
        _group.alpha = 0f;
    }

    /// <summary>メッセージを表示する。表示中なら本文だけ差し替える。</summary>
    public void Show(string text)
    {
        if (_label != null)
            _label.text = text;

        _group.DOKill();
        _group.DOFade(1f, _fadeDuration);
    }

    public void Hide()
    {
        _group.DOKill();
        _group.DOFade(0f, _fadeDuration);
    }
}
