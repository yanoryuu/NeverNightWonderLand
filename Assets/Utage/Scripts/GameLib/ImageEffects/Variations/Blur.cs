using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    [AddComponentMenu ("Utage/Lib/Image Effects/Blur/Blur")]
    public class Blur : ImageEffectSingelShaderBase
	{

        //キーフレームアニメーションに対応するために、intからfloatに
        [Range(0, 2)]
        public float downsample = 1;

        public enum BlurType {
            StandardGauss = 0,
            SgxGauss = 1,
        }

        [Range(0.0f, 10.0f)]
        public float blurSize = 3.0f;

        //キーフレームアニメーションに対応するために、intからfloatに
        [Range(1, 4)]
        public float blurIterations = 2;

        public BlurType blurType= BlurType.StandardGauss;


		protected override bool NeedRenderTexture { get { return true; } }


		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
        {
            int iDownSample = Mathf.FloorToInt(downsample); 
            float widthMod = 1.0f / (1.0f * (1<<iDownSample));

            Material.SetVector ("_Parameter", new Vector4 (blurSize * widthMod, -blurSize * widthMod, 0.0f, 0.0f));
            source.filterMode = FilterMode.Bilinear;

            int rtW = source.width >> iDownSample;
            int rtH = source.height >> iDownSample;

            // downsample
            RenderTexture rt = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);

            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit (source, rt, Material, 0);

            var passOffs= blurType == BlurType.StandardGauss ? 0 : 2;

            for(int i = 0; i < blurIterations; i++) {
                float iterationOffs = (i*1.0f);
				Material.SetVector ("_Parameter", new Vector4 (blurSize * widthMod + iterationOffs, -blurSize * widthMod - iterationOffs, 0.0f, 0.0f));

                // vertical blur
                RenderTexture rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit (rt, rt2, Material, 1 + passOffs);
                RenderTexture.ReleaseTemporary (rt);
                rt = rt2;

                // horizontal blur
                rt2 = RenderTexture.GetTemporary (rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit (rt, rt2, Material, 2 + passOffs);
                RenderTexture.ReleaseTemporary (rt);
                rt = rt2;
            }

            Graphics.Blit (rt, destination);

            RenderTexture.ReleaseTemporary (rt);
        }
    }
}
