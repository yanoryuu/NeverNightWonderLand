using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 地図の踏破記録 (入ったことのある部屋の ID) の静的管理。<see cref="GameProgress"/> と同じく永続する進行で、
/// セーブデータ (<see cref="SaveData.visitedRooms"/>) に保存される。旧セーブ (項目なし) は「踏破なし」として読む。
/// 部屋に入ったかの判定は <see cref="MapExplorationTracker"/> が行う。
/// </summary>
public static class MapExploration
{
    private static readonly HashSet<string> Visited = new();

    /// <summary>新しい部屋に入った時に発火する (引数は部屋 ID)。セーブの自動書き込み・地図の更新に使う。</summary>
    public static event Action<string> Changed;

    /// <summary>踏破記録が丸ごと入れ替わった時 (ロード・ニューゲーム) に発火する。</summary>
    public static event Action Reset;

    public static int Count => Visited.Count;

    public static bool IsVisited(string roomId) =>
        !string.IsNullOrEmpty(roomId) && Visited.Contains(roomId);

    /// <summary>部屋を踏破済みにする。新しく記録したら true。</summary>
    public static bool Visit(string roomId)
    {
        if (string.IsNullOrEmpty(roomId) || !Visited.Add(roomId))
            return false;

        Changed?.Invoke(roomId);
        return true;
    }

    /// <summary>セーブ用に全部屋を集める。</summary>
    public static string[] Collect() => Visited.ToArray();

    /// <summary>セーブデータから復元する (項目の無い旧セーブは踏破なし)。</summary>
    public static void LoadFrom(SaveData data)
    {
        Visited.Clear();
        if (data?.visitedRooms != null)
        {
            foreach (var id in data.visitedRooms)
            {
                if (!string.IsNullOrEmpty(id))
                    Visited.Add(id);
            }
        }

        Reset?.Invoke();
    }

    /// <summary>全消去 (ニューゲーム・デバッグ用)。</summary>
    public static void Clear()
    {
        Visited.Clear();
        Reset?.Invoke();
    }
}
