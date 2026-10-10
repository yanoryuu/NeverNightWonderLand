using Spine.Unity;
using UnityEngine;

/// <summary>
/// プレイヤーの見た目を Spine (SkeletonAnimation) で描画するためのブリッジ。
/// <see cref="PlayerController"/> は状態名を直接指定せず Animator のパラメータだけを更新する設計なので、
/// 本クラスはそのパラメータを読み取り、対応する Spine アニメーションへ振り分ける。
/// 攻撃 → ダッシュ → 空中 → 地上 の優先順位で判定する (Warrior.controller の AnyState 遷移と同じ)。
/// Animator のパラメータで足りない情報 (近接コンボの段・二段ジャンプ) だけは <see cref="PlayerController"/> から直接読む。
/// ・近接コンボ: 段ごとに攻撃モーションを切り替え、振りの長さに合わせて再生速度を変える
/// ・空中: 上昇 → 頂点 → 落下、二段ジャンプは専用モーション
/// ・止まった状態で着地したら着地モーションを 1 回流す (動き出せば途中で切り替える)
///
/// 併せて、既存コードが SpriteRenderer に対して行う演出を骨格側へ中継する。
/// ・色の変更 (溜め・パリィ) → Skeleton の頂点カラー
/// ・表示 ON/OFF (無敵点滅) → MeshRenderer の有効/無効
/// 向きは <see cref="PlayerController"/> がルートの localScale.x を反転させるため、
/// 子である骨格は自動的に追従する。本クラスでは何もしない。
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpineVisual : MonoBehaviour
{
    #region Inspector

    [Header("描画先")]
    [Tooltip("子オブジェクトの SkeletonAnimation。未設定なら子から自動取得する")]
    [SerializeField] private SkeletonAnimation _skeleton;

    [Header("Spine アニメーション名")]
    [Tooltip("骨格に存在しない名前を入れた場合は待機用へフォールバックする。空欄も同じ扱い")]
    [SerializeField] private string _idleAnimation = "Itsuku_idle_normal";
    [SerializeField] private string _runAnimation = "Itsuku_run";
    [SerializeField] private string _jumpAnimation = "Itsuku_jump";
    [Tooltip("ジャンプの頂点付近 (上下の速度が小さい間)。骨格に無ければ落下用を使う")]
    [SerializeField] private string _apexAnimation = "Itsuku_apex";
    [Tooltip("落下中。ループ再生する")]
    [SerializeField] private string _fallAnimation = "Itsuku_fall";
    [Tooltip("止まった状態で着地した時に 1 回流す")]
    [SerializeField] private string _landingAnimation = "Itsuku_landing";
    [Tooltip("二段ジャンプした時に流す。落下に移るまで続ける")]
    [SerializeField] private string _doubleJumpAnimation = "itshuku_jump_double";
    [Tooltip("近接コンボの段ごとのモーション。段数が多い時は先頭から繰り返す")]
    [SerializeField] private string[] _comboAttackAnimations = { "Itsuku_attack_1", "Itsuku_attack_2", "Itsuku_attack_3" };
    [Tooltip("近接コンボ以外の攻撃 (特殊攻撃・裁断・アイテム投げなど) 中のモーション")]
    [SerializeField] private string _attackAnimation = "";
    [SerializeField] private string _dashAnimation = "Itsuku_rush";

    [Header("挙動")]
    [Tooltip("待機用アニメーションが移動用と同じ場合、待機中は先頭フレームで静止させる")]
    [SerializeField] private bool _freezeIdleWhenShared = true;

    [Tooltip("ジャンプ・落下・攻撃・ダッシュをループさせず 1 回だけ再生し、終端ポーズで保持する。待機・移動は常にループ")]
    [SerializeField] private bool _holdOneShotAnimations = true;

    [Tooltip("この速度を超えたら移動アニメーションに切り替える。Warrior.controller の閾値と同じ")]
    [SerializeField] private float _runSpeedThreshold = 0.1f;

    [Tooltip("上昇中と判定する Y 速度。Warrior.controller の閾値と同じ")]
    [SerializeField] private float _riseVelocity = 3f;

    [Tooltip("落下中と判定する Y 速度。Warrior.controller の閾値と同じ")]
    [SerializeField] private float _fallVelocity = -3f;

    [Tooltip("アニメーション切り替え時のブレンド時間 (sec)")]
    [SerializeField] private float _mixDuration = 0.12f;

    [Tooltip("近接コンボのモーションへ切り替える時のブレンド時間 (sec)。振りが短いので短めにする")]
    [SerializeField] private float _attackMixDuration = 0.04f;

    [Tooltip("近接コンボのモーションを、振りの長さ (攻撃プロファイルの Duration) に収まる速さで再生する")]
    [SerializeField] private bool _fitAttackToSwing = true;

    [Tooltip("この時間 (sec) 以上空中にいてから着地した時だけ着地モーションを流す。小さな段差で毎回流れないように")]
    [SerializeField] private float _landingMinAirTime = 0.2f;

    #endregion

    #region Fields

    // PlayerController が書き込むパラメータ。名前は PlayerController 側と一致させること
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int ParamYVelocity = Animator.StringToHash("YVelocity");
    private static readonly int ParamIsDashing = Animator.StringToHash("IsDashing");
    private static readonly int ParamIsAttacking = Animator.StringToHash("IsAttacking");

    private Animator _animator;
    private PlayerController _player;
    private SpriteRenderer _sprite;
    private MeshRenderer _meshRenderer;

    // 直前に指示したアニメーション。毎フレーム SetAnimation を呼ばないための比較用
    private string _currentAnimation;
    private bool _currentFrozen;

    private bool _wasGrounded = true;
    private float _airTime;
    private bool _landingActive;      // 着地モーションを流している間 true
    private bool _doubleJumpActive;   // 二段ジャンプから落下に移るまで true
    private int _lastSwingCount;
    private int _lastDoubleJumpCount;

    #endregion

    #region Unity

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _player = GetComponentInParent<PlayerController>();
        _sprite = GetComponent<SpriteRenderer>();

        if (_skeleton == null)
        {
            _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        }

        if (_skeleton == null)
        {
            Debug.LogError($"{nameof(PlayerSpineVisual)}: SkeletonAnimation が見つかりません。子に Spine の骨格を配置してください", this);
            enabled = false;
            return;
        }

        _meshRenderer = _skeleton.GetComponent<MeshRenderer>();

        // Warrior.controller のクリップは SpriteRenderer.m_Sprite 自体をアニメーションさせるため、
        // 絵を空にしても毎フレーム旧スプライトが差し込み直されてしまう。
        // SpriteRenderer は RequireComponent で外せないので、描画だけを止める。
        // enabled は無敵点滅が書き換えるので触らず、forceRenderingOff を使う
        _sprite.forceRenderingOff = true;
    }

    private void Start()
    {
        // Spine 側の初期化は SkeletonAnimation の Awake/Start で完了する
        if (_player != null)
        {
            _lastSwingCount = _player.MeleeSwingCount;
            _lastDoubleJumpCount = _player.DoubleJumpCount;
        }

        ApplyAnimation(ResolveIdle(), IsIdleFrozen(), immediate: true);
    }

    private void LateUpdate()
    {
        if (_skeleton == null || _skeleton.AnimationState == null)
        {
            return;
        }

        UpdateAnimation();
        MirrorSpriteRendererEffects();
    }

    #endregion

    #region Animation

    /// <summary>
    /// Animator のパラメータ (と PlayerController の近接コンボ・二段ジャンプ) から再生すべき Spine アニメーションを決める。
    /// 判定順は Warrior.controller の AnyState 遷移と同じ (攻撃 → ダッシュ → 空中 → 地上)。
    /// </summary>
    private void UpdateAnimation()
    {
        var speed = _animator.GetFloat(ParamSpeed);
        var yVelocity = _animator.GetFloat(ParamYVelocity);
        var isGrounded = _animator.GetBool(ParamIsGrounded);
        var isDashing = _animator.GetBool(ParamIsDashing);
        var isAttacking = _animator.GetBool(ParamIsAttacking);

        UpdateAirTracking(isGrounded, yVelocity);

        string wanted;
        var restart = false;
        var timeScale = 1f;
        var mix = _mixDuration;

        var swingIndex = _player != null ? _player.MeleeSwingIndex : -1;
        if (swingIndex >= 0 && _comboAttackAnimations != null && _comboAttackAnimations.Length > 0)
        {
            wanted = _comboAttackAnimations[swingIndex % _comboAttackAnimations.Length];
            mix = _attackMixDuration;
            _landingActive = false;

            // 段が変わったら同じモーションでも頭から振り直す
            if (_player.MeleeSwingCount != _lastSwingCount)
            {
                _lastSwingCount = _player.MeleeSwingCount;
                restart = true;
            }

            if (_fitAttackToSwing && _player.MeleeSwingDuration > 0f)
            {
                var clip = _skeleton.Skeleton?.Data?.FindAnimation(wanted);
                if (clip != null && clip.Duration > 0f)
                    timeScale = clip.Duration / _player.MeleeSwingDuration;
            }
        }
        else if (isAttacking)
        {
            wanted = _attackAnimation;
            _landingActive = false;
        }
        else if (isDashing)
        {
            wanted = _dashAnimation;
            _landingActive = false;
        }
        else if (!isGrounded)
        {
            wanted = ResolveAirborne(yVelocity);
        }
        else if (speed > _runSpeedThreshold)
        {
            wanted = _runAnimation;
            _landingActive = false;
        }
        else
        {
            wanted = _idleAnimation;
            if (_landingActive && Exists(_landingAnimation))
            {
                // 着地モーションを流し終えたら待機へ戻す
                var current = _skeleton.AnimationState.GetTrack(0);
                if (_currentAnimation == _landingAnimation && current != null && current.IsComplete)
                    _landingActive = false;
                else
                    wanted = _landingAnimation;
            }
        }

        // 骨格に無いアニメーションを指定された場合は待機用へ落とす
        var frozen = false;
        if (!Exists(wanted))
        {
            wanted = ResolveIdle();
            frozen = IsIdleFrozen();
        }
        else if (wanted == _idleAnimation && isGrounded && speed <= _runSpeedThreshold)
        {
            frozen = IsIdleFrozen();
        }

        ApplyAnimation(wanted, frozen, immediate: false, restart, timeScale, mix);
    }

    /// <summary>空中のモーション。二段ジャンプ → 上昇 → 頂点 → 落下 の順で、骨格に無いものは次の候補へ落とす。</summary>
    private string ResolveAirborne(float yVelocity)
    {
        if (_doubleJumpActive && Exists(_doubleJumpAnimation))
            return _doubleJumpAnimation;

        if (yVelocity > _riseVelocity && Exists(_jumpAnimation))
            return _jumpAnimation;

        if (yVelocity >= _fallVelocity && Exists(_apexAnimation))
            return _apexAnimation;

        return Exists(_fallAnimation) ? _fallAnimation : _jumpAnimation;
    }

    /// <summary>着地モーション・二段ジャンプモーションを流すかどうかを、接地の変化から決める。</summary>
    private void UpdateAirTracking(bool isGrounded, float yVelocity)
    {
        if (isGrounded)
        {
            if (!_wasGrounded && _airTime >= _landingMinAirTime)
                _landingActive = true;

            _airTime = 0f;
            _doubleJumpActive = false;
        }
        else
        {
            _airTime += Time.deltaTime;
            _landingActive = false;

            // 落下に移ったら二段ジャンプのモーションは終わり
            if (yVelocity < _fallVelocity)
                _doubleJumpActive = false;
        }

        if (_player != null && _player.DoubleJumpCount != _lastDoubleJumpCount)
        {
            _lastDoubleJumpCount = _player.DoubleJumpCount;
            _doubleJumpActive = true;
        }

        _wasGrounded = isGrounded;
    }

    private void ApplyAnimation(string animationName, bool frozen, bool immediate,
        bool restart = false, float timeScale = 1f, float? mix = null)
    {
        if (!Exists(animationName))
        {
            return;
        }

        if (!restart && animationName == _currentAnimation && frozen == _currentFrozen)
        {
            return;
        }

        var loop = !frozen && IsLooping(animationName);
        var entry = _skeleton.AnimationState.SetAnimation(0, animationName, loop);
        if (entry != null)
        {
            entry.MixDuration = immediate ? 0f : mix ?? _mixDuration;

            if (frozen)
            {
                // 待機用の専用アニメーションが無いので、移動モーションの先頭で静止させる
                entry.TrackTime = 0f;
                entry.TimeScale = 0f;
            }
            else
            {
                entry.TimeScale = timeScale;
            }
        }

        _currentAnimation = animationName;
        _currentFrozen = frozen;
    }

    /// <summary>待機用アニメーション名。空なら骨格の先頭アニメーションを使う。</summary>
    private string ResolveIdle()
    {
        if (Exists(_idleAnimation))
        {
            return _idleAnimation;
        }

        var animations = _skeleton.Skeleton?.Data?.Animations;
        return animations != null && animations.Count > 0 ? animations.Items[0].Name : null;
    }

    /// <summary>
    /// 待機用と移動用が同じアニメーションのときだけ、待機中を静止扱いにする。
    /// 待機専用モーションが追加されたら自動的に通常再生へ戻る。
    /// </summary>
    private bool IsIdleFrozen()
    {
        return _freezeIdleWhenShared && _idleAnimation == _runAnimation;
    }

    /// <summary>
    /// 待機・移動・落下はループ再生、それ以外 (ジャンプ・頂点・着地・攻撃・ダッシュ) は 1 回再生で終端保持。
    /// </summary>
    private bool IsLooping(string animationName)
    {
        if (!_holdOneShotAnimations)
        {
            return true;
        }

        return animationName == _idleAnimation || animationName == _runAnimation
               || animationName == _fallAnimation;
    }

    private bool Exists(string animationName)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            return false;
        }

        return _skeleton.Skeleton?.Data?.FindAnimation(animationName) != null;
    }

    #endregion

    #region SpriteRenderer bridge

    /// <summary>
    /// 既存コードが SpriteRenderer へ行う演出を骨格へ中継する。
    /// 色は溜め・パリィ、表示 ON/OFF は無敵点滅で使われている。
    /// </summary>
    private void MirrorSpriteRendererEffects()
    {
        var skeleton = _skeleton.Skeleton;
        if (skeleton != null)
        {
            var c = _sprite.color;
            skeleton.SetColor(c.r, c.g, c.b, c.a);
        }

        if (_meshRenderer != null && _meshRenderer.enabled != _sprite.enabled)
        {
            _meshRenderer.enabled = _sprite.enabled;
        }
    }

    #endregion
}
