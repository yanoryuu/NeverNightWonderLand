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
	[AddComponentMenu("Utage/Lib/UI/UguiVerticalAlignGroup")]
	public class UguiVerticalAlignGroup : UguiAlignGroup
	{
		public float paddingTop = 0;
		public float paddingBottom = 0;

		public enum AlignDirection
		{
			TopToBottom,
			BottomToTop,
		}
		public AlignDirection direction = AlignDirection.TopToBottom;

		/// <summary>
		/// 
		/// </summary>
		public override void Reposition()
		{
			if (!repositionZeroContent && CachedRectTransform.childCount <= 0) return;
			
			float offset = (direction == AlignDirection.BottomToTop) ? paddingBottom : -paddingTop;
			float totalSize = 0;
			foreach( RectTransform child in CachedRectTransform )
			{
				float h = AlignChild(child,ref offset);
				totalSize += (h + space);
			}
			totalSize += paddingBottom + paddingTop - space;
			LayoutRectTransorm (totalSize);
		}
		
		protected virtual float AlignChild(RectTransform child, ref float offset )
		{
			float directionScale = (direction == AlignDirection.BottomToTop) ? 1 : -1;
			float anchorY = (direction == AlignDirection.BottomToTop) ? 0 : 1;
			
			DrivenTransformProperties childProperties = 
				DrivenTransformProperties.AnchorMinY
					| DrivenTransformProperties.AnchorMaxY
					| DrivenTransformProperties.AnchoredPositionY;
			tracker.Add(this, child,childProperties);
			
			child.anchorMin = new Vector2(child.anchorMin.x, anchorY);
			child.anchorMax = new Vector2(child.anchorMax.x, anchorY);
			CustomChild(child,offset);
			float h = child.rect.height * Mathf.Abs (child.localScale.y);
			offset += directionScale * ( h * child.pivot.y );
			child.anchoredPosition = new Vector2(child.anchoredPosition.x, offset);
			offset += directionScale * ( h * (1.0f - child.pivot.y) + space );
			return h;
		}

		protected virtual void LayoutRectTransorm ( float totalSize)
		{
			if(isAutoResize)
			{
				tracker.Add(this, CachedRectTransform, DrivenTransformProperties.SizeDeltaY);
				CachedRectTransform.sizeDelta = new Vector2( CachedRectTransform.sizeDelta.x, totalSize );
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
