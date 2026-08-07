// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Utage
{
	//TextMeshProのフォント（SDFアセット）を別のアセットに変える
	public class FontChangerWindowTMP : FontChangerWindow < TMP_FontAsset >
	{
		[Serializable]
		class Materials
		{
			[SerializeField] public Material from;
			[SerializeField] public Material to;
		}

		[SerializeField] List<Materials> materials;

		//フォント別のアセットに変えるボタン
		//最後に表示するために、継承元のクラスではフィールドを定義しない
		[SerializeField, Button(nameof(ChangeFont), nameof(CheckDisable), false)]
		string changeFont;

		protected override string SaveKey => nameof(FontChangerWindowTMP);

		protected override bool CheckDisable()
		{
			return base.CheckDisable() || materials.Count <= 0;
		}

		//入れ替えるアセットのペアセットを作成
		protected override Dictionary<Object, Object> MakeReplacePairAssets()
		{
			var result = base.MakeReplacePairAssets();
			foreach (var mat in this.materials)
			{
				result.Add(mat.from, mat.to);
			}
			return result;
		}
	}
}
