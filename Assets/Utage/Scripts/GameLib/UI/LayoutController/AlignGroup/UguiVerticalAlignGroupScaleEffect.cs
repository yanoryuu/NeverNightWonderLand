// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{


	/// <summary>
	///  子オブジェクトを縦に並べる
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/UguiVerticalAlignGroupScaleEffect")]
	public class UguiVerticalAlignGroupScaleEffect : UguiVerticalAlignGroup
	{
		public float scaleRangeTop = -100f;
		public float scaleRangeHeight = 200f;
		public bool ignoreLocalPositionToScaleEffectRage = true;

		public float minScale = 0.5f;
		public float maxScale = 1f;

		protected override void CustomChild(RectTransform child, float offset )
		{
			tracker.Add(this, child,DrivenTransformProperties.Scale);
			
			float scale = minScale;
			float h = child.rect.height*scale;
			float top = ScaleEffectChildLocalPointTop;
			float bottom = ScaleEffectChildLocalPointBottom;
			if (direction == AlignDirection.BottomToTop)
			{
				bottom -= h;
				if (bottom < offset && offset < top)
				{
					float t = (offset -bottom)/(top-bottom);
					if(t>0.5f) t = 1.0f-t;
					scale =  Mathf.Lerp( minScale, maxScale, t );
				}
			}
			else
			{
				top += h;
				if (bottom < offset && offset < top)
				{
					float t = Mathf.Sin( Mathf.PI*(offset -bottom)/(top-bottom) );
					scale =  Mathf.Lerp( minScale, maxScale, t );
				}
			}
			child.localScale = Vector3.one*scale;
		}
		
		protected override void CustomLayoutRectTransform()
		{
			DrivenTransformProperties properties = DrivenTransformProperties.None;
			properties |= DrivenTransformProperties.AnchorMinY
				| DrivenTransformProperties.AnchorMaxY
					| DrivenTransformProperties.PivotY;
			tracker.Add(this, CachedRectTransform, properties);

			if (direction == AlignDirection.BottomToTop)
			{
				CachedRectTransform.anchorMin = new Vector2(CachedRectTransform.anchorMin.x, 0);
				CachedRectTransform.anchorMax = new Vector2(CachedRectTransform.anchorMax.x, 0);
				CachedRectTransform.pivot = new Vector2(CachedRectTransform.pivot.x, 0);
			}
			else
			{
				CachedRectTransform.anchorMin = new Vector2(CachedRectTransform.anchorMin.x, 1);
				CachedRectTransform.anchorMax = new Vector2(CachedRectTransform.anchorMax.x, 1);
				CachedRectTransform.pivot = new Vector2(CachedRectTransform.pivot.x, 1);
			}
		}

		void OnDrawGizmos ()
		{
			Vector3 top = ScaleEffectWorldPointTop;
			Vector3 bottom = ScaleEffectWorldPointBottom;
			Gizmos.DrawLine(top, bottom);
		}

		Vector3 ScaleEffectWorldPointTop
		{
			get
			{
				Vector3 pos = new Vector3(0,scaleRangeTop,0);
				if( ignoreLocalPositionToScaleEffectRage )
				{
					pos -= CachedRectTransform.localPosition;
				}
				return CachedRectTransform.TransformPoint(pos);
			}
		}

		Vector3 ScaleEffectWorldPointBottom
		{
			get
			{
				Vector3 pos = new Vector3(0,scaleRangeTop - scaleRangeHeight,0);
				if( ignoreLocalPositionToScaleEffectRage )
				{
					pos -= CachedRectTransform.localPosition;
				}
				return CachedRectTransform.TransformPoint(pos);
			}
		}

		float ScaleEffectChildLocalPointTop
		{
			get
			{
				Vector3 top = ScaleEffectWorldPointTop;
				return CachedRectTransform.InverseTransformPoint(top).y;
			}
		}
		
		float ScaleEffectChildLocalPointBottom
		{
			get
			{
				Vector3 bottom = ScaleEffectWorldPointBottom;
				return CachedRectTransform.InverseTransformPoint(bottom).y;
			}
		}

	}
}
