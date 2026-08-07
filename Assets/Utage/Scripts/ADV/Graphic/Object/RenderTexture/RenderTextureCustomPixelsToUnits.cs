// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;

namespace Utage
{
	public class RenderTextureCustomPixelsToUnits : MonoBehaviour, IRenderTextureCustomPixelsToUnits
	{
		[SerializeField] float pixelsToUnits = 100;
		[SerializeField] string rowDataKey = "";
		public float GetRenderTexturesPixelsToUnits(AdvGraphicInfo graphic, float defaultPixelsToUnits)
		{
			if (!string.IsNullOrEmpty(rowDataKey))
			{
				if (graphic.RowData.TryParseCell(rowDataKey, out float v))
				{
					return v;
				}
			}
			return pixelsToUnits;
		}
	}
}
