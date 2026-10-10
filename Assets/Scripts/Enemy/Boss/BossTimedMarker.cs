using System;
using UnityEngine;

/// <summary>
/// 一定時間点滅する予告の印。時間が来たらコールバックを呼んで消える (風船の出現位置の予告など)。
/// 弾消し (<see cref="BossHazard.ClearAll"/>) で消された時はコールバックを呼ばない。
/// ボス本体が怯んでも予告どおりに出る (一度出した攻撃は取り消さない)。
/// </summary>
public class BossTimedMarker : BossHazard
{
    private SpriteRenderer _sprite;
    private Color _color;
    private float _delay;
    private float _timer;
    private Action _onElapsed;

    /// <summary>予告の印を出す。delay 秒後に onElapsed を呼んで消える。</summary>
    public static BossTimedMarker Create(string name, Sprite sprite, Color color, Vector2 position, Vector2 size,
        float delay, Action onElapsed)
    {
        var sr = BossKit.CreateMarker(name, sprite, color, position, size);
        var marker = sr.gameObject.AddComponent<BossTimedMarker>();
        marker._sprite = sr;
        marker._color = color;
        marker._delay = delay;
        marker._onElapsed = onElapsed;
        return marker;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 点滅 (出現が近づくほど速く)
        var rate = Mathf.Lerp(4f, 14f, Mathf.Clamp01(_timer / Mathf.Max(0.01f, _delay)));
        var c = _color;
        c.a = _color.a * (Mathf.Repeat(_timer * rate, 1f) < 0.5f ? 1f : 0.45f);
        _sprite.color = c;

        if (_timer < _delay)
            return;

        var callback = _onElapsed;
        _onElapsed = null;
        Destroy(gameObject);
        callback?.Invoke();
    }

    public override void Clear()
    {
        _onElapsed = null;
        base.Clear();
    }
}
