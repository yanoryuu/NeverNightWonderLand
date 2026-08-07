// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// レイキャストを無視する
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiIgnoreRaycaster")]
	public class UguiIgnoreRaycaster : MonoBehaviour, ICanvasRaycastFilter
	{
		public bool ignoreRaycaster = true;

		public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
		{
			return !ignoreRaycaster;
		}
	}
}
