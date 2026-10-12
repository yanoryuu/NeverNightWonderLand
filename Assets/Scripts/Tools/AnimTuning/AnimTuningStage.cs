using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// アニメ・エフェクト調整シーン (Assets/Scenes/Test/AnimEffectTuningScene) の本体。
/// カタログから選んだキャラを人形として出し、選んだアニメを時刻指定で再生しながら、
/// <see cref="AnimEffectCueSet"/> に設定したエフェクトを「開始から Delay 秒後・足元から Offset の位置」に重ねて出す。
/// 時間はこのコンポーネントが進めるので、一時停止・コマ送り・シークができる (エフェクトも同じ時刻に合わせて表示する)。
/// 操作はエディタウィンドウ NeverNight/Anim &amp; Effect Tuner から行う (再生中のみ)。
/// </summary>
public class AnimTuningStage : MonoBehaviour
{
    [SerializeField] private AnimTuningCatalog _catalog;
    [SerializeField] private AnimEffectCueSet _cueSet;
    [Tooltip("キャラの足元になる位置")]
    [SerializeField] private Transform _anchor;
    [SerializeField] private Camera _camera;

    [Header("再生")]
    public bool Playing = true;
    public bool Loop = true;
    [Range(0.05f, 2f)] public float Speed = 1f;
    [Tooltip("ループの合間に空ける時間 (sec)")]
    public float LoopGap = 0.4f;
    public bool FacingRight = true;

    /// <summary>エディタウィンドウで選んでいるエフェクトの番号 (ギズモの強調用)。</summary>
    [HideInInspector] public int SelectedCue = -1;

    public AnimTuningCatalog Catalog => _catalog;
    public AnimEffectCueSet CueSet => _cueSet;
    public Transform Anchor => _anchor;
    public int CharacterIndex { get; private set; } = -1;
    public int ClipIndex { get; private set; } = -1;
    public AnimTuningPuppet Character { get; private set; }
    public AnimTuningPuppet.ITuningClip Clip =>
        Character != null && ClipIndex >= 0 && ClipIndex < Character.Clips.Count ? Character.Clips[ClipIndex] : null;
    public AnimTuningCatalog.CharacterEntry CharacterEntry =>
        _catalog != null && CharacterIndex >= 0 && CharacterIndex < _catalog.Characters.Count ? _catalog.Characters[CharacterIndex] : null;

    /// <summary>今のアニメの開始からの経過時間 (sec)。</summary>
    public float Time { get; set; }

    /// <summary>1 周の長さ = アニメとエフェクトのうち長い方。</summary>
    public float TimelineLength
    {
        get
        {
            var length = Clip?.Duration ?? 1f;
            foreach (var fx in _effects)
                length = Mathf.Max(length, fx.Cue.Delay + fx.Duration);
            return length;
        }
    }

    /// <summary>今のアニメに付いているエフェクト設定。</summary>
    public AnimEffectCueSet.AnimationCues CurrentCues =>
        _cueSet != null && CharacterEntry != null && Clip != null ? _cueSet.Find(CharacterEntry.Id, Clip.Name) : null;

    private readonly List<EffectInstance> _effects = new();
    private Transform _characterPivot;
    private Transform _effectRoot;

    private void Start()
    {
        _characterPivot = new GameObject("Character").transform;
        _characterPivot.SetParent(_anchor, false);
        _effectRoot = new GameObject("Effects").transform;
        _effectRoot.SetParent(_anchor, false);

        if (_catalog != null && _catalog.Characters.Count > 0)
            SelectCharacter(0);
    }

    #region 操作 (エディタウィンドウから呼ぶ)

    public void SelectCharacter(int index)
    {
        ClearEffects();
        Character?.Destroy();
        Character = null;
        ClipIndex = -1;
        CharacterIndex = index;

        var entry = CharacterEntry;
        if (entry == null)
            return;

        if (entry.Prefab != null)
            Character = AnimTuningPuppet.FromPrefab(entry.Prefab, _characterPivot);
        else if (entry.Spine != null)
            Character = AnimTuningPuppet.FromSpine(entry.Spine, entry.SpineSkin, _characterPivot);
        else
            return;

        Character.AddSpriteClips(entry.SpriteClips, entry.SpriteRendererPath);
        var t = Character.Root.transform;
        t.localPosition = entry.Offset;
        t.localScale = new Vector3(entry.FacesLeft ? -entry.Scale : entry.Scale, entry.Scale, 1f);

        if (Character.Clips.Count > 0)
            SelectClip(0);
        FrameCamera();
    }

    public void SelectClip(int index)
    {
        ClipIndex = index;
        Time = 0f;
        RebuildEffects();
    }

    /// <summary>エフェクト設定の追加・削除・種類の変更の後に呼ぶ (位置や時間だけの変更は不要)。</summary>
    public void RebuildEffects()
    {
        ClearEffects();
        var cues = CurrentCues;
        if (cues == null || _catalog == null)
            return;
        foreach (var cue in cues.Cues)
        {
            var entry = _catalog.FindEffect(cue.EffectId);
            _effects.Add(new EffectInstance(cue, entry, _effectRoot));
        }
    }

    /// <summary>キャラ全体が映るようにカメラを合わせる (Spine は描画されるまで大きさが分からないので数フレーム待つ)。</summary>
    public void FrameCamera()
    {
        StopAllCoroutines();
        StartCoroutine(FrameCameraLater());
    }

