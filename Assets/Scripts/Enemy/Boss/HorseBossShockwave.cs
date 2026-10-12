using UnityEngine;

/// <summary>
/// 中ボス「歯車の重馬」の竿立ち踏みつけで出る、床を伝う衝撃波 (Docs/EnemySpec.md「下の中ボス」)。
/// 床の表面を一定の速さで滑り、毎フレーム真下の床の高さに沿う (床の大歯車のこぶも乗り越える)。
/// 高さは低い (既定 0.8) ので跳んでよけられる。闘技場の端・段差 (急に高くなる床) ・床が無い所で消える。
/// 見た目は実行時の四角 (BossKit.SquareSprite) を色付けしたもの。弾消し (BossHazard.ClearAll) で消える。
/// </summary>
public class HorseBossShockwave : BossHazard
{
    private SpriteRenderer _sprite;
    private LayerMask _ground;
    private float _direction;   // +1 右 / -1 左
    private float _speed;
    private Vector2 _size;      // 当たり判定 (幅, 高さ)
    private float _minX;
    private float _maxX;
    private float _floorY;      // 今いる床の高さ
    private float _maxStepUp;   // これより急に高くなる床は壁とみなして消える
    private float _lifetime;
    private float _timer;
    private Color _color;

    /// <summary>
    /// 衝撃波を出す。x = 出る位置, floorY = その床の高さ, direction = 進む向き (符号), minX/maxX = 消える範囲。
    /// </summary>
    public static HorseBossShockwave Create(GameObject source, int damage, float x, float floorY, float direction, float speed,
        Vector2 size, float minX, float maxX, LayerMask groundMask, Color color, float lifetime = 4f, float maxStepUp = 0.6f)
    {
        var sr = BossKit.CreateMarker("HorseShockwave", BossKit.SquareSprite, color,
            new Vector2(x, floorY + size.y * 0.5f), size);
        var wave = sr.gameObject.AddComponent<HorseBossShockwave>();
        wave.Source = source;
        wave.Damage = damage;
        wave._sprite = sr;
        wave._color = color;
        wave._ground = BossKit.GroundMaskOrDefault(groundMask);
        wave._direction = Mathf.Sign(direction);
        wave._speed = speed;
        wave._size = size;
        wave._minX = minX;
        wave._maxX = maxX;
        wave._floorY = floorY;
        wave._lifetime = lifetime;
        wave._maxStepUp = maxStepUp;
        return wave;
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        _timer += dt;
        if (_timer >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        var x = transform.position.x + _direction * _speed * dt;
        if (x < _minX || x > _maxX)
        {
            Destroy(gameObject);
            return;
        }

        // 床に沿う: 今の床の少し上から真下を探す。急に高くなる (段差・壁) か床が無ければ消える
        var hit = Physics2D.Raycast(new Vector2(x, _floorY + _maxStepUp + 0.05f), Vector2.down, _maxStepUp + 3f, _ground);
        if (hit.collider == null || hit.point.y > _floorY + _maxStepUp)
        {
            Destroy(gameObject);
            return;
        }

        _floorY = hit.point.y;
        transform.position = new Vector3(x, _floorY + _size.y * 0.5f, 0f);

        // ちらつき (地を這う感じ)
        var c = _color;
        c.a = _color.a * (Mathf.Repeat(_timer * 12f, 1f) < 0.5f ? 1f : 0.7f);
        _sprite.color = c;

        BossKit.HitPlayerBox(transform.position, _size, Damage, Source != null ? Source : gameObject);
    }
}
