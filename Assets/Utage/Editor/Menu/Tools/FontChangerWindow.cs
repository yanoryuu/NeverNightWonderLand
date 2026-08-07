// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Utage
{
	//フォント別のアセットに変える
	public abstract class FontChangerWindow<T>
		: EditorWindowWithSave
		where T : Object
	{
		[SerializeField] protected bool debugLog = true;
		[SerializeField] protected Object projectDir;
		[SerializeField] protected SceneAsset scene;
		[SerializeField] protected T from;
		[SerializeField] protected T to;

		protected virtual bool CheckDisable()
		{
			if (from == null) return true;
			if (to == null) return true;
			if(scene==null && projectDir==null) return true;
			return false;
		}

		protected virtual void ChangeFont()
		{
			Debug.Log($"Font Asset Change {this.@from.name} to {this.to.name} ");
			var replacePairAssets = MakeReplacePairAssets();
			FontChanger<T> changer = new FontChanger<T>(replacePairAssets)
			{
				DebugLog = this.debugLog,
			};
			
			if(projectDir!=null)
			{
				changer.ChangeFontUnderDir(AssetDatabase.GetAssetPath(projectDir));
			}

			if (scene != null)
			{
				changer.ChangeFontInScene(EditorSceneManagerEx.OpenSceneAsset(scene));
			}
		}
	
		//入れ替えるアセットのペアセットを作成
		protected virtual Dictionary<Object,Object> MakeReplacePairAssets()
		{
			return new Dictionary<Object, Object> { { from, to } };
		}
	}
}
