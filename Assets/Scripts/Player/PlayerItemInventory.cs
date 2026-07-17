using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;

/// <summary>
/// 携帯アイテムの所持数・スロットと素材「糸」の管理 (MonoBehaviour アダプタ)。
/// データの実体は <see cref="PlayerItemInventoryModel"/> が持ち、本クラスはカタログの提供
/// (ScriptableObject 参照) と敵ドロップ購読などシーン側との橋渡しを担う。
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

    /// <summary>入手できる全アイテム (メニューの一覧などに使う)。</summary>
    public IReadOnlyList<ItemDefinition> Catalog => _model.Catalog;

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
        if (_ownsModel)
            _model.Dispose();
    }

    private void OnThreadDropped(Vector2 position, int amount)
    {
        _model.AddThread(amount);
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

    /// <summary>指定アイテムを1消費する。所持数0なら false。</summary>
    public bool TryConsume(ItemDefinition item) => _model.TryConsume(item);

    #endregion

    #region Thread / Refill

    public void AddThread(int amount) => _model.AddThread(amount);

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