    private System.Collections.IEnumerator FrameCameraLater()
    {
        yield return null;
        yield return null;
        FrameCameraNow();
    }

    private void FrameCameraNow()
    {
        if (_camera == null || Character == null)
            return;
        var renderers = Character.Root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1))
            bounds.Encapsulate(r.bounds);
        bounds.Encapsulate(_anchor.position);
        // Spine は初回の描画まで大きさが 0 のことがあるので最低限の範囲を取る
        var halfHeight = Mathf.Max(bounds.extents.y, bounds.extents.x / Mathf.Max(_camera.aspect, 0.1f), 1.5f);
        _camera.orthographicSize = halfHeight * 1.8f;
        var p = bounds.center;
        _camera.transform.position = new Vector3(p.x, p.y, _camera.transform.position.z);
    }

    #endregion

    private void Update()
    {
        if (_anchor != null)
        {
            var s = _anchor.localScale;
            _anchor.localScale = new Vector3(FacingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x), s.y, s.z);
        }

        // 設定の数が変わったら作り直す (ウィンドウ以外 (Inspector・Undo) で変えた時用)
        var cues = CurrentCues;
        if ((cues?.Cues.Count ?? 0) != _effects.Count
            || (cues != null && cues.Cues.Where((c, i) => c != _effects[i].Cue || c.EffectId != _effects[i].EffectId).Any()))
            RebuildEffects();

        var clip = Clip;
        if (clip == null)
            return;

        if (Playing)
        {
            Time += UnityEngine.Time.deltaTime * Speed;
            var length = TimelineLength;
            if (Loop)
            {
                if (Time > length + LoopGap)
                    Time = 0f;
            }
            else if (Time > length)
            {
                Time = length;
                Playing = false;
            }
        }

        Character.Apply(clip, Time);
        foreach (var fx in _effects)
            fx.Apply(Time - fx.Cue.Delay);
    }

    private void ClearEffects()
    {
        foreach (var fx in _effects)
            fx.Destroy();
        _effects.Clear();
    }

    private void OnDestroy() => ClearEffects();

    private void OnDrawGizmos()
    {
        if (_anchor == null)
            return;
        var p = _anchor.position;
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawLine(p + Vector3.left * 6f, p + Vector3.right * 6f);
        Gizmos.DrawLine(p + Vector3.down * 0.3f, p + Vector3.up * 0.3f);

        var cues = Application.isPlaying ? CurrentCues : null;
        if (cues == null)
            return;
        for (var i = 0; i < cues.Cues.Count; i++)
        {
            var w = _anchor.TransformPoint(cues.Cues[i].Offset);
            Gizmos.color = i == SelectedCue ? Color.yellow : new Color(0.3f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireSphere(w, i == SelectedCue ? 0.25f : 0.15f);
        }
    }

    /// <summary>エフェクト設定 1 件ぶんの表示。</summary>
    private sealed class EffectInstance
    {
        public AnimEffectCueSet.Cue Cue { get; }
        public string EffectId { get; }
        public float Duration { get; }

        private readonly AnimTuningPuppet _puppet;
        private readonly AnimTuningPuppet.ITuningClip _clip;
        private readonly ParticleSystem[] _particles;

        public EffectInstance(AnimEffectCueSet.Cue cue, AnimTuningCatalog.EffectEntry entry, Transform parent)
        {
            Cue = cue;
            EffectId = cue.EffectId;
            if (entry == null)
                return;

            if (entry.Prefab != null)
                _puppet = AnimTuningPuppet.FromPrefab(entry.Prefab, parent);
            else if (entry.Frames != null && entry.Frames.Frames.Length > 0)
                _puppet = AnimTuningPuppet.FromFrames(entry.Frames, entry.SortingOrder, parent);
            if (_puppet == null)
                return;

            _clip = _puppet.Clips.Count > 0 ? _puppet.Clips[0] : null;
            // 入れ子のパーティクルは親の Simulate でまとめて進める
            _particles = _puppet.Root.GetComponentsInChildren<ParticleSystem>(true)
                .Where(ps => ps.transform.parent == null || ps.transform.parent.GetComponentInParent<ParticleSystem>(true) == null)
                .ToArray();
            var duration = _clip?.Duration ?? 0f;
            foreach (var ps in _puppet.Root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.useAutoRandomSeed = false; // 毎回同じ形で出す (シークしても揺れない)
                var main = ps.main;
                main.playOnAwake = false;
                duration = Mathf.Max(duration, main.duration + main.startLifetime.constantMax + main.startDelay.constantMax);
            }
            Duration = Mathf.Max(duration, 0.05f);
            _puppet.Root.SetActive(false);
        }

        public void Apply(float localTime)
        {
            if (_puppet == null)
                return;
            var go = _puppet.Root;
            var visible = localTime >= 0f && localTime <= Duration;
            if (go.activeSelf != visible)
                go.SetActive(visible);
            if (!visible)
                return;

            var t = go.transform;
            t.localPosition = Cue.Offset;
            t.localRotation = Quaternion.Euler(0f, 0f, Cue.Rotation);
            t.localScale = Vector3.one * Cue.Scale;

            foreach (var ps in _particles)
                ps.Simulate(localTime, true, true, true);
            if (_clip != null)
                _puppet.Apply(_clip, localTime);
        }

        public void Destroy() => _puppet?.Destroy();
    }
}
