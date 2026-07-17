using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// キーボード/パッド操作の汎用メニューパネル (ポーズ・拠点・ゲームオーバー・リザルト・タイトルが共用)。
/// UI は実行時に生成せず、プレハブ/シーン上で事前配置したものを SerializeField で参照する
/// (枠などの素材はプレハブ側の Image / 行テキストを差し替えれば反映される)。
/// 行も HUD と同様にすべて事前配置し、項目数に応じて表示/非表示を切り替えるだけ。
/// 操作: W/S・↑↓・十字キー = 選択、Enter/Space/J・Aボタン = 決定、Esc・Bボタン = 戻る。
/// Time.timeScale = 0 中でも動作する (入力ポーリングのため)。
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

    [Header("参照 (プレハブ/シーン上で事前配置)")]
    [Tooltip("メニュー全体のルート (暗幕)。開閉で表示切替される")]
    [SerializeField] private GameObject _root;

    [Tooltip("タイトルテキスト")]
    [SerializeField] private TMP_Text _title;

    [Tooltip("本文テキスト")]
    [SerializeField] private TMP_Text _body;

    [Tooltip("行テキスト (上から順に事前配置。項目数がこれを超えた分は表示されない)")]
    [SerializeField] private TMP_Text[] _rows;

    [Header("行の色")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = new(1f, 0.85f, 0.3f);
    [SerializeField] private Color _disabledColor = new(0.5f, 0.5f, 0.5f);

    private readonly List<Entry> _entries = new();
    private int _index;
    private bool _initialized;
    private bool _isValid;

    #region Setup

    /// <summary>
    /// 初期化する (参照の検証・フォント適用・初期非表示)。一度だけ呼ぶ。
    /// font は null なら事前配置のフォントをそのまま使う。
    /// </summary>
    public void Initialize(TMP_FontAsset font)
    {
        if (_initialized)
            return;

        _initialized = true;
        _isValid = _root != null && _title != null && _body != null
                   && _rows != null && _rows.Length > 0;

        if (!_isValid)
        {
            Debug.LogError($"[{nameof(MenuPanelView)}] UI 参照が設定されていません。プレハブ上で配置してください。", this);
            return;
        }

        if (font != null)
        {
            _title.font = font;
            _body.font = font;
            foreach (var row in _rows)
            {
                if (row != null)
                    row.font = font;
            }
        }

        foreach (var row in _rows)
        {
            if (row != null)
                row.gameObject.SetActive(false);
        }

        _root.SetActive(false);
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
        if (!_isValid)
            return;

        _entries.Clear();
        _entries.AddRange(entries);

        // 事前配置した行数を超えた分は表示できない
        if (_entries.Count > _rows.Length)
        {
            Debug.LogWarning(
                $"[{nameof(MenuPanelView)}] 項目数 {_entries.Count} が事前配置の行数 {_rows.Length} を超えています。超過分は表示されません。",
                this);
            _entries.RemoveRange(_rows.Length, _entries.Count - _rows.Length);
        }

        for (var i = 0; i < _rows.Length; i++)
        {
            if (_rows[i] != null)
                _rows[i].gameObject.SetActive(i < _entries.Count);
        }

        _index = FirstEnabledIndex();
        Render();
    }

    public void Open()
    {
        if (IsOpen || !_isValid)
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
        if (_root != null)
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
            if (_rows[i] == null)
                continue;

            var selected = i == _index;
            var entry = _entries[i];
            _rows[i].text = (selected ? "▶ " : "　 ") + entry.Label;
            _rows[i].color = !entry.Enabled
                ? _disabledColor
                : selected ? _selectedColor : _normalColor;
        }
    }

    #endregion
}
