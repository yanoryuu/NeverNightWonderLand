using UnityEngine;

/// <summary>
/// コイン (決まった箱にだけ入っている収集物。ドラクエの「ちいさなメダル」のような物)。
/// 箱 (<see cref="BreakableBox"/> の _coinId) を壊すと飛び出し、触れると 1 枚増える。
/// 取ったら GameProgress のフラグ (<see cref="CoinId"/>, 例 A1_Coin_T3c_1) が立ち、その箱からは二度と出ない。
/// 枚数は PlayerItemInventory (セーブの coins) に数え、入手通知とアイテム画面の収集品に出す (HUD には出さない)。
/// </summary>
public class CoinPiece : LootPiece
{
    [Tooltip("取得の永続化フラグ (GameProgress)。生成した箱が設定する")]
    [SerializeField] private string _coinId = "";

    public string CoinId
    {
        get => _coinId;
        set => _coinId = value ?? "";
    }

    protected override void OnCollected(PlayerItemInventory player)
    {
        if (!string.IsNullOrEmpty(_coinId))
            GameProgress.Set(_coinId);

        if (player == null)
            return;

        player.AddCoins(1);
        Notifier.Notify($"コインを手に入れた (計 {player.Coins.CurrentValue} 枚)");
    }
}
