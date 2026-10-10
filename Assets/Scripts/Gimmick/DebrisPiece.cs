using UnityEngine;

/// <summary>破片 1 個の動き (BreakDebris から使う)。</summary>
public class DebrisPiece : MonoBehaviour
{
    private const float Gravity = 24f;
    private const float Restitution = 0.38f;   // はね返りの強さ (縦)
    private const float BounceFriction = 0.55f; // はねた時の横速度の残り
    private const int MaxBounces = 2;
    private const float FadeTime = 0.35f;

    private static readonly RaycastHit2D[] s_hits = new RaycastHit2D[4];
    private static int s_groundMask = -1;

    private SpriteRenderer _renderer;
    private Vector2 _velocity;
    private float _angularVelocity;
    private float _life;
    private float _age;
    private float _radius;
    private int _bounces;
    private bool _resting;
    private Color _color;

    public void Launch(Sprite sprite, Vector2 position, Vector2 velocity, float angularVelocity, float scale,
        Color color, int sortingLayerId, int sortingOrder, float life)
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (s_groundMask < 0)
            s_groundMask = LayerMask.GetMask("Ground");

        _renderer.sprite = sprite;
        _renderer.sortingLayerID = sortingLayerId;
        _renderer.sortingOrder = sortingOrder;
        _color = color;
        _renderer.color = color;

        transform.position = new Vector3(position.x, position.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        transform.localScale = Vector3.one * scale;

        var ext = sprite.bounds.extents;
        _radius = Mathf.Min(ext.x, ext.y) * scale * 0.8f;
        _velocity = velocity;
        _angularVelocity = angularVelocity;
        _life = life;
        _age = 0f;
        _bounces = 0;
        _resting = false;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        _age += dt;
        if (_age >= _life)
        {
            BreakDebris.Return(this);
            return;
        }

        if (!_resting)
            Step(dt);

        var fade = Mathf.Clamp01((_life - _age) / FadeTime);
        var c = _color;
        c.a *= fade;
        _renderer.color = c;
    }

    private void Step(float dt)
    {
        _velocity.y -= Gravity * dt;
        var from = (Vector2)transform.position;
        var to = from + _velocity * dt;

        // 床 (Ground) との当たりだけを調べる。始点が既に中にある物 (fraction 0) は無視
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = s_groundMask, useTriggers = false };
        var delta = to - from;
        var dir = delta.normalized;
        var count = delta.sqrMagnitude > 1e-8f
            ? Physics2D.Raycast(from, dir, filter, s_hits, delta.magnitude + _radius)
            : 0;

        RaycastHit2D hit = default;
        var found = false;
        for (var i = 0; i < count; i++)
        {
            if (s_hits[i].fraction <= 0f || s_hits[i].collider == null)
                continue;
            if (!found || s_hits[i].distance < hit.distance)
            {
                hit = s_hits[i];
                found = true;
            }
        }

        if (found && _velocity.y < 0f && hit.normal.y > 0.5f)
        {
            // 床ではねる
            to = hit.point + hit.normal * _radius;
            _bounces++;
            if (_bounces > MaxBounces || -_velocity.y < 2f)
            {
                _resting = true;
                _velocity = Vector2.zero;
            }
            else
            {
                _velocity.y = -_velocity.y * Restitution;
                _velocity.x *= BounceFriction;
                _angularVelocity *= 0.5f;
            }
        }
        else if (found && Mathf.Abs(hit.normal.x) > 0.7f && Mathf.Sign(hit.normal.x) != Mathf.Sign(_velocity.x))
        {
            // 壁: 横にはね返す
            to = new Vector2(hit.point.x + hit.normal.x * _radius, to.y);
            _velocity.x = -_velocity.x * 0.35f;
        }

        transform.position = new Vector3(to.x, to.y, 0f);
        transform.Rotate(0f, 0f, _angularVelocity * dt);
    }
}
