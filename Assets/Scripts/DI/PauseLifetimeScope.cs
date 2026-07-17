using VContainer;
using VContainer.Unity;

/// <summary>
/// ポーズ UI (PauseUI プレハブ) に載せる子 LifetimeScope。
/// 自身の GameObject 配下へ Model 群を注入する
/// (PauseMenuView が PlayerRuntime を受け取り、シーンをまたいでプレイヤーを解決できる)。
/// 親 (GameLifetimeScope) は VContainerSettings の RootLifetimeScope 経由で自動解決される。
/// </summary>
public class PauseLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // コンテナ構築後、自分 (ポーズ UI) の階層へ [Inject] を実行する
        builder.RegisterBuildCallback(container => container.InjectGameObject(gameObject));
    }
}
