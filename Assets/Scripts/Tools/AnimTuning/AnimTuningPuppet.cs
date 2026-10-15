using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

/// <summary>
/// 調整シーンに出すキャラ・エフェクトの「人形」。
/// プレハブから作る時は、見た目以外のスクリプト (AI・攻撃・入力など) と物理を外し、
/// 見た目のアニメーションだけを時刻指定で動かせるようにする (<see cref="ITuningClip.Apply"/>)。
/// 対応するアニメ: Spine (SkeletonAnimation) / Animator のクリップ / SpriteFlipbook / 連番の絵。
/// </summary>
public sealed class AnimTuningPuppet
{
    /// <summary>時刻を指定して姿勢を決めるアニメ 1 本。</summary>
    public interface ITuningClip
    {
        /// <summary>一覧での表示名。</summary>
        string Name { get; }
        /// <summary>エフェクト設定の紐付けに使う名前 (ゲームの AnimEffectEmitter が見るアニメ名と同じ。左右の向き違いは 1 つにまとめる)。</summary>
        string Key { get; }
        float Duration { get; }
        /// <summary>time 秒目の姿勢にする (0〜Duration)。</summary>
        void Apply(float time);
    }

    public GameObject Root { get; }
    public IReadOnlyList<ITuningClip> Clips => _clips;

    private readonly List<ITuningClip> _clips = new();
    private readonly List<PlayableGraph> _graphs = new();
    private ITuningClip _lastApplied;

    private AnimTuningPuppet(GameObject root)
    {
        Root = root;
    }

    #region 生成

    /// <summary>プレハブを出し、見た目だけの人形にする。</summary>
    public static AnimTuningPuppet FromPrefab(GameObject prefab, Transform parent)
    {
        // Awake を走らせずに中身を外すため、非アクティブな入れ物の中で生成する
        var holder = new GameObject("(holder)");
        holder.SetActive(false);
        holder.transform.SetParent(parent, false);
        var go = Object.Instantiate(prefab, holder.transform);
        go.name = prefab.name;
        go.transform.localPosition = Vector3.zero;

        var flipbooks = ReadFlipbooks(go);
        HideSpinePlaceholder(go);
        StripToVisual(go);

        go.transform.SetParent(parent, false);
        Object.Destroy(holder);

        var puppet = new AnimTuningPuppet(go);
        puppet.CollectClips(flipbooks);
        return puppet;
    }

    /// <summary>Spine の骨格データから出す。</summary>
    public static AnimTuningPuppet FromSpine(SkeletonDataAsset data, string skin, Transform parent)
    {
        var comps = SkeletonAnimation.NewSkeletonAnimationGameObject(data);
        var go = comps.skeletonAnimation.gameObject;
        go.name = data.name;
        go.transform.SetParent(parent, false);
        if (!string.IsNullOrEmpty(skin))
        {
            comps.skeletonAnimation.Skeleton.SetSkin(skin);
            comps.skeletonAnimation.Skeleton.SetupPoseSlots();
        }

        var puppet = new AnimTuningPuppet(go);
        puppet.CollectClips(new List<(SpriteRenderer, Sprite[], float)>());
        return puppet;
    }

    /// <summary>連番の絵を描く空の人形 (エフェクト用)。</summary>
    public static AnimTuningPuppet FromFrames(string name, Sprite[] frames, float fps, int sortingOrder, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        var puppet = new AnimTuningPuppet(go);
        puppet.AddSpriteClip(name, name, sr, frames, fps);
        return puppet;
    }

    /// <summary>連番アニメを追加する (コードで絵を差し替えているキャラ用)。</summary>
    public void AddSpriteClips(IEnumerable<AnimTuningCatalog.SpriteClip> clips, string rendererPath)
    {
        SpriteRenderer sr = null;
        if (!string.IsNullOrEmpty(rendererPath))
        {
            var t = Root.transform.Find(rendererPath);
            if (t != null)
                sr = t.GetComponent<SpriteRenderer>();
        }
        if (sr == null)
            sr = Root.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null)
            return;

