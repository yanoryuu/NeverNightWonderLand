using TMPro;
using UnityEngine;

/// <summary>
/// エリア進入時のエリア名タイトル表示。シーン開始時にフェードイン→ホールド→フェードアウトする。
/// シーン開始処理 (ポーズ解除・リザルト集計リセット) も兼ねる。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AreaTitleView : MonoBehaviour
{
    private const float FadeIn = 0.5f;
    private const float Hold = 1.8f;
    private const float FadeOut = 0.8f;

    [Tooltip("エリア名")]
    [SerializeField] private string _areaName = "";

    [Tooltip("エリア名のテキスト")]
    [SerializeField] private TMP_Text _label;

    private CanvasGroup _group;
    private float _elapsed;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;

        // シーン開始のリセット処理
        GamePause.Reset();
        GameSession.ResetRun();
    }

    private void Start()
    {
        if (_label != null)
            _label.text = _areaName;
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;

        if (_elapsed < FadeIn)
            _group.alpha = _elapsed / FadeIn;
        else if (_elapsed < FadeIn + Hold)
            _group.alpha = 1f;
        else if (_elapsed < FadeIn + Hold + FadeOut)
            _group.alpha = 1f - (_elapsed - FadeIn - Hold) / FadeOut;
        else
        {
            _group.alpha = 0f;
            enabled = false; // 表示が終わったら更新停止
        }
    }
}
