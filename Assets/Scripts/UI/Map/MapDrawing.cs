using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地図の絵 (部屋のブロック・通路・アイコン・現在地) の Passive View。全体マップ (MapUI) と HUD のミニマップが 1 つずつ持つ。
/// 絵はエディタで <see cref="MapData"/> から事前配置して保存してある (Docs/UI/Build/build_map.cs)。実行時は生成せず、
/// 表示/非表示・アイコンの絵の差し替え・中身 (Content) の位置と拡大だけを行う。
/// Content の座標 = ワールド座標 (1 単位 = 1 ワールド単位)。拡大は Content の localScale で行い、
/// アイコン・矢印・現在地は拡大しても画面上の大きさが変わらないよう逆に縮める。
/// どれを見せるかは <see cref="MapDrawingState"/> が決める。
/// </summary>
public class MapDrawing : MonoBehaviour
{
    [Serializable]
    public class RoomGraphic
    {
        public string RoomId;
        public GameObject Root;
    }

    [Serializable]
    public class LinkGraphic
    {
        public GameObject Root;

        [Tooltip("一方通行の矢印 (無ければ null)")]
        public RectTransform Arrow;
    }

    [Serializable]
    public class IconGraphic
    {
        public Image Image;
    }

    /// <summary>アイコンの種類ごとの絵 (MapData.IconKind の順)。素材の差し替えはここを付け替える。</summary>
    [Serializable]
    public class IconStyle
    {
        public string Name;
        public Sprite Normal;

        [Tooltip("フラグが立った後の絵 (ボス撃破・スピーカー破壊・乗り物の開通)。null なら Normal のまま")]
        public Sprite Done;

        public Color NormalColor = Color.white;
        public Color DoneColor = Color.gray;
    }

    [Tooltip("部屋・通路・アイコンの親 (ワールド座標で配置済み)")]
    [SerializeField] private RectTransform _content;

    [Tooltip("部屋 (MapData.Rooms と同じ並び)")]
    [SerializeField] private RoomGraphic[] _rooms = Array.Empty<RoomGraphic>();

    [Tooltip("通路 (MapData.Links と同じ並び)")]
    [SerializeField] private LinkGraphic[] _links = Array.Empty<LinkGraphic>();

    [Tooltip("アイコン (MapData.Icons と同じ並び)")]
    [SerializeField] private IconGraphic[] _icons = Array.Empty<IconGraphic>();

    [Tooltip("アイコンの絵 (MapData.IconKind の順)")]
    [SerializeField] private IconStyle[] _iconStyles = Array.Empty<IconStyle>();

    [Tooltip("現在地のマーカー (Content の中に置くと地図と一緒に動く。外に置けば固定)")]
    [SerializeField] private RectTransform _marker;

    [Tooltip("アイコン・矢印・マーカーの画面上の大きさの倍率 (拡大率に関係なく一定)")]
    [SerializeField] private float _iconScale = 1f;

    private float _appliedZoom = -1f;

    public int RoomCount => _rooms.Length;
    public int LinkCount => _links.Length;
    public int IconCount => _icons.Length;

    /// <summary>今の拡大率 (画面 px / ワールド単位)。</summary>
    public float Zoom => _content != null ? _content.localScale.x : 1f;

    public void SetRoomVisible(int index, bool visible)
    {
        if (index >= 0 && index < _rooms.Length && _rooms[index].Root != null && _rooms[index].Root.activeSelf != visible)
            _rooms[index].Root.SetActive(visible);
    }

    public void SetLinkVisible(int index, bool visible)
    {
        if (index >= 0 && index < _links.Length && _links[index].Root != null && _links[index].Root.activeSelf != visible)
            _links[index].Root.SetActive(visible);
    }

    /// <summary>アイコンの表示と状態 (done = フラグが立った後の絵)。</summary>
    public void SetIcon(int index, MapData.IconKind kind, bool visible, bool done)
    {
        if (index < 0 || index >= _icons.Length || _icons[index].Image == null)
            return;

        var image = _icons[index].Image;
        if (image.gameObject.activeSelf != visible)
            image.gameObject.SetActive(visible);
        if (!visible)
            return;

        var k = (int)kind;
        if (k < 0 || k >= _iconStyles.Length || _iconStyles[k] == null)
            return;

        var style = _iconStyles[k];
        var sprite = done && style.Done != null ? style.Done : style.Normal;
        if (sprite != null && image.sprite != sprite)
            image.sprite = sprite;
        image.color = done ? style.DoneColor : style.NormalColor;
    }

    /// <summary>ワールド座標 center が自分 (RectTransform) の中心に来るように、zoom 倍で表示する。</summary>
    public void SetView(Vector2 center, float zoom)
    {
        if (_content == null)
            return;

        zoom = Mathf.Max(0.01f, zoom);
        _content.localScale = new Vector3(zoom, zoom, 1f);
        _content.anchoredPosition = -center * zoom;

        if (!Mathf.Approximately(_appliedZoom, zoom))
        {
            _appliedZoom = zoom;
            ApplyCounterScale(zoom);
        }
    }

    /// <summary>現在地のマーカー (Content の中ならワールド座標に置く)。</summary>
    public void SetMarker(bool visible, Vector2 world)
    {
        if (_marker == null)
            return;

        if (_marker.gameObject.activeSelf != visible)
            _marker.gameObject.SetActive(visible);
        if (visible && _marker.parent == _content)
            _marker.anchoredPosition = world;
    }

    /// <summary>マーカーの見た目の大きさ (点滅などの演出用。1 = 通常)。</summary>
    public void SetMarkerPulse(float scale)
    {
        if (_marker == null)
            return;

        var s = _iconScale * scale * (_marker.parent == _content ? 1f / Mathf.Max(0.01f, Zoom) : 1f);
        _marker.localScale = new Vector3(s, s, 1f);
    }

    private void ApplyCounterScale(float zoom)
    {
        var s = _iconScale / zoom;
        var v = new Vector3(s, s, 1f);
        foreach (var icon in _icons)
        {
            if (icon.Image != null)
                icon.Image.rectTransform.localScale = v;
        }

        foreach (var link in _links)
        {
            if (link.Arrow != null)
                link.Arrow.localScale = v;
        }

        if (_marker != null && _marker.parent == _content)
            _marker.localScale = v;
    }

#if UNITY_EDITOR
    /// <summary>ビルダー用 (エディタ専用)。</summary>
    public void EditorSet(RectTransform content, RoomGraphic[] rooms, LinkGraphic[] links, IconGraphic[] icons,
        IconStyle[] styles, RectTransform marker, float iconScale)
    {
        _content = content;
        _rooms = rooms;
        _links = links;
        _icons = icons;
        _iconStyles = styles;
        _marker = marker;
        _iconScale = iconScale;
    }
#endif
}
