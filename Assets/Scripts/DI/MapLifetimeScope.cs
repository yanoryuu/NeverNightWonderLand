using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 全体マップ (MapUI プレハブ) 用の子 LifetimeScope。
/// 地図データと View を登録し、全体マップの Presenter と踏破の判定 (<see cref="MapExplorationTracker"/>) を起動する。
/// 親 (GameLifetimeScope) は VContainerSettings の RootLifetimeScope 経由で自動解決される (PlayerRuntime を受け取る)。
/// </summary>
public class MapLifetimeScope : LifetimeScope
{
    [Tooltip("地図データ (Docs/UI/Build/build_map.cs の BuildMap.Export で書き出したもの)")]
    [SerializeField] private MapData _mapData;

    [SerializeField] private MapScreenView _mapScreenView;

    protected override void Configure(IContainerBuilder builder)
    {
        if (_mapData == null)
        {
            Debug.LogError($"[{nameof(MapLifetimeScope)}] MapData が設定されていません。全体マップと踏破の記録は動きません。", this);
            return;
        }

        builder.RegisterInstance(_mapData);
        builder.RegisterEntryPoint<MapExplorationTracker>();

        if (_mapScreenView != null)
        {
            builder.RegisterComponent(_mapScreenView).As<IMapScreenView>();
            builder.RegisterEntryPoint<MapScreenPresenter>();
        }
    }
}
