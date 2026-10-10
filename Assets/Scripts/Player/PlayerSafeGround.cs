using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 直前に安全に立っていた地点を記録する (トゲ・穴の HazardZone が「戻す」先)。
/// HazardZone (_returnToSafeGround) がシーン開始時にプレイヤーへ自動で AddComponent するので、
/// プレハブへの追加は不要。
/// 記録条件: 接地中 / 動く足場 (Kinematic) の上ではない / 体の周り (左右に余白) に HazardZone が無い /
/// 足元の左右両端の真下に地面がある (崖っぷちを記録しない)。
/// ステージが切り替わったら記録を捨てる (前のステージの座標へ戻さない)。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSafeGround : MonoBehaviour
{
    // 足元の左右端を調べる幅と、足元より下へ見る距離
    private const float EdgeHalfWidth = 0.35f;
    private const float EdgeProbeDistance = 0.6f;

    // HazardZone からこれだけ離れている位置だけを記録する (トゲの縁へ戻して即再被弾するのを防ぐ)
    private static readonly Vector2 HazardMargin = new Vector2(1.2f, 0.6f);

    private readonly Collider2D[] _hits = new Collider2D[16];
    private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[4];

    private PlayerController _controller;
    private Collider2D _body;
    private Vector2 _lastSafePosition;

    /// <summary>安全地点が記録されているか。</summary>
    public bool HasSafePosition { get; private set; }

    public Vector2 LastSafePosition => _lastSafePosition;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _body = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene _, Scene __) => HasSafePosition = false;

    private void FixedUpdate()
    {
        if (!_controller.IsGrounded || _controller.IsDead || _body == null)
            return;

        var ground = _controller.GroundCollider;
        if (ground == null)
            return;

        // 動く足場の上は、戻した時に足場が無いことがあるので記録しない
        var body = ground.attachedRigidbody;
        if (body != null && body.bodyType != RigidbodyType2D.Static)
            return;

        var bounds = _body.bounds;
        if (IsNearHazard(bounds))
            return;

        // 足元の左右両端の真下に地面があること
        var pos = (Vector2)transform.position;
        var depth = pos.y - bounds.min.y + EdgeProbeDistance;
        var groundFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _controller.Consts.GroundLayer,
            useTriggers = false, // ゾーン等のトリガーを地面と誤認しない
        };
        if (Physics2D.Raycast(pos + Vector2.left * EdgeHalfWidth, Vector2.down, groundFilter, _rayHits, depth) == 0
            || Physics2D.Raycast(pos + Vector2.right * EdgeHalfWidth, Vector2.down, groundFilter, _rayHits, depth) == 0)
            return;

        _lastSafePosition = pos + Vector2.up * 0.05f;
        HasSafePosition = true;
    }

    private bool IsNearHazard(Bounds bounds)
    {
        var filter = new ContactFilter2D().NoFilter(); // トリガーも含めて全レイヤー
        var count = Physics2D.OverlapBox(bounds.center, (Vector2)bounds.size + HazardMargin * 2f, 0f, filter, _hits);
        for (var i = 0; i < count; i++)
        {
            if (_hits[i] != null && _hits[i].GetComponent<HazardZone>() != null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 戻し先を返す。記録が無ければステージの最寄りの PlayerSpawnPoint (それも無ければ false)。
    /// </summary>
    public bool TryGetReturnPosition(out Vector2 position)
    {
        if (HasSafePosition)
        {
            position = _lastSafePosition;
            return true;
        }

        position = default;
        var best = float.MaxValue;
        var found = false;
        foreach (var spawn in FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None))
        {
            var sqr = ((Vector2)spawn.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                position = spawn.transform.position;
                found = true;
            }
        }

        return found;
    }
}
