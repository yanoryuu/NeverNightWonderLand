using TMPro;
using UnityEngine;

/// <summary>
/// インタラクトのボタン表示 (キャラクターの頭の上に出す小さなキーの絵)。
/// 「今押せば反応する」時だけ出す = インタラクトの範囲に対象がいて (<see cref="PlayerInteractor.Current"/>)、
/// 立ち止まっている (<see cref="PlayerController.CanInteractNow"/>)。歩いている・空中・梯子の上・会話中・
/// メニュー/ポーズ中・範囲外では隠す。表記は最後に使った入力デバイスに合わせる (キーボード = E / パッド = ↑)。
/// プレイヤー (Player.prefab のルート) に付ける。表示物は実行時に作り、プレイヤーと同じシーンに置く
/// (ルートは向きで localScale.x が反転し ×4 なので子にはしない。位置だけ毎フレーム追従する)。
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInteractor))]
public class InteractButtonPrompt : MonoBehaviour
{
    [Header("見た目")]
    [Tooltip("キーの枠 (角丸)")]
    [SerializeField] private Sprite _badge;

    [Tooltip("ボタン表記のフォント")]
    [SerializeField] private TMP_FontAsset _font;

    [Tooltip("キーボードの表記 (インタラクト = E キー。PlayerInputSource.ReadDevice)")]
    [SerializeField] private string _keyboardGlyph = "E";

    [Tooltip("パッドの表記 (インタラクト = 十字キー/左スティックの上)")]
    [SerializeField] private string _gamepadGlyph = "↑";

    [Tooltip("枠の大きさ (units)")]
    [SerializeField] private float _size = 0.5f;

    [Tooltip("文字の大きさ (TextMeshPro の fontSize)")]
    [SerializeField] private float _fontSize = 3.2f;

    [Tooltip("文字の色")]
    [SerializeField] private Color _textColor = new(1f, 0.9f, 0.68f);

    [Header("配置")]
    [Tooltip("足元 (PlayerController.FeetY) からの高さ (units)。枠の中心")]
    [SerializeField] private float _heightAboveFeet = 1.7f;

    [Tooltip("横のずれ (units, 向きに関係なく)")]
    [SerializeField] private float _offsetX;

    [Header("動き")]
    [Tooltip("出る/消える速さ (sec)")]
    [SerializeField] private float _fadeTime = 0.12f;

    [Tooltip("上下の揺れ幅 (units)")]
    [SerializeField] private float _bobAmplitude = 0.035f;

    [Tooltip("上下の揺れの周期 (sec)")]
    [SerializeField] private float _bobPeriod = 1.4f;

    [Tooltip("描画順 (枠。文字は +1)")]
    [SerializeField] private int _sortingOrder = 120;

    private PlayerController _controller;
    private PlayerInteractor _interactor;

    private GameObject _root;
    private SpriteRenderer _frame;
    private TextMeshPro _label;
    private float _alpha;
    private float _shownTime;

    /// <summary>今表示しているか (フェード中を含む)。</summary>
    public bool IsVisible => _root != null && _root.activeSelf;

    /// <summary>表示中の表記 (テスト用)。</summary>
    public string Glyph => _label != null ? _label.text : "";

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _interactor = GetComponent<PlayerInteractor>();
        Build();
    }

    private void OnDestroy()
    {
        if (_root != null)
            Destroy(_root);
    }

    private void OnDisable()
    {
        if (_root != null)
            _root.SetActive(false);
        _alpha = 0f;
    }

    private void Build()
    {
        _root = new GameObject("InteractButtonPrompt");
        if (gameObject.scene.IsValid())
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_root, gameObject.scene);

        var frameGo = new GameObject("Frame");
        frameGo.transform.SetParent(_root.transform, false);
        _frame = frameGo.AddComponent<SpriteRenderer>();
        _frame.sprite = _badge;
        _frame.sortingOrder = _sortingOrder;
        if (_badge != null)
        {
            var s = _size / Mathf.Max(_badge.bounds.size.x, 0.0001f);
            frameGo.transform.localScale = new Vector3(s, s, 1f);
        }

        var textGo = new GameObject("Glyph");
        textGo.transform.SetParent(_root.transform, false);
        // 枠はキーの厚み (下側の影) のぶん上に寄っているので、文字も少し上へ
        textGo.transform.localPosition = new Vector3(0f, _size * 0.03f, 0f);
        _label = textGo.AddComponent<TextMeshPro>();
        if (_font != null)
            _label.font = _font;
        _label.fontSize = _fontSize;
        _label.alignment = TextAlignmentOptions.Center;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.color = _textColor;
        _label.rectTransform.sizeDelta = new Vector2(_size, _size);
        _label.sortingOrder = _sortingOrder + 1;
        _label.text = _keyboardGlyph;

        _root.SetActive(false);
    }

    /// <summary>今インタラクトすれば反応するか (= 表示する条件)。</summary>
    public bool ShouldShow()
    {
        if (_controller == null || _interactor == null || !isActiveAndEnabled)
            return false;
        if (GamePause.IsPaused || MenuPanelView.AnyOpen || DialogueService.IsBusy)
            return false;

        var current = _interactor.Current.CurrentValue;
        return current != null && current.CanInteract && _controller.CanInteractNow;
    }

    private void LateUpdate()
    {
        if (_root == null)
            return;

        var show = ShouldShow();
        var dt = Time.unscaledDeltaTime;
        var step = _fadeTime > 0f ? dt / _fadeTime : 1f;
        _alpha = Mathf.MoveTowards(_alpha, show ? 1f : 0f, step);

        // 消えた瞬間 (会話やメニューが開いた) は待たずに隠す
        if (!show && (GamePause.IsPaused || MenuPanelView.AnyOpen || DialogueService.IsBusy))
            _alpha = 0f;

        var visible = _alpha > 0.001f;
        if (_root.activeSelf != visible)
        {
            _root.SetActive(visible);
            if (visible)
                _shownTime = Time.unscaledTime;
        }

        if (!visible)
            return;

        _label.text = InputDeviceTracker.Current == InputDeviceKind.Gamepad ? _gamepadGlyph : _keyboardGlyph;

        // 出始めは少し下から持ち上がる + ゆっくり上下に揺れる (控えめ)
        var rise = (1f - _alpha) * -0.12f;
        var bob = _bobPeriod > 0f
            ? Mathf.Sin((Time.unscaledTime - _shownTime) * Mathf.PI * 2f / _bobPeriod) * _bobAmplitude
            : 0f;
        var feet = _controller.FeetY;
        _root.transform.position = new Vector3(_controller.BodyCenterX + _offsetX, feet + _heightAboveFeet + rise + bob, 0f);

        var fc = _frame.color;
        fc.a = _alpha;
        _frame.color = fc;
        _label.alpha = _alpha;
    }
}
