// 雰囲気 (§15) の調査用: RP / レンダラー / カメラ / Canvas の状態を返す (読むだけ)。
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AtmosProbe
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("default RP: " + GraphicsSettings.defaultRenderPipeline?.name);
        for (var i = 0; i < QualitySettings.names.Length; i++)
            sb.AppendLine($"quality[{i}] {QualitySettings.names[i]} rp={QualitySettings.GetRenderPipelineAssetAt(i)?.name} cur={QualitySettings.GetQualityLevel() == i}");
        foreach (var n in new[] { "PC_RPAsset", "Mobile_RPAsset" })
        {
            var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>($"Assets/Settings/{n}.asset");
            sb.AppendLine($"{n}: hdr={a.supportsHDR} msaa={a.msaaSampleCount} colorGrading={a.colorGradingMode} lut={a.colorGradingLutSize} ");
            var so = new SerializedObject(a);
            var list = so.FindProperty("m_RendererDataList");
            for (var i = 0; i < list.arraySize; i++)
            {
                var rd = list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                if (rd == null) continue;
                var rso = new SerializedObject(rd);
                var pp = rso.FindProperty("postProcessData");
                sb.AppendLine($"  renderer {rd.name} type={rd.GetType().Name} ppData={(pp != null ? pp.objectReferenceValue?.name : "?")} features={string.Join(",", rd.rendererFeatures.Select(f => f?.name + (f != null && f.isActive ? "" : "(off)")))}");
            }
        }
        return sb.ToString();
    }

    public static string PlayerScene()
    {
        var sb = new StringBuilder();
        var s = EditorSceneManager.OpenScene("Assets/Scenes/PlayerScene.unity", OpenSceneMode.Additive);
        foreach (var root in s.GetRootGameObjects())
        {
            sb.AppendLine($"root {root.name} layer={root.layer}");
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                var d = cam.GetUniversalAdditionalCameraData();
                sb.AppendLine($"  cam {cam.name} ortho={cam.orthographic} size={cam.orthographicSize} pos={cam.transform.position} clear={cam.clearFlags} bg={cam.backgroundColor} pp={d.renderPostProcessing} mask={d.volumeLayerMask.value} trigger={d.volumeTrigger} type={d.renderType} aa={d.antialiasing} hdr={cam.allowHDR} dither={d.dithering}");
                foreach (Transform c in cam.transform) sb.AppendLine("    child " + c.name);
            }
            foreach (var cv in root.GetComponentsInChildren<Canvas>(true))
                sb.AppendLine($"  canvas {cv.name} mode={cv.renderMode} cam={cv.worldCamera?.name}");
        }
        EditorSceneManager.CloseScene(s, true);
        return sb.ToString();
    }

    /// <summary>プレイ中: 雰囲気の適用状態 (種類・Volume の重み・パーティクル数)。</summary>
    public static string Play()
    {
        Application.runInBackground = true;
        var d = Object.FindAnyObjectByType<NeverNight.Stage.AtmosphereDirector>();
        if (d == null) return "no director";
        var sb = new StringBuilder($"stage={NeverNight.Stage.StageAtmosphere.CurrentKind} applied={d.Applied} t={Time.time:F1}\n");
        foreach (var v in d.GetComponentsInChildren<Volume>(true))
            sb.AppendLine($"  {v.name} w={v.weight:F2} prof={v.sharedProfile?.name} en={v.enabled}");
        foreach (var ps in d.GetComponentsInChildren<ParticleSystem>(false))
            sb.AppendLine($"  {ps.transform.parent.name}/{ps.name} n={ps.particleCount}");
        return sb.ToString();
    }

    public static string Canvases()
    {
        var sb = new StringBuilder();
        foreach (var g in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (!System.IO.Path.GetFileNameWithoutExtension(p).EndsWith("UI")) continue;
            var txt = System.IO.File.ReadAllText(p);
            var modes = System.Text.RegularExpressions.Regex.Matches(txt, @"m_RenderMode: (\d)").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value);
            sb.AppendLine($"{p}: renderModes={string.Join(",", modes)}");
        }
        return sb.ToString();
    }
}
