using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ハサミ強化 (黄/青/赤) の取得状況。鍛冶師から Grant され、
/// 黄 = 壁張り付き/壁ジャンプ、青 = グラップル移動、赤 = 二段ジャンプ/滑空が解禁される。
/// リボンの切断判定にも使われる。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerProgression : MonoBehaviour
{
    private readonly HashSet<ScissorUpgrade> _upgrades = new();

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

    #region Save

    public int[] CollectUpgrades() => _upgrades.Select(u => (int)u).ToArray();

    public void LoadFrom(SaveData data)
    {
        _upgrades.Clear();
        if (data.upgrades == null)
            return;

        foreach (var raw in data.upgrades)
            _upgrades.Add((ScissorUpgrade)raw);
    }

    #endregion
}
