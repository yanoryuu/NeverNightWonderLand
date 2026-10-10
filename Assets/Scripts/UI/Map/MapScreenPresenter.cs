using System;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 全体マップ画面の Presenter (MonoBehaviour 非依存)。MapLifetimeScope からエントリポイントとして起動される。
/// - ゲーム中に M / タッチパッド / R3 で直接開く (ポーズ・アイテム画面など他のメニューが開いている間・会話でポーズ中・
///   読み込み中・死亡中は開かない。このエリアの地図に無いステージでも開かない)。
/// - 開いている間はゲームとプレイ時間を止める (GamePause + MenuPanelView の開いている数に数える)。
/// - 開いた時は現在地を中心に表示。スクロール・ズーム (段階)・現在地に戻る。M / タッチパッド / R3 / Esc / ○(B) で閉じる。
/// - 下の欄に画面中央 (カーソル) の部屋名、見えている部屋が無ければ現在地の部屋名を「エリア名 — 部屋名」で出す。
/// 入力は <see cref="MapInput"/> (直読み)、開閉だけ <see cref="PlayerInputSource"/>。
/// </summary>
public sealed class MapScreenPresenter : IStartable, ITickable, IDisposable
{
    /// <summary>現在地の部屋を探す時の矩形の遊び (ワールド単位)。</summary>
    private const float RoomMargin = 0.5f;

    private readonly MapData _data;
    private readonly IMapScreenView _view;
    private readonly PlayerRuntime _playerRuntime;
    private PlayerController _foundPlayer;

    private bool _open;
    private int _openedFrame = -1;
    private int _closedFrame = -1;
    private int _zoomLevel;
    private Vector2 _center;
    private Vector2 _playerPos;
    private int _playerRoom = -1;
    private InputDeviceKind _guideDevice;

    public MapScreenPresenter(MapData data, IMapScreenView view, PlayerRuntime playerRuntime)
    {
        _data = data;
        _view = view;
        _playerRuntime = playerRuntime;
    }

    public bool IsOpen => _open;

    public void Start()
    {
        _view.Show(false);
    }

    public void Tick()
    {
        if (!_open)
        {
            if (PlayerInputSource.Current.MapPressed && CanOpen())
                Open();
            return;
        }

        if (Time.frameCount == _openedFrame)
            return;

        // 開いたままステージの読み直しが始まったら (通常は止まっているので起きない) 閉じてポーズを戻す
        if (StageLoader.Instance != null && StageLoader.Instance.IsLoading)
        {
            Close();
            return;
        }

        var input = MapInput.Read();
        if (PlayerInputSource.Current.MapPressed || input.Close)
        {
            Close();
            return;
        }

        if (input.Zoom != 0)
            SetZoomLevel(_zoomLevel + input.Zoom);

        if (input.Recenter)
            _center = _playerPos;

        if (input.Scroll != Vector2.zero)
            _center += input.Scroll * (_view.ScrollSpeed * Time.unscaledDeltaTime / Zoom);

        Refresh();
    }

    /// <summary>開ける状況か: 他のメニュー・会話 (ポーズ中)・読み込み中・死亡中・地図の無いステージでは開かない。</summary>
    private bool CanOpen()
    {
        if (_data == null || Time.frameCount == _closedFrame || Time.frameCount == MenuPanelView.LastClosedFrame)
            return false;
        if (MenuPanelView.AnyOpen || GamePause.IsPaused)
            return false;
        if (StageLoader.Instance != null && StageLoader.Instance.IsLoading)
            return false;
        if (!_data.HasScene(MapDrawingState.CurrentScene()))
            return false;

        var player = MapDrawingState.FindPlayer(_playerRuntime, ref _foundPlayer);
        return player != null && !player.IsDead;
    }

    #region API

    public void Open()
    {
        if (_open)
            return;

        _open = true;
        _openedFrame = Time.frameCount;
        MenuPanelView.NotifyOpened();
        GamePause.Push();

        // 現在地 (開いている間はゲームが止まっているので開いた時の位置のまま)
        var player = MapDrawingState.FindPlayer(_playerRuntime, ref _foundPlayer);
        _playerPos = player != null ? (Vector2)player.transform.position : _data.WorldBounds.center;
        var scene = MapDrawingState.CurrentScene();
        var room = _data.FindRoom(scene, _playerPos, RoomMargin);
        if (room >= 0)
            _playerRoom = room;
        else if (_playerRoom >= 0 && _data.Rooms[_playerRoom].Scene != scene)
            _playerRoom = -1;

        // 今いる部屋はまだ記録されていなくても見せる (入った直後に開いた時)
        if (room >= 0)
            MapExploration.Visit(_data.Rooms[room].Id);

        MapDrawingState.Apply(_data, _view.Drawing);
        _view.Drawing.SetMarker(player != null, _playerPos);

        _center = _playerPos;
        _zoomLevel = -1;
        SetZoomLevel(_view.DefaultZoomLevel);
        _guideDevice = InputDeviceTracker.Current;
        _view.SetGuide(_guideDevice);
        _view.Show(true);
        Refresh();
    }

    public void Close()
    {
        if (!_open)
            return;

        _open = false;
        _closedFrame = Time.frameCount;
        _view.Show(false);
        GamePause.Pop();
        MenuPanelView.NotifyClosed();
    }

    #endregion

    public void Dispose()
    {
        // シーン破棄などで閉じずに消えた時もポーズとメニュー数を戻す
        Close();
    }

    private float Zoom
    {
        get
        {
            var levels = _view.ZoomLevels;
            return levels != null && levels.Length > 0 ? levels[Mathf.Clamp(_zoomLevel, 0, levels.Length - 1)] : 1f;
        }
    }

    private void SetZoomLevel(int level)
    {
        var count = _view.ZoomLevels != null ? _view.ZoomLevels.Length : 1;
        level = Mathf.Clamp(level, 0, Mathf.Max(0, count - 1));
        if (level == _zoomLevel)
            return;

        _zoomLevel = level;
        _view.SetZoomLevel(level, count);
    }

    private void Refresh()
    {
        // 地図の外へ行きすぎないように
        var b = _data.WorldBounds;
        _center = new Vector2(Mathf.Clamp(_center.x, b.xMin, b.xMax), Mathf.Clamp(_center.y, b.yMin, b.yMax));

        var drawing = _view.Drawing;
        drawing.SetView(_center, Zoom);
        drawing.SetMarkerPulse(1f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f));

        _view.SetPlace(PlaceText());

        if (InputDeviceTracker.Current != _guideDevice)
        {
            _guideDevice = InputDeviceTracker.Current;
            _view.SetGuide(_guideDevice);
        }
    }

    /// <summary>カーソル (画面中央) の部屋、無ければ現在地の部屋の「エリア名 — 部屋名」。</summary>
    private string PlaceText()
    {
        var index = _data.FindRoom(null, _center);
        if (index < 0 || !MapDrawingState.IsRoomVisible(_data, _data.Rooms[index]))
            index = _playerRoom;

        var name = index >= 0 ? _data.Rooms[index].Name : "";
        return string.IsNullOrEmpty(name) ? _data.AreaName : $"{_data.AreaName} — {name}";
    }
}
