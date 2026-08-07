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
	/// ボタン三つのダイアログ(TexMeshPro版)
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDialog3ButtonTMP")]
	public class SystemUiDialog3ButtonTMP : SystemUiDialog3Button
		, IUsingTextMeshPro
	{
	}
}
