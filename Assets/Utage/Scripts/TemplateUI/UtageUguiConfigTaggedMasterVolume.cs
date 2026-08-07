// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// コンフィグ画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiConfigTaggedMasterVolume")]
	public class UtageUguiConfigTaggedMasterVolume : MonoBehaviour
	{
		public string volumeTag = "";
		public UtageUguiConfig config;

		//タグつきボリュームの設定
		public virtual void OnValugeChanged(float value)
		{
			if (string.IsNullOrEmpty(volumeTag)) return;
			config.OnValugeChangedTaggedMasterVolume(volumeTag, value);
		}
	}
}
