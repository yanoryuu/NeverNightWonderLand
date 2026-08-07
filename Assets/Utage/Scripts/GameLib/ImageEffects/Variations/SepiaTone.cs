using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [AddComponentMenu("Utage/Lib/Image Effects/Color Adjustments/SepiaTone")]
    public class SepiaTone : ImageEffectSingelShaderBase, IImageEffectStrength
	{
		public float Strength { get { return strength; } set { strength = value; } }
		[Range(0, 1.0f)]
		public float strength = 1;

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
			Material.SetFloat("_Strength", strength);
			Graphics.Blit (source, destination, Material);
        }
    }
}
