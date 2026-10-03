using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーが中にいる間だけ効くトリガーゾーンの共通基底 (横風・水中・接触ダメージ)。
/// プレイヤーが複数のコライダーを持っていても、入った/出たは1回ずつ通知する。
/// トリガーの Collider2D (isTrigger は Awake で自動で立てる) を同じ GameObject に置く。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class PlayerAreaZone : MonoBehaviour
{
    private readonly HashSet<Collider2D> _insideColliders = new();

    /// <summary>中にいるプレイヤー (いなければ null)。</summary>
    protected PlayerController Player { get; private set; }

    protected Collider2D ZoneCollider { get; private set; }

    protected virtual void Awake()
    {
        ZoneCollider = GetComponent<Collider2D>();
        ZoneCollider.isTrigger = true;
    }

    protected virtual void OnDisable()
    {
        // 無効化・破棄された時は「出た」扱いにする (倍率の解除漏れを防ぐ)
        if (Player != null)
        {
            var player = Player;
            Player = null;
            OnPlayerExit(player);
        }

        _insideColliders.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null || !_insideColliders.Add(other))
            return;

        if (Player == null)
        {
            Player = player;
            OnPlayerEnter(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_insideColliders.Remove(other) || _insideColliders.Count > 0 || Player == null)
            return;

        var player = Player;
        Player = null;
        OnPlayerExit(player);
    }

    protected virtual void OnPlayerEnter(PlayerController player) { }

    protected virtual void OnPlayerExit(PlayerController player) { }
}
