using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// ヒット時に飛び出すダメージ数値 (ワールド空間の TextMeshPro)。
/// HP ダメージ=赤、防御値(崩し)ダメージ=白 で色分けする。
/// 上昇しながらフェードアウトして自壊する。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class DamageFloater : MonoBehaviour
{
    private static readonly Color HpColor = new(1f, 0.3f, 0.3f);
    private static readonly Color GuardColor = Color.white;

    [Tooltip("上昇距離 (units)")]
    [SerializeField] private float _riseDistance = 1f;

    [Tooltip("表示時間 (sec)")]
    [SerializeField] private float _lifetime = 0.6f;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    /// <summary>数値と種別を設定して演出を開始する (Spawner から呼ばれる)。</summary>
    public void Play(int amount, DamageEvents.Kind kind)
    {
        _text.text = amount.ToString();
        _text.color = kind == DamageEvents.Kind.Hp ? HpColor : GuardColor;

        transform.DOMoveY(transform.position.y + _riseDistance, _lifetime).SetEase(Ease.OutCubic);

        // TMP_Text.DOFade は DOTWEEN_TEXTMESHPRO モジュール無効のため使えない。汎用 To でフェードする
        DOTween.To(() => _text.alpha, a => _text.alpha = a, 0f, _lifetime)
            .SetEase(Ease.InQuad)
            .SetTarget(_text)
            .OnComplete(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        transform.DOKill();
        _text.DOKill();
    }
}
