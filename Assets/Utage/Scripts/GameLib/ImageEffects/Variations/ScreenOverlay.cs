using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Utage/Lib/Image Effects/Other/ScreenOverlay")]
    public class ScreenOverlay : ImageEffectSingelShaderBase
	{
	    public enum OverlayBlendMode
		{
            Additive = 0,
            ScreenBlend = 1,
            Multiply = 2,
            Overlay = 3,
            AlphaBlend = 4,
        }

        public OverlayBlendMode blendMode = OverlayBlendMode.Overlay;
        public float intensity = 1.0f;
        public Texture2D texture = null;

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
            Vector4 UV_Transform = new  Vector4(1, 0, 0, 1);

#if UNITY_WP8
	    	// WP8 has no OS support for rotating screen with device orientation,
	    	// so we do those transformations ourselves.
			if (Screen.orientation == ScreenOrientation.LandscapeLeft) {
				UV_Transform = new Vector4(0, -1, 1, 0);
			}
			if (Screen.orientation == ScreenOrientation.LandscapeRight) {
				UV_Transform = new Vector4(0, 1, -1, 0);
			}
			if (Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
				UV_Transform = new Vector4(-1, 0, 0, -1);
			}
#endif

			Material.SetVector("_UV_Transform", UV_Transform);
			Material.SetFloat ("_Intensity", intensity);
			Material.SetTexture ("_Overlay", texture);
            Graphics.Blit (source, destination, Material, (int) blendMode);
        }
	}
}
