using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 地図に何を見せるかの決まり (全体マップとミニマップで共通。MonoBehaviour 非依存)。
/// - 部屋: 入ったことのある部屋だけ (<see cref="MapExploration"/>)。乗り物の通路 (リフト・エレベーター) は開通フラグが立ち、
///   つながる部屋のどちらかに入っていれば出る。
/// - 通路: 両側の部屋が見えていれば出る。片側だけ見えている時も、隠し通路 (gate = hidden) でなければ出す (行き先の手がかり)。
///   開通フラグの要る通路 (ジップライン) はフラグが立つまで出さない。
/// - アイコン: 置かれた部屋が見えていれば出る。フラグ (ボス撃破・スピーカー破壊・乗り物の開通) で絵が変わる。
/// </summary>
public static class MapDrawingState
{
    /// <summary>部屋が地図に出るか。</summary>
    public static bool IsRoomVisible(MapData data, MapData.Room room)
    {
        if (room == null)
            return false;
        if (!room.IsTransport)
            return MapExploration.IsVisited(room.Id);
        if (!string.IsNullOrEmpty(room.RequiredFlag) && !GameProgress.Has(room.RequiredFlag))
            return false;

        // 乗り物の通路: つながる部屋のどちらかに入っていれば
        foreach (var link in data.Links)
        {
            if (link.A == room.Id && MapExploration.IsVisited(link.B))
                return true;
            if (link.B == room.Id && MapExploration.IsVisited(link.A))
                return true;
        }

        return false;
    }

    public static bool IsLinkVisible(MapData data, MapData.Link link)
    {
        if (!string.IsNullOrEmpty(link.RequiredFlag) && !GameProgress.Has(link.RequiredFlag))
            return false;

        var a = IsRoomVisible(data, data.GetRoom(link.A));
        var b = IsRoomVisible(data, data.GetRoom(link.B));
        if (a && b)
            return true;
        if (!a && !b)
            return false;
        if (link.Gate == "hidden")
            return false;

        // 片側だけ: 乗り物の通路へ続く線は、通路が見えるまで出さない (開通前の手がかりにしない)
        var other = data.GetRoom(a ? link.B : link.A);
        return other == null || !other.IsTransport;
    }

    /// <summary>全部の絵に反映する。</summary>
    public static void Apply(MapData data, MapDrawing drawing)
    {
        if (data == null || drawing == null)
            return;

        var rooms = data.Rooms;
        var n = Mathf.Min(rooms.Length, drawing.RoomCount);
        for (var i = 0; i < n; i++)
            drawing.SetRoomVisible(i, IsRoomVisible(data, rooms[i]));

        var links = data.Links;
        n = Mathf.Min(links.Length, drawing.LinkCount);
        for (var i = 0; i < n; i++)
            drawing.SetLinkVisible(i, IsLinkVisible(data, links[i]));

        var icons = data.Icons;
        n = Mathf.Min(icons.Length, drawing.IconCount);
        for (var i = 0; i < n; i++)
        {
            var icon = icons[i];
            var visible = IsRoomVisible(data, data.GetRoom(icon.Room));
            var done = !string.IsNullOrEmpty(icon.Flag) && GameProgress.Has(icon.Flag);
            drawing.SetIcon(i, icon.Kind, visible, done);
        }
    }

    /// <summary>今のステージのシーン名 (PlayerScene 方式なら StageLoader、無ければアクティブシーン)。</summary>
    public static string CurrentScene()
    {
        if (StageLoader.Instance != null && !string.IsNullOrEmpty(StageLoader.Instance.CurrentStageName))
            return StageLoader.Instance.CurrentStageName;
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>今のプレイヤー (PlayerRuntime → シーン検索)。</summary>
    public static PlayerController FindPlayer(PlayerRuntime runtime, ref PlayerController cache)
    {
        if (runtime != null && runtime.Current.CurrentValue != null)
            return runtime.Current.CurrentValue;
        if (cache == null)
            cache = Object.FindAnyObjectByType<PlayerController>();
        return cache;
    }
}
