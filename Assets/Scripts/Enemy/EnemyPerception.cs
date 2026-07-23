using System;
using UnityEngine;

/// <summary>
/// 敵の索敵 (プレイヤー発見判定)。敵の GameObject に付けると、
/// EnemyController.IsPlayerDetected とうごき部品のフェーズ (索敵中/発見後) がこれに従う。
/// 未装着の敵は EnemyConsts.ChaseRange の単純な距離判定にフォールバックする。
/// - 発見: 発見距離内 (必要なら視線が通っていること)
/// - 見失う: 見失う距離の外に出て記憶時間が経過 (ヒステリシスで発見/見失うのチラつきを防ぐ)
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyPerception : MonoBehaviour
{
    [Tooltip("発見距離。0 なら EnemyConsts.ChaseRange を使う")]
    [SerializeField] private float _detectRange = 0f;

    [Tooltip("見失う距離。0 なら発見距離の 1.5 倍")]
    [SerializeField] private float _loseRange = 0f;

    [Tooltip("範囲外に出てから見失うまでの時間 (sec)。追跡のしつこさ")]
    [SerializeField] private float _memoryTime = 2f;

    [Tooltip("発見に視線 (地形に遮られていないこと) を必要とするか")]
    [SerializeField] private bool _requireLineOfSight = false;

    [Tooltip("視線を遮る地形のレイヤー。未設定なら Ground")]
    [SerializeField] private LayerMask _obstacleLayer;

    private EnemyController _enemy;
    private float _memoryTimer;

    /// <summary>プレイヤーを発見しているか (発見後フェーズ)。</summary>
    public bool IsAlert { get; private set; }

    /// <summary>発見状態が変わった時に発火する (true=発見, false=見失った)。部品の初動リセットなどに使う。</summary>
    public event Action<bool> AlertChanged;

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
    }

    private void Update()
    {
        if (_enemy == null || _enemy.IsDead)
            return;

        var player = _enemy.PlayerTransform;
        if (player == null)
        {
            SetAlert(false);
            return;
        }

        var detectRange = _detectRange > 0f
            ? _detectRange
            : _enemy.Consts != null ? _enemy.Consts.ChaseRange : 0f;
        if (detectRange <= 0f)
        {
            SetAlert(false);
            return;
        }

        var loseRange = _loseRange > 0f ? _loseRange : detectRange * 1.5f;
        var distance = Vector2.Distance(transform.position, player.position);

        if (!IsAlert)
        {
            // 索敵中: 発見距離内 (+視線) で発見する
            if (distance <= detectRange && HasLineOfSight(player))
                SetAlert(true);
        }
        else
        {
            // 発見後: 見失う距離の内側にいる間は記憶を更新し、外れたら記憶時間の経過で見失う
            var inSight = distance <= loseRange && (!_requireLineOfSight || HasLineOfSight(player));
            if (inSight)
            {
                _memoryTimer = _memoryTime;
            }
            else
            {
                _memoryTimer -= Time.deltaTime;
                if (_memoryTimer <= 0f)
                    SetAlert(false);
            }
        }
    }

    private void SetAlert(bool alert)
    {
        if (IsAlert == alert)
            return;

        IsAlert = alert;
        _memoryTimer = _memoryTime;
        AlertChanged?.Invoke(alert);
    }

    private bool HasLineOfSight(Transform player)
    {
        if (!_requireLineOfSight)
            return true;

        var mask = _obstacleLayer.value != 0 ? _obstacleLayer : (LayerMask)LayerMask.GetMask("Ground");
        return !Physics2D.Linecast(transform.position, player.position, mask);
    }

    private void OnDrawGizmosSelected()
    {
        var enemy = GetComponent<EnemyController>();
        var detectRange = _detectRange > 0f
            ? _detectRange
            : enemy != null && enemy.Consts != null ? enemy.Consts.ChaseRange : 0f;
        if (detectRange <= 0f)
            return;

        // 発見距離 (赤) / 見失う距離 (橙)
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _loseRange > 0f ? _loseRange : detectRange * 1.5f);
    }
}
