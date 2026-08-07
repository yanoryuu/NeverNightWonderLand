// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// 背景（画面全体）に対するレイ
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiBackgroundRaycaster ")]
	[RequireComponent(typeof(Camera))]
	public class UguiBackgroundRaycaster : BaseRaycaster
	{
		//カメラ
		public override Camera eventCamera
		{
			get
			{
				return CachedCamera;
			}
		}
		Camera CachedCamera { get { return this.GetComponentCache(ref cachedCamera); } }
		Camera cachedCamera;

		[SerializeField]
		LetterBoxCamera letterBoxCamera = null;

		public override int sortOrderPriority { get { return m_Priority; } }
		[SerializeField]
		int m_Priority = int.MaxValue;

		[System.NonSerialized]
		List<GameObject> targetObjectList = new List<GameObject>();

		public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
		{
			Vector2 pos;
			if (letterBoxCamera == null)
				pos = new Vector2(eventData.position.x / Screen.width, eventData.position.y / Screen.height);
			else
				pos = letterBoxCamera.CachedCamera.ScreenToViewportPoint(eventData.position);

			if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f)
				return;

			int index = 0;
			foreach (GameObject target in targetObjectList)
			{
				RaycastResult result = new RaycastResult();
				result.distance = float.MaxValue;
				result.gameObject = target;
				result.index = index++;
				result.module = this;
				resultAppendList.Add(result);
			}
		}

		public void AddTarget(GameObject go)
		{
			if (!targetObjectList.Contains(go))
			{
				targetObjectList.Add(go);
			}
		}

		public void RemoveTarget(GameObject go)
		{
			if (targetObjectList.Contains(go))
			{
				targetObjectList.Remove(go);
			}
		}
	}
}

