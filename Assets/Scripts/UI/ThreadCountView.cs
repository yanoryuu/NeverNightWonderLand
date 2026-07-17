using TMPro;
using UnityEngine;

/// <summary>糸カウント表示の Passive View 抽象 (Presenter が参照する)。</summary>
public interface IThreadCountView
{
    void SetText(string text);
}

/// <summary>
/// HUD の素材「糸」所持数表示。表示のみを担い、
/// 所持数の購読とテキスト整形は ThreadCountPresenter が行う。
/// </summary>
public class ThreadCountView : MonoBehaviour, IThreadCountView
{
    [Tooltip("表示テキスト")]
    [SerializeField] private TMP_Text _label;

    public void SetText(string text)
    {
        if (_label != null)
            _label.text = text;
    }
}
