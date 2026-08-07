using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Utage/Lib/Image Effects/Displacement/FishEye")]
    public class FishEye : ImageEffectSingelShaderBase
	{
        [Range(0.0f, 1.5f)]
        public float strengthX = 0.5f;
        [Range(0.0f, 1.5f)]
        public float strengthY = 0.5f;

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
            float oneOverBaseSize = 80.0f / 512.0f; // to keep values more like in the old version of fisheye

            float ar = (source.width * 1.0f) / (source.height * 1.0f);

			Material.SetVector ("intensity", new Vector4 (strengthX * ar * oneOverBaseSize, strengthY * oneOverBaseSize, strengthX * ar * oneOverBaseSize, strengthY * oneOverBaseSize));
            Graphics.Blit (source, destination, Material);
        }
    }
}
