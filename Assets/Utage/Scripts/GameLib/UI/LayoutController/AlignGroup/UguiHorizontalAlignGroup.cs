// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	///  子オブジェクトを横に並べる
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/UguiHorizontalAlignGroup")]
	public class UguiHorizontalAlignGroup : UguiAlignGroup
	{
		public float paddingLeft = 0;
		public float paddingRight = 0;
		public enum AlignDirection
		{
			LeftToRight,
			RightToLeft,
		}
		public AlignDirection direction = AlignDirection.LeftToRight;

		/// <summary>
		/// 
		/// </summary>
		public override void Reposition()
		{
			if (!repositionZeroContent && CachedRectTransform.childCount <= 0) return;

			float offset = (direction == AlignDirection.LeftToRight) ? paddingLeft : -paddingRight;
			float totalSize = 0;
			foreach( RectTransform child in CachedRectTransform )
			{
				float w = AlignChild(child,ref offset);
				totalSize += (w + space);
			}
			totalSize += paddingLeft + paddingRight - space;
			LayoutRectTransorm (totalSize);
		}

		protected virtual float AlignChild(RectTransform child, ref float offset )
		{
			float directionScale = (direction == AlignDirection.LeftToRight) ? 1 : -1;
			float anchorX = (direction == AlignDirection.LeftToRight) ? 0 : 1;

			DrivenTransformProperties childProperties = 
				DrivenTransformProperties.AnchorMinX
					| DrivenTransformProperties.AnchorMaxX
					| DrivenTransformProperties.AnchoredPositionX;
			tracker.Add(this, child,childProperties);

			child.anchorMin = new Vector2(anchorX, child.anchorMin.y);
			child.anchorMax = new Vector2(anchorX, child.anchorMax.y);
			CustomChild(child,offset);
			float w = child.rect.width * Mathf.Abs (child.localScale.x);
			offset += directionScale * ( w * child.pivot.x );
			child.anchoredPosition = new Vector2(offset, child.anchoredPosition.y);
			offset += directionScale * ( w * (1.0f - child.pivot.x) + space );
			return w;
		}

		protected virtual void LayoutRectTransorm ( float totalSize)
		{
			if(isAutoResize)
			{
				tracker.Add(this, CachedRectTransform, DrivenTransformProperties.SizeDeltaX);
				CachedRectTransform.sizeDelta = new Vector2( totalSize, CachedRectTransform.sizeDelta.y );
			}
			CustomLayoutRectTransform();
		}

		protected virtual void CustomChild(RectTransform child, float offset )
		{
		}

		protected virtual void CustomLayoutRectTransform()
		{
		}
	}
}
