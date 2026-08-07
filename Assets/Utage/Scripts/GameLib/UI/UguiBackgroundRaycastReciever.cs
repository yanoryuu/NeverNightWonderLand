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
	/// 背景（画面全体）に対するレイを受け取る
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiBackgroundRaycastReciever")]
	public class UguiBackgroundRaycastReciever : MonoBehaviour
	{
		public UguiBackgroundRaycaster Raycaster
		{ 
			get { return this.GetComponentCacheFindIfMissing( ref raycaster); }
			set { raycaster = value; }
		}
		[SerializeField]
		UguiBackgroundRaycaster raycaster;

		void OnEnable()
		{
			Raycaster.AddTarget(this.gameObject);
		}

		void OnDisable()
		{
			var raycaster = Raycaster;
			if (raycaster != null)
			{
				raycaster.RemoveTarget(this.gameObject);
			}
		}
	}
}

