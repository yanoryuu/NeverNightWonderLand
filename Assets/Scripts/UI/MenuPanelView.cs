using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// キーボード/パッド操作の汎用メニューパネル。UI は実行時にコードで構築する
/// (ポーズ・拠点・ゲームオーバー・リザルト・タイトルが共用)。
/// 操作: W/S・↑↓・十字キー = 選択、Enter/Space/J・Aボタン = 決定、Esc・Bボタン = 戻る。
/// Time.timeScale = 0 中でも動作する (入力ポーリングのため)。
/// Canvas の子に置いて <see cref="Initialize"/> を呼んでから使う。
/// </summary>
public class MenuPanelView : MonoBehaviour
{
    public readonly struct Entry
    {
        public readonly string Label;
        public readonly Action OnSelect;
        public readonly bool Enabled;

        public Entry(string label, Action onSelect, bool enabled = true)
        {
            Label = label;
            OnSelect = onSelect;
            Enabled = enabled;
        }
    }

    private static int _openCount;

    /// <summary>いずれかのメニューが開いているか (ポーズトグルの多重防止用)。</summary>
    public static bool AnyOpen => _openCount > 0;

    /// <summary>Esc / B での「戻る」を受け付けるか (ゲームオーバー等では false)。</summary>
    public bool AllowCancel { get; set; } = true;

    public bool IsOpen { get; private set; }

    /// <summary>「戻る」操作をした時に発火する。ページ遷移や Close は所有者が行う。</summary>
    public event Action OnCancelled;

    private TMP_FontAsset _font;
    private GameObject _root;
    private TMP_Text _title;
    private TMP_Text _body;
    private RectTransform _rowsRoot;
    private readonly List<TMP_Text> _rowTexts = new();
    private readonly List<Entry> _entries = new();
    private int _index;

    #region Build

    /// <summary>UI を構築する。一度だけ呼ぶ。</summary>
    public void Initialize(TMP_FontAsset font)
    {
        if (_root != null)
            return;

        _font = font;

        // 全面の暗幕
        _root = new GameObject("MenuRoot", typeof(RectTransform));
        _root.transform.SetParent(transform, false);
        Stretch((RectTransform)_root.transform);
        var dim = _root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);

        // ウィンドウ
        var window = new GameObject("Window", typeof(RectTransform));
        window.transform.SetParent(_root.transform, false);
        var windowRt = (RectTransform)window.transform;
        windowRt.sizeDelta = new Vector2(680f, 560f);
        var windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        _title = CreateText(windowRt, "Title", 40f, FontStyles.Bold);
        var titleRt = _title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        titleRt.sizeDelta = new Vector2(0f, 60f);

        _body = CreateText(windowRt, "Body", 26f, FontStyles.Normal);
        var bodyRt = _body.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.anchoredPosition = new Vector2(0f, -92f);
        bodyRt.sizeDelta = new Vector2(-60f, 190f);
        _body.alignment = TextAlignmentOptions.Top;

        var rows = new GameObject("Rows", typeof(RectTransform));
        rows.transform.SetParent(windowRt, false);
        _rowsRoot = (RectTransform)rows.transform;
        _rowsRoot.anchorMin = new Vector2(0f, 0f);
        _rowsRoot.anchorMax = new Vector2(1f, 1f);
        _rowsRoot.offsetMin = new Vector2(60f, 30f);
        _rowsRoot.offsetMax = new Vector2(-60f, -100f);

        _root.SetActive(false);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private TMP_Text CreateText(RectTransform parent, string name, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        if (_font != null)
            text.font = _font;
        return text;
    }

    #endregion

    #region API

    public void SetTitle(string title)
    {
        if (_title != null)
            _title.text = title;
    }

    public void SetBody(string body)
    {
        if (_body != null)
            _body.text = body ?? "";
    }

    public void SetEntries(IReadOnlyList<Entry> entries)
    {
        _entries.Clear();
        _entries.AddRange(entries);

        // 行テキストを必要数まで用意する
        while (_rowTexts.Count < _entries.Count)
        {
            var row = CreateText(_rowsRoot, $"Row{_rowTexts.Count}", 30f, FontStyles.Normal);
            row.alignment = TextAlignmentOptions.Left;
            var rt = row.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 44f);
            _rowTexts.Add(row);
        }

        for (var i = 0; i < _rowTexts.Count; i++)
        {
            var active = i < _entries.Count;
            _rowTexts[i].gameObject.SetActive(active);
            if (active)
                _rowTexts[i].rectTransform.anchoredPosition = new Vector2(0f, -i * 48f);
        }

        _index = FirstEnabledIndex();
        Render();
    }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;
        _openCount++;
        _root.SetActive(true);
        Render();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        _openCount = Mathf.Max(0, _openCount - 1);
        _root.SetActive(false);
    }

    #endregion

    #region Navigation

    private void Update()
    {
        if (!IsOpen || _entries.Count == 0)
            return;

        // プレイヤー不在のシーン (タイトル等) でもデバイス切替を追跡できるようにする
        InputDeviceTracker.Poll();

        var move = 0;
        var submit = false;
        var cancel = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) move -= 1;
            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) move += 1;
            submit |= keyboard.enterKey.wasPressedThisFrame
                      || keyboard.spaceKey.wasPressedThisFrame
                      || keyboard.jKey.wasPressedThisFrame;
            cancel |= keyboard.escapeKey.wasPressedThisFrame;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame) move -= 1;
            if (gamepad.dpad.down.wasPressedThisFrame) move += 1;
            submit |= gamepad.buttonSouth.wasPressedThisFrame;
            cancel |= gamepad.buttonEast.wasPressedThisFrame;
        }

        if (move != 0)
        {
            MoveSelection(move);
            Render();
        }

        if (submit)
        {
            var entry = _entries[_index];
            if (entry.Enabled)
                entry.OnSelect?.Invoke();
            return;
        }

        if (cancel && AllowCancel)
            OnCancelled?.Invoke();
    }

    private void MoveSelection(int direction)
    {
        if (_entries.Count == 0)
            return;

        // 無効な項目はスキップする
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
        for (var i = 0; i < _entries.Count; i++)
        {
            var selected = i == _index;
            var entry = _entries[i];
            _rowTexts[i].text = (selected ? "▶ " : "　 ") + entry.Label;
            _rowTexts[i].color = !entry.Enabled
                ? new Color(0.5f, 0.5f, 0.5f)
                : selected ? new Color(1f, 0.85f, 0.3f) : Color.white;
        }
    }

    #endregion
}
