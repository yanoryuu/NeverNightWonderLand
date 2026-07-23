using DG.Tweening;
using UnityEngine;

/// <summary>
/// 画面全体の暗転オーバーレイ。ステージ遷移時に一瞬暗転させ、
/// プレイヤーのワープとカメラのスナップを見せないために使う。
/// PlayerScene に事前配置し、StageLoader が FadeOut / FadeIn を呼ぶ。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Tooltip("暗転にかける時間 (sec)")]
    [SerializeField] private float _fadeOutDuration = 0.12f;

    [Tooltip("明転にかける時間 (sec)")]
    [SerializeField] private float _fadeInDuration = 0.25f;

    private CanvasGroup _group;

    private void Awake()
    {
        Instance = this;
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        _group.DOKill();
    }

    /// <summary>暗転する。完了時に onComplete を呼ぶ (ポーズ中でも進むよう unscaled)。</summary>
    public void FadeOut(System.Action onComplete)
    {
        _group.DOKill();
        _group.DOFade(1f, _fadeOutDuration)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>明転する。</summary>
    public void FadeIn()
    {
        _group.DOKill();
        _group.DOFade(0f, _fadeInDuration).SetUpdate(true);
    }
}
