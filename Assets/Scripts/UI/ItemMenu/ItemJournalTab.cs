using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテム画面「資料」タブ (完成形 item/shiryou/装備_shiryou.png / shiryou_detail_shiryou_detail.png)。
/// - 紙の上に日誌の枠 (shiryou_frame) を縦に並べる。CollectionCatalog の資料を先頭から 1 枠ずつ、入手済みならタイトルを出す (未入手は空の枠)
/// - 上下で選択。見えている枠の外へ出たら一覧を 1 行ずつ送る (右のスクロールの目印 = 矢印 + 位置のつまみ)
/// - 決定で詳細 (暗幕の上にタイトル + 区切り + 本文)。戻る (閉じる) で一覧へ
/// - 詳細の本文が枠に入りきらない時は上下で 1 行ずつ送る (右に一覧と同じスクロールの目印。入りきる時は目印を隠す)
/// </summary>
public class ItemJournalTab : ItemMenuTab
{
    [SerializeField] private CollectionCatalog _catalog;

    [Header("一覧")]
    [Tooltip("枠を並べた中身 (スクロールで上下に動かす)")]
    [SerializeField] private RectTransform _content;

    [Tooltip("日誌の枠 (上から)")]
    [SerializeField] private Cell[] _rows = new Cell[5];

    [Tooltip("枠の間隔 (px)")]
    [SerializeField] private float _rowPitch = 175.5f;

    [Tooltip("一度に見えている枠の数 (これを超えたら送る)")]
    [SerializeField] private int _visibleRows = 4;

    [Header("スクロールの目印")]
    [Tooltip("つまみ (溝の中を動く)")]
    [SerializeField] private RectTransform _scrollThumb;

    [Tooltip("溝 (つまみの動く範囲)")]
    [SerializeField] private RectTransform _scrollTrack;

    [Header("詳細")]
    [SerializeField] private GameObject _detailRoot;
    [SerializeField] private TMP_Text _detailTitle;
    [SerializeField] private TMP_Text _detailBody;

    [Tooltip("本文の切り抜き (RectMask2D)。本文はこの上端から下へ伸び、送りで上へ動く")]
    [SerializeField] private RectTransform _bodyViewport;

    [Tooltip("本文のスクロールの目印 (矢印 + 溝 + つまみ。送れない時は隠す)")]
    [SerializeField] private GameObject _bodyScrollIndicator;

    [SerializeField] private RectTransform _bodyScrollTrack;
    [SerializeField] private RectTransform _bodyScrollThumb;

    private int _index;
    private int _scroll;
    private float? _contentBaseY; // 送り 0 の時の中身の位置 (プレハブの配置)
    private bool _detailOpen;
    private int _bodyStep;     // 本文の送り (行)
    private int _bodyMaxStep;  // 送れる最大 (行)
    private float _bodyPitch;  // 1 行の間隔 (px)

    private int Count => Mathf.Min(_rows.Length, _catalog != null ? _catalog.Journals.Count : 0);

    protected override void OnShow()
    {
        CloseDetail();
        Render();
    }

    protected override void OnHide()
    {
        CloseDetail();
    }

    public override bool HandleInput(MenuInput.Frame input)
    {
        if (_detailOpen)
        {
            if (input.Cancel)
            {
                CloseDetail();
                return true;
            }

            if (input.Move != 0 && _bodyMaxStep > 0)
            {
                var next = Mathf.Clamp(_bodyStep + input.Move, 0, _bodyMaxStep);
                if (next != _bodyStep)
                {
                    _bodyStep = next;
                    RenderBodyScroll();
                }
            }

            return false;
        }

        if (input.Move != 0 && Count > 0)
        {
            _index = Mathf.Clamp(_index + input.Move, 0, Count - 1);
            Render();
        }

        if (input.Submit)
        {
            var entry = EntryAt(_index);
            if (entry != null && entry.IsObtained)
                OpenDetail(entry);
        }

        return false;
    }

    private CollectionCatalog.Entry EntryAt(int i) =>
        _catalog != null && i >= 0 && i < _catalog.Journals.Count ? _catalog.Journals[i] : null;

