using UnityEngine;
using UnityEngine.UI;

/// <summary>HP バーの Passive View 抽象 (Presenter が参照する)。</summary>
public interface IPlayerHpBarView
{
    /// <summary>残量割合 (0..1) を表示に反映する。</summary>
    void SetRatio(float ratio);
}

/// <summary>
/// HUD のプレイヤー HP バー(赤)。表示のみを担い、
/// HP の購読と割合計算は PlayerHpBarPresenter が行う。
/// 見た目は Prefab 側 (Image) の差し替えで変更できる。
/// </summary>
public class PlayerHpBarView : MonoBehaviour, IPlayerHpBarView
{
    [Tooltip("残量を表すフィル画像 (Image Type = Filled)")]
    [SerializeField] private Image _fill;

    public void SetRatio(float ratio)
    {
        if (_fill != null)
            _fill.fillAmount = Mathf.Clamp01(ratio);
    }
}
