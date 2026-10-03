using UnityEngine;

/// <summary>
/// 敵を倒した時に散らばる糸の粒 (<see cref="ThreadDropSpawner"/> が生成)。触れると糸が増える。
/// 1 粒ずつ入手通知の帯を出すとうるさいので帯は出さない (HUD の所持数がその場で増える)。
/// </summary>
public class ThreadPiece : LootPiece
{
    [Tooltip("この粒で増える糸の数 (生成時に設定)")]
    [SerializeField] private int _value = 1;

    public int Value
    {
        get => _value;
        set => _value = Mathf.Max(1, value);
    }

    protected override void OnCollected(PlayerItemInventory player)
    {
        if (player != null)
            player.AddThread(_value, notify: false);
    }
}
