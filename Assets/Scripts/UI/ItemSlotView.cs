using R3;
using TMPro;
using UnityEngine;

/// <summary>
/// HUD のアイテムスロット表示。3つのスロット (↓/←/→) にセットされたアイテムと残数を表示する。
/// 使用は方向入力+アイテムボタン (ホロウナイトの術形式)。残数0や空スロットは薄く表示する。
/// </summary>
public class ItemSlotView : MonoBehaviour
{
    [Tooltip("参照するインベントリ")]
    [SerializeField] private PlayerItemInventory _inventory;

    [Tooltip("各スロットの行テキスト (ItemSlot の並び順: 下/左/右)")]
    [SerializeField] private TMP_Text[] _labels;

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        if (_inventory == null || _labels == null || _labels.Length < ItemSlotExtensions.SlotCount)
        {
            Debug.LogError($"[{nameof(ItemSlotView)}] 参照が設定されていません。", this);
            return;
        }

        for (var i = 0; i < ItemSlotExtensions.SlotCount; i++)
            _inventory.SlotRP((ItemSlot)i).Subscribe(_ => RefreshAll()).AddTo(_disposables);

        foreach (var item in _inventory.Catalog)
        {
            var countRp = _inventory.CountOf(item);
            countRp?.Subscribe(_ => RefreshAll()).AddTo(_disposables);
        }
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void RefreshAll()
    {
        for (var i = 0; i < ItemSlotExtensions.SlotCount; i++)
            RefreshSlot((ItemSlot)i);
    }

    private void RefreshSlot(ItemSlot slot)
    {
        var label = _labels[(int)slot];
        if (label == null)
            return;

        var item = _inventory.GetSlotItem(slot);
        if (item == null)
        {
            label.text = $"{slot.Glyph()} ---";
            label.color = new Color(1f, 1f, 1f, 0.3f);
            return;
        }

        var count = _inventory.GetCount(item);
        label.text = $"{slot.Glyph()} {item.DisplayName}  x{count}";

        var color = item.IconColor;
        color.a = count > 0 ? 1f : 0.35f; // 使い切ったら薄く
        label.color = color;
    }
}
