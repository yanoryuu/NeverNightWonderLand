using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ハサミ強化 (黄/青/赤) と移動スキル (落下攻撃/大ジャンプ/横突進) の取得状況。
/// 強化は鍛冶師から Grant され、黄 = 壁張り付き/壁ジャンプ、青 = グラップル移動、
/// 赤 = 二段ジャンプ/滑空が解禁される。リボンの切断判定にも使われる。
/// スキルはメリーゴーランド後のコンテンツで入手する (現状はデバッグパネルからのみ)。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerProgression : MonoBehaviour
{
    private readonly HashSet<ScissorUpgrade> _upgrades = new();
    private readonly HashSet<PlayerSkill> _skills = new();

    /// <summary>強化を取得した時に発火する (UI・演出用)。</summary>
    public event Action<ScissorUpgrade> OnUpgradeGranted;

    public bool Has(ScissorUpgrade upgrade) => _upgrades.Contains(upgrade);

    public void Grant(ScissorUpgrade upgrade)
    {
        if (!_upgrades.Add(upgrade))
            return;

        Notifier.Notify($"{upgrade.DisplayName()}を手に入れた!");
        OnUpgradeGranted?.Invoke(upgrade);
    }

    /// <summary>強化を取り消す (デバッグ用)。</summary>
    public void Revoke(ScissorUpgrade upgrade) => _upgrades.Remove(upgrade);

    public bool HasSkill(PlayerSkill skill) => _skills.Contains(skill);

    public void GrantSkill(PlayerSkill skill)
    {
        if (!_skills.Add(skill))
            return;

        Notifier.Notify($"スキル「{skill.DisplayName()}」を手に入れた!");
    }

    /// <summary>スキルを取り消す (デバッグ用)。</summary>
    public void RevokeSkill(PlayerSkill skill) => _skills.Remove(skill);

    /// <summary>
    /// 鍛冶強化の回数 (能力の強化とは別の有償強化)。
    /// 1回ごとに近接攻撃力と最大コンボ数が伸びる (コンボは PlayerConsts.MaxComboCap まで)。
    /// </summary>
    public int ForgeLevel { get; private set; }

    /// <summary>鍛冶強化を1段階進める (鍛冶師から呼ばれる)。</summary>
    public void AddForgeLevel() => ForgeLevel++;

    /// <summary>鍛冶強化の回数を直接設定する (デバッグ用)。</summary>
    public void SetForgeLevel(int level) => ForgeLevel = Mathf.Max(0, level);

    #region Save

    public int[] CollectUpgrades() => _upgrades.Select(u => (int)u).ToArray();

    public int[] CollectSkills() => _skills.Select(s => (int)s).ToArray();

    public void LoadFrom(SaveData data)
    {
        _upgrades.Clear();
        if (data.upgrades != null)
        {
            foreach (var raw in data.upgrades)
                _upgrades.Add((ScissorUpgrade)raw);
        }

        _skills.Clear();
        if (data.skills != null)
        {
            foreach (var raw in data.skills)
                _skills.Add((PlayerSkill)raw);
        }

        ForgeLevel = Mathf.Max(0, data.forgeLevel);
    }

    #endregion
}
