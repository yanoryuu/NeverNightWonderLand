using TMPro;
using UnityEngine;

/// <summary>アイテムスロット表示の Passive View 抽象 (Presenter が参照する)。</summary>
public interface IItemSlotView
{
    /// <summary>表示できるスロット行数。</summary>
    int SlotCount { get; }

    /// <summary>スロット行のテキストと色を表示に反映する。</summary>
    void SetSlotLabel(int index, string text, Color color);
}

/// <summary>
/// HUD のアイテムスロット表示。3つのスロット (↓/←/→) の行を表示するのみで、
/// インベントリの購読とテキスト整形は ItemSlotPresenter が行う。
/// </summary>
public class ItemSlotView : MonoBehaviour, IItemSlotView
{
    [Tooltip("各スロットの行テキスト (ItemSlot の並び順: 下/左/右)")]
    [SerializeField] private TMP_Text[] _labels;

    public int SlotCount => _labels != null ? _labels.Length : 0;

    public void SetSlotLabel(int index, string text, Color color)
    {
        if (_labels == null || index < 0 || index >= _labels.Length || _labels[index] == null)
            return;

        _labels[index].text = text;
        _labels[index].color = color;
    }
}
