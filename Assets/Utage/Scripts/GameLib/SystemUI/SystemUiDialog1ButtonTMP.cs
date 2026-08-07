// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;

namespace Utage
{

	/// <summary>
	/// ボタン一つのダイアログ(TexMeshPro版)
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDialog1ButtonTMP")]
	public class SystemUiDialog1ButtonTMP : SystemUiDialog1Button
		, IUsingTextMeshPro
	{
	}
}
