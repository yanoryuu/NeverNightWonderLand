// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using TMPro;

namespace Utage
{

	// バージョンのテキスト表記
	[AddComponentMenu("Utage/TemplateUI/UtageUguiVersionTextTMP")]
	public class UtageUguiVersionTextTMP : MonoBehaviour
	{
		[SerializeField] protected TextMeshProUGUI text;
		[SerializeField] protected string format = "Version {0}";
		
		void Start()
		{
			if (text!=null)
			{
				text.text = string.Format(format, Application.version);
			}
		}
	}
}
