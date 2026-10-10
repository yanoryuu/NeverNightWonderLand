using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 新素材 (2026-10-08 PendingChanges 42, Drive 6 フォルダ) の準備。何度流してもよい。
// 元画像は Drive の「アニメーション用 / door / button / speaker / lever / fountain」。PSD は py (psd-tools) で
// 状態ごとに合成し、コマどうしが同じキャンバス (= 同じピボット) になるよう切り抜いてある (手順は PendingChanges 42)。
// 1. インポート設定 (Sprite / FullRect / ミップマップなし / 非圧縮 / Bilinear。PPU とピボットは下の表)
// 2. 野良ウマ (Character/horse.png) のアニメーション (Animations/StrayHorse.controller: Idle / Gallop (Speed で速さ) / Hurt / Death)
// 3. Enemy.prefab = 地上の雑魚: 絵は子 Visual (野良ウマ + Animator)、ルートの SpriteRenderer は無効 (仮ボスはビルダーの BossRoom が戻す)
// 3b. 敵の HP / 防御ゲージ (StatusCanvas) の Fill が 100x100 の中心アンカー (= 1x1 単位の四角) になっていたのを、
//     枠 (GuardBg / HpBg, 110x10) いっぱいに伸ばす細いバーに直す (Enemy / FlyingEnemy)
// 4. Door.prefab / Lever.prefab (テストシーン用) を開き戸・レバーの新素材に
// unity command run_script --file Docs/Area1/SceneScripts/prep_newart.cs --entry PrepNewArt.Run --timeout_ms 600000 --timeout 620
public static class PrepNewArt
{
    const string Gimmick = "Assets/LocalData/StageAsset/gimmick";
    const string Prop = "Assets/LocalData/StageAsset/prop";
    const string Chara = "Assets/LocalData/Character";
    const string AnimDir = Chara + "/Animations";
    const string HorsePath = Chara + "/horse.png";
    const string ControllerPath = AnimDir + "/StrayHorse.controller";

    // ピボット (正規化, 左下 0,0)。値は SceneScripts の外で py が測った物 (PendingChanges 42)
    static readonly Vector2 DoorPivot = new Vector2(0.1738f, 0f);      // 閉じた扉 (真横向き) の中心の下端
    static readonly Vector2 LeverPivot = new Vector2(0.5f, 0.0321f);   // 柄の回転の中心 (台座の半円の中心, 床から 60px)
    static readonly Dictionary<string, Vector2> SpeakerPivot = new Dictionary<string, Vector2>
    {
        { "speaker_01", new Vector2(0.5813f, 0f) },
        { "speaker_02", new Vector2(0.5039f, 0f) },
        { "speaker_03", new Vector2(0.5674f, 0f) },
    };
    // 野良ウマ: ルート (当たり 1x1 の中心) = 柱の中心付近 x900 / 柱の下端 (y1400 = 地面) から 0.5 単位上 (PPU 850 → 425px)
    const float HorsePpu = 850f;
    static readonly Vector2 HorsePivot = new Vector2(900f / 2047f, 1f - 975f / 1447f);
    // 歯車の重馬 (地上の中ボス A1_MB2, PendingChanges 43): bigHorse.png (4093x2894, 左向き, 絵の範囲 x426..4001 y61..2793)。
    // ボスは Enemy.prefab ×2 (当たり 1x1 → 2x2)。絵の高さ 2732px を 1.3 単位 (×2 = 2.6) に = PPU 2100 → 胴 (x1023..3069) ≈ 当たりの幅 2。
    // ピボット = 胴の中心 x2046 / 蹄の下端 (下から 101px) から 0.5 単位 (1050px) 上 = 当たりの中心
    public const string BigHorsePath = Chara + "/bigHorse.png";
    const float BigHorsePpu = 2100f;
    static readonly Vector2 BigHorsePivot = new Vector2(2046f / 4093f, (101f + 1050f) / 2894f);

