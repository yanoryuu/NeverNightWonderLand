using UnityEngine;

/// <summary>
/// 壊れる物 (木箱・隠し壁のセル・能力ブロックのセル) の破片の絵と色 (PendingChanges 36)。
/// Resources/BreakDebrisCatalog.asset を <see cref="BreakDebris"/> が実行時に読む (シーン側の設定は不要)。
/// 絵は Area1Gen/debris/ (gpt-image-2 のグリーンバック → クロマキー → 部品ごとに切り出し)。
/// 岩の破片は明るい灰色で描いてあり、色 (Tint) を掛けて隠し壁・能力ブロックの色に合わせる。
/// </summary>
[CreateAssetMenu(menuName = "NeverNight/Break Debris Catalog", fileName = "BreakDebrisCatalog")]
public class BreakDebrisCatalog : ScriptableObject
{
    public const string ResourcePath = "BreakDebrisCatalog";

    [Header("絵")]
    [Tooltip("木箱の破片 (板・木っ端)")]
    public Sprite[] Wood;

    [Tooltip("岩・レンガの破片 (明るい灰色。Tint を掛けて使う)")]
    public Sprite[] Rock;

    [Tooltip("落下攻撃ブロック (橙) の破片")]
    public Sprite[] Slam;

    [Header("色 (岩の破片に掛ける)")]
    public Color WallTint = new Color(0.62f, 0.26f, 0.44f);
    public Color RushTint = new Color(0.74f, 0.52f, 0.96f);
    public Color SuperTint = new Color(0.52f, 0.80f, 1f);

    [Header("数")]
    public int CrateCount = 6;
    public int CellCount = 4;
}
