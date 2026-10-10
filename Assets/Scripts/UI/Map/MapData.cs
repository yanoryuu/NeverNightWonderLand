using System;
using UnityEngine;

/// <summary>
/// エリアの地図データ (全体マップ・ミニマップが共用)。
/// エディタで Docs/Area1/area1_layout.json と各ステージの配置 (拠点・ボス・スピーカー・乗り物) から書き出す
/// (Docs/UI/Build/build_map.cs の BuildMap.Export。手順は Docs/UI/MapScreen.md)。ゲーム中に JSON は読まない。
/// 座標はすべて全シーン共通のワールド座標 (1 マップセル = 3 単位)。
/// 地図の絵 (部屋のブロック・通路・アイコン) はこのデータからエディタで事前配置し (<see cref="MapDrawing"/>)、
/// 実行時は表示/非表示・位置・拡大だけを切り替える。
/// </summary>
[CreateAssetMenu(fileName = "MapData", menuName = "NeverNight/Map Data")]
public class MapData : ScriptableObject
{
    /// <summary>区画 (色分けの単位)。</summary>
    [Serializable]
    public class Zone
    {
        public string Id;
        public string Name;
        public Color Color = Color.white;
    }

    /// <summary>部屋 (踏破の単位)。矩形は 1 つ以上。</summary>
    [Serializable]
    public class Room
    {
        public string Id;

        [Tooltip("部屋名 (空の部屋もある)")]
        public string Name;

        [Tooltip("部屋のあるステージのシーン名。乗り物の通路 (リフト・エレベーター) は空")]
        public string Scene;

        public string Zone;

        public Rect[] Rects = Array.Empty<Rect>();

        [Tooltip("乗り物の通路だけ: このフラグが立つ (開通する) と地図に出る")]
        public string RequiredFlag;

        /// <summary>乗り物の通路 (入れない部屋。踏破ではなくフラグで出る)。</summary>
        public bool IsTransport => string.IsNullOrEmpty(Scene);

        public Vector2 Center
        {
            get
            {
                if (Rects == null || Rects.Length == 0)
                    return Vector2.zero;

                var r = Rects[0];
                foreach (var rc in Rects)
                    r = Rect.MinMaxRect(Mathf.Min(r.xMin, rc.xMin), Mathf.Min(r.yMin, rc.yMin), Mathf.Max(r.xMax, rc.xMax), Mathf.Max(r.yMax, rc.yMax));
                return r.center;
            }
        }

        public bool Contains(Vector2 world, float margin)
        {
            if (Rects == null)
                return false;

            foreach (var r in Rects)
            {
                if (world.x >= r.xMin - margin && world.x <= r.xMax + margin
                    && world.y >= r.yMin - margin && world.y <= r.yMax + margin)
                    return true;
            }

            return false;
        }
    }

    /// <summary>部屋どうしの通路 (細線)。一方通行は A → B。</summary>
    [Serializable]
    public class Link
    {
        public string A;
        public string B;

        [Tooltip("線の始点 (ワールド)")]
        public Vector2 From;

        [Tooltip("線の終点 (ワールド)")]
        public Vector2 To;

        public bool OneWay;
        public bool Zipline;

        [Tooltip("通るのに要るもの (layout の gate: hidden / slam / rush / dj / super / boss / speakers / fence)。hidden は両側を踏破するまで出さない")]
        public string Gate;

        [Tooltip("このフラグが立つまで出さない (ジップラインなど。空なら条件なし)")]
        public string RequiredFlag;
    }

    public enum IconKind
    {
        Save = 0,
        Shop = 1,
        Smith = 2,
        Boss = 3,
        Speaker = 4,
        Exit = 5,
        Zipline = 6,
        Elevator = 7,
    }

    /// <summary>地図のアイコン。部屋が見えている時に出る。</summary>
    [Serializable]
    public class Icon
    {
        public IconKind Kind;
        public string Room;
        public Vector2 Position;

        [Tooltip("表示が変わるフラグ (ボス撃破・スピーカー破壊・乗り物の開通)。空なら変わらない")]
        public string Flag;

        [Tooltip("回転 (度。出口の向き)")]
        public float Rotation;

        [Tooltip("名前 (出口の行き先など。今は表示に使っていない)")]
        public string Label;
    }

    [Tooltip("エリア名 (例: 鳴りやまぬ遊園地)")]
    [SerializeField] private string _areaName = "";

    [Tooltip("全部屋を囲む範囲 (ワールド)")]
    [SerializeField] private Rect _worldBounds;

    [SerializeField] private Zone[] _zones = Array.Empty<Zone>();
    [SerializeField] private Room[] _rooms = Array.Empty<Room>();
    [SerializeField] private Link[] _links = Array.Empty<Link>();
    [SerializeField] private Icon[] _icons = Array.Empty<Icon>();

    public string AreaName => _areaName;
    public Rect WorldBounds => _worldBounds;
    public Zone[] Zones => _zones;
    public Room[] Rooms => _rooms;
    public Link[] Links => _links;
    public Icon[] Icons => _icons;

    /// <summary>部屋 ID の添字 (無ければ -1)。</summary>
    public int IndexOfRoom(string id)
    {
        if (string.IsNullOrEmpty(id))
            return -1;

        for (var i = 0; i < _rooms.Length; i++)
        {
            if (_rooms[i].Id == id)
                return i;
        }

        return -1;
    }

    public Room GetRoom(string id)
    {
        var i = IndexOfRoom(id);
        return i >= 0 ? _rooms[i] : null;
    }

    public Zone GetZone(string id)
    {
        foreach (var z in _zones)
        {
            if (z.Id == id)
                return z;
        }

        return null;
    }

    /// <summary>このシーンの部屋が地図にあるか (このエリアのステージか)。</summary>
    public bool HasScene(string scene)
    {
        if (string.IsNullOrEmpty(scene))
            return false;

        foreach (var r in _rooms)
        {
            if (r.Scene == scene)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 位置を含む部屋の添字 (無ければ -1)。scene を渡すとそのシーンの部屋だけを探す (null なら全部屋、乗り物の通路は除く)。
    /// </summary>
    public int FindRoom(string scene, Vector2 world, float margin = 0f)
    {
        for (var i = 0; i < _rooms.Length; i++)
        {
            var r = _rooms[i];
            if (r.IsTransport && scene != null)
                continue;
            if (scene != null && r.Scene != scene)
                continue;
            if (r.Contains(world, margin))
                return i;
        }

        return -1;
    }

#if UNITY_EDITOR
    /// <summary>書き出し用 (エディタ専用)。</summary>
    public void EditorSet(string areaName, Rect bounds, Zone[] zones, Room[] rooms, Link[] links, Icon[] icons)
    {
        _areaName = areaName;
        _worldBounds = bounds;
        _zones = zones;
        _rooms = rooms;
        _links = links;
        _icons = icons;
    }
#endif
}
