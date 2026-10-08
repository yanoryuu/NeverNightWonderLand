using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 梯子。範囲内で「上 + ジャンプ」(ほぼ同時、どちらが先でもよい) を押すと掴まり、上下で上り下りする
/// (重力なし・横入力では動かない)。梯子の上端にはすり抜け床 (<see cref="TopPlatform"/>) があり、
/// 登り切るとそこに立てる。上端に立って「下 + ジャンプ」で掴まって降り始める (すり抜け床から落ちるより優先)。
/// 掴まり・上り下りの処理は <see cref="PlayerController"/> / LadderState が行う。ここは範囲の情報だけを持つ。
/// 当たり判定のクエリを使わず、有効な梯子の一覧 (<see cref="Active"/>) を座標で調べる
/// (FlagActivator で梯子ごと表示/非表示にすると一覧からも出入りする)。
/// </summary>
public class ClimbableLadder : MonoBehaviour
{
    [Tooltip("梯子の高さの半分 (units)。中心 = この Transform の位置。下端 = 中心 - これ、上端 = 中心 + これ")]
    [SerializeField] private float _halfHeight = 5f;

    [Tooltip("掴める横の範囲 (梯子の中心から左右へ, units)")]
    [SerializeField] private float _grabHalfWidth = 0.6f;

    [Tooltip("上端のすり抜け床 (登り切って立つ所)。掴まっている間はプレイヤーとの衝突を切る")]
    [SerializeField] private Collider2D _topPlatform;

    private static readonly List<ClimbableLadder> _active = new();

    /// <summary>今有効な梯子の一覧。</summary>
    public static IReadOnlyList<ClimbableLadder> Active => _active;

    public float CenterX => transform.position.x;
    public float Bottom => transform.position.y - _halfHeight;
    public float Top => transform.position.y + _halfHeight;
    public float GrabHalfWidth => _grabHalfWidth;
    public Collider2D TopPlatform => _topPlatform;

    private void OnEnable() => _active.Add(this);

    private void OnDisable() => _active.Remove(this);

    /// <summary>体 (中心 x・足元の高さ) が梯子の途中にあって掴めるか。上端に立っている時は含めない。</summary>
    public bool CanGrabAt(float x, float feetY) =>
        Mathf.Abs(x - CenterX) <= _grabHalfWidth && feetY >= Bottom - 0.1f && feetY < Top - 0.15f;

    /// <summary>上端の近く (足元が上端から below 下 〜 above 上) にいるか。上端から掴まって降りる判定。</summary>
    public bool IsNearTop(float x, float feetY, float below, float above) =>
        Mathf.Abs(x - CenterX) <= _grabHalfWidth && feetY >= Top - below && feetY <= Top + above;

    /// <summary>途中を掴める梯子 (最も中心に近いもの)。無ければ null。</summary>
    public static ClimbableLadder FindGrabbable(float x, float feetY)
    {
        ClimbableLadder best = null;
        var bestDx = float.MaxValue;
        foreach (var ladder in _active)
        {
            if (ladder == null || !ladder.CanGrabAt(x, feetY))
                continue;

            var dx = Mathf.Abs(x - ladder.CenterX);
            if (dx < bestDx)
            {
                bestDx = dx;
                best = ladder;
            }
        }

        return best;
    }

    /// <summary>足元の近くに上端がある梯子。無ければ null。</summary>
    public static ClimbableLadder FindTopNear(float x, float feetY, float below, float above)
    {
        foreach (var ladder in _active)
        {
            if (ladder != null && ladder.IsNearTop(x, feetY, below, above))
                return ladder;
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        var p = transform.position;
        Gizmos.DrawWireCube(p, new Vector3(_grabHalfWidth * 2f, _halfHeight * 2f, 0f));
    }
}
