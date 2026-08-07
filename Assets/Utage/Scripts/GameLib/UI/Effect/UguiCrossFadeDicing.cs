// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utage
{

	/// <summary>
	/// クロスフェード可能なDicing表示
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiCrossFadeDicing")]
    public class UguiCrossFadeDicing : UguiCrossFadeRawImage
	{
		protected DicingTextureData fadePatternData;
		internal void CrossFade(DicingTextureData fadePatternData, Texture fadeTexture, float time, Action onComplete)
		{
			this.fadePatternData = fadePatternData;
			Target.SetAllDirty();
			base.CrossFade(fadeTexture, time, onComplete);
		}

		public override Graphic Target { get { return this.GetComponentCache( ref target); } }



		public override void RebuildVertex(VertexHelper vh)
		{
			if (fadePatternData == null) return;

			vh.Clear();

			var r = Target.GetPixelAdjustedRect();
			var color32 = Target.color;

			DicingImage targetDicing = Target as DicingImage;

			float scaleX = r.width / fadePatternData.Width;
			float scaleY = r.height / fadePatternData.Height;
			int index = 0;

			List<DicingTextureData.QuadVerts> defaultVerts = targetDicing.GetVerts(targetDicing.PatternData);
			List<DicingTextureData.QuadVerts> fadeVerts = targetDicing.GetVerts(fadePatternData);

			int count = defaultVerts.Count;
			if (count != fadeVerts.Count)
			{
				count = Mathf.Min(count, fadeVerts.Count);
				Debug.LogError( string.Format("Not equal texture size {0} and {1}", targetDicing.PatternData.Name, fadePatternData.Name));
			}

			for (int i = 0; i < count; ++i)
			{
				var vert0 = defaultVerts[i];
				var vert1 = fadeVerts[i];

				if (targetDicing.SkipTransParentCell && vert0.isAllTransparent && vert1.isAllTransparent) continue;

				Vector4 v = new Vector4(
					r.x + scaleX * vert0.v.x,
					r.y + scaleY * vert0.v.y,
					r.x + scaleX * vert0.v.z,
					r.y + scaleY * vert0.v.w
					);
				Rect uvRect0 = vert0.uvRect;
				Rect uvRect1 = vert1.uvRect;

				vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uvRect0.xMin, uvRect0.yMin), new Vector2(uvRect1.xMin, uvRect1.yMin), Vector3.zero, Vector4.zero);
				vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uvRect0.xMin, uvRect0.yMax), new Vector2(uvRect1.xMin, uvRect1.yMax), Vector3.zero, Vector4.zero);
				vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uvRect0.xMax, uvRect0.yMax), new Vector2(uvRect1.xMax, uvRect1.yMax), Vector3.zero, Vector4.zero);
				vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uvRect0.xMax, uvRect0.yMin), new Vector2(uvRect1.xMax, uvRect1.yMin), Vector3.zero, Vector4.zero);

				vh.AddTriangle(index + 0, index + 1, index + 2);
				vh.AddTriangle(index + 2, index + 3, index + 0);
				index += 4;
			}
		}
	}
}