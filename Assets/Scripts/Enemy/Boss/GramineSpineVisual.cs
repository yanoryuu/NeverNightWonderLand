using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// グラミネ (<see cref="GramineBossBehaviour"/>) の見た目を Spine (Assets/Motion/gramine) で描く。
/// ボスのうごきは仮の四角 (ルートの SpriteRenderer = EnemyController._visual) に向き・色を書くので、
/// 本クラスは四角の描画だけを止め、そこから向き (flipX) と色 (被弾・ブレイクの色) を読んで骨格に写す。
///
/// アニメーション (骨格には向きごとに "L/名前_L" と "R/名前_R" がある):
///   ・構えに合わせて待機 (浮遊 = idle_floating / 地上 = idle_normal) をループ
///   ・合図 (<see cref="GramineBossBehaviour.Cue"/>) で 1 回きりのモーション:
///     StompJump = jump (終端で保持) → StompFall = stomp_fall (ループ) → StompLand = landing → to idle → 待機
///     Retreat / PhaseShift = smile → 待機
///   ・それ以外の合図 (ナイフ・横払いなど、まだモーションの無い技) は待機のまま (予告の光だけで伝える)
/// 向きが変わったらモーションの再生位置を保ったまま反対向きのアニメーションへ切り替える。
/// L も R も素の絵は左向きで、R は骨格を左右反転して右向きに使う前提の作り (左右非対称の飾りの重なりが反転後に正しくなる)。
/// そのため右向き = R + Skeleton.ScaleX = -1、左向き = L + ScaleX = 1 (2026-10-12 4 通りを並べて撮影して確認)。
/// </summary>
[RequireComponent(typeof(GramineBossBehaviour))]
public class GramineSpineVisual : MonoBehaviour
{
    #region Serialized Fields

    [Header("描画先")]
    [Tooltip("子の SkeletonAnimation。未設定なら子から探す")]
    [SerializeField] private SkeletonAnimation _skeleton;

    [Tooltip("仮の四角 (EnemyController._visual)。描画だけ止め、向きと色を読む。未設定ならルートの SpriteRenderer")]
    [SerializeField] private SpriteRenderer _placeholder;

    [Tooltip("四角が flipX の時に左を向いている (EnemyController._visualFacesLeft = false の時)")]
    [SerializeField] private bool _flipXMeansLeft = true;

    [Tooltip("右向きは R のアニメーションを骨格ごと左右反転して描く (R は反転して使う前提で作られている。L も R も素の向きは左)")]
    [SerializeField] private bool _mirrorRight = true;

    [Header("Spine アニメーション名 (向きの L/ R/ と _L _R は自動で付ける)")]
    [SerializeField] private string _idleGround = "guramine_idle_normal";
    [SerializeField] private string _idleFloating = "guramine_idle_floating";
    [SerializeField] private string _jump = "guramine_jump";
    [SerializeField] private string _stompFall = "guramine_attack_stomp_fall";
    [SerializeField] private string _stompLanding = "guramine_attack_stomp_landing";
    [SerializeField] private string _stompToIdle = "guramine_attack_stomp_to idle";
    [SerializeField] private string _smile = "guramine_smile";

    [Header("挙動")]
    [Tooltip("アニメーション切り替え時のブレンド時間 (sec)")]
    [SerializeField] private float _mixDuration = 0.12f;

    #endregion

    #region Fields

    private GramineBossBehaviour _boss;
    private MeshRenderer _meshRenderer;

    // 1 回きりのモーションの並び (null なら待機)。_oneShotIndex 番目を再生中
    private string[] _oneShot;
    private bool _oneShotHoldLast;   // 最後のモーションを終端で保持する (ジャンプ・落下)。false なら終わったら待機へ
    private int _oneShotIndex;

