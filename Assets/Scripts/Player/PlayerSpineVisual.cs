using Spine.Unity;
using UnityEngine;

/// <summary>
/// プレイヤーの見た目を Spine (SkeletonAnimation) で描画するためのブリッジ。
/// <see cref="PlayerController"/> は状態名を直接指定せず Animator のパラメータだけを更新する設計なので、
/// 本クラスはそのパラメータを読み取り、対応する Spine アニメーションへ振り分ける。
/// Warrior.controller の AnyState 遷移条件と同じ優先順位で判定するため、
/// スプライト時代と見た目の切り替わるタイミングが一致する。
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
    [SerializeField] private string _idleAnimation = "walk";
    [SerializeField] private string _runAnimation = "walk";
    [SerializeField] private string _jumpAnimation = "";
    [SerializeField] private string _fallAnimation = "";
    [SerializeField] private string _attackAnimation = "";
    [SerializeField] private string _dashAnimation = "";

    [Header("挙動")]
    [Tooltip("待機用アニメーションが移動用と同じ場合、待機中は先頭フレームで静止させる")]
    [SerializeField] private bool _freezeIdleWhenShared = true;

    [Tooltip("この速度を超えたら移動アニメーションに切り替える。Warrior.controller の閾値と同じ")]
    [SerializeField] private float _runSpeedThreshold = 0.1f;

    [Tooltip("上昇中と判定する Y 速度。Warrior.controller の閾値と同じ")]
    [SerializeField] private float _riseVelocity = 3f;

    [Tooltip("落下中と判定する Y 速度。Warrior.controller の閾値と同じ")]
    [SerializeField] private float _fallVelocity = -3f;

    [Tooltip("アニメーション切り替え時のブレンド時間 (sec)")]
    [SerializeField] private float _mixDuration = 0.12f;

    #endregion

    #region Fields

    // PlayerController が書き込むパラメータ。名前は PlayerController 側と一致させること
    private static readonly int ParamSpeed = Animator.StringToHash("Speed");
    private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int ParamYVelocity = Animator.StringToHash("YVelocity");
    private static readonly int ParamIsDashing = Animator.StringToHash("IsDashing");
    private static readonly int ParamIsAttacking = Animator.StringToHash("IsAttacking");

    private Animator _animator;
    private SpriteRenderer _sprite;
    private MeshRenderer _meshRenderer;

    // 直前に指示したアニメーション。毎フレーム SetAnimation を呼ばないための比較用
    private string _currentAnimation;
    private bool _currentFrozen;

    #endregion

    #region Unity

    private void Awake()
    {
        _animator = GetComponent<Animator>();
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
    /// Animator のパラメータから再生すべき Spine アニメーションを決める。
    /// 判定順は Warrior.controller の AnyState 遷移と同じにしてある。
    /// </summary>
    private void UpdateAnimation()
    {
        var speed = _animator.GetFloat(ParamSpeed);
        var yVelocity = _animator.GetFloat(ParamYVelocity);
        var isGrounded = _animator.GetBool(ParamIsGrounded);
        var isDashing = _animator.GetBool(ParamIsDashing);
        var isAttacking = _animator.GetBool(ParamIsAttacking);

        string wanted;

        if (isAttacking)
        {
            wanted = _attackAnimation;
        }
        else if (isDashing)
        {
            wanted = _dashAnimation;
        }
        else if (!isGrounded)
        {
            wanted = yVelocity > _riseVelocity ? _jumpAnimation : _fallAnimation;
            // 上昇でも落下でもない滞空中は落下側を流用する
            if (!Exists(wanted) && yVelocity <= _riseVelocity)
            {
                wanted = _fallAnimation;
            }
        }
        else
        {
            wanted = speed > _runSpeedThreshold ? _runAnimation : _idleAnimation;
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

        ApplyAnimation(wanted, frozen, immediate: false);
    }

    private void ApplyAnimation(string animationName, bool frozen, bool immediate)
    {
        if (!Exists(animationName))
        {
            return;
        }

        if (animationName == _currentAnimation && frozen == _currentFrozen)
        {
            return;
        }

        var entry = _skeleton.AnimationState.SetAnimation(0, animationName, !frozen);
        if (entry != null)
        {
            entry.MixDuration = immediate ? 0f : _mixDuration;

            if (frozen)
            {
                // 待機用の専用アニメーションが無いので、移動モーションの先頭で静止させる
                entry.TrackTime = 0f;
                entry.TimeScale = 0f;
            }
            else
            {
                entry.TimeScale = 1f;
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
