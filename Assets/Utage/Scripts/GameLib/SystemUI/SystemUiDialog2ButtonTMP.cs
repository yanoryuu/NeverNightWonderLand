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
	/// ボタン二つのダイアログ(TexMeshPro版)
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDialog2ButtonTMP")]
	public class SystemUiDialog2ButtonTMP : SystemUiDialog2Button
		, IUsingTextMeshPro
	{
	}
}
