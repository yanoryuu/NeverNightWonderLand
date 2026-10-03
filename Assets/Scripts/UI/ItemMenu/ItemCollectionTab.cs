using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテム画面「収集品」タブ (完成形 item/shushu/装備_shushu.png / 装備_shushu_detail.png)。
/// - 18 枠 (6×3) × 3 ページ。CollectionCatalog の収集品を先頭から並べ、入手済み (フラグが立っている) の物だけ名前 (絵) を出す。未入手は空の枠
/// - 上下左右で枠を移動。上の段から上 / 下の段から下でページを移る (右端のページの点 shushu_page1〜3 と矢印)
/// - 決定で詳細 (左に絵・右に名前と説明)。BACK / NEXT (左右) で前後の入手済みの物へ、戻る (閉じる) で一覧へ
/// </summary>
public class ItemCollectionTab : ItemMenuTab
{
    public const int CellsPerPage = 18;
    private const int Columns = 6;

    [SerializeField] private CollectionCatalog _catalog;

    [Header("一覧")]
    [Tooltip("18 枠 (左上から右へ、上の段から)")]
    [SerializeField] private Cell[] _cells = new Cell[CellsPerPage];

    [Tooltip("ページの点 (右端)")]
    [SerializeField] private Image _pageDots;

    [Tooltip("ページの点の絵 (shushu_page1〜3)")]
    [SerializeField] private Sprite[] _pageSprites = new Sprite[3];

    [Header("詳細")]
    [Tooltip("詳細のルート (暗幕 shushu_black + 枠 + BACK / NEXT / 閉じる)")]
    [SerializeField] private GameObject _detailRoot;

    [Tooltip("左の枠の絵 (素材が無い間は非表示)")]
    [SerializeField] private Image _detailIcon;

    [Tooltip("左の枠: 絵が無い時に出す名前")]
    [SerializeField] private TMP_Text _detailIconName;

    [Tooltip("右の枠: 名前")]
    [SerializeField] private TMP_Text _detailName;

    [Tooltip("右の枠: 説明")]
    [SerializeField] private TMP_Text _detailText;

    [Tooltip("BACK / NEXT (前後に入手済みの物が無い時は薄くする)")]
    [SerializeField] private Graphic _backArrow;
    [SerializeField] private Graphic _nextArrow;

    [Tooltip("前後が無い時の BACK / NEXT のアルファ")]
    [Range(0f, 1f)]
    [SerializeField] private float _disabledArrowAlpha = 0.3f;

    private int _page;
    private int _index;       // ページ内の枠 (0..17)
    private int _detailEntry = -1; // 詳細で表示中の収集品 (カタログの番号)。-1 = 一覧

    private int PageCount => _pageSprites != null && _pageSprites.Length > 0 ? _pageSprites.Length : 1;

    public bool IsDetailOpen => _detailEntry >= 0;

    protected override void OnShow()
    {
        CloseDetail();
        RenderPage();
    }

    protected override void OnHide()
    {
        CloseDetail();
    }

    public override bool HandleInput(MenuInput.Frame input)
    {
        if (IsDetailOpen)
        {
            if (input.Cancel)
            {
                CloseDetail();
                return true;
            }

            if (input.MoveX != 0)
            {
                var next = FindObtained(_detailEntry, input.MoveX);
                if (next >= 0)
                    ShowDetail(next);
            }

            return false;
        }

        if (input.MoveX != 0)
        {
            var row = _index / Columns;
            var col = (_index % Columns + input.MoveX + Columns) % Columns;
            _index = row * Columns + col;
            RenderSelection();
        }

        if (input.Move != 0)
        {
            var row = _index / Columns + input.Move;
            var rows = CellsPerPage / Columns;
            if (row < 0)
            {
                // 上の段から上: 前のページの下の段へ
                if (_page > 0)
                {
                    _page--;
                    _index = (rows - 1) * Columns + _index % Columns;
                    RenderPage();
                }
            }
            else if (row >= rows)
            {
                // 下の段から下: 次のページの上の段へ
                if (_page < PageCount - 1)
                {
                    _page++;
                    _index %= Columns;
                    RenderPage();
                }
            }
            else
            {
                _index = row * Columns + _index % Columns;
                RenderSelection();
            }
        }

        if (input.Submit)
        {
            var entry = _page * CellsPerPage + _index;
            if (IsObtained(entry))
                ShowDetail(entry);
        }

        return false;
    }

    #region Render

    private CollectionCatalog.Entry EntryAt(int i) =>
        _catalog != null && i >= 0 && i < _catalog.Collectibles.Count ? _catalog.Collectibles[i] : null;

    private bool IsObtained(int i) => EntryAt(i)?.IsObtained == true;

    private void RenderPage()
    {
        if (_pageDots != null && _page < _pageSprites.Length && _pageSprites[_page] != null)
            _pageDots.sprite = _pageSprites[_page];

        for (var i = 0; i < _cells.Length; i++)
        {
            var cell = _cells[i];
            if (cell == null)
                continue;

            var entry = EntryAt(_page * CellsPerPage + i);
            var obtained = entry != null && entry.IsObtained;
            SetIcon(cell.Icon, obtained ? entry.Icon : null);
            SetText(cell.Label, obtained && entry.Icon == null ? entry.Name : "");
        }

        RenderSelection();
    }

    private void RenderSelection()
    {
        for (var i = 0; i < _cells.Length; i++)
            SetSelected(_cells[i], i == _index && !IsDetailOpen);
    }

    /// <summary>from から dir 方向 (±1) に最初の入手済みの物。無ければ -1。</summary>
    private int FindObtained(int from, int dir)
    {
        var count = _catalog != null ? _catalog.Collectibles.Count : 0;
        for (var i = from + dir; i >= 0 && i < count; i += dir)
        {
            if (IsObtained(i))
                return i;
        }

        return -1;
    }

    private void ShowDetail(int entryIndex)
    {
        var entry = EntryAt(entryIndex);
        if (entry == null)
            return;

        _detailEntry = entryIndex;
        if (_detailRoot != null)
            _detailRoot.SetActive(true);

        SetIcon(_detailIcon, entry.Icon);
        SetText(_detailIconName, entry.Icon == null ? entry.Name : "");
        SetText(_detailName, entry.Name);
        SetText(_detailText, entry.Description);
        SetArrowAlpha(_backArrow, FindObtained(entryIndex, -1) >= 0);
        SetArrowAlpha(_nextArrow, FindObtained(entryIndex, +1) >= 0);

        // 一覧の選択も表示中の物に合わせる (閉じた時にそこへ戻る)
        _page = entryIndex / CellsPerPage;
        _index = entryIndex % CellsPerPage;
        RenderPage();
    }

    private void CloseDetail()
    {
        _detailEntry = -1;
        if (_detailRoot != null)
            _detailRoot.SetActive(false);
        RenderSelection();
    }

    private void SetArrowAlpha(Graphic g, bool enabled)
    {
        if (g == null)
            return;

        var c = g.color;
        c.a = enabled ? 1f : _disabledArrowAlpha;
        g.color = c;
    }

    #endregion
}
