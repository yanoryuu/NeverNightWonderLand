using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// アニメ・エフェクト調整シーンで決めた位置・タイミング (<see cref="AnimEffectCueSet"/>) で、ゲーム中にエフェクトを出す。
/// キャラ (敵のプレハブなど) のルートに付け、調整シーンで使ったキャラ Id を設定する
/// (調整ウィンドウの「このキャラのプレハブに付ける」で付けられる)。
///
/// アニメの開始は次の方法でつかむ (どれもアニメの再生位置で時間を数えるので、ヒットストップ・スロー・再生速度でずれない):
///   ・Spine: トラック 0 で再生中の TrackEntry が変わったら開始。向き違い (L / R) への切り替えは同じアニメの続きとして扱う
///   ・Animator: レイヤー 0 のステートが変わったら開始 (遷移中は遷移先)。ループは周回ごとに出す
///   ・コードで絵を差し替えているキャラ: その場所から <see cref="Play"/> を呼ぶ (経過時間は Time.deltaTime で数える)
/// アニメが途中で切り替わったら、まだ出していないエフェクトは出さない。
/// 位置はルートからのずれで、左を向いている時は X を反転する (向きは EnemyController、無ければルートの左右の大きさから決める)。
/// </summary>
[DisallowMultipleComponent]
public class AnimEffectEmitter : MonoBehaviour
{
    [Tooltip("調整した値 (Assets/Data/AnimTuning/AnimEffectCueSet.asset)")]
    [SerializeField] private AnimEffectCueSet _cueSet;

    [Tooltip("調整シーンのキャラ Id (例: 敵: Boss_Gramine)")]
    [SerializeField] private string _characterId = "";

    [Tooltip("見張る Spine。未設定なら子から探す")]
    [SerializeField] private SkeletonAnimation _skeleton;

    [Tooltip("見張る Animator。未設定なら子から探す")]
    [SerializeField] private Animator _animator;

    /// <summary>今アニメ 1 本ぶんの再生の記録。</summary>
    private sealed class Playback
    {
        public object Source;           // Spine の TrackEntry / Animator のステート (hash) / 手動は null
        public string Animation;        // 正規化した名前
        public AnimEffectCueSet.AnimationCues Cues;
        public float LastTime;          // 前のフレームの再生位置
        public float Length;            // 1 周の長さ (ループの時に使う)
        public bool Loop;
    }

    private EnemyController _enemy;
    private Playback _spine;
    private Playback _animatorPlayback;
    private float _animatorLastNormalized;
    private readonly List<Playback> _manual = new();

    public string CharacterId => _characterId;

