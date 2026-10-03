using UnityEngine;

/// <summary>
/// 横風のゾーン。中にいるプレイヤーを一定方向へ押す (UnityEngine.WindZone と名前がぶつかるため WindArea)。
/// AreaEffector2D は速度直接指定の PlayerController には効かないので、
/// PlayerController.AddExternalVelocity で毎物理ステップ押す。
/// 常時吹く (Continuous) か、吹く/止むを繰り返す (Gust) を選べる。
/// Gust では吹いている間だけ _activeVisual (風の線・パーティクル) を表示し、
/// 吹き始めの直前 _warningTime 秒は _warningVisual を表示する (予兆)。
/// </summary>
[DefaultExecutionOrder(-50)] // プレイヤーの FixedUpdate より先に外力を積む
public class WindArea : PlayerAreaZone
{
    public enum Pattern
    {
        Continuous,
        Gust,
    }

    [Tooltip("風の速度 (units/sec)。横風なら (±x, 0)")]
    [SerializeField] private Vector2 _velocity = new Vector2(-4f, 0f);

    [Tooltip("接地中に掛ける倍率 (地上では弱めたい場合に 1 未満)")]
    [SerializeField] private float _groundedMultiplier = 1f;

    [Tooltip("吹き方")]
    [SerializeField] private Pattern _pattern = Pattern.Continuous;

    [Tooltip("Gust: 吹いている時間 (sec)")]
    [SerializeField] private float _gustOnTime = 2f;

    [Tooltip("Gust: 止んでいる時間 (sec)")]
    [SerializeField] private float _gustOffTime = 2f;

    [Tooltip("Gust: 吹き始め/止み際に強さを補間する時間 (sec)")]
    [SerializeField] private float _rampTime = 0.3f;

    [Tooltip("Gust: 周期の開始オフセット (sec)。複数の風の位相をずらす用")]
    [SerializeField] private float _phaseOffset;

    [Tooltip("Gust: 吹き始めの何秒前から予兆を出すか")]
    [SerializeField] private float _warningTime = 0.6f;

    [Tooltip("吹いている間だけ表示するオブジェクト (任意)")]
    [SerializeField] private GameObject _activeVisual;

    [Tooltip("吹き始めの予兆として表示するオブジェクト (任意, Gust のみ)")]
    [SerializeField] private GameObject _warningVisual;

    private float _time;

    /// <summary>現在の風の強さ (0..1)。</summary>
    public float Strength { get; private set; } = 1f;

    protected override void Awake()
    {
        base.Awake();
        _time = _phaseOffset;
        UpdateCycle(0f);
    }

    private void FixedUpdate()
    {
        UpdateCycle(Time.fixedDeltaTime);

        if (Player == null || Strength <= 0f)
            return;

        var scale = Player.IsGrounded ? _groundedMultiplier : 1f;
        Player.AddExternalVelocity(_velocity * (Strength * scale));
    }

    private void UpdateCycle(float dt)
    {
        if (_pattern == Pattern.Continuous)
        {
            Strength = 1f;
            SetVisuals(active: true, warning: false);
            return;
        }

        var period = Mathf.Max(0.01f, _gustOnTime + _gustOffTime);
        _time = Mathf.Repeat(_time + dt, period);

        // 周期: [0, on) 吹く → [on, period) 止む
        if (_time < _gustOnTime)
        {
            var ramp = Mathf.Max(0.0001f, _rampTime);
            var fadeIn = Mathf.Clamp01(_time / ramp);
            var fadeOut = Mathf.Clamp01((_gustOnTime - _time) / ramp);
            Strength = Mathf.Min(fadeIn, fadeOut);
            SetVisuals(active: true, warning: false);
        }
        else
        {
            Strength = 0f;
            SetVisuals(active: false, warning: period - _time <= _warningTime);
        }
    }

    private void SetVisuals(bool active, bool warning)
    {
        if (_activeVisual != null && _activeVisual.activeSelf != active)
            _activeVisual.SetActive(active);
        if (_warningVisual != null && _warningVisual.activeSelf != warning)
            _warningVisual.SetActive(warning);
    }
}
