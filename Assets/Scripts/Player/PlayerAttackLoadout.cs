using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;

/// <summary>
/// 攻撃方法の装備状況 (MonoBehaviour アダプタ)。データの実体は
/// <see cref="PlayerAttackLoadoutModel"/> が持ち、本クラスはカタログの提供
/// (ScriptableObject 参照) とゲームプレイ側との橋渡しを担う。
/// Model は PlayerLifetimeScope (プレハブ同梱) 経由で注入され、DI の無いシーンでは自前生成にフォールバックする。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAttackLoadout : MonoBehaviour
{
    [Tooltip("ゲームに存在する全攻撃方法の定義")]
    [SerializeField] private AttackDefinition[] _catalog;

    [Tooltip("初期状態で解放済みの攻撃方法")]
    [SerializeField] private AttackDefinition[] _defaultUnlocked;

    [Tooltip("初期装備の近接攻撃 (□)")]
    [SerializeField] private MeleeAttackDefinition _defaultMelee;

    [Tooltip("初期装備の特殊攻撃 (△)")]
    [SerializeField] private SpecialAttackDefinition _defaultSpecial;

    private PlayerAttackLoadoutModel _model;
    private bool _ownsModel;

    /// <summary>装備中の近接攻撃 (□) の購読用。</summary>
    public ReadOnlyReactiveProperty<MeleeAttackDefinition> Melee => _model.Melee;

    /// <summary>装備中の特殊攻撃 (△) の購読用。</summary>
    public ReadOnlyReactiveProperty<SpecialAttackDefinition> Special => _model.Special;

    /// <summary>装備中の近接攻撃。未装備なら null。</summary>
    public MeleeAttackDefinition CurrentMelee => _model.Melee.CurrentValue;

    /// <summary>装備中の特殊攻撃。未装備なら null。</summary>
    public SpecialAttackDefinition CurrentSpecial => _model.Special.CurrentValue;

    /// <summary>ゲームに存在する全攻撃方法。</summary>
    public IReadOnlyList<AttackDefinition> Catalog => _model.Catalog;

    [Inject]
    public void Construct(PlayerAttackLoadoutModel model)
    {
        _model = model;
    }

    private void Awake()
    {
        if (_model == null)
        {
            _model = new PlayerAttackLoadoutModel();
            _ownsModel = true;
        }

        // Model はシーンをまたいで生存するため、スポーン時に初期構成へ戻す
        _model.ResetForSpawn(_catalog, _defaultUnlocked, _defaultMelee, _defaultSpecial);
    }

    private void OnDestroy()
    {
        if (_ownsModel)
            _model.Dispose();
    }

    public bool IsUnlocked(AttackDefinition attack) => _model.IsUnlocked(attack);

    /// <summary>攻撃方法を解放する (ゲーム進行での入手)。</summary>
    public void Unlock(AttackDefinition attack) => _model.Unlock(attack);

    /// <summary>解放済みの攻撃方法を装備する (セーブポイントの入れ替えメニューから呼ばれる)。</summary>
    public bool TryEquip(AttackDefinition attack) => _model.TryEquip(attack);

    #region Save

    /// <summary>セーブデータへ装備・解放状況を書き込む。</summary>
    public void CollectTo(SaveData data)
    {
        data.equippedMeleeId = _model.EquippedMeleeId;
        data.equippedSpecialId = _model.EquippedSpecialId;
        data.unlockedAttackIds = _model.CollectUnlocked();
    }

    public void LoadFrom(SaveData data) => _model.LoadFrom(data);

    #endregion
}
