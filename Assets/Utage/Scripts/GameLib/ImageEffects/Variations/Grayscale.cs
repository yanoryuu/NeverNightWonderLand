using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [AddComponentMenu("Utage/Lib/Image Effects/Color Adjustments/Grayscale")]
    public class Grayscale : ImageEffectSingelShaderBase, IImageEffectStrength
	{
		public float Strength { get { return strength; } set { strength = value; } }
		[Range(0, 1.0f)]
		public float strength = 1;

		public Texture  textureRamp;

        [Range(-1.0f,1.0f)]
        public float    rampOffset;

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
			Material.SetFloat("_Strength", strength);
			Material.SetTexture("_RampTex", textureRamp);
            Material.SetFloat("_RampOffset", rampOffset);
            Graphics.Blit (source, destination, Material);
        }

		Texture tmpTextureRamp;
		//Json読み込みの時にUnityEngine.Objectも対象になってしまうので、それをいったん記録して戻す
		protected override void StoreObjectsOnJsonRead()
		{
			base.StoreObjectsOnJsonRead();
			tmpTextureRamp = textureRamp;
		}
		protected override void RestoreObjectsOnJsonRead()
		{
			base.RestoreObjectsOnJsonRead();
			textureRamp = tmpTextureRamp;
		}
	}
}
