using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [AddComponentMenu("Utage/Lib/Image Effects/Color Adjustments/ColorFade")]
    public class ColorFade : ImageEffectSingelShaderBase, IImageEffectStrength
	{
		public float Strength { get { return strength; } set { strength = value; } }
		[Range(0, 1.0f)]
		public float strength = 1;

		public Color color = new Color(0, 0, 0, 0);

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
			Material.SetFloat("_Strength", Strength);
			Material.color = color;
            Graphics.Blit (source, destination, Material);
        }
    }
}
