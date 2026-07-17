using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>特殊攻撃クールダウンの円形表示の Passive View 抽象 (Presenter が参照する)。</summary>
public interface ISpecialCooldownView
{
    /// <summary>装備中の特殊攻撃のアイコン色と有無を反映する。</summary>
    void SetIcon(Color color, bool hasSpecial);

    /// <summary>クールダウンの残り割合 (1=使った直後, 0=使用可能) を反映する。</summary>
    void SetCooldown(float remainingRatio, bool ready);
}

/// <summary>
/// HUD の特殊攻撃 (△) クールダウン円形表示。表示のみを担い、
/// 割合の計算と装備の購読は SpecialCooldownPresenter が行う。
/// 円形の見た目は Image (Fill Method = Radial 360) の差し替えで変更できる。
/// </summary>
public class SpecialCooldownView : MonoBehaviour, ISpecialCooldownView
{
    // クールダウン中はアイコンを暗くする
    private static readonly Color CoolingTint = new(1f, 1f, 1f, 0.35f);

    [Tooltip("中央のアイコン画像 (特殊攻撃の色に染める)")]
    [SerializeField] private Image _icon;

    [Tooltip("残りクールダウンを表す円形フィル画像 (Image Type = Filled / Radial 360)")]
    [SerializeField] private Image _cooldownFill;

    [Tooltip("ボタン表記テキスト (△)")]
    [SerializeField] private TMP_Text _label;

    private Color _iconColor = Color.white;

    public void SetIcon(Color color, bool hasSpecial)
    {
        _iconColor = color;
        if (_icon != null)
            _icon.color = hasSpecial ? color : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        if (_label != null)
            _label.alpha = hasSpecial ? 1f : 0.4f;
    }

    public void SetCooldown(float remainingRatio, bool ready)
    {
        if (_cooldownFill != null)
            _cooldownFill.fillAmount = remainingRatio;

        if (_icon != null)
            _icon.color = ready ? _iconColor : _iconColor * CoolingTint;
    }
}
