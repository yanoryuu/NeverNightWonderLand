using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// <see cref="AnimEffectEmitter"/> が出すエフェクト 1 つぶん。最後まで流したらプールに戻る (エフェクトの種類ごとに使い回す)。
/// プレハブ (パーティクル・Spine・Animator) か連番の絵 (<see cref="AnimEffectCueSet.Effect.Frames"/>) を 1 回だけ流す。
/// 時間は Time.deltaTime で進む (ポーズ・ヒットストップ・スローで一緒に止まる / 遅くなる)。
/// </summary>
public class AnimEffectPlayer : MonoBehaviour
{
    private const float FallbackDuration = 2f;

    private static readonly Dictionary<AnimEffectCueSet.Effect, Stack<AnimEffectPlayer>> Pools = new();
    private static Transform _poolRoot;

    private AnimEffectCueSet.Effect _effect;
    private ParticleSystem[] _particles;
    private SkeletonAnimation[] _skeletons;
    private Animator[] _animators;
    private SpriteRenderer _frameRenderer;
    private Vector3 _baseScale;
    private float _duration;
    private float _time;

    // キャラに付いて動く時の追従先
    private AnimEffectEmitter _follow;
    private AnimEffectCueSet.Cue _cue;

    /// <summary>エフェクトを出す。follow を渡すと、その間はキャラに付いて動く。</summary>
    public static AnimEffectPlayer Spawn(AnimEffectCueSet.Effect effect, Vector3 position, bool facingRight,
        AnimEffectCueSet.Cue cue, AnimEffectEmitter follow)
    {
        if (effect == null || (effect.Prefab == null && (effect.Frames == null || effect.Frames.Length == 0)))
            return null;

        var player = Take(effect);
        player._cue = cue;
        player._follow = follow;
        player.Place(position, facingRight);
        player.gameObject.SetActive(true);
        player.Restart();
        return player;
    }

    private static AnimEffectPlayer Take(AnimEffectCueSet.Effect effect)
    {
        if (Pools.TryGetValue(effect, out var pool))
        {
            while (pool.Count > 0)
            {
                var pooled = pool.Pop();
                if (pooled != null) // シーンごと消えた物は捨てる
                {
                    pooled.transform.SetParent(null, false);
                    return pooled;
                }
            }
        }
        return Create(effect);
    }

    private static AnimEffectPlayer Create(AnimEffectCueSet.Effect effect)
    {
        GameObject go;
        if (effect.Prefab != null)
        {
            go = Instantiate(effect.Prefab);
            go.name = effect.Prefab.name;
        }
        else
        {
            go = new GameObject(effect.Id);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = effect.SortingOrder;
        }

        var player = go.AddComponent<AnimEffectPlayer>();
        player._effect = effect;
        player._baseScale = go.transform.localScale;
        player._particles = go.GetComponentsInChildren<ParticleSystem>(true);
        player._skeletons = go.GetComponentsInChildren<SkeletonAnimation>(true);
        player._animators = go.GetComponentsInChildren<Animator>(true);
        if (effect.Prefab == null)
            player._frameRenderer = go.GetComponent<SpriteRenderer>();
        player._duration = player.MeasureDuration();

        foreach (var ps in player._particles)
        {
            var main = ps.main;
            main.playOnAwake = false;
        }
        return player;
    }

    private float MeasureDuration()
    {
        var duration = 0f;
        if (_frameRenderer != null)
            duration = _effect.Frames.Length / Mathf.Max(_effect.Fps, 0.01f);
        foreach (var ps in _particles)
        {
            var main = ps.main;
            duration = Mathf.Max(duration, main.duration + main.startDelay.constantMax + main.startLifetime.constantMax);
        }
        foreach (var sa in _skeletons)
        {
            var anim = sa.skeletonDataAsset != null ? sa.skeletonDataAsset.GetSkeletonData(true)?.Animations.Items.FirstOrDefault() : null;
            if (anim != null)
                duration = Mathf.Max(duration, anim.Duration);
        }
        foreach (var animator in _animators)
        {
            var clips = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.animationClips : null;
            if (clips != null && clips.Length > 0)
                duration = Mathf.Max(duration, clips.Max(c => c.length));
        }
        return duration > 0f ? duration : FallbackDuration;
    }

    private void Place(Vector3 position, bool facingRight)
    {
        var t = transform;
        t.position = position;
        // 調整シーンでは左向き = 親ごと左右反転なので、回転の向きも反転する
        var rotation = _cue != null ? _cue.Rotation : 0f;
        var scale = _cue != null ? _cue.Scale : 1f;
        t.rotation = Quaternion.Euler(0f, 0f, facingRight ? rotation : -rotation);
        t.localScale = new Vector3(_baseScale.x * (facingRight ? scale : -scale), _baseScale.y * scale, _baseScale.z);
    }

    private void Restart()
    {
        _time = 0f;
        if (_frameRenderer != null)
            _frameRenderer.sprite = _effect.Frames[0];
        foreach (var ps in _particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }
        foreach (var sa in _skeletons)
        {
            var anim = sa.skeletonDataAsset != null ? sa.skeletonDataAsset.GetSkeletonData(true)?.Animations.Items.FirstOrDefault() : null;
            if (anim != null && sa.AnimationState != null)
                sa.AnimationState.SetAnimation(0, anim, false);
        }
        foreach (var animator in _animators)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void Update()
    {
        _time += Time.deltaTime;
        if (_time >= _duration)
        {
            Release();
            return;
        }

        if (_frameRenderer != null)
        {
            var i = Mathf.Clamp(Mathf.FloorToInt(_time * _effect.Fps), 0, _effect.Frames.Length - 1);
            _frameRenderer.sprite = _effect.Frames[i];
        }
    }

    private void LateUpdate()
    {
        if (_follow == null)
            return;
        if (!_follow.isActiveAndEnabled)
        {
            _follow = null; // キャラが消えたらその場に残す
            return;
        }
        Place(_follow.CuePosition(_cue), _follow.FacingRight);
    }

    private void Release()
    {
        _follow = null;
        _cue = null;
        gameObject.SetActive(false);
        if (_poolRoot == null)
        {
            var root = new GameObject("[AnimEffect Pool]");
            _poolRoot = root.transform;
        }
        transform.SetParent(_poolRoot, false);
        if (!Pools.TryGetValue(_effect, out var pool))
            Pools[_effect] = pool = new Stack<AnimEffectPlayer>();
        pool.Push(this);
    }
}
