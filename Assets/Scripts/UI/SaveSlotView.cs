using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// セーブスロット画面 (3 スロット)。完成形 load/ui_アートボード 1 のコピー 3.png の通りに事前配置した
/// 紙・枠 (load_frame)・スロット (load_inframe ×3)・下矢印・戻る矢印を参照する。
/// 3 つの用途で共用する:
/// - <see cref="Mode.Load"/>    : タイトルの「つづきから」。データのあるスロットだけ選べる。見出しは枠の絵の「どのデータをロードしますか?」
/// - <see cref="Mode.NewGame"/> : タイトルの「はじめから」。記録先を選ぶ。見出しを差し替える
/// - <see cref="Mode.Save"/>    : 拠点 (ホーム画面・ポーズ) でのセーブ。記録先を選ぶ。見出しを差し替える
/// データのあるスロットへ記録する時は、見出しが上書き確認に変わり、もう一度決定で確定する。
/// 入力は MenuPanelView と同じ直読み (<see cref="MenuInput"/>)。
/// タイトルではシーンに置いたプレハブを、ゲーム中は SaveSlotUI シーン (Additive) のものを使う。
/// </summary>
public class SaveSlotView : MonoBehaviour
{
    public enum Mode { Load, NewGame, Save }

    [Serializable]
    public class SlotWidgets
    {
        [Tooltip("スロットのルート (選択中は拡大される)")]
        public RectTransform Root;

        [Tooltip("スロットの枠画像 (load_inframe)")]
        public Image Frame;

        public TMP_Text Number;
        public TMP_Text Chapter;

        [Tooltip("場所のアイコン (素材が来たら設定。無ければ空でよい)")]
        public GameObject PlaceIcon;

        public TMP_Text Place;
        public TMP_Text Date;
        public TMP_Text PlayTime;

        [Tooltip("データが無い時に出す文字")]
        public TMP_Text Empty;
    }

    [Header("参照 (プレハブ上で事前配置)")]
    [Tooltip("画面全体のルート。開閉で表示切替される")]
    [SerializeField] private GameObject _root;

    [Tooltip("ゲーム中に開いた時の背景 (all_background)。タイトルではタイトルの背景を見せるので出さない")]
    [SerializeField] private GameObject _background;

    [Tooltip("枠の絵の見出しを隠す板 (セーブ/はじめからの時だけ出す)")]
    [SerializeField] private GameObject _headingCover;

    [Tooltip("差し替えの見出し文字")]
    [SerializeField] private TMP_Text _headingText;

    [Tooltip("スロット (上から 01, 02, 03)")]
    [SerializeField] private SlotWidgets[] _slots;

    [Header("文言")]
    [SerializeField] private string _recordHeading = "どのデータに記録しますか?";
    [SerializeField] private string _overwriteHeading = "上書きしますか? (もう一度決定)";
    [SerializeField] private string _emptyLabel = "データなし";

    [Header("見た目")]
    [Tooltip("選択中のスロットの拡大率")]
    [SerializeField] private float _selectedScale = 1.04f;

    [Tooltip("選べないスロット (ロード時の空きスロット) の枠に掛ける色")]
    [SerializeField] private Color _disabledFrameColor = new(0.7f, 0.7f, 0.7f, 1f);

    public bool IsOpen { get; private set; }

    public Mode CurrentMode { get; private set; }

    private readonly SaveData[] _data = new SaveData[SaveSystem.SlotCount];
    private Action<int> _onChosen;
    private Action _onCancelled;
    private int _index;
    private int _confirmIndex = -1; // 上書き確認中のスロット
    private int _openedFrame = -1;

