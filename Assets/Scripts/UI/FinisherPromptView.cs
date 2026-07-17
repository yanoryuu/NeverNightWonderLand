using TMPro;
using UnityEngine;

/// <summary>裁断プロンプトの Passive View 抽象 (Presenter が参照する)。</summary>
public interface IFinisherPromptView
{
    void SetVisible(bool visible);

    /// <summary>プロンプトのテキストと不透明度 (点滅は Presenter が計算) を反映する。</summary>
    void SetPrompt(string text, float alpha);
}

/// <summary>
/// HUD の裁断プロンプト。表示のみを担い、発動可否の判定・点滅・
/// ボタン表記の決定は FinisherPromptPresenter が行う。
/// </summary>
public class FinisherPromptView : MonoBehaviour, IFinisherPromptView
{
    [Tooltip("表示の親 (表示切替の対象)")]
    [SerializeField] private GameObject _root;

    [Tooltip("プロンプトのテキスト")]
    [SerializeField] private TMP_Text _label;

    private void Awake()
    {
        // Presenter が起動するまでは非表示にしておく
        if (_root != null)
            _root.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (_root != null && _root.activeSelf != visible)
            _root.SetActive(visible);
    }

    public void SetPrompt(string text, float alpha)
    {
        if (_label == null)
            return;

        _label.text = text;

        var c = _label.color;
        c.a = alpha;
        _label.color = c;
    }
}
