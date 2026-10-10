using TMPro;
using UnityEngine;

/// <summary>全体マップ画面の Passive View 抽象 (<see cref="MapScreenPresenter"/> が参照する)。</summary>
public interface IMapScreenView
{
    bool IsShown { get; }

    /// <summary>地図の絵 (部屋・通路・アイコン・現在地)。</summary>
    MapDrawing Drawing { get; }

    /// <summary>ズームの段階 (画面 px / ワールド単位。小さい順)。</summary>
    float[] ZoomLevels { get; }

    /// <summary>開いた時のズームの段階 (ZoomLevels の添字)。</summary>
    int DefaultZoomLevel { get; }

    /// <summary>スクロールの速さ (画面 px / 秒)。</summary>
    float ScrollSpeed { get; }

    void Show(bool show);

    /// <summary>下の欄の場所名 (「エリア名 — 部屋名」)。</summary>
    void SetPlace(string text);

    /// <summary>操作ガイドを入力機器に合わせて切り替える。</summary>
    void SetGuide(InputDeviceKind device);

    /// <summary>ズームの段階の表示 (level は 0 始まり)。</summary>
    void SetZoomLevel(int level, int count);
}

/// <summary>
/// 全体マップ画面 (MapUI シーン)。部品はエディタで事前配置 (Docs/UI/Build/build_map.cs) して SerializeField で参照する。
/// 表示だけを担い、開閉・スクロール・ズーム・場所名の決定は <see cref="MapScreenPresenter"/> が行う。
/// 見た目は仮の単色 (ポーズ画面の紙の色味)。素材が届いたら各 Image の Sprite を差し替える。
/// </summary>
public class MapScreenView : MonoBehaviour, IMapScreenView
{
    [Header("参照 (事前配置)")]
    [Tooltip("画面全体のルート。開閉で表示切替される")]
    [SerializeField] private GameObject _root;

    [SerializeField] private MapDrawing _drawing;

    [Tooltip("下の欄の場所名")]
    [SerializeField] private TMP_Text _placeText;

    [Tooltip("ズームの段階の表示")]
    [SerializeField] private TMP_Text _zoomText;

    [Tooltip("キーボード操作のガイド")]
    [SerializeField] private GameObject _keyboardGuide;

    [Tooltip("パッド操作のガイド")]
    [SerializeField] private GameObject _gamepadGuide;

    [Header("操作")]
    [Tooltip("ズームの段階 (画面 px / ワールド単位。小さい順)")]
    [SerializeField] private float[] _zoomLevels = { 1.3f, 2.6f, 5f };

    [Tooltip("開いた時のズームの段階 (添字)")]
    [SerializeField] private int _defaultZoomLevel = 1;

    [Tooltip("スクロールの速さ (画面 px / 秒)")]
    [SerializeField] private float _scrollSpeed = 700f;

    public bool IsShown => _root != null && _root.activeSelf;
    public MapDrawing Drawing => _drawing;
    public float[] ZoomLevels => _zoomLevels;
    public int DefaultZoomLevel => _defaultZoomLevel;
    public float ScrollSpeed => _scrollSpeed;

    private void Awake()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void Show(bool show)
    {
        if (_root != null)
            _root.SetActive(show);
    }

    public void SetPlace(string text)
    {
        if (_placeText != null && _placeText.text != text)
            _placeText.text = text;
    }

    public void SetGuide(InputDeviceKind device)
    {
        if (_keyboardGuide != null)
            _keyboardGuide.SetActive(device == InputDeviceKind.Keyboard);
        if (_gamepadGuide != null)
            _gamepadGuide.SetActive(device == InputDeviceKind.Gamepad);
    }

    public void SetZoomLevel(int level, int count)
    {
        if (_zoomText != null)
            _zoomText.text = $"ズーム {level + 1}/{count}";
    }
}