    /// <summary>今右を向いているか。</summary>
    public bool FacingRight => _enemy != null ? _enemy.FacingRight : transform.lossyScale.x >= 0f;

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        if (_skeleton == null)
            _skeleton = GetComponentInChildren<SkeletonAnimation>(true);
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);
    }

    private void OnDisable()
    {
        _spine = null;
        _animatorPlayback = null;
        _manual.Clear();
    }

    /// <summary>
    /// コードで絵を差し替えているキャラ用: animation の開始としてエフェクトを出し始める
    /// (調整シーンのカタログに登録した連番アニメの名前を渡す)。
    /// </summary>
    public void Play(string animation)
    {
        var cues = FindCues(animation);
        if (cues == null)
            return;
        _manual.Add(new Playback
        {
            Animation = AnimEffectCueSet.NormalizeAnimation(animation), Cues = cues, LastTime = -1e-4f,
        });
    }

    /// <summary>エフェクトを出す位置 (ワールド座標)。</summary>
    public Vector3 CuePosition(AnimEffectCueSet.Cue cue)
    {
        var offset = cue.Offset;
        if (!FacingRight)
            offset.x = -offset.x;
        return transform.position + (Vector3)offset;
    }

    private void LateUpdate()
    {
        if (_cueSet == null)
            return;
        WatchSpine();
        WatchAnimator();
        UpdateManual();
    }

    #region アニメの見張り

    private void WatchSpine()
    {
        var state = _skeleton != null ? _skeleton.AnimationState : null;
        var entry = state?.GetTrack(0);
        if (entry == null || entry.Animation == null)
        {
            _spine = null;
            return;
        }

        if (_spine == null || _spine.Source != entry)
        {
            var name = AnimEffectCueSet.NormalizeAnimation(entry.Animation.Name);
            // 向きの切り替え (再生位置を引き継いで反対向きのアニメにする) は続きとして扱い、出したエフェクトを出し直さない
            var continued = _spine != null && _spine.Animation == name && entry.TrackTime > 1e-4f;
            _spine = new Playback
            {
                Source = entry,
                Animation = name,
                Cues = FindCues(name),
                LastTime = continued ? _spine.LastTime : -1e-4f,
            };
        }
        _spine.Length = entry.Animation.Duration;
        _spine.Loop = entry.Loop;
        Advance(_spine, entry.TrackTime);
    }

    private void WatchAnimator()
    {
        if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
        {
            _animatorPlayback = null;
            return;
        }

        var inTransition = _animator.IsInTransition(0);
        var info = inTransition ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
        var clips = inTransition ? _animator.GetNextAnimatorClipInfo(0) : _animator.GetCurrentAnimatorClipInfo(0);
        AnimationClip clip = null;
        var weight = -1f;
        foreach (var c in clips)
        {
            if (c.clip != null && c.weight > weight)
            {
                clip = c.clip;
                weight = c.weight;
            }
        }
        if (clip == null)
        {
            _animatorPlayback = null;
            return;
        }

        // 同じステートに入り直した時は再生位置が戻るので、それも開始として扱う
        var restarted = _animatorPlayback != null && info.normalizedTime + 1e-4f < _animatorLastNormalized;
        if (_animatorPlayback == null || !Equals(_animatorPlayback.Source, info.fullPathHash) || restarted)
        {
            _animatorPlayback = new Playback
            {
                Source = info.fullPathHash,
                Animation = clip.name,
                Cues = FindCues(clip.name),
                LastTime = -1e-4f,
            };
        }
        _animatorLastNormalized = info.normalizedTime;
        _animatorPlayback.Length = clip.length;
        _animatorPlayback.Loop = info.loop;
        Advance(_animatorPlayback, info.normalizedTime * clip.length);
    }

    private void UpdateManual()
    {
        for (var i = _manual.Count - 1; i >= 0; i--)
        {
            var p = _manual[i];
            var time = p.LastTime + Time.deltaTime;
            Advance(p, time);
            if (AllFired(p))
                _manual.RemoveAt(i);
        }
    }

    private static bool AllFired(Playback p)
    {
        foreach (var cue in p.Cues.Cues)
            if (cue.Delay >= p.LastTime)
                return false;
        return true;
    }

    #endregion

    /// <summary>再生位置を time まで進め、その間に開始時刻が来たエフェクトを出す。</summary>
    private void Advance(Playback p, float time)
    {
        var prev = p.LastTime;
        p.LastTime = time;
        if (p.Cues == null || time <= prev)
            return;

        foreach (var cue in p.Cues.Cues)
        {
            if (p.Loop && p.Length > 0.01f)
            {
                // 周回ごとに出す (1 フレームで何周も進んだ時は出しすぎないよう 1 回まで)
                var prevLap = Mathf.FloorToInt((prev - cue.Delay) / p.Length);
                var lap = Mathf.FloorToInt((time - cue.Delay) / p.Length);
                if (time >= cue.Delay && lap > prevLap)
                    Emit(cue);
            }
            else if (prev < cue.Delay && cue.Delay <= time)
            {
                Emit(cue);
            }
        }
    }

    private void Emit(AnimEffectCueSet.Cue cue)
    {
        var effect = _cueSet.FindEffect(cue.EffectId);
        if (effect == null)
            return;
        AnimEffectPlayer.Spawn(effect, CuePosition(cue), FacingRight, cue, cue.FollowCharacter ? this : null);
    }

    private AnimEffectCueSet.AnimationCues FindCues(string animation)
    {
        var cues = _cueSet != null ? _cueSet.Find(_characterId, animation) : null;
        return cues != null && cues.Cues.Count > 0 ? cues : null;
    }
}
