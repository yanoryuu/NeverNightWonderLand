using System;
using UnityEngine;

namespace Utage
{
    [ExecuteInEditMode]
    [AddComponentMenu("Utage/Lib/Image Effects/Color Adjustments/NegaPosi")]
    public class NegaPosi : ImageEffectSingelShaderBase
	{
		//描画ロジック
		protected override void RenderImage(RenderTexture source, RenderTexture destination)
		{
			Graphics.Blit(source, destination, Material);
		}
	}
}
