using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// アイテム画面 (装備品 / 収集品 / 資料)。完成形 item/*/装備_*.png の通りに ItemMenuUI プレハブ上で事前配置した部品を参照する。
/// - ゲーム中にキーボード Tab / パッド Select(View) で直接開く (他のメニューが開いている間・会話などでポーズ中は開かない)。
///   開いている間はゲームを止める (GamePause)。Tab / Select / Esc / B で閉じる (詳細を開いている時の Esc / B は詳細を閉じる)。
/// - タブ切替: Q / E、L1 / R1 (上部の「L 装備品 収集品 資料 R」。選択中のタブは *_select の絵で示す)。
/// - 各タブの中身は <see cref="ItemMenuTab"/> の派生 (装備品 = <see cref="ItemEquipTab"/>,
///   収集品 = <see cref="ItemCollectionTab"/>, 資料 = <see cref="ItemJournalTab"/>)。
/// 入力は他のメニューと同じ直読み (<see cref="MenuInput"/>、開閉だけ <see cref="PlayerInputSource"/>)。
/// </summary>
public class ItemMenuView : MonoBehaviour
{
    public enum Tab { Equip = 0, Collection = 1, Journal = 2 }

    [Header("参照 (プレハブ上で事前配置)")]
    [Tooltip("画面全体のルート。開閉で表示切替される")]
    [SerializeField] private GameObject _root;

    [Tooltip("上部のタブ見出し (L 装備品 収集品 資料 R)")]
    [SerializeField] private Image _header;

    [Tooltip("タブ見出しの絵 (装備品 / 収集品 / 資料 を選んでいる時: soubi_select / shushu_select / shiryou_select)")]
    [SerializeField] private Sprite[] _headerSprites = new Sprite[3];

    [Tooltip("左上の見出し札")]
    [SerializeField] private Image _titleTicket;

    [Tooltip("見出し札の絵 (soubi_title / shushu_title / shiryou_title)")]
    [SerializeField] private Sprite[] _titleSprites = new Sprite[3];

    [Tooltip("見出し札の位置 (完成形ごとの実測)")]
    [SerializeField] private Vector2[] _titlePositions = new Vector2[3];

    [Tooltip("タブの中身 (装備品 / 収集品 / 資料 の順)")]
    [SerializeField] private ItemMenuTab[] _tabs = new ItemMenuTab[3];

    [Tooltip("参照するプレイヤー (任意。未設定なら実行時に解決する)")]
    [SerializeField] private PlayerController _player;

    public bool IsOpen { get; private set; }

    public Tab CurrentTab { get; private set; }

    private PlayerRuntime _playerRuntime;
    private int _openedFrame = -1;
    private int _closedFrame = -1;
    private bool _paused;

    [Inject]
    public void Construct(PlayerRuntime playerRuntime)
    {
        _playerRuntime = playerRuntime;
    }

    /// <summary>現在のプレイヤー (Additive シーン運用でも解決できる)。</summary>
    public PlayerController Player
    {
        get
        {
            if (_playerRuntime != null && _playerRuntime.Current.CurrentValue != null)
                return _playerRuntime.Current.CurrentValue;

            if (_player == null)
                _player = FindAnyObjectByType<PlayerController>();
            return _player;
        }
    }

    private void Awake()
    {
        if (_root != null && !IsOpen)
            _root.SetActive(false);
    }

    private void OnDisable()
    {
        // シーン破棄などで閉じずに消えた時もポーズとメニュー数を戻す
        if (IsOpen)
            Close();
    }

    private void Update()
    {
        if (!IsOpen)
        {
            if (CanOpen() && PlayerInputSource.Current.ItemMenuPressed)
                Open(Tab.Equip); // 毎回「装備品」タブから開く (UIAssignment.md「3回目の決定」7)
            return;
        }

        if (Time.frameCount == _openedFrame)
            return;

        // Tab / Select はどこからでも閉じる
        if (PlayerInputSource.Current.ItemMenuPressed)
        {
            Close();
            return;
        }

        var input = MenuInput.Read();
        if (input.Tab != 0)
        {
            SwitchTab((Tab)(((int)CurrentTab + input.Tab + _tabs.Length) % _tabs.Length));
            return;
        }

        var tab = TabOf(CurrentTab);
        var cancelHandled = tab != null && tab.HandleInput(input);
        if (input.Cancel && !cancelHandled)
            Close();
    }

    /// <summary>開ける状況か: 他のメニュー・会話 (ポーズ中)・読み込み中・死亡中は開かない。</summary>
    private bool CanOpen()
    {
        if (Time.frameCount == _closedFrame || Time.frameCount == MenuPanelView.LastClosedFrame)
            return false;
        if (MenuPanelView.AnyOpen || GamePause.IsPaused)
            return false;
        if (StageLoader.Instance != null && StageLoader.Instance.IsLoading)
            return false;

        var player = Player;
        return player != null && !player.IsDead;
    }

    #region API

    public void Open(Tab tab)
    {
        if (IsOpen)
            return;

        IsOpen = true;
        _openedFrame = Time.frameCount;
        MenuPanelView.NotifyOpened();
        GamePause.Push();
        _paused = true;
        if (_root != null)
            _root.SetActive(true);

        CurrentTab = tab;
        ShowTab(tab);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        _closedFrame = Time.frameCount;
        foreach (var t in _tabs)
        {
            if (t != null)
                t.Hide();
        }

        if (_root != null)
            _root.SetActive(false);
        if (_paused)
        {
            _paused = false;
            GamePause.Pop();
        }

        MenuPanelView.NotifyClosed();
    }

    public void SwitchTab(Tab tab)
    {
        if (!IsOpen || tab == CurrentTab)
            return;

        CurrentTab = tab;
        ShowTab(tab);
    }

    #endregion

    private ItemMenuTab TabOf(Tab tab)
    {
        var i = (int)tab;
        return _tabs != null && i >= 0 && i < _tabs.Length ? _tabs[i] : null;
    }

    private void ShowTab(Tab tab)
    {
        var index = (int)tab;
        for (var i = 0; i < _tabs.Length; i++)
        {
            if (_tabs[i] != null && i != index)
                _tabs[i].Hide();
        }

        if (_header != null && index < _headerSprites.Length && _headerSprites[index] != null)
            _header.sprite = _headerSprites[index];

        if (_titleTicket != null)
        {
            if (index < _titleSprites.Length && _titleSprites[index] != null)
            {
                _titleTicket.sprite = _titleSprites[index];
                _titleTicket.rectTransform.sizeDelta = _titleSprites[index].rect.size;
            }

            if (index < _titlePositions.Length)
                _titleTicket.rectTransform.anchoredPosition = _titlePositions[index];
        }

        var t = TabOf(tab);
        if (t != null)
            t.Show(this);
    }
}
