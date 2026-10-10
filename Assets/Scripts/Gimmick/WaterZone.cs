using UnityEngine;

/// <summary>
/// 水中のゾーン。中にいる間はプレイヤーの移動・ジャンプ・落下が遅くなる
/// (PlayerController.SetMovementModifier で倍率を登録し、出たら外す)。
/// 任意で水流 (_current) を足せる。BuoyancyEffector2D は速度直接指定の
/// PlayerController には効かないため使わない。
/// ジャンプの高さは (ジャンプ倍率)² / 重力倍率 倍になる (既定値で 0.6²/0.4 = 0.9 倍)。
/// </summary>
[DefaultExecutionOrder(-50)] // プレイヤーの FixedUpdate より先に外力を積む
public class WaterZone : PlayerAreaZone
{
    [Tooltip("中にいる間の移動倍率 (1 で変化なし)")]
    [SerializeField] private PlayerMovementModifier _modifier = new(0.6f, 0.6f, 0.4f, 0.4f);

    [Tooltip("水流 (units/sec)。中にいる間ずっと足される。不要なら (0,0)")]
    [SerializeField] private Vector2 _current = Vector2.zero;

    protected override void OnPlayerEnter(PlayerController player)
    {
        player.SetMovementModifier(this, _modifier);
    }

    protected override void OnPlayerExit(PlayerController player)
    {
        if (player != null)
            player.RemoveMovementModifier(this);
    }

    private void FixedUpdate()
    {
        if (Player != null && _current != Vector2.zero)
            Player.AddExternalVelocity(_current);
    }
}
