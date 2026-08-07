using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// FPS表示(TexMeshPro版)
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiFramerateChangerTMP")]
	public class SystemUiFramerateChangerTMP : SystemUiFramerateChanger
		, IUsingTextMeshPro
	{
	}
}
