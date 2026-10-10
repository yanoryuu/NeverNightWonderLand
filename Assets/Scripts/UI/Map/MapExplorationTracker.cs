using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 踏破の判定 (MonoBehaviour 非依存)。プレイヤーの位置が今のステージのどの部屋の矩形に入ったかを数フレームに 1 回調べ、
/// 入った部屋を <see cref="MapExploration"/> に記録する。ステージ側に置き物は要らない。
/// 部屋どうしのすき間 (通路) にいる間は何も記録しない。MapLifetimeScope (MapUI シーン) からエントリポイントとして起動される。
/// </summary>
public sealed class MapExplorationTracker : ITickable
{
    /// <summary>調べる間隔 (秒, 実時間)。</summary>
    private const float Interval = 0.1f;

    /// <summary>部屋の矩形の外側の遊び (ワールド単位)。</summary>
    private const float Margin = 0.5f;

    private readonly MapData _data;
    private readonly PlayerRuntime _playerRuntime;
    private PlayerController _foundPlayer;
    private float _nextCheck;

    public MapExplorationTracker(MapData data, PlayerRuntime playerRuntime)
    {
        _data = data;
        _playerRuntime = playerRuntime;
    }

    public void Tick()
    {
        if (_data == null || Time.unscaledTime < _nextCheck)
            return;

        _nextCheck = Time.unscaledTime + Interval;

        // 遷移中はプレイヤーがまだ前のステージの位置にいるので調べない
        if (StageLoader.Instance != null && StageLoader.Instance.IsLoading)
            return;

        var player = MapDrawingState.FindPlayer(_playerRuntime, ref _foundPlayer);
        if (player == null || player.IsDead)
            return;

        var index = _data.FindRoom(MapDrawingState.CurrentScene(), player.transform.position, Margin);
        if (index >= 0)
            MapExploration.Visit(_data.Rooms[index].Id);
    }
}
