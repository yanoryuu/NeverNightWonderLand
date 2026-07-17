using VContainer;
using VContainer.Unity;

/// <summary>
/// プレイヤープレハブに載せる子 LifetimeScope。
/// 自身の GameObject 配下へ Model 群を注入する ([Inject] メソッドが呼ばれる) ため、
/// ステージシーン側にスコープを置く必要はなく、プレイヤーを配置するだけでよい。
/// 親 (GameLifetimeScope) は VContainerSettings の RootLifetimeScope 経由で自動解決される。
/// </summary>
public class PlayerLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // コンテナ構築後、自分 (プレイヤー) の階層へ [Inject] を実行する
        builder.RegisterBuildCallback(container => container.InjectGameObject(gameObject));
    }
}
