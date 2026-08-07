using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// FPS表示
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiFramerateChanger")]
	public class SystemUiFramerateChanger : MonoBehaviour
	{
		[SerializeField, HideIfTMP] protected Text text;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI textTmp;

		List<int> frameRateList = new List<int>() { 30, 60, 120 };
		List<int> vSyncCountList = new List<int>() { 2, 1, 0 };

		int currentIndex = 0;

		void Update()
		{
			SetText(string.Format("FPS:{0}",Application.targetFrameRate));
		}

		public virtual void SetText(string str)
		{
			TextComponentWrapper.SetText(this.text,textTmp, str);
		}

		public int TargetFrameRate()
		{
			return Application.targetFrameRate;
		}

		//FPS切り替え
		public void OnClickChangeFrameRate()
		{
			currentIndex = (currentIndex + 1) % frameRateList.Count;
			Application.targetFrameRate = frameRateList[currentIndex];
			QualitySettings.vSyncCount = vSyncCountList[currentIndex];
		}
	}
}
