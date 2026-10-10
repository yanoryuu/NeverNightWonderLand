using UnityEngine;

/// <summary>
/// ベルトコンベア。上に立っている (接地している) プレイヤーを横へ押し流す。
/// プレイヤーの移動は速度直接指定のため SurfaceEffector2D は効かないので、
/// PlayerController.AddExternalVelocity で毎物理ステップ押す。
/// 判定は「プレイヤーの GroundCollider がこのオブジェクト (子を含む) のコライダーか」で行うので、
/// 地形 Tilemap とは別の GameObject に、Ground レイヤーの実体コライダー (非トリガー) を付けて置く。
/// _reverseFlag が立つと向きが反転する (レバーで逆回転、ライブで切り替わる)。
/// </summary>
[DefaultExecutionOrder(-50)] // プレイヤーの FixedUpdate より先に外力を積む
public class Conveyor : MonoBehaviour
{
    [Tooltip("押し流す速度 (units/sec)。正で右、負で左")]
    [SerializeField] private float _speed = 3f;

    [Tooltip("このフラグ (GameProgress) が立つと向きが反転する。例: A1_BeltReversed。空なら反転しない")]
    [SerializeField] private string _reverseFlag = "";

    [Tooltip("向きの表示 (矢印など)。localScale.x の符号を流れる向きに合わせる (任意)")]
    [SerializeField] private Transform[] _directionVisuals;

    private Collider2D[] _colliders;
    private PlayerController _player;

    /// <summary>現在の流れの速度 (反転を反映済み)。</summary>
    public float CurrentSpeed =>
        !string.IsNullOrEmpty(_reverseFlag) && GameProgress.Has(_reverseFlag) ? -_speed : _speed;

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        GameProgress.Changed += OnFlagChanged;
        UpdateVisuals();
    }

    private void OnDisable()
    {
        GameProgress.Changed -= OnFlagChanged;
    }

    private void OnFlagChanged(string flag)
    {
        if (flag == _reverseFlag)
            UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (_player == null)
        {
            // プレイヤーは PlayerScene に常駐しているので、ステージ側から一度探せば足りる
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null)
                return;
        }

        var ground = _player.GroundCollider;
        if (ground == null || !IsOwnCollider(ground))
            return;

        _player.AddExternalVelocity(new Vector2(CurrentSpeed, 0f));
    }

    private bool IsOwnCollider(Collider2D col)
    {
        foreach (var own in _colliders)
        {
            if (own == col)
                return true;
        }

        return false;
    }

    private void UpdateVisuals()
    {
        if (_directionVisuals == null)
            return;

        var sign = CurrentSpeed >= 0f ? 1f : -1f;
        foreach (var visual in _directionVisuals)
        {
            if (visual == null)
                continue;

            var s = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(s.x) * sign, s.y, s.z);
        }
    }
}
