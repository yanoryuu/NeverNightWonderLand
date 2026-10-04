using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;

/// <summary>
/// 携帯アイテムの所持数・スロットと素材「糸」の管理 (MonoBehaviour アダプタ)。
/// データの実体は <see cref="PlayerItemInventoryModel"/> が持ち、本クラスはカタログの提供
/// (ScriptableObject 参照) などシーン側との橋渡しを担う。敵の糸のドロップは ThreadDropSpawner が地面に散らす粒になり、
/// 拾った時に <see cref="AddThread"/> される。
/// Model は PlayerLifetimeScope (プレハブ同梱) 経由で注入され、DI の無いシーンでは自前生成にフォールバックする。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerItemInventory : MonoBehaviour
{
    [Tooltip("入手できる全アイテムの定義")]
    [SerializeField] private ItemDefinition[] _catalog;

    [Tooltip("初期スロット (下/左/右 の順)。null で空")]
    [SerializeField] private ItemDefinition[] _defaultSlots = new ItemDefinition[ItemSlotExtensions.SlotCount];

    private PlayerItemInventoryModel _model;
    private bool _ownsModel;

    public ReadOnlyReactiveProperty<int> Thread => _model.Thread;

    /// <summary>コインの枚数 (収集品。HUD には出さない)。</summary>
    public ReadOnlyReactiveProperty<int> Coins => _model.Coins;

    /// <summary>入手できる全アイテム (メニューの一覧などに使う)。</summary>
    public IReadOnlyList<ItemDefinition> Catalog => _model.Catalog;

    /// <summary>スロット・所持数のどれかが変化した時に発火する (セーブへの自動書き込み用)。</summary>
    public Observable<Unit> Changed => _model.Changed;

    [Inject]
    public void Construct(PlayerItemInventoryModel model)
    {
        _model = model;
    }

    private void Awake()
    {
        if (_model == null)
        {
            _model = new PlayerItemInventoryModel(GetComponent<PlayerController>().Consts);
            _ownsModel = true;
        }

        // Model はシーンをまたいで生存するため、スポーン時にカタログ登録と初期化を行う
        _model.ResetForSpawn(_catalog, _defaultSlots);
    }

    private void OnDestroy()
    {
        if (_ownsModel)
            _model.Dispose();
    }

    #region Slots

    /// <summary>スロットの購読用 (空なら null)。</summary>
    public ReadOnlyReactiveProperty<ItemDefinition> SlotRP(ItemSlot slot) => _model.SlotRP(slot);

    /// <summary>スロットにセットされたアイテム。空なら null。</summary>
    public ItemDefinition GetSlotItem(ItemSlot slot) => _model.GetSlotItem(slot);

    /// <summary>スロットにアイテムをセットする (null で外す)。</summary>
    public void SetSlot(ItemSlot slot, ItemDefinition item) => _model.SetSlot(slot, item);

    #endregion

    #region Counts

    /// <summary>所持数の購読用。カタログ外のアイテムなら null。</summary>
    public ReadOnlyReactiveProperty<int> CountOf(ItemDefinition item) => _model.CountOf(item);

    public int GetCount(ItemDefinition item) => _model.GetCount(item);

    /// <summary>アイテムを1つ入手する (ショップ購入・拾得用)。最大数なら false。</summary>
    public bool TryAddItem(ItemDefinition item) => _model.TryAddItem(item);

    /// <summary>指定アイテムを1消費する。所持数0なら false。</summary>
    public bool TryConsume(ItemDefinition item) => _model.TryConsume(item);

    #endregion

    #region Thread / Refill

    /// <summary>糸を増やす。notify = false なら入手通知の帯を出さない (敵の糸の粒など、HUD の所持数だけ増える物)。</summary>
    public void AddThread(int amount, bool notify = true) => _model.AddThread(amount, notify);

    /// <summary>コインを増やす (通知は呼び出し側)。</summary>
    public void AddCoins(int amount) => _model.AddCoins(amount);

    /// <summary>糸を消費する (ショップ購入用)。足りなければ false。</summary>
    public bool TrySpendThread(int amount) => _model.TrySpendThread(amount);

    /// <summary>
    /// 拠点でのアイテム補充 (再生成)。糸を消費して全アイテムを最大数に戻す。
    /// </summary>
    public bool TryRefillAll() => _model.TryRefillAll();

    #endregion

    #region Save

    /// <summary>所持数をセーブ用に集める (ID = アセット名)。</summary>
    public void CollectCounts(out string[] ids, out int[] counts) => _model.CollectCounts(out ids, out counts);

    /// <summary>スロットをセーブ用に集める (空は "")。</summary>
    public string[] CollectSlots() => _model.CollectSlots();

    public void LoadFrom(SaveData data) => _model.LoadFrom(data);

    #endregion
}
