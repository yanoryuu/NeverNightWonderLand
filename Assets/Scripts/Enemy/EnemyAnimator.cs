using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵のアニメーション制御 (任意)。敵本体または子のビジュアルに付いた Animator を駆動する。
/// - 自動パラメータ (コアの状態から毎フレーム反映):
///   Speed (float, 水平速度の絶対値) / IsHurt (bool) / IsBroken (bool) / Death (trigger)
///   … Animator に同名パラメータがある場合のみ反映され、無いものはスキップされる
/// - 攻撃などのモーションは、うごき部品から <see cref="SetTrigger"/> / <see cref="SetBool"/> /
///   <see cref="SetFloat"/> / <see cref="Play"/> で切り替える (EnemyModuleBase の Animation から使える)
/// 素材が無い敵には付けなくてよい (未装着でも動作は変わらない)。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyAnimator : MonoBehaviour
{
    // 自動反映するパラメータ名 (Animator 側に同名で追加すること)
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsHurt = Animator.StringToHash("IsHurt");
    private static readonly int ParamIsBroken = Animator.StringToHash("IsBroken");
    private static readonly int ParamDeath = Animator.StringToHash("Death");

    [Tooltip("駆動する Animator。未設定なら自身と子から自動検出する")]
    [SerializeField] private Animator _animator;

    private EnemyController _enemy;
    private Rigidbody2D _rb;
    private readonly HashSet<int> _availableParams = new();

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        _rb = GetComponent<Rigidbody2D>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogWarning($"[{nameof(EnemyAnimator)}] Animator が見つかりません。", this);
            enabled = false;
            return;
        }

        // 存在するパラメータだけ反映する (敵ごとに用意するモーションが違ってもエラーにしない)
        foreach (var param in _animator.parameters)
            _availableParams.Add(param.nameHash);
    }

    private void Update()
    {
        if (_enemy.IsDead)
            return; // Death 発火後は状態パラメータを触らない

        SetFloatHash(ParamSpeed, Mathf.Abs(_rb != null ? _rb.linearVelocity.x : 0f));
        SetBoolHash(ParamIsHurt, _enemy.IsHurt);
        SetBoolHash(ParamIsBroken, _enemy.IsBroken.CurrentValue);
    }

    /// <summary>死亡モーションを発火する (EnemyController.Die から呼ばれる)。</summary>
    public void PlayDeath() => SetTriggerHash(ParamDeath);

    #region うごき部品用 API (パラメータが無ければ何もしない)

    public bool HasParameter(string parameterName) =>
        _availableParams.Contains(Animator.StringToHash(parameterName));

    /// <summary>トリガーを発火する (攻撃モーションの開始など)。</summary>
    public void SetTrigger(string parameterName) =>
        SetTriggerHash(Animator.StringToHash(parameterName));

    /// <summary>bool パラメータを設定する (チャージ中フラグなど)。</summary>
    public void SetBool(string parameterName, bool value) =>
        SetBoolHash(Animator.StringToHash(parameterName), value);

    /// <summary>float パラメータを設定する。</summary>
    public void SetFloat(string parameterName, float value) =>
        SetFloatHash(Animator.StringToHash(parameterName), value);

    /// <summary>ステートを直接再生する (パラメータを使わない切替)。</summary>
    public void Play(string stateName)
    {
        if (_animator != null)
            _animator.Play(stateName);
    }

    #endregion

    private void SetTriggerHash(int hash)
    {
        if (_availableParams.Contains(hash))
            _animator.SetTrigger(hash);
    }

    private void SetBoolHash(int hash, bool value)
    {
        if (_availableParams.Contains(hash))
            _animator.SetBool(hash, value);
    }

    private void SetFloatHash(int hash, float value)
    {
        if (_availableParams.Contains(hash))
            _animator.SetFloat(hash, value);
    }
}
