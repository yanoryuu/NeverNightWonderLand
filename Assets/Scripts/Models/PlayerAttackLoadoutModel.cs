using System;
using System.Collections.Generic;
using R3;

/// <summary>
/// 攻撃方法の装備状況の Model (MonoBehaviour 非依存)。
/// 装備枠は □=近接 / △=特殊 の2つ。解放済み (Unlock) の攻撃方法の中から
/// セーブポイントで入れ替えられる (TryEquip)。
/// GameLifetimeScope に Singleton 登録され、シーンをまたいで生存する。
/// </summary>
public sealed class PlayerAttackLoadoutModel : IDisposable
{
    private readonly List<AttackDefinition> _catalog = new();
    private readonly HashSet<AttackDefinition> _unlocked = new();
    private readonly ReactiveProperty<MeleeAttackDefinition> _melee = new(null);
    private readonly ReactiveProperty<SpecialAttackDefinition> _special = new(null);
    private readonly Subject<Unit> _changed = new();

    /// <summary>装備中の近接攻撃 (□) の購読用。</summary>
    public ReadOnlyReactiveProperty<MeleeAttackDefinition> Melee => _melee;

    /// <summary>装備中の特殊攻撃 (△) の購読用。</summary>
    public ReadOnlyReactiveProperty<SpecialAttackDefinition> Special => _special;

    /// <summary>ゲームに存在する全攻撃方法 (メニューの一覧に使う)。</summary>
    public IReadOnlyList<AttackDefinition> Catalog => _catalog;

    /// <summary>装備・解放状況のどれかが変化した時に発火する。</summary>
    public Observable<Unit> Changed => _changed;

    /// <summary>
    /// スポーン時の初期化。カタログと初期解放・初期装備を登録する
    /// (Model はシーンをまたぐため明示リセットが必要。セーブ適用は LoadFrom が上書きする)。
    /// </summary>
    public void ResetForSpawn(
        IReadOnlyList<AttackDefinition> catalog,
        IReadOnlyList<AttackDefinition> defaultUnlocked,
        MeleeAttackDefinition defaultMelee,
        SpecialAttackDefinition defaultSpecial)
    {
        _catalog.Clear();
        _unlocked.Clear();

        if (catalog != null)
        {
            foreach (var attack in catalog)
            {
                if (attack != null && !_catalog.Contains(attack))
                    _catalog.Add(attack);
            }
        }

        if (defaultUnlocked != null)
        {
            foreach (var attack in defaultUnlocked)
            {
                if (attack != null && _catalog.Contains(attack))
                    _unlocked.Add(attack);
            }
        }

        _melee.Value = defaultMelee != null && _unlocked.Contains(defaultMelee) ? defaultMelee : null;
        _special.Value = defaultSpecial != null && _unlocked.Contains(defaultSpecial) ? defaultSpecial : null;
        _changed.OnNext(Unit.Default);
    }

    public bool IsUnlocked(AttackDefinition attack) => attack != null && _unlocked.Contains(attack);

    /// <summary>攻撃方法を解放する (ゲーム進行での入手)。カタログ外は無視。</summary>
    public void Unlock(AttackDefinition attack)
    {
        if (attack == null || !_catalog.Contains(attack) || !_unlocked.Add(attack))
            return;

        Notifier.Notify($"新しい攻撃方法「{attack.DisplayName}」を習得した!");
        _changed.OnNext(Unit.Default);
    }

    /// <summary>
    /// 解放済みの攻撃方法を装備する。近接/特殊は型で自動的に対応する枠へ入る。
    /// 装備できたら true。
    /// </summary>
    public bool TryEquip(AttackDefinition attack)
    {
        if (!IsUnlocked(attack))
            return false;

        switch (attack)
        {
            case MeleeAttackDefinition melee:
                _melee.Value = melee;
                break;
            case SpecialAttackDefinition special:
                _special.Value = special;
                break;
            default:
                return false;
        }

        _changed.OnNext(Unit.Default);
        return true;
    }

    #region Save

    /// <summary>解放済みの攻撃方法をセーブ用に集める (ID = アセット名)。</summary>
    public string[] CollectUnlocked()
    {
        var ids = new string[_unlocked.Count];
        var i = 0;
        foreach (var attack in _unlocked)
            ids[i++] = attack.name;
        return ids;
    }

    public string EquippedMeleeId => _melee.Value != null ? _melee.Value.name : "";
    public string EquippedSpecialId => _special.Value != null ? _special.Value.name : "";

    public void LoadFrom(SaveData data)
    {
        if (data.unlockedAttackIds != null)
        {
            _unlocked.Clear();
            foreach (var id in data.unlockedAttackIds)
            {
                var attack = FindByName(id);
                if (attack != null)
                    _unlocked.Add(attack);
            }
        }

        if (FindByName(data.equippedMeleeId) is MeleeAttackDefinition melee && _unlocked.Contains(melee))
            _melee.Value = melee;
        if (FindByName(data.equippedSpecialId) is SpecialAttackDefinition special && _unlocked.Contains(special))
            _special.Value = special;

        _changed.OnNext(Unit.Default);
    }

    private AttackDefinition FindByName(string attackName)
    {
        if (string.IsNullOrEmpty(attackName))
            return null;

        foreach (var attack in _catalog)
        {
            if (attack != null && attack.name == attackName)
                return attack;
        }

        return null;
    }

    #endregion

    public void Dispose()
    {
        _melee.Dispose();
        _special.Dispose();
        _changed.Dispose();
    }
}
