using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

/// <summary>
/// 携帯アイテムの所持数・スロットと素材「糸」の Model (MonoBehaviour 非依存)。
/// GameLifetimeScope に Singleton 登録され、シーンをまたいで生存する。
/// カタログ (入手できる全アイテム) はプレイヤーのスポーン時に
/// <see cref="ResetForSpawn"/> で登録される。
/// </summary>
public sealed class PlayerItemInventoryModel : IDisposable
{
    // PlayerConsts 未設定時のフォールバック (アセットのデフォルト値と揃える)
    private const int DefaultRefillThreadCost = 3;

    private readonly int _refillThreadCost;

    private readonly List<ItemDefinition> _catalog = new();
    private readonly Dictionary<ItemDefinition, ReactiveProperty<int>> _counts = new();
    private readonly ReactiveProperty<ItemDefinition>[] _slots;
    private readonly ReactiveProperty<int> _thread = new(0);
    private readonly Subject<Unit> _changed = new();

    /// <summary>素材「糸」の所持数の購読用。</summary>
    public ReadOnlyReactiveProperty<int> Thread => _thread;

    /// <summary>入手できる全アイテム (メニューの一覧などに使う)。</summary>
    public IReadOnlyList<ItemDefinition> Catalog => _catalog;

    /// <summary>スロット・所持数のどれかが変化した時に発火する (HUD の一括更新用)。</summary>
    public Observable<Unit> Changed => _changed;

    public PlayerItemInventoryModel(PlayerConsts consts)
    {
        _refillThreadCost = consts != null ? consts.RefillThreadCost : DefaultRefillThreadCost;

        _slots = new ReactiveProperty<ItemDefinition>[ItemSlotExtensions.SlotCount];
        for (var i = 0; i < _slots.Length; i++)
            _slots[i] = new ReactiveProperty<ItemDefinition>(null);
    }

    /// <summary>
    /// スポーン時の初期化。カタログを登録し、所持数を 0 (未所持)・スロットを初期構成・糸を 0 に戻す
    /// (Model はシーンをまたぐため明示リセットが必要)。
    /// アイテムはゲーム進行での入手や拠点の補充で手に入れる。
    /// </summary>
    public void ResetForSpawn(IReadOnlyList<ItemDefinition> catalog, IReadOnlyList<ItemDefinition> defaultSlots)
    {
        _catalog.Clear();

        if (catalog != null)
        {
            foreach (var item in catalog)
            {
                if (item == null || _catalog.Contains(item))
                    continue;

                _catalog.Add(item);

                if (_counts.TryGetValue(item, out var count))
                    count.Value = 0;
                else
                    _counts.Add(item, new ReactiveProperty<int>(0));
            }
        }

        for (var i = 0; i < _slots.Length; i++)
        {
            var initial = (defaultSlots != null && i < defaultSlots.Count) ? defaultSlots[i] : null;
            _slots[i].Value = initial != null && _counts.ContainsKey(initial) ? initial : null;
        }

        _thread.Value = 0;
        _changed.OnNext(Unit.Default);
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
        _changed.OnNext(Unit.Default);
    }

    #endregion

    #region Counts

    /// <summary>所持数の購読用。カタログ外のアイテムなら null。</summary>
    public ReadOnlyReactiveProperty<int> CountOf(ItemDefinition item) =>
        item != null && _counts.TryGetValue(item, out var count) ? count : null;

    public int GetCount(ItemDefinition item) =>
        item != null && _counts.TryGetValue(item, out var count) ? count.Value : 0;

    /// <summary>アイテムを1つ入手する (ショップ購入・拾得用)。カタログ外や最大数なら false。</summary>
    public bool TryAddItem(ItemDefinition item)
    {
        if (item == null || !_counts.TryGetValue(item, out var count))
            return false;

        if (count.Value >= item.MaxCount)
            return false;

        count.Value++;
        _changed.OnNext(Unit.Default);
        return true;
    }

    /// <summary>指定アイテムを1消費する。所持数0なら false。</summary>
    public bool TryConsume(ItemDefinition item)
    {
        if (item == null || !_counts.TryGetValue(item, out var count) || count.Value <= 0)
            return false;

        count.Value--;
        _changed.OnNext(Unit.Default);
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

    /// <summary>糸を消費する (ショップ購入用)。足りなければ false。</summary>
    public bool TrySpendThread(int amount)
    {
        if (amount <= 0)
            return true;

        if (_thread.Value < amount)
            return false;

        _thread.Value -= amount;
        _changed.OnNext(Unit.Default);
        return true;
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

        if (_thread.Value < _refillThreadCost)
        {
            Notifier.Notify($"糸が足りない… (補充には糸 {_refillThreadCost} が必要)");
            return false;
        }

        _thread.Value -= _refillThreadCost;
        foreach (var pair in _counts)
            pair.Value.Value = pair.Key.MaxCount;

        Notifier.Notify($"アイテムを補充した (糸 -{_refillThreadCost})");
        _changed.OnNext(Unit.Default);
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
        _changed.OnNext(Unit.Default);
    }

    private ItemDefinition FindByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        foreach (var item in _catalog)
        {
            if (item != null && item.name == itemName)
                return item;
        }

        return null;
    }

    #endregion

    public void Dispose()
    {
        _thread.Dispose();
        _changed.Dispose();
        foreach (var count in _counts.Values)
            count.Dispose();
        foreach (var slot in _slots)
            slot?.Dispose();
    }
}
