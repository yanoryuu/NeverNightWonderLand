using VContainer;
using VContainer.Unity;

/// <summary>
/// ホーム画面 UI (HomeUI プレハブ) に載せる子 LifetimeScope。
/// 自身の GameObject 配下へ Model 群を注入する
/// (ホームの View が PlayerRuntime や各 Model を受け取れる)。
/// 親 (GameLifetimeScope) は VContainerSettings の RootLifetimeScope 経由で自動解決される。
/// </summary>
public class HomeLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // コンテナ構築後、自分 (ホーム画面) の階層へ [Inject] を実行する
        builder.RegisterBuildCallback(container => container.InjectGameObject(gameObject));
    }
}
