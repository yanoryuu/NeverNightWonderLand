/// <summary>
/// ハサミ強化の色。鍛冶師に強化してもらうことで新しいアクションが解禁され、
/// 対応する色のリボンを切って先へ進めるようになる。
/// </summary>
public enum ScissorUpgrade
{
    /// <summary>黄色 (カブトムシの鍛冶師): 壁に張り付き、壁ジャンプできる。</summary>
    Yellow = 0,

    /// <summary>青色 (クモの鍛冶師): 特殊な糸でハサミを飛ばして移動。壁に刺さると張り付く。</summary>
    Blue = 1,

    /// <summary>赤色 (トビムシの鍛冶師): 二段ジャンプと滑空が解禁。</summary>
    Red = 2,
}

public static class ScissorUpgradeExtensions
{
    public static string DisplayName(this ScissorUpgrade upgrade) => upgrade switch
    {
        ScissorUpgrade.Yellow => "黄色のハサミ",
        ScissorUpgrade.Blue => "青色のハサミ",
        ScissorUpgrade.Red => "赤色のハサミ",
        _ => upgrade.ToString(),
    };
}
