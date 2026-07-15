/// <summary>
/// ハサミの攻撃スタイル。切り替え自体にも攻撃判定がある(切り替え攻撃)。
/// </summary>
public enum ScissorStyle
{
    /// <summary>二刀流。ハサミを分割した素早い攻撃。HP(赤)削り向け。</summary>
    DualBlades,

    /// <summary>両手持ち。ハサミを合体させた重い攻撃。防御値(白)削り向け。</summary>
    TwoHanded,
}
