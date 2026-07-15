using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ハサミ強化 (黄/青/赤) の取得状況。鍛冶師から Grant され、
/// 黄 = 攻撃時の斬撃波、青 = グラップル移動、赤 = 二段ジャンプ/滑空が解禁される。
/// リボンの切断判定にも使われる。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerProgression : MonoBehaviour
{
    [Tooltip("黄ハサミの斬撃波 Prefab")]
    [SerializeField] private Projectile _slashWavePrefab;

    private PlayerController _controller;
    private readonly HashSet<ScissorUpgrade> _upgrades = new();

    /// <summary>強化を取得した時に発火する (UI・演出用)。</summary>
    public event Action<ScissorUpgrade> OnUpgradeGranted;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    public bool Has(ScissorUpgrade upgrade) => _upgrades.Contains(upgrade);

    public void Grant(ScissorUpgrade upgrade)
    {
        if (!_upgrades.Add(upgrade))
            return;

        Notifier.Notify($"{upgrade.DisplayName()}を手に入れた!");
        OnUpgradeGranted?.Invoke(upgrade);
    }

    /// <summary>
    /// 黄ハサミ所持時、攻撃に合わせて前方へ斬撃波を飛ばす (AttackState から呼ばれる)。
    /// </summary>
    public bool TrySpawnSlashWave(Vector2 origin, int facing)
    {
        if (!Has(ScissorUpgrade.Yellow) || _slashWavePrefab == null)
            return false;

        var consts = _controller.Consts;
        var wave = Instantiate(_slashWavePrefab, origin, Quaternion.identity);
        wave.Launch(
            new Vector2(facing * consts.SlashWaveSpeed, 0f),
            gravityScale: 0f,
            lifetime: consts.SlashWaveLifetime,
            hpDamage: consts.SlashWaveHpDamage,
            guardDamage: consts.SlashWaveGuardDamage,
            source: gameObject,
            stickAsPlatform: false,
            damageLayer: consts.AttackTargetLayer,
            groundLayer: consts.GroundLayer);
        return true;
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
