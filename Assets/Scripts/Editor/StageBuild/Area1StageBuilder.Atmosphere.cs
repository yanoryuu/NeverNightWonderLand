using System.Linq;
using NeverNight.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace NeverNight.StageBuild
{
    /// <summary>
    /// 雰囲気 (ポストプロセス + 環境パーティクル) の組み立て。
    /// ・ステージ側: <see cref="Atmosphere"/> がシーンに StageAtmosphere (種類の指定だけ) を置く。
    /// ・PlayerScene 側: <see cref="SetupPlayerAtmosphere"/> が Main Camera の子 "Atmosphere" に
    ///   AtmosphereDirector + Base/Mood の Global Volume + 種類ごとの環境パーティクルを組む。
    /// ・アセット: <see cref="PrepareAtmosProfiles"/> (Assets/Settings/Volumes/) と
    ///   <see cref="PrepareAtmosMaterials"/> (Area1Gen/atmos/mat/。atmos/ の PNG があれば使い、無ければ URP 既定の丸いぼかし)。
    /// 手順と値は Docs/Area1/SceneBuildGuide.md §15。何度流してもよい (作り直し)。
    /// </summary>
    public static partial class Area1StageBuilder
    {
        public const string VolumeDir = "Assets/Settings/Volumes";
        public const string AtmosDir = "Assets/LocalData/StageAsset/Area1Gen/atmos";
        public const string AtmosMatDir = AtmosDir + "/mat";

        /// <summary>環境パーティクルの並び順: 空中 (背景より手前・岩の塗り (Backfill -10) より奥) / 前景 (プレイヤー・地形より手前)。</summary>
        public const int OrderAtmosMid = -12, OrderAtmosFront = 30;

        /// <summary>カメラの周りの発生範囲 (正射影 size 5.5 の 16:9 = 19.6×11 より一回り大きく)。</summary>
        private static readonly Vector3 AtmosBox = new Vector3(26f, 16f, 0.1f);

        // ================================================================== ステージ側

        /// <summary>
        /// シーンの雰囲気の種類: ボスの部屋を含む → Boss / 拠点 (A1_H) → Haven / 屋外の部屋を含む → Outdoor (Background() と同じ判定) / それ以外 → Underground。
        /// </summary>
        public static AtmosphereKind AtmosphereKindOf(string sceneId)
        {
            var rooms = Area1Layout.Load().RoomsInScene(sceneId).ToList();
            if (rooms.Any(r => !string.IsNullOrEmpty(r.Boss)))
                return AtmosphereKind.Boss;
            if (sceneId == "A1_H")
                return AtmosphereKind.Haven;
            return rooms.Any(r => r.Outdoor) ? AtmosphereKind.Outdoor : AtmosphereKind.Underground;
        }

        /// <summary>
        /// 開いているシーンに StageAtmosphere (ルートの "StageAtmosphere") を置いて種類を設定する。既にあれば種類だけ更新。
        /// kind を省くと <see cref="AtmosphereKindOf"/> で決める。他の物には触らない。
        /// </summary>
        public static string Atmosphere(string sceneId, AtmosphereKind? kind = null)
        {
            var k = kind ?? AtmosphereKindOf(sceneId);
            var scene = ActiveScene;
            var existing = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<StageAtmosphere>(true)).ToList();
            StageAtmosphere comp;
            if (existing.Count > 0)
            {
                comp = existing[0];
                for (var i = 1; i < existing.Count; i++)
                    Object.DestroyImmediate(existing[i].gameObject);
            }
            else
            {
                var go = new GameObject("StageAtmosphere");
                SceneManager_MoveToActive(go);
                comp = go.AddComponent<StageAtmosphere>();
            }
            Set(comp, "_kind", (int)k);
            EditorSceneManager.MarkSceneDirty(scene);
            return $"[Atmosphere] {sceneId} = {k}";
        }

        private static void SceneManager_MoveToActive(GameObject go) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, ActiveScene);

        // ================================================================== Volume プロファイル

        /// <summary>プロファイルのパス (A1_Base / A1_Underground / A1_Outdoor / A1_Boss / A1_Haven)。</summary>
        public static string ProfilePath(string name) => $"{VolumeDir}/{name}.asset";

        /// <summary>種類ごとの Mood プロファイル名。</summary>
        public static string ProfileName(AtmosphereKind kind) => "A1_" + kind;

        /// <summary>Base と 4 種類の Mood プロファイルを作り直す (中身を消して値を入れ直す)。</summary>
        public static string PrepareAtmosProfiles()
        {
            EnsureFolder(VolumeDir);

            // 全種類共通: 色が濁らない Neutral、加算の光 (HDR で 1 を超える所) だけ少しにじむ高めの閾値、弱いビネット・ごく弱い粒子
            var b = ResetProfile("A1_Base");
            Comp<Tonemapping>(b).mode.Override(TonemappingMode.Neutral);
            var bloom = Comp<Bloom>(b);
            bloom.threshold.Override(0.9f);   // 月・白い星・加算の光だけが少しにじむ
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.65f);
            bloom.clamp.Override(16f);
            bloom.highQualityFiltering.Override(false);
            var ca = Comp<ColorAdjustments>(b);
            ca.postExposure.Override(0.4f);    // Neutral で沈む分を戻す (元の明るさに揃える)
            ca.contrast.Override(8f);
            ca.saturation.Override(8f);
            var vig = Comp<Vignette>(b);
            vig.color.Override(Color.black);
            vig.intensity.Override(0.2f);
            vig.smoothness.Override(0.45f);
            var grain = Comp<FilmGrain>(b);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.1f);
            grain.response.Override(0.85f);
            Save(b);

            // 地下: 冷たい青紫、コントラスト強め、暗部を締める
            var u = ResetProfile(ProfileName(AtmosphereKind.Underground));
            var uca = Comp<ColorAdjustments>(u);
            uca.contrast.Override(6f);         // 元が暗いので強くしすぎない (18 だと潰れた)
            uca.saturation.Override(2f);
            uca.colorFilter.Override(new Color(0.93f, 0.95f, 1.06f));
            var usmh = Comp<ShadowsMidtonesHighlights>(u);
            usmh.shadows.Override(new Vector4(0.92f, 0.94f, 1.1f, -0.03f));
            usmh.midtones.Override(new Vector4(0.98f, 0.98f, 1.03f, 0f));
            var uv = Comp<Vignette>(u);
            uv.color.Override(new Color(0.02f, 0f, 0.06f));
            uv.intensity.Override(0.3f);
            uv.smoothness.Override(0.45f);
            Save(u);

            // 地上の夜: 紫〜紺、灯や星がにじむ (Bloom 少し強め・広め、ラベンダー寄り)
            var o = ResetProfile(ProfileName(AtmosphereKind.Outdoor));
            var oca = Comp<ColorAdjustments>(o);
            oca.contrast.Override(10f);
            oca.saturation.Override(10f);
            oca.colorFilter.Override(new Color(0.95f, 0.92f, 1f));
            var ost = Comp<SplitToning>(o);
            ost.shadows.Override(new Color(0.5f, 0.47f, 0.58f));   // 強いとプレイヤーまで紫に染まる
            ost.highlights.Override(new Color(0.56f, 0.53f, 0.5f));
            ost.balance.Override(-25f);
            var ob = Comp<Bloom>(o);
            ob.intensity.Override(0.85f);
            ob.scatter.Override(0.72f);
            ob.tint.Override(new Color(1f, 0.92f, 1f));
            var ov = Comp<Vignette>(o);
            ov.color.Override(new Color(0.03f, 0.01f, 0.1f));
            ov.intensity.Override(0.24f);
            ov.smoothness.Override(0.5f);
            Save(o);

            // ボス部屋: 少し赤み、ビネット強め
            var bo = ResetProfile(ProfileName(AtmosphereKind.Boss));
            var bca = Comp<ColorAdjustments>(bo);
            bca.contrast.Override(10f);
            bca.saturation.Override(6f);
            bca.colorFilter.Override(new Color(1f, 0.9f, 0.88f));
            var bsmh = Comp<ShadowsMidtonesHighlights>(bo);
            bsmh.shadows.Override(new Vector4(1.1f, 0.94f, 0.96f, -0.08f));
            var bv = Comp<Vignette>(bo);
            bv.color.Override(new Color(0.12f, 0f, 0.02f));
            bv.intensity.Override(0.38f);
            bv.smoothness.Override(0.42f);
            var bb = Comp<Bloom>(bo);
            bb.intensity.Override(0.8f);
            bb.tint.Override(new Color(1f, 0.85f, 0.75f));
            Save(bo);

            // 拠点: 暖かく落ち着いた色味
            var h = ResetProfile(ProfileName(AtmosphereKind.Haven));
            var hca = Comp<ColorAdjustments>(h);
            hca.contrast.Override(6f);
            hca.saturation.Override(8f);
            hca.colorFilter.Override(new Color(1f, 0.95f, 0.88f));
            var hv = Comp<Vignette>(h);
            hv.color.Override(new Color(0.06f, 0.02f, 0f));
            hv.intensity.Override(0.24f);
            hv.smoothness.Override(0.5f);
            var hb = Comp<Bloom>(h);
            hb.intensity.Override(0.75f);
            hb.tint.Override(new Color(1f, 0.9f, 0.78f));
            Save(h);

            AssetDatabase.SaveAssets();
            return "[Atmosphere] profiles: A1_Base, A1_Underground, A1_Outdoor, A1_Boss, A1_Haven";
        }

        private static VolumeProfile ResetProfile(string name)
        {
            var path = ProfilePath(name);
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(p, path);
            }
            foreach (var c in p.components.ToList())
                Object.DestroyImmediate(c, true);
            p.components.Clear();
            return p;
        }

        private static T Comp<T>(VolumeProfile p) where T : VolumeComponent
        {
            var c = p.Add<T>();
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, p);
            return c;
        }

        private static void Save(VolumeProfile p)
        {
            foreach (var c in p.components)
                EditorUtility.SetDirty(c);
            EditorUtility.SetDirty(p);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        // ================================================================== パーティクルのマテリアル

        /// <summary>
        /// 環境パーティクルのマテリアル (URP Particles/Unlit) を Area1Gen/atmos/mat/ に作る/更新する:
        /// A1_AtmosDust (通常合成, p_soft_dot) / A1_AtmosGlow (加算, p_glow) / A1_AtmosEmber (加算, p_ember) /
        /// A1_AtmosSparkle (加算, p_sparkle)。PNG がまだ無ければ URP 既定の丸いぼかしのまま。PNG が届いたら流し直すと差し替わる。
        /// 加算のものは _BaseColor を 1 超え (HDR) にして Bloom で少しにじませる。
        /// </summary>
        public static string PrepareAtmosMaterials()
        {
            EnsureFolder(AtmosMatDir);
            var log = new System.Text.StringBuilder("[Atmosphere] materials:");
            log.Append(AtmosMaterial("A1_AtmosDust", "p_soft_dot.png", false, Color.white));
            log.Append(AtmosMaterial("A1_AtmosGlow", "p_glow.png", true, new Color(1.8f, 1.8f, 1.8f, 1f)));
            log.Append(AtmosMaterial("A1_AtmosEmber", "p_ember.png", true, new Color(2.2f, 2.2f, 2.2f, 1f)));
            log.Append(AtmosMaterial("A1_AtmosSparkle", "p_sparkle.png", true, new Color(2f, 2f, 2f, 1f)));
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        /// <summary>環境パーティクルのマテリアル (PrepareAtmosMaterials 済み)。</summary>
        public static Material AtmosMat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{AtmosMatDir}/{name}.mat");
            if (m == null)
                throw new System.ArgumentException("material not found: " + name + " (PrepareAtmosMaterials 済み?)");
            return m;
        }

        private static string AtmosMaterial(string name, string texFile, bool additive, Color tint)
        {
            var path = $"{AtmosMatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(ParticleMaterial) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.CopyPropertiesFromMaterial(ParticleMaterial);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{AtmosDir}/{texFile}");
            if (tex != null)
                m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHAMODULATE_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return $" {name}({(tex != null ? texFile : "default")})";
        }

        // ================================================================== PlayerScene 側

        /// <summary>
        /// 開いている PlayerScene の Main Camera に雰囲気一式を組む (子 "Atmosphere" を作り直す):
        /// カメラの Post Processing を有効化 (+ ディザ)、AtmosphereDirector、Base Volume (priority 0) / Mood Volume (priority 10)、
        /// 種類ごとの環境パーティクル。プロファイル・マテリアルも作り直す。保存は呼び出し側で。
        /// </summary>
        public static string SetupPlayerAtmosphere()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine(PrepareAtmosProfiles());
            log.AppendLine(PrepareAtmosMaterials());

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("PlayerScene");
            if (!scene.IsValid() || !scene.isLoaded)
                throw new System.InvalidOperationException("PlayerScene が開いていない");
            var cam = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(c => c.CompareTag("MainCamera")) ?? throw new System.InvalidOperationException("Main Camera が無い");
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.dithering = true;          // 暗い夜空のグラデーションの縞を抑える
            data.volumeLayerMask = data.volumeLayerMask | 1; // Default (Volume を置く層)
            EditorUtility.SetDirty(data);

            var old = cam.transform.Find("Atmosphere");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Atmosphere");
            root.transform.SetParent(cam.transform, false);
            root.transform.localPosition = new Vector3(0f, 0f, -cam.transform.position.z); // ワールド z=0 (ステージの面)

            var baseVol = NewVolume(root.transform, "BaseVolume", 0f, AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath("A1_Base")));
            var moodVol = NewVolume(root.transform, "MoodVolume", 10f, null);

            var kinds = new[] { AtmosphereKind.Underground, AtmosphereKind.Outdoor, AtmosphereKind.Boss, AtmosphereKind.Haven };
            var presets = new System.Collections.Generic.List<AtmosphereDirector.Preset>();
            foreach (var k in kinds)
            {
                var group = new GameObject("Particles_" + k);
                group.transform.SetParent(root.transform, false);
                BuildAmbient(group.transform, k);
                group.SetActive(false);
                presets.Add(new AtmosphereDirector.Preset
                {
                    Kind = k,
                    Profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath(ProfileName(k))),
                    Particles = group,
                });
            }

            var director = root.AddComponent<AtmosphereDirector>();
            var so = new SerializedObject(director);
            so.FindProperty("_baseVolume").objectReferenceValue = baseVol;
            so.FindProperty("_moodVolume").objectReferenceValue = moodVol;
            var arr = so.FindProperty("_presets");
            arr.arraySize = presets.Count;
            for (var i = 0; i < presets.Count; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Kind").enumValueIndex = (int)presets[i].Kind;
                e.FindPropertyRelative("Profile").objectReferenceValue = presets[i].Profile;
                e.FindPropertyRelative("Particles").objectReferenceValue = presets[i].Particles;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            log.Append($"[Atmosphere] PlayerScene: camera pp=on, presets={presets.Count}");
            return log.ToString();
        }

        private static Volume NewVolume(Transform parent, string name, float priority, VolumeProfile profile)
        {
            var go = new GameObject(name) { layer = 0 };
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = priority;
            v.weight = 0f;   // AtmosphereDirector が種類に合わせて上げる
            v.sharedProfile = profile;
            return v;
        }

        // ================================================================== 環境パーティクルの中身

        private static Gradient AlphaCurve(params (float t, float a)[] keys)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                keys.Select(k => new GradientAlphaKey(k.a, k.t)).ToArray());
            return g;
        }

        private static Gradient FadeInOut => AlphaCurve((0f, 0f), (0.2f, 1f), (0.8f, 1f), (1f, 0f));
        private static Gradient Twinkle => AlphaCurve((0f, 0f), (0.15f, 1f), (0.3f, 0.3f), (0.45f, 1f), (0.6f, 0.35f), (0.78f, 1f), (1f, 0f));
        private static Gradient Flicker => AlphaCurve((0f, 0f), (0.08f, 1f), (0.25f, 0.55f), (0.4f, 1f), (0.6f, 0.65f), (0.8f, 0.9f), (1f, 0f));
        private static Gradient Blink => AlphaCurve((0f, 0f), (0.5f, 1f), (1f, 0f));

        /// <summary>種類ごとの環境パーティクルを group の子に作る (数は控えめ。中景 = OrderAtmosMid、前景 = OrderAtmosFront)。</summary>
        private static void BuildAmbient(Transform group, AtmosphereKind kind)
        {
            var dust = AtmosMat("A1_AtmosDust");
            var glow = AtmosMat("A1_AtmosGlow");
            var ember = AtmosMat("A1_AtmosEmber");
            var sparkle = AtmosMat("A1_AtmosSparkle");
            switch (kind)
            {
                case AtmosphereKind.Underground:
                    Ambient(group, "Dust", dust, OrderAtmosMid, 7f, 110, new Vector2(10f, 16f), new Vector2(0.12f, 0.3f),
                        new Color(0.9f, 0.9f, 1f, 0.85f), new Color(0.72f, 0.74f, 0.95f, 0.45f),
                        new Vector2(-0.12f, 0.12f), new Vector2(-0.06f, 0.08f), 0.12f, 0.25f, FadeInOut);
                    Ambient(group, "DustFront", dust, OrderAtmosFront, 0.35f, 6, new Vector2(7f, 10f), new Vector2(0.3f, 0.5f),
                        new Color(0.8f, 0.8f, 1f, 0.1f), new Color(0.7f, 0.7f, 0.9f, 0.06f),
                        new Vector2(-0.25f, 0.25f), new Vector2(-0.08f, 0.08f), 0.1f, 0.2f, FadeInOut);
                    Ambient(group, "Sparks", ember, OrderAtmosMid, 0.8f, 12, new Vector2(2.5f, 4f), new Vector2(0.1f, 0.2f),
                        new Color(1f, 0.55f, 0.2f, 1f), new Color(1f, 0.8f, 0.4f, 0.8f),
                        new Vector2(-0.2f, 0.2f), new Vector2(0.2f, 0.7f), 0.35f, 0.8f, Flicker);
                    break;
                case AtmosphereKind.Outdoor:
                    Ambient(group, "Fireflies", glow, OrderAtmosMid, 3f, 45, new Vector2(7f, 11f), new Vector2(0.25f, 0.45f),
                        new Color(1f, 0.75f, 0.3f, 0.95f), new Color(0.8f, 0.65f, 1f, 0.85f),
                        new Vector2(-0.1f, 0.1f), new Vector2(-0.08f, 0.1f), 0.35f, 0.2f, Twinkle);
                    Ambient(group, "Motes", dust, OrderAtmosMid, 3f, 50, new Vector2(10f, 14f), new Vector2(0.08f, 0.16f),
                        new Color(0.7f, 0.75f, 1f, 0.3f), new Color(0.85f, 0.75f, 1f, 0.15f),
                        new Vector2(0.05f, 0.2f), new Vector2(-0.05f, 0.05f), 0.1f, 0.25f, FadeInOut);
                    Ambient(group, "Sparkle", sparkle, OrderAtmosMid, 0.6f, 8, new Vector2(1.5f, 2.5f), new Vector2(0.15f, 0.25f),
                        new Color(1f, 1f, 1f, 0.9f), new Color(0.85f, 0.8f, 1f, 0.7f),
                        Vector2.zero, Vector2.zero, 0f, 0f, Blink, randomRotation: true);
                    Ambient(group, "GlowFront", glow, OrderAtmosFront, 0.2f, 4, new Vector2(8f, 12f), new Vector2(0.35f, 0.55f),
                        new Color(1f, 0.85f, 0.6f, 0.16f), new Color(0.8f, 0.72f, 1f, 0.12f),
                        new Vector2(-0.15f, 0.15f), new Vector2(-0.05f, 0.08f), 0.2f, 0.15f, FadeInOut);
                    break;
                case AtmosphereKind.Boss:
                    Ambient(group, "Embers", ember, OrderAtmosMid, 7f, 70, new Vector2(4f, 7f), new Vector2(0.1f, 0.24f),
                        new Color(1f, 0.45f, 0.15f, 1f), new Color(1f, 0.75f, 0.35f, 0.85f),
                        new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 1f), 0.4f, 0.6f, Flicker);
                    Ambient(group, "EmbersFront", ember, OrderAtmosFront, 0.7f, 8, new Vector2(3f, 5f), new Vector2(0.12f, 0.2f),
                        new Color(1f, 0.5f, 0.2f, 0.7f), new Color(1f, 0.7f, 0.3f, 0.5f),
                        new Vector2(-0.4f, 0.4f), new Vector2(0.8f, 1.6f), 0.5f, 0.5f, Flicker);
                    Ambient(group, "Ash", dust, OrderAtmosMid, 2f, 30, new Vector2(8f, 12f), new Vector2(0.12f, 0.24f),
                        new Color(0.35f, 0.28f, 0.32f, 0.4f), new Color(0.55f, 0.42f, 0.46f, 0.22f),
                        new Vector2(-0.12f, 0.12f), new Vector2(-0.15f, -0.05f), 0.15f, 0.3f, FadeInOut);
                    break;
                case AtmosphereKind.Haven:
                    Ambient(group, "WarmMotes", glow, OrderAtmosMid, 2.5f, 30, new Vector2(8f, 12f), new Vector2(0.12f, 0.24f),
                        new Color(1f, 0.8f, 0.5f, 0.7f), new Color(1f, 0.65f, 0.4f, 0.5f),
                        new Vector2(-0.08f, 0.08f), new Vector2(0f, 0.1f), 0.2f, 0.2f, FadeInOut);
                    Ambient(group, "Dust", dust, OrderAtmosMid, 3f, 40, new Vector2(10f, 14f), new Vector2(0.08f, 0.18f),
                        new Color(1f, 0.9f, 0.78f, 0.3f), new Color(0.9f, 0.8f, 0.7f, 0.15f),
                        new Vector2(-0.08f, 0.08f), new Vector2(-0.04f, 0.06f), 0.1f, 0.25f, FadeInOut);
                    break;
            }
        }

        /// <summary>
        /// カメラの周り (AtmosBox) に漂う粒を 1 種類作る。ワールド空間シミュレーション + prewarm (入った瞬間から満ちている)。
        /// rate = 毎秒の発生数 (範囲全体)、max = 上限。色は a〜b のランダム、alpha は寿命に沿ったアルファ。
        /// </summary>
        private static ParticleSystem Ambient(Transform parent, string name, Material mat, int order, float rate, int max,
            Vector2 life, Vector2 size, Color a, Color b, Vector2 velX, Vector2 velY, float noise, float noiseFreq,
            Gradient alpha, bool randomRotation = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.startRotation = randomRotation ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 0.5f) : 0f;
            main.maxParticles = max;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = AtmosBox;
            var vel = ps.velocityOverLifetime;
            vel.enabled = velX != Vector2.zero || velY != Vector2.zero;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(velX.x, velX.y);
            vel.y = new ParticleSystem.MinMaxCurve(velY.x, velY.y);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var nz = ps.noise;
            nz.enabled = noise > 0f;
            nz.strength = noise;
            nz.frequency = noiseFreq;
            nz.scrollSpeed = 0.1f;
            nz.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = alpha;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = order;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }
    }
}