        foreach (var c in clips)
            AddSpriteClip(c.Name, c.Name, sr, c.Frames, c.Fps);
    }

    public void Destroy()
    {
        foreach (var g in _graphs)
            if (g.IsValid())
                g.Destroy();
        _graphs.Clear();
        if (Root != null)
            Object.Destroy(Root);
    }

    #endregion

    /// <summary>指定アニメの time 秒目の姿勢にする。</summary>
    public void Apply(ITuningClip clip, float time)
    {
        if (clip == null)
            return;
        if (_lastApplied != clip && clip is SpineClip spine)
            spine.Restart();
        _lastApplied = clip;
        clip.Apply(Mathf.Clamp(time, 0f, clip.Duration));
    }

    #region 見た目以外を外す

    private static List<(SpriteRenderer, Sprite[], float)> ReadFlipbooks(GameObject go)
    {
        var result = new List<(SpriteRenderer, Sprite[], float)>();
        var framesField = typeof(SpriteFlipbook).GetField("_frames", BindingFlags.Instance | BindingFlags.NonPublic);
        var fpsField = typeof(SpriteFlipbook).GetField("_fps", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var fb in go.GetComponentsInChildren<SpriteFlipbook>(true))
        {
            var frames = framesField?.GetValue(fb) as Sprite[];
            var fps = fpsField != null ? (float)fpsField.GetValue(fb) : 4f;
            if (frames != null && frames.Length > 0)
                result.Add((fb.GetComponent<SpriteRenderer>(), frames, fps));
        }
        return result;
    }

    /// <summary>
    /// Spine で描く敵は、ルートの仮の四角 (EnemyController の SpriteRenderer) の描画を実行時に止めている
    /// (GramineSpineVisual)。人形ではそのスクリプトを外すので、ここで同じように隠す。
    /// </summary>
    private static void HideSpinePlaceholder(GameObject go)
    {
        if (go.GetComponent<EnemyController>() == null || go.GetComponentInChildren<SkeletonAnimation>(true) == null)
            return;
        var placeholder = go.GetComponent<SpriteRenderer>();
        if (placeholder != null)
            placeholder.forceRenderingOff = true;
    }

    private static bool IsVisualScript(MonoBehaviour mb) =>
        mb.GetType().Namespace?.StartsWith("Spine", StringComparison.Ordinal) == true;

    /// <summary>Spine 以外のスクリプトを外し、物理を止める。他から必須にされているものは無効化だけする。</summary>
    private static void StripToVisual(GameObject go)
    {
        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;
        foreach (var audio in go.GetComponentsInChildren<AudioSource>(true))
            audio.enabled = false;

        // 依存 (RequireComponent) されていないものから順に外す
        for (var pass = 0; pass < 8; pass++)
        {
            var removed = false;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || IsVisualScript(mb) || IsRequiredByOthers(mb))
                    continue;
                Object.DestroyImmediate(mb);
                removed = true;
            }
            if (!removed)
                break;
        }
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null && !IsVisualScript(mb))
                mb.enabled = false;
    }

    private static bool IsRequiredByOthers(Component target)
    {
        var type = target.GetType();
        foreach (var other in target.GetComponents<Component>())
        {
            if (other == target || other == null)
                continue;
            foreach (RequireComponent req in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
            {
                if (Requires(req.m_Type0, type) || Requires(req.m_Type1, type) || Requires(req.m_Type2, type))
                    return true;
            }
        }
        return false;
    }

    private static bool Requires(Type required, Type actual) => required != null && required.IsAssignableFrom(actual);

    #endregion

    #region アニメの収集

    private void CollectClips(List<(SpriteRenderer renderer, Sprite[] frames, float fps)> flipbooks)
    {
        var skeletons = Root.GetComponentsInChildren<SkeletonAnimation>(true);
        var animators = Root.GetComponentsInChildren<Animator>(true)
            .Where(a => a.runtimeAnimatorController != null).ToArray();
        var showSource = skeletons.Length + animators.Length + flipbooks.Count > 1;

        foreach (var sa in skeletons)
        {
            var data = sa.skeletonDataAsset != null ? sa.skeletonDataAsset.GetSkeletonData(true) : null;
            if (data == null)
                continue;
            var prefix = showSource ? $"[Spine {sa.name}] " : "";
            // 左右の向き違い (L/名前_L と R/名前_R) は同じ動きなので 1 つだけ出す (エフェクトは右向き基準で 1 回調整すればよい)
            var keys = new HashSet<string>();
            foreach (var anim in data.Animations)
            {
                var key = AnimEffectCueSet.NormalizeAnimation(anim.Name);
                if (keys.Add(key))
                    _clips.Add(new SpineClip(prefix + key, key, sa, anim));
            }
        }

        foreach (var animator in animators)
        {
            var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            // コントローラーの遷移に姿勢を上書きされないよう外し、クリップを直接流す
            animator.runtimeAnimatorController = null;
            animator.enabled = true;
            var graph = PlayableGraph.Create($"AnimTuning {animator.name}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "out", animator);
            _graphs.Add(graph);
            var prefix = showSource ? $"[Animator {animator.name}] " : "";
            foreach (var clip in clips)
                _clips.Add(new AnimatorClip(prefix + clip.name, clip.name, graph, output, clip));
        }

        foreach (var (renderer, frames, fps) in flipbooks)
            AddSpriteClip($"[Flipbook {renderer.name}]", $"Flipbook {renderer.name}", renderer, frames, fps);
    }

    private void AddSpriteClip(string name, string key, SpriteRenderer renderer, Sprite[] frames, float fps)
    {
        if (renderer == null || frames == null || frames.Length == 0)
            return;
        _clips.Add(new SpriteClip(name, key, renderer, frames, fps));
    }

    #endregion

    #region アニメの種類

    private sealed class SpineClip : ITuningClip
    {
        private readonly SkeletonAnimation _skeleton;
        private readonly Spine.Animation _animation;
        private Spine.TrackEntry _entry;

        public SpineClip(string name, string key, SkeletonAnimation skeleton, Spine.Animation animation)
        {
            Name = name;
            Key = key;
            _skeleton = skeleton;
            _animation = animation;
        }

        public string Name { get; }
        public string Key { get; }
        public float Duration => Mathf.Max(_animation.Duration, 1f / 30f);

        public void Restart() => _entry = null;

        public void Apply(float time)
        {
            if (_entry == null || _skeleton.AnimationState.GetTrack(0) != _entry)
            {
                _skeleton.Skeleton.SetupPose();
                _entry = _skeleton.AnimationState.SetAnimation(0, _animation, false);
                _entry.MixDuration = 0f;
                _entry.TimeScale = 0f; // 時間は調整シーン側で決める
            }
            _entry.TrackTime = time;
        }
    }

    private sealed class AnimatorClip : ITuningClip
    {
        private readonly PlayableGraph _graph;
        private readonly AnimationPlayableOutput _output;
        private readonly AnimationClip _clip;
        private AnimationClipPlayable _playable;

        public AnimatorClip(string name, string key, PlayableGraph graph, AnimationPlayableOutput output, AnimationClip clip)
        {
            Name = name;
            Key = key;
            _graph = graph;
            _output = output;
            _clip = clip;
        }

        public string Name { get; }
        public string Key { get; }
        public float Duration => Mathf.Max(_clip.length, 1f / 30f);

        public void Apply(float time)
        {
            if (!_graph.IsValid())
                return;
            if (!_playable.IsValid())
                _playable = AnimationClipPlayable.Create(_graph, _clip);
            _output.SetSourcePlayable(_playable); // 同じ Animator の他のクリップから切り替わる場合がある
            _playable.SetTime(time);
            _graph.Evaluate();
        }
    }

    private sealed class SpriteClip : ITuningClip
    {
        private readonly SpriteRenderer _renderer;
        private readonly Sprite[] _frames;
        private readonly float _fps;

        public SpriteClip(string name, string key, SpriteRenderer renderer, Sprite[] frames, float fps)
        {
            Name = name;
            Key = key;
            _renderer = renderer;
            _frames = frames;
            _fps = Mathf.Max(fps, 0.01f);
        }

        public string Name { get; }
        public string Key { get; }
        public float Duration => _frames.Length / _fps;

        public void Apply(float time)
        {
            var i = Mathf.Clamp(Mathf.FloorToInt(time * _fps), 0, _frames.Length - 1);
            _renderer.sprite = _frames[i];
        }
    }

    #endregion
}
