using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>装備中の攻撃方法表示の Passive View 抽象 (Presenter が参照する)。</summary>
public interface IAttackLoadoutView
{
    /// <summary>装備中の近接/特殊攻撃の表示を更新する。flash が true なら入れ替え演出を再生する。</summary>
    void SetLoadout(string meleeName, string specialName, Color specialColor, bool flash);
}

/// <summary>
/// HUD の装備中攻撃方法表示 (□=近接 / △=特殊)。表示のみを担い、
/// ロードアウトの購読とテキストの決定は AttackLoadoutPresenter が行う。
/// 入れ替え時はフラッシュ (スケールパンチ) する。
/// </summary>
public class AttackLoadoutView : MonoBehaviour, IAttackLoadoutView
{
    [Tooltip("装備を表示するテキスト (2行: □近接 / △特殊)")]
    [SerializeField] private TMP_Text _label;

    [Tooltip("背景 (任意)。特殊攻撃の色に薄く染める")]
    [SerializeField] private Image _background;

    public void SetLoadout(string meleeName, string specialName, Color specialColor, bool flash)
    {
        if (_label != null)
            _label.text = $"□ {meleeName}\n△ {specialName}";

        if (_background != null)
            _background.color = new Color(specialColor.r, specialColor.g, specialColor.b, 0.25f);

        if (flash)
        {
            transform.DOKill(complete: true);
            transform.DOPunchScale(Vector3.one * 0.25f, 0.25f, vibrato: 6);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
