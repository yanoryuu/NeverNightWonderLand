/// <summary>
/// メリーゴーランド後に入手する移動スキル。ハサミ強化 (ScissorUpgrade) とは別系統。
/// int 値はセーブデータに保存されるため、追記のみ可 (既存値の変更禁止)。
/// </summary>
public enum PlayerSkill
{
    /// <summary>地面落下攻撃。空中で下+攻撃。着地衝撃+落下攻撃ブロックの破壊。</summary>
    GroundSlam = 0,

    /// <summary>大ジャンプ。地上で上+ジャンプ長押し→溜め→解放。頭上の大ジャンプブロックを破壊。</summary>
    SuperJump = 1,

    /// <summary>横突進。地上で下+ダッシュ長押し→溜め→解放。正面の突進ブロックを破壊。</summary>
    ChargeRush = 2,

    /// <summary>パリィ。方向入力なしでダッシュ。受付中に受けた攻撃を無効化する。</summary>
    Parry = 3,
}

public static class PlayerSkillExtensions
{
    public static string DisplayName(this PlayerSkill skill) => skill switch
    {
        PlayerSkill.GroundSlam => "落下攻撃",
        PlayerSkill.SuperJump => "大ジャンプ",
        PlayerSkill.ChargeRush => "横突進",
        PlayerSkill.Parry => "パリィ",
        _ => skill.ToString(),
    };
}