    public static string Run()
    {
        AssetDatabase.Refresh();
        var log = new System.Text.StringBuilder();

        foreach (var n in new[] { "01", "02", "03", "04" })
            Import($"{Gimmick}/door/door_{n}.png", 546f, DoorPivot, log);
        foreach (var n in new[] { "lever_01", "lever_02", "lever_01_red", "lever_01_teal" })
            Import($"{Gimmick}/lever/{n}.png", 546f, LeverPivot, log);
        foreach (var kv in SpeakerPivot)
            foreach (var suf in new[] { "a", "b", "broken" })
                Import($"{Gimmick}/speaker/{kv.Key}_{suf}.png", 546f, kv.Value, log);
        Import($"{Prop}/fountain_01.png", 546f, new Vector2(0.5f, 0f), log);
        Import($"{Prop}/fountain_02.png", 546f, new Vector2(0.5f, 0f), log);
        Import(HorsePath, HorsePpu, HorsePivot, log);
        Import($"{Chara}/blacksmith_batta.png", 450f, new Vector2(0.5f, 0f), log);
        Import(BigHorsePath, BigHorsePpu, BigHorsePivot, log);
        // まだ割り当てていない素材 (popduck / popbomb / オリゼ) はふつうの中心ピボットで取り込むだけ
        foreach (var p in new[] { "popduck", "oryze" })
            Import($"{Chara}/{p}.png", 546f, new Vector2(0.5f, 0.5f), log);
        for (var i = 1; i <= 5; i++)
            Import($"{Chara}/popbomb/popbomb_{i}.png", 546f, new Vector2(0.5f, 0.5f), log);

        var ctrl = BuildHorseController(log);
        PatchEnemyPrefab(ctrl, log);
        PatchGauge("Assets/Prefabs/Enemy.prefab", log);
        PatchGauge("Assets/Prefabs/FlyingEnemy.prefab", log);
        PatchDoorPrefab(log);
        PatchLeverPrefab(log);
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    /// <summary>bigHorse (歯車の重馬) の取り込み設定だけを流す (PendingChanges 43)。</summary>
    public static string ImportBigHorse()
    {
        var log = new System.Text.StringBuilder();
        Import(BigHorsePath, BigHorsePpu, BigHorsePivot, log);
        return log.ToString();
    }

    static void Import(string path, float ppu, Vector2 pivot, System.Text.StringBuilder log)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            log.AppendLine("MISSING " + path);
            return;
        }
        importer.GetSourceTextureWidthAndHeight(out var w, out var h);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var max = 2048;
        while (max < Mathf.Max(w, h) && max < 8192) max *= 2;
        importer.maxTextureSize = max;
        var s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(s);
        importer.SaveAndReimport();
        log.AppendLine($"import {path} {w}x{h} ppu={ppu} pivot={pivot} max={max}");
    }

    static Sprite S(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path)
                                    ?? throw new System.ArgumentException("sprite not found: " + path);

    // ------------------------------------------------------------------ 野良ウマのアニメーション

    static AnimationClip Clip(string name, bool loop, float len, (float t, float y, float rot, float x)[] keys)
    {
        var path = $"{AnimDir}/StrayHorse_{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        AnimationCurve C(System.Func<(float t, float y, float rot, float x), float> f)
        {
            var c = new AnimationCurve(keys.Select(k => new Keyframe(k.t, f(k))).ToArray());
            for (var i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            return c;
        }
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", C(k => k.x));
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", C(k => k.y));
        clip.SetCurve("", typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, len, 0f));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.Constant(0f, len, 0f));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Constant(0f, len, 0f));
        clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", C(k => k.rot));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimatorController BuildHorseController(System.Text.StringBuilder log)
    {
        if (!AssetDatabase.IsValidFolder(AnimDir))
            AssetDatabase.CreateFolder(Chara, "Animations");

        // 回転木馬の馬らしく、柱ごと上下に弾む。Gallop は Speed (水平速度) を再生速度に掛ける (速さ 1 で 1 周 0.9 秒 = 1 跳ね)
        var idle = Clip("Idle", true, 1.6f, new[] { (0f, 0f, 0f, 0f), (0.8f, 0.03f, 1.5f, 0f), (1.6f, 0f, 0f, 0f) });
        var gallop = Clip("Gallop", true, 0.9f, new[]
        {
            (0f, 0f, -4f, 0f), (0.25f, 0.12f, 5f, 0f), (0.45f, 0.14f, 6f, 0f), (0.7f, 0.03f, -2f, 0f), (0.9f, 0f, -4f, 0f),
        });
        var hurt = Clip("Hurt", true, 0.24f, new[]
        {
            (0f, 0f, -8f, 0f), (0.06f, 0.02f, -10f, 0.05f), (0.12f, 0f, -8f, -0.05f), (0.18f, 0.02f, -10f, 0.04f), (0.24f, 0f, -8f, 0f),
        });
        var death = Clip("Death", false, 0.3f, new[] { (0f, 0f, 0f, 0f), (0.3f, 0.1f, 25f, 0f) });

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl != null)
            AssetDatabase.DeleteAsset(ControllerPath);
        ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle");
        sIdle.motion = idle;
        var sGallop = sm.AddState("Gallop");
        sGallop.motion = gallop;
        sGallop.speedParameterActive = true;
        sGallop.speedParameter = "Speed";
        var sHurt = sm.AddState("Hurt");
        sHurt.motion = hurt;
        var sDeath = sm.AddState("Death");
        sDeath.motion = death;
        sm.defaultState = sIdle;

        AnimatorStateTransition T(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.08f;
            return t;
        }
        T(sIdle, sGallop).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        T(sGallop, sIdle).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        var toHurt = sm.AddAnyStateTransition(sHurt);
        toHurt.hasExitTime = false;
        toHurt.duration = 0.04f;
        toHurt.canTransitionToSelf = false;
        toHurt.AddCondition(AnimatorConditionMode.If, 0f, "IsHurt");
        T(sHurt, sIdle).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsHurt");
        var toDeath = sm.AddAnyStateTransition(sDeath);
        toDeath.hasExitTime = false;
        toDeath.duration = 0.04f;
        toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");
        EditorUtility.SetDirty(ctrl);
        log.AppendLine("controller " + ControllerPath);
        return ctrl;
    }

    // ------------------------------------------------------------------ プレハブ

    static void SetField(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field) ?? throw new System.ArgumentException(target.GetType().Name + "." + field);
        switch (value)
        {
            case bool b: p.boolValue = b; break;
            case float f: p.floatValue = f; break;
            case Color c: p.colorValue = c; break;
            case Vector3 v: p.vector3Value = v; break;
            case Object o: p.objectReferenceValue = o; break;
            case Sprite[] arr:
                p.arraySize = arr.Length;
                for (var i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static Transform Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
        }
        t.gameObject.layer = parent.gameObject.layer;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
        return t;
    }

    static void PatchEnemyPrefab(AnimatorController ctrl, System.Text.StringBuilder log)
    {
        const string path = "Assets/Prefabs/Enemy.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rootSr = root.GetComponent<SpriteRenderer>();
            rootSr.enabled = false; // 仮の四角 (仮ボスはビルダーの BossRoom がこちらを有効に戻して使う)
            var vis = Child(root.transform, "Visual");
            vis.SetSiblingIndex(0);
            var sr = GetOrAdd<SpriteRenderer>(vis.gameObject);
            sr.sprite = S(HorsePath);
            sr.color = Color.white;
            sr.sortingLayerID = rootSr.sortingLayerID;
            sr.sortingOrder = rootSr.sortingOrder;
            sr.sharedMaterial = rootSr.sharedMaterial;
            var anim = GetOrAdd<Animator>(vis.gameObject);
            anim.runtimeAnimatorController = ctrl;
            anim.cullingMode = AnimatorCullingMode.CullCompletely;
            var ea = GetOrAdd<EnemyAnimator>(root);
            SetField(ea, "_animator", anim);
            var ec = root.GetComponent<EnemyController>();
            SetField(ec, "_visual", sr);
            SetField(ec, "_visualFacesLeft", true);
            SetField(ec, "_hitFlashColor", new Color(1f, 0.45f, 0.45f));
            // HP ゲージは柱の先 (+1.14) より上へ
            var canvas = root.transform.Find("StatusCanvas") as RectTransform;
            if (canvas != null)
                canvas.anchoredPosition = new Vector2(0f, 1.35f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine("patched " + path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ゲージの Fill を親 (枠) いっぱい - 1px の細いバーに
    static void PatchGauge(string path, System.Text.StringBuilder log)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var n = 0;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "Fill" || rt.parent == null || !(rt.parent.name == "GuardBg" || rt.parent.name == "HpBg"))
                    continue;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(-2f, -2f);
                n++;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine($"gauge {path} fills={n}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Sprite[] DoorFrames() =>
        new[] { "01", "02", "03", "04" }.Select(n => S($"{Gimmick}/door/door_{n}.png")).ToArray();

    // 扉 (高さ 3, 当たり 1x3)。絵は子 Art (ピボット = 閉じた扉の下端の中心) をルートの下端に置く
    static void PatchDoorPrefab(System.Text.StringBuilder log)
    {
        const string path = "Assets/Prefabs/Door.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.transform.localScale = Vector3.one;
            var box = root.GetComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 3f);
            box.offset = Vector2.zero;
            var rootSr = root.GetComponent<SpriteRenderer>();
            if (rootSr != null)
                Object.DestroyImmediate(rootSr);
            var frames = DoorFrames();
            var art = Child(root.transform, "Art");
            var k = 3f / frames[0].bounds.size.y;
            art.localPosition = new Vector3(0f, -1.5f, 0f);
            art.localScale = new Vector3(k, k, 1f);
            var sr = GetOrAdd<SpriteRenderer>(art.gameObject);
            sr.sprite = frames[0];
            sr.sortingOrder = -1; // 開いた扉の前をプレイヤーが通る
            var door = root.GetComponent<Door>();
            SetField(door, "_swingRenderer", sr);
            SetField(door, "_swingFrames", frames);
            SetField(door, "_slideDuration", 0.6f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine("patched " + path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // レバー (台座 + 傾く柄)。ルートは従来どおり当たりの中心 (高さ 1.2 の柱の中心)。絵の子は床 (= ルート -0.6) に置く
    static void PatchLeverPrefab(System.Text.StringBuilder log)
    {
        const string path = "Assets/Prefabs/Lever.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            const float k = 0.6f;
            root.transform.localScale = Vector3.one;
            var box = root.GetComponent<BoxCollider2D>();
            box.size = new Vector2(1.4f, 2f);
            box.offset = new Vector2(0f, 0.4f);
            var rootSr = root.GetComponent<SpriteRenderer>();
            rootSr.sprite = null; // LeverSwitch が SpriteRenderer を要求するので残す (絵は子)
            rootSr.color = Color.white;
            var hingeY = -0.6f + 60f / 546f * k;
            var bas = Child(root.transform, "Base");
            bas.localPosition = new Vector3(0f, hingeY, 0f);
            bas.localScale = new Vector3(k, k, 1f);
            var bsr = GetOrAdd<SpriteRenderer>(bas.gameObject);
            bsr.sprite = S($"{Gimmick}/lever/lever_02.png");
            bsr.sortingOrder = rootSr.sortingOrder;
            var handle = Child(root.transform, "Handle");
            handle.localPosition = new Vector3(0f, hingeY, 0f);
            handle.localScale = new Vector3(k, k, 1f);
            var hsr = GetOrAdd<SpriteRenderer>(handle.gameObject);
            hsr.sprite = S($"{Gimmick}/lever/lever_01.png");
            hsr.sortingOrder = rootSr.sortingOrder - 1; // 柄は台座の後ろ
            var lever = root.GetComponent<LeverSwitch>();
            SetField(lever, "_handle", handle);
            SetField(lever, "_promptOffset", new Vector3(0f, 1.8f, 0f));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            log.AppendLine("patched " + path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
