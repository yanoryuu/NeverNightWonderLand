using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ボス HP バーの Passive View 抽象 (Presenter が参照する)。</summary>
public interface IBossHpBarView
{
    /// <summary>枠を選んでバーを出す。残量は ratio (0..1) から始める。</summary>
    void Show(BossHpFrame frame, float ratio);

    /// <summary>バーを消す。</summary>
    void Hide();

    /// <summary>残量割合 (0..1) を表示に反映する。</summary>
    void SetRatio(float ratio);
}

/// <summary>
/// HUD のボス HP バー (画面下の中央)。表示のみを担い、ボスの購読は BossHpBarPresenter が行う。
/// 赤 (残量) は即座に減り、緑 (遅れて減る帯) は少し待ってから赤に追いつく。
/// 枠は 1戦目 (firsthp) と 2戦目 (lasthp) で差し替える。
/// </summary>
public class BossHpBarView : MonoBehaviour, IBossHpBarView
{
    [Tooltip("出し入れのフェードに使う CanvasGroup")]
    [SerializeField] private CanvasGroup _group;

    [Tooltip("残量 (赤) のフィル画像 (Image Type = Filled)")]
    [SerializeField] private Image _fill;

    [Tooltip("遅れて減る帯 (緑) のフィル画像 (Image Type = Filled)")]
    [SerializeField] private Image _trail;

    [Tooltip("1戦目の枠 (firsthp)")]
    [SerializeField] private GameObject _frameFirst;

    [Tooltip("2戦目の枠 (lasthp)")]
    [SerializeField] private GameObject _frameLast;

    [Tooltip("被弾してから緑の帯が減り始めるまでの秒数")]
    [SerializeField] private float _trailDelay = 0.5f;

    [Tooltip("緑の帯が減る速さ (割合/秒)")]
    [SerializeField] private float _trailSpeed = 0.5f;

    [Tooltip("出し入れのフェード秒数")]
    [SerializeField] private float _fadeDuration = 0.3f;

    private float _ratio = 1f;
    private float _trailRatio = 1f;
    private float _trailWait;

    // プレハブでは非アクティブ・CanvasGroup.alpha = 0 で置いておく (Show で出す)
    public void Show(BossHpFrame frame, float ratio)
    {
        if (_frameFirst != null) _frameFirst.SetActive(frame == BossHpFrame.First);
        if (_frameLast != null) _frameLast.SetActive(frame == BossHpFrame.Last);

        _ratio = _trailRatio = Mathf.Clamp01(ratio);
        _trailWait = 0f;
        Apply();

        gameObject.SetActive(true);
        if (_group != null)
        {
            _group.DOKill();
            _group.DOFade(1f, _fadeDuration).SetUpdate(true);
        }
    }

    public void Hide()
    {
        if (!gameObject.activeSelf)
            return;

        if (_group == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _group.DOKill();
        _group.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));
    }

    public void SetRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        if (ratio < _ratio)
            _trailWait = _trailDelay; // 削られるたびに待ち直し、連撃の間は削った量を見せておく
        else if (ratio > _trailRatio)
            _trailRatio = ratio;      // 回復した時は緑も一緒に戻す

        _ratio = ratio;
        Apply();
    }

    private void Update()
    {
        if (_trailRatio <= _ratio)
            return;

        if (_trailWait > 0f)
        {
            _trailWait -= Time.deltaTime;
            return;
        }

        _trailRatio = Mathf.Max(_ratio, _trailRatio - _trailSpeed * Time.deltaTime);
        Apply();
    }

    private void Apply()
    {
        if (_fill != null) _fill.fillAmount = _ratio;
        if (_trail != null) _trail.fillAmount = _trailRatio;
    }

    private void OnDestroy()
    {
        if (_group != null)
            _group.DOKill();
    }
}
