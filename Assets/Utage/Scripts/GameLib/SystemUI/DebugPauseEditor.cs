using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// デバッグ用に、マウスダウン、アウップでUnityエディタをポーズさせる
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/DebugPauseEditor")]
	public class DebugPauseEditor : MonoBehaviour
	{
		public bool isPauseOnMouseDown = false;
		public bool isPauseOnMouseUp = false;
		 [Range(0.00001f, 10)] 
		public float timeScale = 1.0f;

		bool Started { get; set; }

#if UNITY_EDITOR
		void Start()
		{
			timeScale = Time.timeScale;
			Started = true;
		}

		void Update()
		{
			if ( IsMouseDown() || IsMouseUp() )
			{
				PauseEditor();
			}
		}

		bool IsMouseDown()
		{
			if (!isPauseOnMouseDown || !InputUtil.IsInputDebugPause0() )
				return false;
//			return IsInputAlt() && IsInputShift();
			return true;
		}

		bool IsMouseUp()
		{
			if (!isPauseOnMouseUp || !InputUtil.IsInputDebugPause1() )
				return false;
//			return IsInputAlt();
			return true;
		}
		

		public void PauseEditor()
		{
			UnityEditor.EditorApplication.isPaused = true;
		}

		void OnValidate()
		{
			if (!Application.isPlaying)
			{
				return;
			}
			if (!Started)
			{
				return;
			}
			if (!Mathf.Approximately(Time.timeScale, timeScale)) Time.timeScale = timeScale;
		}
#endif
	}
}
