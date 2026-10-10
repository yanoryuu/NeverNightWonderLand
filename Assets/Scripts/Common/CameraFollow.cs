using UnityEngine;

/// <summary>
/// ターゲットを滑らかに追従するシンプルなカメラ。
/// チュートリアルシーンをスクリプトから確実に構築するために Cinemachine の代わりに使う
/// (本編シーンでは Cinemachine に置き換えてよい)。
/// 右スティックの縦でカメラを上下に見回せる (Ender Lilies / Hollow Knight の見上げ・見下ろし相当)。
/// 倒している間だけずらし、離すと戻る。下限 (CameraLimit) のクランプはずらした後に掛かる。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("追従するターゲット (プレイヤー)")]
    [SerializeField] private Transform _target;

    [Tooltip("ターゲットからのオフセット")]
    [SerializeField] private Vector3 _offset = new(2f, 2f, -10f);

    [Tooltip("追従の滑らかさ (小さいほど機敏)")]
    [SerializeField] private float _smoothTime = 0.15f;

    [Tooltip("カメラ位置の下限 (これより下へは追従しない)")]
    [SerializeField] private float _minY = 2f;

    [Header("見回し (右スティックの縦)")]
    [Tooltip("上を見る時の最大のずらし量 (ワールド単位)。プレイヤーが画面の下端に残る程度にする")]
    [SerializeField] private float _lookUpDistance = 2f;

    [Tooltip("下を見る時の最大のずらし量 (ワールド単位)")]
    [SerializeField] private float _lookDownDistance = 4f;

    [Tooltip("見回しでずらす時の滑らかさ (sec)")]
    [SerializeField] private float _lookSmoothTime = 0.25f;

    [Tooltip("スティックを離して戻る時の滑らかさ (sec)")]
    [SerializeField] private float _lookReturnTime = 0.18f;

    private Vector3 _velocity;
    private float _lookOffset;   // 現在の見回しのずらし量 (上 = +)
    private float _lookVelocity;
    private PlayerController _player;

    // 揺れ (CameraShake) を足す前の追従位置。揺れを追従の SmoothDamp に混ぜないため分けて持つ
    private Vector3 _followPosition;
    private Vector3 _lastWrittenPosition;
    private bool _hasFollowPosition;

    // 寄せ (CameraFocus) のズーム。寄せている間だけ orthographicSize を変え、終わったら元の値に戻す
    private Camera _camera;
    private float _baseOrthoSize;
    private bool _zoomed;

    /// <summary>
    /// ターゲット位置へ即座に移動する (ステージ遷移でプレイヤーがワープした時用。
    /// 暗転中に呼ぶことで、カメラが移動していく様子を見せない)。
    /// </summary>
    /// <summary>実効の下限。ステージに CameraLimit があればそちらを優先する。</summary>
    private float EffectiveMinY => CameraLimit.OverrideMinY ?? _minY;

    public void SnapToTarget()
    {
        if (_target == null)
            return;

        var desired = _target.position + _offset;
        desired.y = Mathf.Max(desired.y, EffectiveMinY);
        transform.position = desired;
        _velocity = Vector3.zero;
        _hasFollowPosition = false;
        CameraShake.Stop();
        CameraFocus.Stop();
        ApplyZoom(1f);

        // ステージ遷移・ワープでは見回しも解除する
        _lookOffset = 0f;
        _lookVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        UpdateLook();

        var desired = _target.position + _offset;
        desired.y += _lookOffset; // 見回しはクランプの前に足す (下限で見下ろしてもそれ以上は下がらない)
        desired.y = Mathf.Max(desired.y, EffectiveMinY);

        // 他所から位置を書き換えられていたら (ワープ等) そこを追従の起点にし直す
        if (!_hasFollowPosition || transform.position != _lastWrittenPosition)
        {
            _followPosition = transform.position;
            _hasFollowPosition = true;
        }

        _followPosition = Vector3.SmoothDamp(_followPosition, desired, ref _velocity, _smoothTime);
        var shake = GamePause.IsPaused ? Vector2.zero : CameraShake.Evaluate(Time.unscaledDeltaTime);

        // 寄せ (スピーカー破壊などの見せ場): 追従位置と寄せ先を寄せ具合で混ぜ、表示範囲を狭める
        var position = _followPosition;
        var focus = CameraFocus.Evaluate(Time.unscaledDeltaTime, GamePause.IsPaused, out var zoomScale);
        if (focus > 0f)
        {
            var point = CameraFocus.Point;
            position = Vector3.Lerp(position, new Vector3(point.x, point.y, _followPosition.z), focus);
        }
        ApplyZoom(zoomScale);

        transform.position = position + (Vector3)shake;
        _lastWrittenPosition = transform.position;
    }

    /// <summary>表示範囲を寄せる前の scale 倍にする (1 で元に戻す)。正投影のカメラだけ。</summary>
    private void ApplyZoom(float scale)
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
        if (_camera == null || !_camera.orthographic)
            return;

        if (!_zoomed)
        {
            if (Mathf.Approximately(scale, 1f))
                return;
            _baseOrthoSize = _camera.orthographicSize;
            _zoomed = true;
        }

        _camera.orthographicSize = _baseOrthoSize * scale;
        if (Mathf.Approximately(scale, 1f))
            _zoomed = false;
    }

    /// <summary>右スティックの縦の入力から見回しのずらし量を更新する。</summary>
    private void UpdateLook()
    {
        var input = CanLook() ? PlayerInputSource.Current.Look : 0f;
        var goal = input >= 0f ? input * _lookUpDistance : input * _lookDownDistance;
        var smooth = Mathf.Approximately(goal, 0f) ? _lookReturnTime : _lookSmoothTime;
        _lookOffset = Mathf.SmoothDamp(_lookOffset, goal, ref _lookVelocity, smooth);
    }

    /// <summary>
    /// 見回しできる状態か。会話・メニュー (ポーズ / 拠点 / ショップ / アイテム画面等)・ステージ遷移中・死亡中は不可
    /// (この間はスティックを倒していても戻る)。ポーズ中は入力を読まない (仮想入力のタップを消費しないため)。
    /// </summary>
    private bool CanLook()
    {
        if (GamePause.IsPaused || MenuPanelView.AnyOpen || DialogueService.IsBusy)
            return false;
        if (StageLoader.Instance != null && StageLoader.Instance.IsLoading)
            return false;

        if (_player == null)
            _player = _target.GetComponentInParent<PlayerController>();
        return _player == null || !_player.IsDead;
    }
}
