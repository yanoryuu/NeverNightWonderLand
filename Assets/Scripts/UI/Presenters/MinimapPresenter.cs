using System;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// HUD のミニマップの Presenter (MonoBehaviour 非依存)。全体マップと同じ <see cref="MapData"/> と見せ方
/// (<see cref="MapDrawingState"/>) で、プレイヤーを中心に入ったことのある部屋を描く。
/// 踏破・進行フラグが変わった時だけ表示を更新し、毎フレームは位置合わせだけ。UILifetimeScope から起動される。
/// </summary>
public sealed class MinimapPresenter : IStartable, ITickable, IDisposable
{
    private readonly MapData _data;
    private readonly IMinimapView _view;
    private readonly PlayerRuntime _playerRuntime;
    private PlayerController _foundPlayer;
    private bool _dirty = true;
    private string _lastScene;

    public MinimapPresenter(MapData data, IMinimapView view, PlayerRuntime playerRuntime)
    {
        _data = data;
        _view = view;
        _playerRuntime = playerRuntime;
    }

    public void Start()
    {
        MapExploration.Changed += OnExplorationChanged;
        MapExploration.Reset += MarkDirty;
        GameProgress.Changed += OnExplorationChanged;
    }

    public void Dispose()
    {
        MapExploration.Changed -= OnExplorationChanged;
        MapExploration.Reset -= MarkDirty;
        GameProgress.Changed -= OnExplorationChanged;
    }

    private void OnExplorationChanged(string _) => _dirty = true;

    private void MarkDirty() => _dirty = true;

    public void Tick()
    {
        if (_data == null || _view.Drawing == null)
            return;

        var scene = MapDrawingState.CurrentScene();
        if (scene != _lastScene)
        {
            _lastScene = scene;
            _view.SetMapVisible(_data.HasScene(scene));
            _dirty = true; // フラグのデバッグ操作 (Unset はイベントを出さない) もステージ入替で拾う
        }

        if (_dirty)
        {
            _dirty = false;
            MapDrawingState.Apply(_data, _view.Drawing);
        }

        var player = MapDrawingState.FindPlayer(_playerRuntime, ref _foundPlayer);
        if (player != null)
            _view.Drawing.SetView(player.transform.position, _view.Zoom);
    }
}
