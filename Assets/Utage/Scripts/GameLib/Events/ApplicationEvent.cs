// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	[AddComponentMenu("Utage/Lib/Events/ApplicationEvent")]
	public class ApplicationEvent : MonoBehaviour
	{
		public static ApplicationEvent Get()
		{
			if (instance==null)
			{
				GameObject go = new GameObject();
				go.hideFlags = HideFlags.HideAndDontSave;
				instance = go.AddComponent<ApplicationEvent>();
			}
			return instance;
		}
		[StaticField] static ApplicationEvent instance;

		public UnityEvent OnScreenSizeChanged = new UnityEvent();

		void Awake()
		{
			instance = this;
			screenWidth = Screen.width;
			screenHeight = Screen.height;
		}


		int screenWidth;
		int screenHeight;

		void Update()
		{
			if (screenWidth != Screen.width || screenHeight != Screen.height)
			{
				screenWidth = Screen.width;
				screenHeight = Screen.height;
				OnScreenSizeChanged.Invoke();
			}
		}
	};
}
