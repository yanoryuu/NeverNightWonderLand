using System.Collections;
using UnityEngine;

/// <summary>
/// スピーカーが壊れる瞬間の演出 (プレイヤーが壊しても、ボスの流れ弾 (ForceBreak) でも同じ)。
/// - カメラをスピーカーへ寄せて少しズームイン (<see cref="CameraFocus"/>。画面外のスピーカーでも見せる)
/// - 一瞬のヒットストップ → スロー (<see cref="GamePause.HitStop"/> / <see cref="GamePause.SlowMotion"/>。
///   timeScale は GamePause がまとめて管理するので、ポーズ・速度調整と衝突しない)
/// - 爆発のパーティクル (閃光・火の玉・火花・煙の玉) + 金属片 (<see cref="BreakDebris"/>) + 画面揺れ
/// - 壊れた後は煙が出続ける (<see cref="StartSmoke"/>。再訪時もスピーカー側から呼ぶ)
/// パーティクルの素材は URP の ParticlesUnlit (既定の丸いぼかし) をビルダーが BreakableSpeaker に渡す (新しい絵は使わない)。
/// 演出中は <see cref="IsPlaying"/> が true (撤退の会話など、スローを壊す処理は終わるまで待つ)。
/// </summary>
public static class SpeakerBreakEffect
{
    // ---- 調整用 (実時間の秒) ----
    public static float HitStopTime = 0.07f;   // 壊れた瞬間の一瞬の止まり
    public static float SlowScale = 0.25f;     // スロー中の時間の進み (0.25 = 4 分の 1)
    public static float SlowTime = 1.2f;       // スローの長さ
    public static float FocusZoom = 0.72f;     // 寄せきった時の表示範囲 (0.72 = 約 1.4 倍に拡大)
    public static float FocusInTime = 0.22f;   // 寄るまで
    public static float FocusHoldTime = 0.95f; // 寄せたまま
    public static float FocusOutTime = 0.45f;  // 戻るまで
    public static float ShakeAmplitude = 0.35f;
    public static float ShakeTime = 0.7f;
    public static int DebrisCount = 7;
    public static Color DebrisTint = new Color(0.42f, 0.38f, 0.46f); // 黒っぽい筐体の破片
    public static float SmokeDelay = 0.6f;     // 爆発から煙が出始めるまで (実時間)

    // 爆発のパーティクルはスピーカーより手前に出す
    private const int ExplosionOrderOffset = 20;
    // 煙は壊れた絵 (OrderProp) と通常の物の間あたり。スピーカーの描画順 +1
    private const int SmokeOrderOffset = 1;

    private static Runner _runner;
    private static float _playingUntil;
    private static Material _additive;
    private static Material _additiveSource;

    /// <summary>演出 (スロー・カメラの寄せ) の最中か。</summary>
    public static bool IsPlaying => (_runner != null && Time.unscaledTime < _playingUntil) || GamePause.IsSlowed;

    /// <summary>
    /// 壊れる演出を出す。source = スピーカーの絵 (位置・大きさ・描画順の基準。呼び出し後に隠してよい)、
    /// material = パーティクルの素材 (null ならパーティクルは出さない)、away = 破片を飛ばす向きの偏り。
    /// </summary>
    public static void Play(SpriteRenderer source, Material material, Vector2 away)
    {
        if (source == null)
            return;
        EnsureRunner();

        var bounds = source.bounds;
        var center = (Vector2)bounds.center;

        GamePause.HitStop(HitStopTime);
        GamePause.SlowMotion(SlowScale, SlowTime);
        // 寄せ先はスピーカーの少し上 (足元の床より、壊れた筐体と立ち上る煙を見せる)
        CameraFocus.Request(center + new Vector2(0f, bounds.extents.y * 0.3f), FocusZoom, FocusInTime, FocusHoldTime,
            FocusOutTime);
        CameraShake.Shake(ShakeAmplitude, ShakeTime);
        _playingUntil = Time.unscaledTime + Mathf.Max(SlowTime, FocusInTime + FocusHoldTime + FocusOutTime);

        BreakDebris.Burst(source, BreakDebris.Kind.Rock, away, DebrisTint, DebrisCount);

        if (material != null)
            _runner.StartCoroutine(Explode(center, Mathf.Max(bounds.size.x, bounds.size.y), material,
                source.sortingLayerID, source.sortingOrder + ExplosionOrderOffset));
    }

