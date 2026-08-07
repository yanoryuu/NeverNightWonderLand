// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using Utage;
using UtageExtensions;

namespace UtageExtensions
{
	public enum RectType
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Center,
		Right,
		BottomLeft,
		Bottom,
		BottomRight,
	};

	//RectTransformの拡張メソッド
	public static class ExtensionMethodsRectTransform
	{
		public static void SetAnchoredPosition(this RectTransform r, float x, float y)
		{
			r.anchoredPosition = new Vector2(x, y);
		}

		public static void SetAnchoredPositionX(this RectTransform r, float x)
		{
			var position = r.anchoredPosition;
			position.x = x;
			r.anchoredPosition = position;
		}

		public static void SetAnchoredPositionY(this RectTransform r, float y)
		{
			var position = r.anchoredPosition;
			position.y = y;
			r.anchoredPosition = position;
		}

		public static void AddAnchoredPosition(this RectTransform r, float x, float y)
		{
			var position = r.anchoredPosition;
			position.x += x;
			position.y += y;
			r.anchoredPosition = position;
		}

		public static void AddAnchoredPositionX(this RectTransform r, float x)
		{
			var position = r.anchoredPosition;
			position.x += x;
			r.anchoredPosition = position;
		}

		public static void AddAnchoredPositionY(this RectTransform r, float y)
		{
			var position = r.anchoredPosition;
			position.y += y;
			r.anchoredPosition = position;
		}

		//サイズの取得
		public static Vector2 GetSize(this RectTransform t)
		{
			Rect rect = t.rect;
			return new Vector2(rect.width, rect.height);
		}
		//ローカルスケールを反映したサイズの取得
		public static Vector2 GetSizeScaled(this RectTransform t)
		{
			Rect rect = t.rect;
			return new Vector2(rect.width * t.localScale.x, rect.height * t.localScale.y);
		}

		//サイズの設定
		public static void SetSize(this RectTransform t, Vector2 size)
		{
			t.SetWidth(size.x);
			t.SetHeight(size.y);
		}
		//サイズの設定
		public static void SetSize(this RectTransform t, float width, float height)
		{
			t.SetWidth(width);
			t.SetHeight(height);
		}

		//Widthの取得
		public static float GetWith(this RectTransform t)
		{
			return t.rect.width;
		}
		//Widthの設定
		public static void SetWidth(this RectTransform t, float width)
		{
			t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
		}

		//Widthの設定(親の幅との割合で。ゲージなどに使う)
		public static void SetWidthWidthParentRatio(this RectTransform t, float ratio)
		{
			RectTransform p = t.parent as RectTransform;
			float w = p.GetWith() * ratio;
			t.SetWidth(w);
		}

		//Heightの取得
		public static float GetHeight(this RectTransform t)
		{
			return t.rect.height;
		}
		//Heightの設定
		public static void SetHeight(this RectTransform t, float height)
		{
			t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		}

		//ストレッチ（親オブジェクトの大きさに合わせた矩形）に設定する
		public static void SetStretch(this RectTransform t)
		{
			t.anchorMin = Vector2.zero;
			t.anchorMax = Vector2.one;
			t.sizeDelta = Vector2.zero;
		}

		//キャンバス内での相対座標としての、矩形を取得（回転はないものとする）
		public static Rect RectInCanvas(this RectTransform t, Canvas canvas)
		{
			Rect rect = t.rect;
			Vector3 position = t.TransformPoint(rect.center);
			position = canvas.transform.InverseTransformPoint(position);
			Vector3 size = t.GetSizeScaled();
			t.TransformVector(size);
			canvas.transform.InverseTransformVector(size);
			Rect ret = new Rect();
			ret.size = size;
			ret.center = position;
			return ret;
		}

		//ローカル座標を、ピボットとしての値へ変換
		public static Vector2 LocalPointToPivot(this RectTransform t, Vector2 localPoint)
		{
			var rect = t.rect;
			Vector2 center = rect.center;
			Vector2 pivot;
			pivot.x = 0.5f + (localPoint.x - center.x)/rect.width;
			pivot.y = 0.5f + (localPoint.y - center.y)/rect.height;
			return pivot;
		}

		//ピボット値を、ローカル座標へ変換
		public static Vector2 PivotToLocalPoint(this RectTransform t, Vector2 pivot)
		{
			var rect = t.rect;
			Vector2 center = rect.center;
			Vector2 localPoint;
			localPoint.x= (pivot.x-0.5f) * rect.width + center.x;
			localPoint.y= (pivot.y-0.5f) * rect.height + center.y;
			return localPoint;
		}

		//ワールド座標を、ピボットとしての値へ変換
		public static Vector2 WorldPointToPivot(this RectTransform t, Vector3 worldPoint)
		{
			Vector3 localPoint = t.WorldPointToLocalPoint(worldPoint);
			return t.LocalPointToPivot(localPoint);
		}


		//ピボットを矩形を保持したまま設定
		public static void SetPivotKeepRect(this RectTransform t, Vector2 pivot)
		{
			var offset=pivot - t.pivot;
			offset.Scale(t.rect.size);
			var worldPos= t.position + t.TransformVector(offset);
			t.pivot = pivot;
			t.position = worldPos;
		}
/*
		//RectTransformを取得
		public static RectTransform RectTransform(this GameObject go)
		{
			return go.transform as RectTransform;
		}

		//RectTransformを取得
		public static RectTransform RectTransform(this MonoBehaviour target)
		{
			return target.transform as RectTransform;
		}
*/


		//アンカーを左上設定にし、Rect（左上座標と幅と高さ）で矩形を設定
		public static void SetRectAndLeftTopAnchor(this RectTransform t, Rect rect)
		{
			t.SetRectAndLeftTopAnchor(rect.x, rect.y, rect.width, rect.height);
		}

		//アンカーを左上設定にし、左上座標と幅と高さを指定して矩形を設定
		public static void SetRectAndLeftTopAnchor(this RectTransform t, float x0, float y0, float w, float h)
		{
			t.anchorMin = new Vector2(0, 1);
			t.anchorMax = new Vector2(0, 1);
			t.SetWidth(w);
			t.SetHeight(h);
			var pivot = t.pivot;
			float x = x0 + pivot.x*w;
			float y = y0 + (pivot.y-1.0f)*h;
			t.SetAnchoredPosition(x,y);
		}

		//parent以下の子要素をアンカーを左上設定にして、左上から順番にグリッド配置する
		public static void AlignChildrenByGridLeftTop(this RectTransform parent, int columnCount, RectOffset padding, Vector2 cellSize, Vector2 spacing, Action<RectTransform> onAlignChild = null)
		{
			float x0 = padding.left;
			float y0 = -padding.top;
			float w = cellSize.x;
			float h = cellSize.y;
			float spacedW = w + spacing.x;
			float spacedH = h + spacing.y;

			int column = 0;
			float x = x0;
			float y = y0;;
			foreach (RectTransform child in parent)
			{
				onAlignChild?.Invoke(child);
				child.SetRectAndLeftTopAnchor(x,y,w,h);
				++column;
				if (column>=columnCount)
				{
					column = 0;
					x = x0;
					y -= spacedH;
				}
				else
				{
					x += spacedW;
				}
			}
		}

		//指定のスクリーン座標に設定
		public static void SetScreenPosition(this RectTransform t, Vector2 screenPoint, Canvas canvas)
		{
			t.SetScreenPosition(screenPoint, canvas.worldCamera);
		}

		//指定のスクリーン座標に設定
		public static void SetScreenPosition(this RectTransform t, Vector2 screenPoint, Camera uiCamera)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(t.parent as RectTransform, screenPoint, uiCamera, out Vector2 localPoint);
			t.localPosition = new Vector3(localPoint.x, localPoint.y,t.localPosition.z);
		}

		//指定の
		public static Vector3 RectToWorldPosition(this RectTransform rt, RectType rectType)
		{
			Vector3 position = rt.RectPosition(rectType);
			position += rt.localPosition;
			return rt.ToWorldPosition(position);
		}

		//指定の
		public static Vector2 RectPosition(this RectTransform rt, RectType rectType)
		{
			Rect rect = rt.rect;
			Vector2 position = Vector3.zero;
			switch (rectType)
			{
				case RectType.TopLeft:
					position.x = rect.xMin;
					position.y = rect.yMax;
					break;
				case RectType.Top:
					position.x = rect.xMin + rect.width / 2;
					position.y = rect.yMax;
					break;
				case RectType.TopRight:
					position.x = rect.xMax;
					position.y = rect.yMax;
					break;
				case RectType.Left:
					position.x = rect.xMin;
					position.y = rect.yMin + rect.height / 2;
					break;
				case RectType.Center:
					position.x = rect.xMin + rect.width / 2;
					position.y = rect.yMin + rect.height / 2;
					break;
				case RectType.Right:
					position.x = rect.xMax;
					position.y = rect.yMin + rect.height / 2;
					break;
				case RectType.BottomLeft:
					position.x = rect.xMin;
					position.y = rect.yMin;
					break;
				case RectType.Bottom:
					position.x = rect.xMin + rect.width / 2;
					position.y = rect.yMin;
					break;
				case RectType.BottomRight:
					position.x = rect.xMax;
					position.y = rect.yMin;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(rectType), rectType, null);
			}
			return position;
		}
	}
}
