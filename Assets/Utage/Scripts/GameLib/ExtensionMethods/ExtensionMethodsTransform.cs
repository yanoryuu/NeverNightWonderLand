// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using Utage;
using UtageExtensions;

namespace UtageExtensions
{

	//Transformの拡張メソッド
	//座標系（位置、回転、拡縮）に関する操作
	public static class UtageExtensionMethodsTransform
	{
		public static void SetPosition(this Transform transform, float x, float y, float z)
		{
			transform.position = new Vector3(x, y, z);
		}
		public static void SetPositionX(this Transform transform, float x)
		{
			var vector3 = transform.position;
			vector3.x = x;
			transform.position = vector3;
		}
		public static void SetPositionY(this Transform transform, float y)
		{
			var vector3 = transform.position;
			vector3.y = y;
			transform.position = vector3;
		}
		public static void SetPositionZ(this Transform transform, float z)
		{
			var vector3 = transform.position;
			vector3.z = z;
			transform.position = vector3;
		}

		public static void AddPosition(this Transform transform, Vector3 offset)
		{
			transform.AddPosition(offset.x,offset.y,offset.z);
		}
		public static void AddPosition(this Transform transform, float x, float y, float z)
		{
			var vector3 = transform.position;
			vector3.x += x;
			vector3.y += y;
			vector3.z += z;
			transform.position = vector3;
		}
		public static void AddPositionX(this Transform transform, float x)
		{
			var vector3 = transform.position;
			vector3.x += x;
			transform.position = vector3;
		}
		public static void AddPositionY(this Transform transform, float y)
		{
			var vector3 = transform.position;
			vector3.y += y;
			transform.position = vector3;
		}
		public static void AddPositionZ(this Transform transform, float z)
		{
			var vector3 = transform.position;
			vector3.z += z;
			transform.position = vector3;
		}

		public static void SetLocalPosition(this Transform transform, float x, float y, float z)
		{
			transform.localPosition = new Vector3(x, y, z);
		}
		public static void SetLocalPositionX(this Transform transform, float x)
		{
			var vector3 = transform.localPosition;
			vector3.x = x;
			transform.localPosition = vector3;
		}
		public static void SetLocalPositionY(this Transform transform, float y)
		{
			var vector3 = transform.localPosition;
			vector3.y = y;
			transform.localPosition = vector3;
		}
		public static void SetLocalPositionZ(this Transform transform, float z)
		{
			var vector3 = transform.localPosition;
			vector3.z = z;
			transform.localPosition = vector3;
		}

		public static void AddLocalPosition(this Transform transform, float x, float y, float z)
		{
			var vector3 = transform.localPosition;
			vector3.x += x;
			vector3.y += y;
			vector3.z += z;
			transform.localPosition = vector3;
		}
		public static void AddLocalPositionX(this Transform transform, float x)
		{
			var vector3 = transform.localPosition;
			vector3.x += x;
			transform.localPosition = vector3;
		}
		public static void AddLocalPositionY(this Transform transform, float y)
		{
			var vector3 = transform.localPosition;
			vector3.y += y;
			transform.localPosition = vector3;
		}
		public static void AddLocalPositionZ(this Transform transform, float z)
		{
			var vector3 = transform.localPosition;
			vector3.z += z;
			transform.localPosition = vector3;
		}

		public static void SetLocalScale(this Transform transform, float x, float y, float z)
		{
			transform.localScale = new Vector3(x, y, z);
		}
		public static void SetLocalScaleX(this Transform transform, float x)
		{
			var vector3 = transform.localScale;
			vector3.x = x;
			transform.localScale = vector3;
		}
		public static void SetLocalScaleY(this Transform transform, float y)
		{
			var vector3 = transform.localScale;
			vector3.y = y;
			transform.localScale = vector3;
		}
		public static void SetLocalScaleZ(this Transform transform, float z)
		{
			var vector3 = transform.localScale;
			vector3.z = z;
			transform.localScale = vector3;
		}

		public static void AddLocalScale(this Transform transform, float x, float y, float z)
		{
			var vector3 = transform.localScale;
			vector3.x += x;
			vector3.y += y;
			vector3.z += z;
			transform.localScale = vector3;
		}
		public static void AddLocalScaleX(this Transform transform, float x)
		{
			var vector3 = transform.localScale;
			vector3.x += x;
			transform.localScale = vector3;
		}
		public static void AddLocalScaleY(this Transform transform, float y)
		{
			var vector3 = transform.localScale;
			vector3.y += y;
			transform.localScale = vector3;
		}
		public static void AddLocalScaleZ(this Transform transform, float z)
		{
			var vector3 = transform.localScale;
			vector3.z += z;
			transform.localScale = vector3;
		}

