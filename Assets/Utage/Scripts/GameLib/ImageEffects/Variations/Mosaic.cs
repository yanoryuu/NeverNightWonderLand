using System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    [ExecuteInEditMode]
    [AddComponentMenu("Utage/Lib/Image Effects/Color Adjustments/Mosaic")]
    public class Mosaic : ImageEffectSingelShaderBase
	{
		[Min(1)]
		public float size = 8;

		LetterBoxCamera LetterBoxCamera { get { return this.GetComponentCache<LetterBoxCamera>(ref letterBoxCamera); } }
		LetterBoxCamera letterBoxCamera;

		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
			//現在のカメラの描画サイズと、実際のスクリーンの描画ピクセルサイズに合わせて
			//モザイクのサイズをかえる
			float scaleSize = 1;
			if (LetterBoxCamera != null)
			{
				scaleSize = Mathf.Min(source.width / LetterBoxCamera.CurrentSize.x, source.height / LetterBoxCamera.CurrentSize.y);
			}
			if (size <= 1)
			{
				Graphics.Blit(source, destination);
				return;
			}

			Material.SetFloat("_Size", Mathf.CeilToInt( size * scaleSize));
			Graphics.Blit(source, destination, Material);
		}
    }
}
