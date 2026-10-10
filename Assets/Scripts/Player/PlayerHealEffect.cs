using UnityEngine;

/// <summary>
/// 回復した時にプレイヤーの体から緑色の光の粒を立ちのぼらせる演出。
/// パーティクルは素材 (<see cref="PlayerConsts.HealParticleMaterial"/>。URP の ParticlesUnlit) を加算合成にしてコードで組み立てる
/// (<see cref="SpeakerBreakEffect"/> と同じ作り方。新しい絵は使わない)。
/// <see cref="PlayerHealth"/> が HP が実際に増えた時に <see cref="Play"/> を呼ぶ。プレハブへの追加は不要 (初回に自動で付く)。
/// 見た目だけでゲーム進行には影響しない。
/// </summary>
public class PlayerHealEffect : MonoBehaviour
{
    // 骨格 (Spine) より手前に出す
    private const int OrderOffset = 5;

    private ParticleSystem _sparkles; // 体のまわりから立ちのぼる小さな粒
    private ParticleSystem _glow;     // 体を包む大きくぼんやりした光
    private Material _additive;
    private bool _built;

    /// <summary>回復の演出を出す。素材が設定されていなければ何もしない。</summary>
    public void Play(PlayerController player)
    {
        if (!_built)
            Build(player);
        if (_sparkles == null)
            return;

        _sparkles.Play(true);
        _glow.Play(true);
    }

    private void Build(PlayerController player)
    {
        _built = true;
        var consts = player.Consts;
        if (consts.HealParticleMaterial == null)
            return;

        _additive = new Material(consts.HealParticleMaterial) { name = consts.HealParticleMaterial.name + " (Heal Additive)" };
        _additive.SetFloat("_Blend", 2f);
        _additive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _additive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);

        // 体の中心と大きさは当たり判定から取る (見た目の絵は向きで反転するので使わない)
        var body = GetComponent<Collider2D>();
        var size = body != null ? (Vector2)body.bounds.size : new Vector2(0.8f, 1.6f);
        var center = body != null ? (Vector2)(body.bounds.center - transform.position) : new Vector2(0f, size.y * 0.5f);

        // 描画順は骨格のすぐ手前
        var skeleton = GetComponentInChildren<MeshRenderer>();
        var layerId = skeleton != null ? skeleton.sortingLayerID : 0;
        var order = (skeleton != null ? skeleton.sortingOrder : 0) + OrderOffset;

        var color = consts.HealParticleColor;
        var duration = Mathf.Max(0.2f, consts.HealDuration);

        _sparkles = NewSystem("HealSparkles", center, size, layerId, order + 1);
        var main = _sparkles.main;
        main.duration = duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
        var em = _sparkles.emission;
        em.rateOverTime = 22f / duration; // 回復の硬直中に 20 粒ほど
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        var vel = _sparkles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var sizeOver = _sparkles.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        var col = _sparkles.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(0.1f, 0.55f);

        _glow = NewSystem("HealGlow", center, size * 0.6f, layerId, order);
        main = _glow.main;
        main.duration = duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x * 0.9f, size.x * 1.4f);
        main.startColor = new Color(color.r, color.g, color.b, 0.35f);
        em = _glow.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3), new ParticleSystem.Burst(duration * 0.5f, 2) });
        vel = _glow.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        col = _glow.colorOverLifetime;
        col.enabled = true;
        col.color = Fade(0.25f, 0.4f);
    }

    /// <summary>体の範囲 (箱) から粒を出すパーティクルを子に作る。止めた状態で返す。</summary>
    private ParticleSystem NewSystem(string systemName, Vector2 localCenter, Vector2 area, int layerId, int order)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localCenter;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        // 立ちのぼった粒がプレイヤーに付いて動かないよう世界座標で流す
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // プレイヤーは向きで localScale.x を反転するので、粒の大きさには親の拡縮を効かせない
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 80;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(area.x, area.y, 1f);
        shape.rotation = Vector3.zero;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = _additive;
        r.sortingLayerID = layerId;
        r.sortingOrder = order;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    /// <summary>fadeIn までに現れて fadeOutStart から消えていく白のグラデーション (色は startColor で決める)。</summary>
    private static ParticleSystem.MinMaxGradient Fade(float fadeIn, float fadeOutStart)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
        return new ParticleSystem.MinMaxGradient(g);
    }

    private void OnDestroy()
    {
        if (_additive != null)
            Destroy(_additive);
    }
}