		public static void SetEulerAngles(this Transform transform, float x, float y, float z)
		{
			transform.eulerAngles = new Vector3(x, y, z);
		}
		public static void SetEulerAnglesX(this Transform transform, float x)
		{
			var vector3 = transform.eulerAngles;
			vector3.x = x;
			transform.eulerAngles = vector3;
		}
		public static void SetEulerAnglesY(this Transform transform, float y)
		{
			var vector3 = transform.eulerAngles;
			vector3.y = y;
			transform.eulerAngles = vector3;
		}
		public static void SetEulerAnglesZ(this Transform transform, float z)
		{
			var vector3 = transform.eulerAngles;
			vector3.z = z;
			transform.eulerAngles = vector3;
		}

		public static void AddEulerAngles(this Transform transform, float x, float y, float z)
		{
			var vector3 = transform.eulerAngles;
			vector3.x += x;
			vector3.y += y;
			vector3.z += z;
			transform.eulerAngles = vector3;
		}
		public static void AddEulerAnglesX(this Transform transform, float x)
		{
			var vector3 = transform.eulerAngles;
			vector3.x += x;
			transform.eulerAngles = vector3;
		}
		public static void AddEulerAnglesY(this Transform transform, float y)
		{
			var vector3 = transform.eulerAngles;
			vector3.y += y;
			transform.eulerAngles = vector3;
		}
		public static void AddEulerAnglesZ(this Transform transform, float z)
		{
			var vector3 = transform.eulerAngles;
			vector3.z += z;
			transform.eulerAngles = vector3;
		}

		public static void SetLocalEulerAngles(this Transform transform, float x, float y, float z)
		{
			transform.localEulerAngles = new Vector3(x, y, z);
		}
		public static void SetLocalEulerAnglesX(this Transform transform, float x)
		{
			var vector3 = transform.localEulerAngles;
			vector3.x = x;
			transform.localEulerAngles = vector3;
		}
		public static void SetLocalEulerAnglesY(this Transform transform, float y)
		{
			var vector3 = transform.localEulerAngles;
			vector3.y = y;
			transform.localEulerAngles = vector3;
		}
		public static void SetLocalEulerAnglesZ(this Transform transform, float z)
		{
			var vector3 = transform.localEulerAngles;
			vector3.z = z;
			transform.localEulerAngles = vector3;
		}

		public static void AddLocalEulerAngles(this Transform transform, float x, float y, float z)
		{
			var vector3 = transform.localEulerAngles;
			vector3.x += x;
			vector3.y += y;
			vector3.z += z;
			transform.localEulerAngles = vector3;
		}
		public static void AddLocalEulerAnglesX(this Transform transform, float x)
		{
			var vector3 = transform.localEulerAngles;
			vector3.x += x;
			transform.localEulerAngles = vector3;
		}
		public static void AddLocalEulerAnglesY(this Transform transform, float y)
		{
			var vector3 = transform.localEulerAngles;
			vector3.y += y;
			transform.localEulerAngles = vector3;
		}
		public static void AddLocalEulerAnglesZ(this Transform transform, float z)
		{
			var vector3 = transform.localEulerAngles;
			vector3.z += z;
			transform.localEulerAngles = vector3;
		}

		//ローカル座標をワールド座標に（わかりづらいので名前をつける）
		public static Vector3 LocalPointToWorldPoint(this Transform t, Vector3 localPoint)
		{
			return t.TransformPoint(localPoint);
		}

		//ワールド座標をローカル座標に（わかりづらいので名前をつける）
		public static Vector3 WorldPointToLocalPoint(this Transform t, Vector3 worldPoint)
		{
			return t.InverseTransformPoint(worldPoint);
		}

		//指定ぶんロカール座標がずれた位置のワールド座標を取得する
		public static Vector3 LocalOffsetToWorldPosition(this Transform t, Vector3 localOffset)
		{
			Vector3 pos = t.localPosition;
			pos += localOffset;
			return t.ToWorldPosition(pos);
		}

		//指定のローカル座標をワールド座標に変換する
		public static Vector3 ToWorldPosition(this Transform t, Vector3 position)
		{
			return t.parent.TransformPoint(position);
		}

		//描画カメラでのスクリーン座標を取得
		public static Vector3 GetScreenPoint(this Transform t, Camera camera)
		{
			return t.GetScreenPoint(camera,Vector3.zero);
		}
		public static Vector3 GetScreenPoint(this Transform t, Camera camera, Vector3 offset)
		{
			Vector3 point = camera.WorldToScreenPoint(t.position + offset);

			var renderTexture = camera.targetTexture;
			if (renderTexture == null) return point;
			
			//RenderTextureを使っている場合を考慮
			point.x = point.x * Screen.width / renderTexture.width;
			point.y = point.y * Screen.height / renderTexture.height;
			return point;
		}


		public static void CopyTransform(this Transform transform, Transform srcTransform)
		{
			transform.localPosition = srcTransform.localPosition;
			transform.localRotation = srcTransform.localRotation;
			transform.localScale = srcTransform.localScale;
			if (transform is RectTransform r && srcTransform is RectTransform srcR)
			{
				r.pivot = srcR.pivot;
				r.anchorMin = srcR.anchorMin;
				r.anchorMax = srcR.anchorMax;
				r.anchoredPosition = srcR.anchoredPosition;
				r.sizeDelta = srcR.sizeDelta;
			}
		}
	}
}
