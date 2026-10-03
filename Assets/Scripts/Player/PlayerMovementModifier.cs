using System;
using UnityEngine;

/// <summary>
/// プレイヤーの移動倍率 (水中の減速など)。<see cref="PlayerController.SetMovementModifier"/> で登録する。
/// 各値は PlayerConsts の対応値に掛かる倍率で、1 なら変化なし。
/// </summary>
[Serializable]
public struct PlayerMovementModifier
{
    [Tooltip("歩き/走りの水平速度の倍率")]
    public float MoveSpeed;

    [Tooltip("ジャンプ初速の倍率 (二段ジャンプにも効く)")]
    public float Jump;

    [Tooltip("重力の倍率 (小さいほどふわっと落ちる)")]
    public float Gravity;

    [Tooltip("最大落下速度の倍率")]
    public float MaxFallSpeed;

    /// <summary>変化なし (全て 1)。</summary>
    public static PlayerMovementModifier Identity => new(1f, 1f, 1f, 1f);

    public PlayerMovementModifier(float moveSpeed, float jump, float gravity, float maxFallSpeed)
    {
        MoveSpeed = moveSpeed;
        Jump = jump;
        Gravity = gravity;
        MaxFallSpeed = maxFallSpeed;
    }
}
