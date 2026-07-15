using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD の現在スタイル表示。PlayerController.StyleRP を購読し、
/// 「二刀流 / 両手持ち」のテキストと色を切り替え、切替時にフラッシュ(スケールパンチ)する。
/// </summary>
public class StyleIndicatorView : MonoBehaviour
{
    private static readonly Color DualColor = new(0.4f, 0.8f, 1f);   // 二刀流 = 水色 (速さ)
    private static readonly Color HeavyColor = new(1f, 0.55f, 0.25f); // 両手持ち = オレンジ (重さ)

    [Tooltip("参照するプレイヤー")]
    [SerializeField] private PlayerController _player;

    [Tooltip("スタイル名を表示するテキスト")]
    [SerializeField] private TMP_Text _label;

    [Tooltip("背景 (任意)。スタイル色に染める")]
    [SerializeField] private Image _background;

    private System.IDisposable _subscription;
    private bool _first = true;

    private void Start()
    {
        if (_player == null || _label == null)
        {
            Debug.LogError($"[{nameof(StyleIndicatorView)}] 参照が設定されていません。", this);
            return;
        }

        _subscription = _player.StyleRP.Subscribe(style =>
        {
            var isDual = style == ScissorStyle.DualBlades;
            _label.text = isDual ? "二刀流" : "両手持ち";

            var color = isDual ? DualColor : HeavyColor;
            _label.color = color;
            if (_background != null)
                _background.color = new Color(color.r, color.g, color.b, 0.25f);

            // 初期値の反映ではフラッシュしない
            if (_first)
            {
                _first = false;
                return;
            }

            transform.DOKill(complete: true);
            transform.DOPunchScale(Vector3.one * 0.25f, 0.25f, vibrato: 6);
        });
    }

    private void OnDestroy()
    {
        transform.DOKill();
        _subscription?.Dispose();
    }
}
