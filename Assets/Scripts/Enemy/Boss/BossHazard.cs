using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスが出した「場に残る危険物」(弾・爆弾・風船の予告など) の基底。
/// 生存中のものを静的リストで管理し、第2形態への遷移や撤退の時に <see cref="ClearAll"/> で一斉に消す (弾消し)。
/// </summary>
public abstract class BossHazard : MonoBehaviour
{
    private static readonly List<BossHazard> s_active = new();

    /// <summary>発射元 (ダメージ情報の Source)。</summary>
    protected GameObject Source { get; set; }

    /// <summary>与えるダメージ。</summary>
    protected int Damage { get; set; }

    protected virtual void OnEnable() => s_active.Add(this);

    protected virtual void OnDisable() => s_active.Remove(this);

    /// <summary>消す (弾消し)。予告の印など付属物があればオーバーライドで一緒に消す。</summary>
    public virtual void Clear() => Destroy(gameObject);

    /// <summary>場にあるボスの危険物をすべて消す (第2形態への遷移・撤退時の弾消し)。</summary>
    public static void ClearAll()
    {
        for (var i = s_active.Count - 1; i >= 0; i--)
        {
            if (i >= s_active.Count)
                continue;
            var hazard = s_active[i];
            if (hazard != null)
                hazard.Clear();
        }
        s_active.Clear();
    }
}