    private void Render()
    {
        // 選択が見えている範囲に入るように送る
        var maxScroll = Mathf.Max(0, Count - _visibleRows);
        if (_index < _scroll) _scroll = _index;
        if (_index > _scroll + _visibleRows - 1) _scroll = _index - _visibleRows + 1;
        _scroll = Mathf.Clamp(_scroll, 0, maxScroll);
        if (_content != null)
        {
            _contentBaseY ??= _content.anchoredPosition.y;
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, _contentBaseY.Value + _scroll * _rowPitch);
        }

        for (var i = 0; i < _rows.Length; i++)
        {
            var row = _rows[i];
            if (row?.Root == null)
                continue;

            var entry = i < Count ? EntryAt(i) : null;
            row.Root.gameObject.SetActive(i < Count);
            SetText(row.Label, entry != null && entry.IsObtained ? entry.Name : "");
            SetIcon(row.Icon, null);
            SetSelected(row, i == _index);
        }

        // つまみ: 溝の長さ × 見えている割合、位置は送りの割合
        if (_scrollThumb != null && _scrollTrack != null)
        {
            var track = _scrollTrack.rect.height;
            var ratio = Count > 0 ? Mathf.Clamp01((float)_visibleRows / Count) : 1f;
            var thumb = Mathf.Max(8f, track * ratio);
            var t = maxScroll > 0 ? (float)_scroll / maxScroll : 0f;
            _scrollThumb.sizeDelta = new Vector2(_scrollThumb.sizeDelta.x, thumb);
            _scrollThumb.anchoredPosition = new Vector2(_scrollThumb.anchoredPosition.x, -(track - thumb) * t);
        }
    }

    private void OpenDetail(CollectionCatalog.Entry entry)
    {
        _detailOpen = true;
        SetText(_detailTitle, entry.Name);
        SetText(_detailBody, entry.Description);
        if (_detailRoot != null)
            _detailRoot.SetActive(true);
        MeasureBody();
        RenderBodyScroll();
    }

    /// <summary>本文の行の間隔と、枠に入りきらない行数を測る。</summary>
    private void MeasureBody()
    {
        _bodyStep = 0;
        _bodyMaxStep = 0;
        _bodyPitch = 0f;
        if (_detailBody == null || _bodyViewport == null)
            return;

        _detailBody.ForceMeshUpdate();
        var info = _detailBody.textInfo;
        if (info.lineCount <= 0)
            return;

        _bodyPitch = info.lineCount > 1
            ? info.lineInfo[0].baseline - info.lineInfo[1].baseline
            : info.lineInfo[0].lineHeight;
        if (_bodyPitch <= 0f)
            return;

        // 1 行目の上端から最後の行の下端まで
        var top = info.lineInfo[0].ascender;
        var bottom = info.lineInfo[info.lineCount - 1].descender;
        var overflow = (top - bottom) - _bodyViewport.rect.height;
        if (overflow > 0.5f)
            _bodyMaxStep = Mathf.CeilToInt(overflow / _bodyPitch);
    }

    private void RenderBodyScroll()
    {
        if (_detailBody != null)
        {
            var rt = _detailBody.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _bodyStep * _bodyPitch);
        }

        var scrollable = _bodyMaxStep > 0;
        if (_bodyScrollIndicator != null)
            _bodyScrollIndicator.SetActive(scrollable);
        if (!scrollable || _bodyScrollThumb == null || _bodyScrollTrack == null)
            return;

        // つまみ: 見えている割合の長さ、位置は送りの割合 (一覧のつまみと同じ)
        var track = _bodyScrollTrack.rect.height;
        var visibleLines = Mathf.Max(1f, _bodyViewport.rect.height / _bodyPitch);
        var ratio = Mathf.Clamp01(visibleLines / (visibleLines + _bodyMaxStep));
        var thumb = Mathf.Max(8f, track * ratio);
        var t = (float)_bodyStep / _bodyMaxStep;
        _bodyScrollThumb.sizeDelta = new Vector2(_bodyScrollThumb.sizeDelta.x, thumb);
        _bodyScrollThumb.anchoredPosition = new Vector2(_bodyScrollThumb.anchoredPosition.x, -(track - thumb) * t);
    }

    private void CloseDetail()
    {
        _detailOpen = false;
        if (_detailRoot != null)
            _detailRoot.SetActive(false);
    }
}