    private string _currentBase;     // 今再生中のアニメーション名 (向き無し)
    private bool _currentLeft;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _boss = GetComponent<GramineBossBehaviour>();
        if (_skeleton == null)
            _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_placeholder == null)
            _placeholder = GetComponent<SpriteRenderer>();

        if (_skeleton == null || _placeholder == null)
        {
            Debug.LogError($"{nameof(GramineSpineVisual)}: SkeletonAnimation か仮の四角が見つかりません", this);
            enabled = false;
            return;
        }

        _meshRenderer = _skeleton.GetComponent<MeshRenderer>();
        // 四角は EnemyController が色・向き・表示 ON/OFF を書き続けるので外さず、描画だけ止める
        _placeholder.forceRenderingOff = true;
        _boss.Cue += OnCue;
    }

    private void OnDestroy()
    {
        if (_boss != null)
            _boss.Cue -= OnCue;
    }

    private void LateUpdate()
    {
        if (_skeleton.AnimationState == null)
            return;

        UpdateAnimation();
        MirrorPlaceholder();
    }

    #endregion

    #region Animation

    private void OnCue(string cue)
    {
        switch (cue)
        {
            case "StompJump": StartOneShot(true, _jump); break;
            case "StompFall": StartOneShot(true, _stompFall); break;
            case "StompLand": StartOneShot(false, _stompLanding, _stompToIdle); break;
            case "Retreat":
            case "PhaseShift": StartOneShot(false, _smile); break;
            case "Rise":
            case "Descend":
                // 構えの切り替え: 1 回きりのモーションを打ち切って新しい構えの待機へ
                _oneShot = null;
                break;
        }
    }

    private void StartOneShot(bool holdLast, params string[] sequence)
    {
        _oneShot = sequence;
        _oneShotHoldLast = holdLast;
        _oneShotIndex = 0;
        _currentBase = null;   // 次の LateUpdate で頭から流す
    }

    private void UpdateAnimation()
    {
        var left = _placeholder.flipX == _flipXMeansLeft;
        var state = _skeleton.AnimationState;
        var track = state.GetTrack(0);

        // 1 回きりのモーション: 今のものが終わったら次へ、全部終わったら待機へ
        if (_oneShot != null && _currentBase == _oneShot[_oneShotIndex] && track != null && track.IsComplete)
        {
            var last = _oneShotIndex >= _oneShot.Length - 1;
            if (!last)
            {
                _oneShotIndex++;
            }
            else if (!_oneShotHoldLast)
            {
                _oneShot = null;
            }
        }

        string wanted;
        bool loop;
        if (_oneShot != null)
        {
            wanted = _oneShot[_oneShotIndex];
            // 落下はループ、ジャンプは終端で保持 (ループしない)
            loop = wanted == _stompFall;
        }
        else
        {
            wanted = _boss.IsFloating ? _idleFloating : _idleGround;
            loop = true;
        }

        if (wanted == _currentBase && left == _currentLeft)
            return;

        var animation = Find(wanted, left) ?? Find(_idleGround, left);
        if (animation == null)
            return;

        // 向きだけが変わった時は再生位置を引き継ぐ (振り向くたびに頭から流れないように)
        var keepTime = wanted == _currentBase && track != null ? track.TrackTime : 0f;
        var skeleton = _skeleton.Skeleton;
        if (skeleton != null)
            skeleton.ScaleX = _mirrorRight && !left ? -1f : 1f;
        var entry = state.SetAnimation(0, animation, loop);
        entry.MixDuration = wanted == _currentBase ? 0f : _mixDuration;
        entry.TrackTime = keepTime;
        _currentBase = wanted;
        _currentLeft = left;
    }

    /// <summary>向き付きのアニメーション ("L/名前_L" / "R/名前_R") を探す。無ければ null。</summary>
    private Spine.Animation Find(string baseName, bool left)
    {
        if (string.IsNullOrEmpty(baseName))
            return null;
        var data = _skeleton.Skeleton?.Data;
        if (data == null)
            return null;
        var dir = left ? "L" : "R";
        return data.FindAnimation($"{dir}/{baseName}_{dir}");
    }

    /// <summary>四角に書かれた色 (被弾の光・ブレイクの色・フェード) と表示 ON/OFF を骨格へ写す。</summary>
    private void MirrorPlaceholder()
    {
        var skeleton = _skeleton.Skeleton;
        if (skeleton != null)
        {
            var c = _placeholder.color;
            skeleton.SetColor(c.r, c.g, c.b, c.a);
        }

        if (_meshRenderer != null && _meshRenderer.enabled != _placeholder.enabled)
            _meshRenderer.enabled = _placeholder.enabled;
    }

    #endregion
}