    private void Awake()
    {
        if (_root != null && !IsOpen)
            _root.SetActive(false);
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            IsOpen = false;
            MenuPanelView.NotifyClosed();
        }
    }

    #region API

    /// <summary>
    /// 開く。onChosen はスロット (0 始まり) を選んで確定した時、onCancelled は戻る操作の時に呼ばれる
    /// (どちらの場合も画面は閉じてから呼ぶ)。showBackground はゲーム中に開く時 true。
    /// </summary>
    public void Open(Mode mode, Action<int> onChosen, Action onCancelled, bool showBackground)
    {
        if (IsOpen)
            return;

        CurrentMode = mode;
        _onChosen = onChosen;
        _onCancelled = onCancelled;
        _confirmIndex = -1;

        for (var i = 0; i < _data.Length; i++)
            _data[i] = SaveSystem.TryLoad(i);

        if (_background != null)
            _background.SetActive(showBackground);

        // 初期選択: ロードは最初のデータあり、記録は現在のスロット
        _index = mode == Mode.Load ? FirstSelectable() : Mathf.Clamp(SaveSystem.ActiveSlot, 0, SlotCountShown - 1);

        IsOpen = true;
        _openedFrame = Time.frameCount;
        MenuPanelView.NotifyOpened();
        if (_root != null)
            _root.SetActive(true);

        RenderSlots();
        RenderSelection();
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

    private int SlotCountShown => Mathf.Min(_slots?.Length ?? 0, SaveSystem.SlotCount);

    private bool IsSelectable(int slot) =>
        CurrentMode != Mode.Load || (_data[slot] != null && !string.IsNullOrEmpty(_data[slot].sceneName));

    private int FirstSelectable()
    {
        for (var i = 0; i < SlotCountShown; i++)
        {
            if (IsSelectable(i))
                return i;
        }

        return 0;
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == _openedFrame)
            return;

        var input = MenuInput.Read();
        if (input.Move != 0)
        {
            Move(input.Move);
            _confirmIndex = -1;
            RenderSelection();
        }

        if (input.Submit)
        {
            Submit();
            return;
        }

        if (input.Cancel)
        {
            if (_confirmIndex >= 0)
            {
                _confirmIndex = -1;
                RenderSelection();
                return;
            }

            var cancelled = _onCancelled;
            Close();
            cancelled?.Invoke();
        }
    }

    private void Move(int direction)
    {
        var count = SlotCountShown;
        for (var step = 0; step < count; step++)
        {
            _index = (_index + direction + count) % count;
            if (IsSelectable(_index))
                return;
        }
    }

    private void Submit()
    {
        if (!IsSelectable(_index))
            return;

        // 記録先にデータがある時は上書き確認 (もう一度決定で確定)
        if (CurrentMode != Mode.Load && _data[_index] != null && _confirmIndex != _index)
        {
            _confirmIndex = _index;
            RenderSelection();
            return;
        }

        var chosen = _index;
        var callback = _onChosen;
        Close();
        callback?.Invoke(chosen);
    }

    #region Render

    private void RenderSlots()
    {
        for (var i = 0; i < (_slots?.Length ?? 0); i++)
        {
            var w = _slots[i];
            if (w == null)
                continue;

            var data = i < _data.Length ? _data[i] : null;
            var has = data != null;

            SetText(w.Number, $"{i + 1:00}");
            SetActive(w.Chapter, has);
            SetActive(w.Place, has);
            SetActive(w.Date, has);
            SetActive(w.PlayTime, has);
            if (w.PlaceIcon != null)
                w.PlaceIcon.SetActive(has);
            if (w.Empty != null)
            {
                w.Empty.gameObject.SetActive(!has);
                w.Empty.text = _emptyLabel;
            }

            if (!has)
                continue;

            SetText(w.Chapter, FormatChapter(data.chapterName));
            SetText(w.Place, data.placeName ?? ""); // 場所が分からない旧データは空 (シーン名は出さない)
            var date = SaveSystem.SavedAt(i, data);
            SetText(w.Date, date.HasValue ? date.Value.ToLocalTime().ToString("yyyy/MM/dd") : "");
            SetText(w.PlayTime, FormatPlayTime(data.playTimeSeconds));
        }
    }

    private void RenderSelection()
    {
        for (var i = 0; i < (_slots?.Length ?? 0); i++)
        {
            var w = _slots[i];
            if (w?.Root == null)
                continue;

            var selectable = i < SlotCountShown && IsSelectable(i);
            w.Root.localScale = Vector3.one * (selectable && i == _index ? _selectedScale : 1f);
            if (w.Frame != null)
                w.Frame.color = selectable ? Color.white : _disabledFrameColor;
        }

        // 見出し: ロードは枠の絵のまま。記録 (はじめから/セーブ) は差し替え
        var replaceHeading = CurrentMode != Mode.Load;
        if (_headingCover != null)
            _headingCover.SetActive(replaceHeading);
        if (_headingText != null)
        {
            _headingText.gameObject.SetActive(replaceHeading);
            _headingText.text = _confirmIndex >= 0 ? _overwriteHeading : _recordHeading;
        }
    }

    /// <summary>
    /// 章の表示: 「第1章 鳴りやまぬ遊園地」は枠に 1 行で収まらないので、最初の空白で改行して 2 行にする
    /// (文字の大きさは Chapter の TMP の自動調整で枠に収める)。
    /// </summary>
    public static string FormatChapter(string chapter)
    {
        if (string.IsNullOrEmpty(chapter))
            return "";

        var space = chapter.IndexOf(' ');
        return space > 0 ? chapter.Substring(0, space) + "\n" + chapter.Substring(space + 1) : chapter;
    }

    /// <summary>プレイ時間を 00:00:00 (時:分:秒) で表す。</summary>
    public static string FormatPlayTime(double seconds)
    {
        var total = (long)Math.Max(0, Math.Floor(seconds));
        return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void SetActive(Component c, bool active)
    {
        if (c != null)
            c.gameObject.SetActive(active);
    }

    #endregion
}
