// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Utage
{
	//フォント別のアセットに変える
	public class FontChangerWindowLegacy : FontChangerWindow<Font>
	{
		//フォント別のアセットに変えるボタン
		//最後に表示するために、継承元のクラスではフィールドを定義しない
		[SerializeField, Button(nameof(ChangeFont), nameof(CheckDisable), false)]
		string changeFont;

		protected override string SaveKey => nameof(FontChangerWindowTMP);
	}
}
