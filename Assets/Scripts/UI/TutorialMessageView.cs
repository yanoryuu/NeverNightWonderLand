using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 画面上部のチュートリアル案内メッセージ。TutorialManager から Show / Hide で操作する。
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
    }

    private void OnDestroy()
    {
        _group.DOKill();
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