    /// <summary>
    /// 壊れた後の煙 (ループ) を parent の子に出す。top = 煙の出口 (ワールド座標)。
    /// prewarm = 最初から立ち上っている状態で出す (再訪時)。parent ごと消えれば煙も消える。
    /// </summary>
    public static ParticleSystem StartSmoke(Transform parent, Vector2 top, float width, Material material,
        int sortingLayerId, int sortingOrder, bool prewarm)
    {
        if (material == null || parent == null)
            return null;

        var ps = NewSystem("BrokenSmoke", parent, top, material, sortingLayerId, sortingOrder + SmokeOrderOffset, false);
        var main = ps.main;
        main.loop = true;
        main.prewarm = prewarm;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        // 夜空・暗い地下のどちらでも見えるよう、明るめの灰色 (黒煙だと背景に沈む)
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.6f, 0.66f, 0.7f),
            new Color(0.78f, 0.76f, 0.8f, 0.6f));
        main.maxParticles = 60;

        var em = ps.emission;
        em.rateOverTime = 7f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = Mathf.Max(0.1f, width * 0.2f);
        shape.rotation = new Vector3(-90f, 0f, 0f); // 上へ

        // ゆらゆら横に流れながら上る
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.35f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.6f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(new Color(0.9f, 0.88f, 0.92f), new Color(0.6f, 0.58f, 0.62f), 0.15f, 0.6f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

        ps.Play();
        return ps;
    }

    #region 爆発

    private static IEnumerator Explode(Vector2 center, float size, Material alpha, int layerId, int order)
    {
        var root = new GameObject("[SpeakerExplosion]");
        root.transform.position = center;
        var additive = Additive(alpha);
        var s = Mathf.Clamp(size, 1f, 4f);

        // 閃光: 一瞬だけ大きく光る
        var flash = NewSystem("Flash", root.transform, center, additive, layerId, order + 3, true);
        {
            var main = flash.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 0f;
            main.startSize = s * 2.6f;
            main.startColor = new Color(1f, 0.95f, 0.75f, 1f);
            Burst(flash, 2);
            var sz = flash.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));
            var col = flash.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(Color.white, new Color(1f, 0.7f, 0.3f), 0f, 0.1f);
        }

        // 火の玉: 橙の玉がふくらんで暗くなりながら消える
        var fire = NewSystem("Fireball", root.transform, center, additive, layerId, order + 2, true);
        {
            var main = fire.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(s * 0.45f, s * 0.9f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.15f));
            var shape = fire.shape;
            shape.radius = s * 0.18f;
            Burst(fire, 14);
            var sz = fire.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.5f));
            var col = fire.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(1f, 0.9f, 0.6f), new Color(0.8f, 0.2f, 0.05f), 0f, 0.35f);
            Drag(fire, 3f);
        }

        // 火花: 細長く伸びて四方へ飛び、重力で落ちる
        var sparks = NewSystem("Sparks", root.transform, center, additive, layerId, order + 4, true);
        {
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.2f));
            main.gravityModifier = 2.2f;
            var shape = sparks.shape;
            shape.radius = s * 0.12f;
            Burst(sparks, 42);
            var col = sparks.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(1f, 1f, 0.8f), new Color(1f, 0.45f, 0.1f), 0f, 0.6f);
            Drag(sparks, 1.5f);
            var r = sparks.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.045f;
            r.lengthScale = 2f;
        }

        // 煙の玉: 爆発の後に残る黒煙 (ゆっくり膨らんで上へ)
        var smoke = NewSystem("SmokePuffs", root.transform, center, alpha, layerId, order + 1, true);
        {
            var main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(s * 0.5f, s * 0.95f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.22f, 0.2f, 0.24f, 0.9f),
                new Color(0.4f, 0.38f, 0.42f, 0.85f));
            main.gravityModifier = -0.15f;
            var shape = smoke.shape;
            shape.radius = s * 0.25f;
            Burst(smoke, 12, 0.04f);
            var sz = smoke.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.8f));
            var col = smoke.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(new Color(0.45f, 0.42f, 0.46f), new Color(0.3f, 0.28f, 0.32f), 0.08f, 0.5f);
            Drag(smoke, 2.5f);
        }

        flash.Play();
        fire.Play();
        sparks.Play();
        smoke.Play();

        // 実時間で進むパーティクルなので、片付けも実時間で待つ
        yield return new WaitForSecondsRealtime(3f);
        if (root != null)
            Object.Destroy(root);
    }

    #endregion

    #region 共通

    private static ParticleSystem NewSystem(string name, Transform parent, Vector2 position, Material material,
        int layerId, int order, bool unscaledTime)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.useUnscaledTime = unscaledTime;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 200;

        var em = ps.emission;
        em.rateOverTime = 0f;

        // 2D なので XY 平面の円から外向きに出す
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.rotation = Vector3.zero;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.sortingLayerID = layerId;
        r.sortingOrder = order;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    private static void Burst(ParticleSystem ps, int count, float time = 0f)
    {
        var em = ps.emission;
        em.enabled = true;
        em.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
    }

    private static void Drag(ParticleSystem ps, float drag)
    {
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 1000f;
        limit.drag = drag;
    }

    /// <summary>from → to の色で、fadeIn までに現れて fadeOutStart から消えていくグラデーション。</summary>
    private static ParticleSystem.MinMaxGradient Fade(Color from, Color to, float fadeIn, float fadeOutStart)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            fadeIn > 0f
                ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, Mathf.Max(0.01f, fadeOutStart)), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    /// <summary>素材の加算合成版 (閃光・火の玉・火花用)。素材ごとに 1 つ作って使い回す。</summary>
    private static Material Additive(Material source)
    {
        if (_additive != null && _additiveSource == source)
            return _additive;
        _additiveSource = source;
        _additive = new Material(source) { name = source.name + " (Additive)" };
        _additive.SetFloat("_Blend", 2f);
        _additive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _additive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        return _additive;
    }

    private static void EnsureRunner()
    {
        if (_runner != null)
            return;
        var go = new GameObject("[SpeakerBreakEffect]") { hideFlags = HideFlags.HideInHierarchy };
        Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<Runner>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _runner = null;
        _playingUntil = 0f;
        _additive = null;
        _additiveSource = null;
    }

    private sealed class Runner : MonoBehaviour
    {
    }

    #endregion
}
