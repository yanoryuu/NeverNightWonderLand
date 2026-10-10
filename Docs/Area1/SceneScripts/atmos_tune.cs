// 雰囲気 (§15) の調整用: プレイ中にプロファイル・パーティクルの値をその場で変えて見比べる (アセットを直接書き換える)。
// 決めた値は Area1StageBuilder.Atmosphere.cs に書き戻して SetupPlayerAtmosphere を流し直すこと (流し直すと上書きされる)。
// 例: --entry AtmosTune.Vol --args '["A1_Base","ColorAdjustments","postExposure","0.4"]'
//     --entry AtmosTune.Vol --args '["A1_Underground","ColorAdjustments","colorFilter","0.9,0.9,1"]'
//     --entry AtmosTune.Ps  --args '["Dust","1.5"]'   (startSize を倍率で)
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AtmosTune
{
    public static string Vol(string profile, string comp, string param, string value)
    {
        var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"Assets/Settings/Volumes/{profile}.asset");
        var c = p.components.First(x => x.GetType().Name == comp);
        var f = c.GetType().GetField(param);
        var vp = (VolumeParameter)f.GetValue(c);
        var nums = value.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        object v = vp switch
        {
            VolumeParameter<Color> _ => new Color(nums[0], nums[1], nums[2], nums.Length > 3 ? nums[3] : 1f),
            VolumeParameter<Vector4> _ => new Vector4(nums[0], nums[1], nums[2], nums[3]),
            VolumeParameter<bool> _ => nums[0] != 0f,
            _ => (object)nums[0],
        };
        var vt = vp.GetType().GetProperty("value").PropertyType;
        if (vt.IsEnum) v = System.Enum.ToObject(vt, (int)nums[0]);
        else if (vt == typeof(int)) v = (int)nums[0];
        vp.overrideState = true;
        vp.GetType().GetProperty("value").SetValue(vp, v);
        EditorUtility.SetDirty(c);
        return $"{profile}.{comp}.{param} = {v}";
    }

    public static string Ps(string name, float sizeMul)
    {
        var n = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.name == name))
        {
            var m = ps.main;
            m.startSizeMultiplier *= sizeMul;
            n++;
        }
        return $"{name} x{sizeMul} ({n})";
    }
}
