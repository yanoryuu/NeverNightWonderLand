// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using Utage;

//ゲームのカスタムプロジェクト設定
namespace Utage
{
	[AddComponentMenu("Utage/Lib/Other/BootCustomProjectSetting")]
	public class BootCustomProjectSetting : MonoBehaviour
	{
		//独自カスタムのプロジェクト設定
		public CustomProjectSetting CustomProjectSetting
		{
			get { return customProjectSetting; }
			set { customProjectSetting = value; }
		}

		[SerializeField] CustomProjectSetting customProjectSetting;

		private void OnDestroy()
		{
			if (LanguageManagerBase.Instance != null)
			{
				LanguageManagerBase.Instance.OnFinalize();
			}
		}
	}
}
