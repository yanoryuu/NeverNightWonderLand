using UnityEngine;

/// <summary>
/// 敵の攻撃1種の定義 (EnemyAttackSelectorBase の攻撃リストに Inspector で並べる)。
/// 実行時のクールダウン計時もこのインスタンスが持つ (コンポーネントごとに独立)。
/// </summary>
[System.Serializable]
public class EnemyAttack
{
    [Tooltip("識別名。選択ロジック (SelectAttack) から参照する")]
    [SerializeField] private string _name = "Attack";

    [Tooltip("発火する Animator トリガー。空なら識別名をそのまま使う")]
    [SerializeField] private string _animationTrigger = "";

    [Tooltip("攻撃モーションの継続時間 (sec)。この間は次の攻撃を選択しない")]
    [SerializeField] private float _duration = 0.6f;

    [Tooltip("この攻撃自体の再使用クールダウン (sec)。全体クールダウンとは別")]
    [SerializeField] private float _cooldown = 0f;

    private float _cooldownTimer;

    public string Name => _name;
    public string AnimationTrigger => string.IsNullOrEmpty(_animationTrigger) ? _name : _animationTrigger;
    public float Duration => _duration;

    /// <summary>この攻撃のクールダウンが明けているか。</summary>
    public bool IsReady => _cooldownTimer <= 0f;

    /// <summary>残りクールダウン (演出・デバッグ用)。</summary>
    public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

    internal void TickCooldown(float deltaTime)
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= deltaTime;
    }

    internal void BeginCooldown() => _cooldownTimer = _cooldown;
}
