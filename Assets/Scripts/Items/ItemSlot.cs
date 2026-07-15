/// <summary>アイテムをセットするスロット。使用時の方向入力に対応する。</summary>
public enum ItemSlot
{
    Down = 0,
    Left = 1,
    Right = 2,
}

public static class ItemSlotExtensions
{
    public const int SlotCount = 3;

    public static string Glyph(this ItemSlot slot) => slot switch
    {
        ItemSlot.Down => "↓",
        ItemSlot.Left => "←",
        ItemSlot.Right => "→",
        _ => "",
    };

    public static string DisplayName(this ItemSlot slot) => slot switch
    {
        ItemSlot.Down => "下スロット",
        ItemSlot.Left => "左スロット",
        ItemSlot.Right => "右スロット",
        _ => slot.ToString(),
    };
}
