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
	//フォントを別のフォントアセットに変えるための処理
	public abstract class FontChanger
	{
		public bool DebugLog { get; set; } = true;
		private Dictionary<Object, Object> FontAssetPare { get; }

		protected FontChanger(Dictionary<Object, Object> fontAssetPare)
		{
			FontAssetPare = new Dictionary<Object, Object>(fontAssetPare);
		}

		//				string dir = AssetDatabase.GetAssetPath(ProjectDir);
//		EditorSceneManagerEx.OpenSceneAsset(Scene)

		//指定のディレクトリ以下のシーン以外のアセット（プレハブやScriptableObject）のフォントを入れ替える
		public virtual void ChangeFontUnderDir(string projectDirPath)
		{
			if (!string.IsNullOrEmpty(projectDirPath))
			{
				var replacer = new DependencyReplacer(projectDirPath, FontAssetPare);
				replacer.EnableDebugLog = DebugLog;
				replacer.IgnoreType.AddRange(MakeIgnoreTypeList());
				replacer.Replace();
			}
		}

		//指定のシーン内のコンポーネントのフォントを入れ替える
		public virtual void ChangeFontInScene(Scene scene)
		{
			if (scene.IsValid())
			{
				var replacer = new DependencyReplacer(scene, FontAssetPare);
				replacer.EnableDebugLog = DebugLog;
				replacer.IgnoreType.AddRange(MakeIgnoreTypeList());
				replacer.Replace();
			}
		}
		protected abstract List<Type> MakeIgnoreTypeList();
	}

	public class FontChanger<T> : FontChanger
		where T : Object
	{
		public FontChanger(Dictionary<Object, Object> fontAssetPare)
		: base(fontAssetPare)
		{
		}

		protected override List<Type> MakeIgnoreTypeList() 
		{
			return new List<Type>(){ typeof(T) };
		}
	}
}
