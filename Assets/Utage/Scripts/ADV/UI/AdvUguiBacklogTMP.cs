// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UtageExtensions;

namespace Utage
{
	/// バックログ用UI(TextMeshPro版)
	[AddComponentMenu("Utage/ADV/TMP/AdvUguiBacklogTMP")]
	public class AdvUguiBacklogTMP
		: AdvUguiBacklog
			, IUsingTextMeshPro
	{
		public TextMeshProNovelText TextMeshProLogText => textMeshProLogText;
		public TextMeshProNovelText TextMeshProCharacterName => textMeshProCharacterName;
		TMP_Text TextMeshPro { get { return TextMeshProLogText.TextMeshPro;} }


		//ボイスが複数ある場合の初期化を行う
		protected override void InitVoiceIfMulti()
		{
			Button.interactable = false;
			AdvUguiBacklogTMPEventTrigger trigger = TextMeshProLogText.gameObject.GetComponentCreateIfMissing<AdvUguiBacklogTMPEventTrigger>();
			trigger.InitAsBackLog(this);
		}


		public void OnClicked(AdvBacklog.AdvBacklogDataInPage dataInPage )
		{
			OnClicked(dataInPage.VoiceFileName);
		}
	}
}

