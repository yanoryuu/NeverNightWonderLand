using System.Collections.Generic;
using R3;
using UnityEngine;

/// <summary>
/// 携帯アイテムの所持数・スロットと素材「糸」の管理。
/// アイテムは <see cref="ItemDefinition"/> (ScriptableObject) で定義され、
/// カタログ (入手できる全アイテム) の中から3つのスロット (下/左/右) にセットして、
/// 方向入力+アイテムボタンで使う (ホロウナイトの術形式)。
/// 糸は敵撃破でドロップし、拠点でのアイテム補充 (再生成) に消費する。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerItemInventory : MonoBehaviour
{
    [Tooltip("入手できる全アイテムの定義")]
    [SerializeField] private ItemDefinition[] _catalog;

    [Tooltip("初期スロット (下/左/右 の順)。null で空")]
    [SerializeField] private ItemDefinition[] _defaultSlots = new ItemDefinition[ItemSlotExtensions.SlotCount];

    private PlayerController _controller;

    private readonly Dictionary<ItemDefinition, ReactiveProperty<int>> _counts = new();
    private readonly ReactiveProperty<ItemDefinition>[] _slots =
        new ReactiveProperty<ItemDefinition>[ItemSlotExtensions.SlotCount];
    private readonly ReactiveProperty<int> _thread = new(0);

    public ReadOnlyReactiveProperty<int> Thread => _thread;

    /// <summary>入手できる全アイテム (メニューの一覧などに使う)。</summary>
    public IReadOnlyList<ItemDefinition> Catalog => _catalog;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();

        if (_catalog != null)
        {
            foreach (var item in _catalog)
            {
                if (item != null && !_counts.ContainsKey(item))
                    _counts.Add(item, new ReactiveProperty<int>(item.MaxCount));
            }
        }

        for (var i = 0; i < _slots.Length; i++)
        {
            var initial = (_defaultSlots != null && i < _defaultSlots.Length) ? _defaultSlots[i] : null;
            _slots[i] = new ReactiveProperty<ItemDefinition>(
                initial != null && _counts.ContainsKey(initial) ? initial : null);
        }
    }

    private void OnEnable()
    {
        EnemyController.ThreadDropped += OnThreadDropped;
    }

    private void OnDisable()
    {
        EnemyController.ThreadDropped -= OnThreadDropped;
    }

    private void OnDestroy()
    {
        _thread.Dispose();
        foreach (var count in _counts.Values)
            count.Dispose();
        foreach (var slot in _slots)
            slot?.Dispose();
    }

    #region Slots

    /// <summary>スロットの購読用 (空なら null)。</summary>
    public ReadOnlyReactiveProperty<ItemDefinition> SlotRP(ItemSlot slot) => _slots[(int)slot];

    /// <summary>スロットにセットされたアイテム。空なら null。</summary>
    public ItemDefinition GetSlotItem(ItemSlot slot) => _slots[(int)slot].Value;

    /// <summary>スロットにアイテムをセットする (null で外す)。</summary>
    public void SetSlot(ItemSlot slot, ItemDefinition item)
    {
        if (item != null && !_counts.ContainsKey(item))
            return;

        _slots[(int)slot].Value = item;
    }

    #endregion

    #region Counts

    /// <summary>所持数の購読用。カタログ外のアイテムなら null。</summary>
    public ReadOnlyReactiveProperty<int> CountOf(ItemDefinition item) =>
        item != null && _counts.TryGetValue(item, out var count) ? count : null;

    public int GetCount(ItemDefinition item) =>
        item != null && _counts.TryGetValue(item, out var count) ? count.Value : 0;

    /// <summary>指定アイテムを1消費する。所持数0なら false。</summary>
    public bool TryConsume(ItemDefinition item)
    {
        if (item == null || !_counts.TryGetValue(item, out var count) || count.Value <= 0)
            return false;

        count.Value--;
        return true;
    }

    #endregion

    #region Thread / Refill

    public void AddThread(int amount)
    {
        if (amount <= 0)
            return;

        _thread.Value += amount;
        Notifier.Notify($"糸を {amount} 手に入れた (所持: {_thread.Value})");
    }

    private void OnThreadDropped(Vector2 position, int amount)
    {
        AddThread(amount);
    }

    /// <summary>
    /// 拠点でのアイテム補充 (再生成)。糸を消費して全アイテムを最大数に戻す。
    /// </summary>
    public bool TryRefillAll()
    {
        var anyMissing = false;
        foreach (var pair in _counts)
        {
            if (pair.Value.Value < pair.Key.MaxCount)
            {
                anyMissing = true;
                break;
            }
        }

        if (!anyMissing)
        {
            Notifier.Notify("アイテムは満タンだ");
            return false;
        }

        var cost = _controller.Consts.RefillThreadCost;
        if (_thread.Value < cost)
        {
            Notifier.Notify($"糸が足りない… (補充には糸 {cost} が必要)");
            return false;
        }

        _thread.Value -= cost;
        foreach (var pair in _counts)
            pair.Value.Value = pair.Key.MaxCount;

        Notifier.Notify($"アイテムを補充した (糸 -{cost})");
        return true;
    }

    #endregion

    #region Save

    /// <summary>所持数をセーブ用に集める (ID = アセット名)。</summary>
    public void CollectCounts(out string[] ids, out int[] counts)
    {
        ids = new string[_counts.Count];
        counts = new int[_counts.Count];
        var i = 0;
        foreach (var pair in _counts)
        {
            ids[i] = pair.Key.name;
            counts[i] = pair.Value.Value;
            i++;
        }
    }

    /// <summary>スロットをセーブ用に集める (空は "")。</summary>
    public string[] CollectSlots()
    {
        var slots = new string[_slots.Length];
        for (var i = 0; i < _slots.Length; i++)
            slots[i] = _slots[i].Value != null ? _slots[i].Value.name : "";
        return slots;
    }

    public void LoadFrom(SaveData data)
    {
        if (data.itemIds != null && data.itemCounts != null)
        {
            for (var i = 0; i < data.itemIds.Length && i < data.itemCounts.Length; i++)
            {
                var item = FindByName(data.itemIds[i]);
                if (item != null && _counts.TryGetValue(item, out var count))
                    count.Value = Mathf.Clamp(data.itemCounts[i], 0, item.MaxCount);
            }
        }

        if (data.slotItemIds != null)
        {
            for (var i = 0; i < _slots.Length && i < data.slotItemIds.Length; i++)
                _slots[i].Value = FindByName(data.slotItemIds[i]);
        }

        _thread.Value = Mathf.Max(0, data.thread);
    }

    private ItemDefinition FindByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || _catalog == null)
            return null;

        foreach (var item in _catalog)
        {
            if (item != null && item.name == itemName)
                return item;
        }

        return null;
    }

    #endregion
}
