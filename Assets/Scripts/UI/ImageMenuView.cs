using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画像ボタンを縦に並べたメニュー (タイトルのメニュー・ポーズが使う)。
/// MenuPanelView の画像版: ボタン (枠画像 + 文字) はプレハブ/シーン上で事前配置し、
/// 項目の有効/無効と選択中の強調だけを切り替える (実行時生成はしない)。
/// 入力は MenuPanelView と同じ直読み (<see cref="MenuInput"/>): 上下 = 選択、決定、戻る。
/// 無効の項目は選択できず (飛ばす)、枠と文字を暗くして表示する。
/// 選択中の強調は「少し拡大」(完成形に選択状態の絵が無いため、見た目を変えない最小限の強調)。
/// </summary>
public class ImageMenuView : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        [Tooltip("ボタンのルート (強調時に拡大される)")]
        public RectTransform Root;

        [Tooltip("ボタンの枠画像")]
        public Image Frame;

        [Tooltip("ボタンの文字")]
        public TMP_Text Label;
    }

    [Header("参照 (プレハブ/シーン上で事前配置)")]
    [Tooltip("メニュー全体のルート。開閉で表示切替される")]
    [SerializeField] private GameObject _root;

    [Tooltip("ボタン (上から順)。SetEntries の項目数を超えた分は非表示")]
    [SerializeField] private Item[] _items;

    [Header("見た目")]
    [Tooltip("選択中のボタンの拡大率")]
    [SerializeField] private float _selectedScale = 1.06f;

    [Tooltip("無効なボタンの枠に掛ける色")]
    [SerializeField] private Color _disabledFrameColor = new(0.6f, 0.6f, 0.6f, 1f);

    [Tooltip("無効なボタンの文字のアルファ")]
    [Range(0f, 1f)]
    [SerializeField] private float _disabledLabelAlpha = 0.45f;

    /// <summary>Esc / B での「戻る」を受け付けるか。</summary>
    public bool AllowCancel { get; set; } = true;

    /// <summary>
    /// 入力を受け付けるか。false の間は表示したまま操作だけ止める
    /// (上に確認画面を重ねている間など)。
    /// </summary>
    public bool Interactable
    {
        get => _interactable;
        set
        {
            if (value && !_interactable)
                _openedFrame = Time.frameCount; // 再開したフレームの押下 (確認画面を閉じた決定/戻る) は無視する
            _interactable = value;
        }
    }

    private bool _interactable = true;

    public bool IsOpen { get; private set; }

    /// <summary>現在選択中の項目の番号。</summary>
    public int SelectedIndex => _index;

    /// <summary>「戻る」操作をした時に発火する。Close は所有者が行う。</summary>
    public event Action OnCancelled;

    private readonly List<MenuPanelView.Entry> _entries = new();
    private Color[] _labelColors;
    private int _index;
    private int _openedFrame = -1;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        // シーン破棄などで閉じずに消えた時も AnyOpen を戻す
        if (IsOpen)
        {
            IsOpen = false;
            MenuPanelView.NotifyClosed();
        }
    }

    /// <summary>初期化する (文字色の記録・初期非表示)。何度呼んでもよい。</summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _items ??= Array.Empty<Item>();
        _labelColors = new Color[_items.Length];
        for (var i = 0; i < _items.Length; i++)
            _labelColors[i] = _items[i]?.Label != null ? _items[i].Label.color : Color.white;

        if (_root != null && !IsOpen)
            _root.SetActive(false);
    }

    #region API

    /// <summary>
    /// 項目を設定する。Label が null/空ならボタンに事前配置した文字をそのまま使う。
    /// 選択は最初の有効な項目に戻る。
    /// </summary>
    public void SetEntries(IReadOnlyList<MenuPanelView.Entry> entries, int selectIndex = -1)
    {
        Initialize();
        _entries.Clear();
        _entries.AddRange(entries);

        if (_entries.Count > _items.Length)
        {
            Debug.LogWarning($"[{nameof(ImageMenuView)}] 項目数 {_entries.Count} がボタン数 {_items.Length} を超えています。", this);
            _entries.RemoveRange(_items.Length, _entries.Count - _items.Length);
        }

        for (var i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (item?.Root == null)
                continue;

            item.Root.gameObject.SetActive(i < _entries.Count);
            if (i < _entries.Count && item.Label != null && !string.IsNullOrEmpty(_entries[i].Label))
                item.Label.text = _entries[i].Label;
        }

        _index = selectIndex >= 0 && selectIndex < _entries.Count && _entries[selectIndex].Enabled
            ? selectIndex
            : FirstEnabledIndex();
        Render();
    }

    public void Open()
    {
        Initialize();
        if (IsOpen)
            return;

        IsOpen = true;
        _openedFrame = Time.frameCount; // 開いたのと同じ押下で決定/戻るが走らないように
        MenuPanelView.NotifyOpened();
        if (_root != null)
            _root.SetActive(true);
        Render();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        MenuPanelView.NotifyClosed();
        if (_root != null)
            _root.SetActive(false);
    }

    #endregion

    #region Navigation

    private void Update()
    {
        if (!IsOpen || !_interactable || _entries.Count == 0 || Time.frameCount == _openedFrame)
            return;

        var input = MenuInput.Read();
        if (input.Move != 0)
        {
            MoveSelection(input.Move);
            Render();
        }

        if (input.Submit)
        {
            var entry = _entries[_index];
            if (entry.Enabled)
                entry.OnSelect?.Invoke();
            return;
        }

        if (input.Cancel && AllowCancel)
            OnCancelled?.Invoke();
    }

    private void MoveSelection(int direction)
    {
        for (var step = 0; step < _entries.Count; step++)
        {
            _index = (_index + direction + _entries.Count) % _entries.Count;
            if (_entries[_index].Enabled)
                return;
        }
    }

    private int FirstEnabledIndex()
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Enabled)
                return i;
        }

        return 0;
    }

    private void Render()
    {
        for (var i = 0; i < _entries.Count && i < _items.Length; i++)
        {
            var item = _items[i];
            if (item?.Root == null)
                continue;

            var enabled = _entries[i].Enabled;
            var selected = enabled && i == _index;
            item.Root.localScale = Vector3.one * (selected ? _selectedScale : 1f);

            if (item.Frame != null)
                item.Frame.color = enabled ? Color.white : _disabledFrameColor;

            if (item.Label != null)
            {
                var color = _labelColors[i];
                if (!enabled)
                    color.a *= _disabledLabelAlpha;
                item.Label.color = color;
            }
        }
    }

    #endregion
}
