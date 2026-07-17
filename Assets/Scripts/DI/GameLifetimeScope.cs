using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// ゲーム全体のルート LifetimeScope。
/// プレイヤー・アイテムなどシーンをまたぐ Model 群を Singleton 登録する。
/// VContainerSettings の RootLifetimeScope に本プレハブを登録しておくと、
/// 子スコープ (PlayerLifetimeScope / UILifetimeScope / PauseLifetimeScope など) の Build 時に
/// 自動で生成 (DontDestroyOnLoad) されて親になる。手動でシーンに置く必要はない。
/// </summary>
public class GameLifetimeScope : LifetimeScope
{
    [Tooltip("プレイヤーの挙動を定義する定数アセット (Model の初期値に使う)")]
    [SerializeField] private PlayerConsts _playerConsts;

    protected override void Configure(IContainerBuilder builder)
    {
        var consts = _playerConsts;
        if (consts == null)
            Debug.LogError($"[{nameof(GameLifetimeScope)}] PlayerConsts が設定されていません。Model はデフォルト値で動作します。", this);
        else
            builder.RegisterInstance(consts);

        // プレイヤー・アイテムのデータ (Model) はゲーム全体で1つ
        builder.Register(_ => new PlayerHealthModel(consts), Lifetime.Singleton);
        builder.Register(_ => new HealGaugeModel(consts), Lifetime.Singleton);
        builder.Register(_ => new PlayerItemInventoryModel(consts), Lifetime.Singleton);
        builder.Register<PlayerAttackLoadoutModel>(Lifetime.Singleton);
        builder.Register<PlayerRuntime>(Lifetime.Singleton);
    }
}
