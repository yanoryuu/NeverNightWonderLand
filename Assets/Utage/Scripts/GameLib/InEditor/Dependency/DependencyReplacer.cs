// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage
{
	//参照アセットを入れ替える処理
    public class DependencyReplacer
    {
	    //操作対象がアセットか（シーン内のinstanceではなく）
	    public bool TargetIsAsset { get; set; }

	    //無視する型
	    public List<Type> IgnoreType { get; } = new ();

	    //デバッグ出力するか
	    public bool EnableDebugLog { get; set; }

	    //アセットを対象にするか
	    protected List<SerializedObject> DependencySources { get; } = new ();
	    
	    protected Dictionary<Object, Object> ReplaceAssetPair { get; }


	    //指定のアセット内のreplaceAssetPairに登録されているアセットの参照を入れ替える
	    //searchInAssetがtrueなら、アセット内から参照されているアセットも入れ替え対象にする
	    public DependencyReplacer(Object asset, bool searchInAsset, Dictionary<Object, Object> replaceAssetPair)
		    : this(new DependencyParser(asset).GetDependencySources(replaceAssetPair.Keys.ToList(), searchInAsset), replaceAssetPair)
	    {
		    TargetIsAsset = true;
	    }
	    
	    //指定のシーン内の全コンポーネントに対して、replaceAssetPairに登録されているアセットの参照を入れ替える
	    public DependencyReplacer(Scene scene, Dictionary<Object, Object> replaceAssetPair, bool includePrefabInstance = false)
		    : this(DependencyParser.GetDependencySourceComponentsInScene(scene, replaceAssetPair.Keys.ToList(), includePrefabInstance),
			    replaceAssetPair)
	    {
		    TargetIsAsset = false;
	    }
	    public DependencyReplacer(Scene scene, Object srcAsset, Object dstAsset, bool includePrefabInstance = false)
			: this( scene, new Dictionary<Object, Object> { { srcAsset, dstAsset } }, includePrefabInstance)
		{
		}

		//指定ディレクトリ以下の全アセット内からreplaceAssetPairに登録されているアセットの参照を入れ替える
		public DependencyReplacer(string assetDirPath, Dictionary<Object, Object> replaceAssetPair)
			: this(DependencyParser.GetDependencySourceAssetsInProject(assetDirPath, replaceAssetPair.Keys.ToList()),
				replaceAssetPair)
		{
			TargetIsAsset = true;
		}
		public DependencyReplacer(string assetDirPath, Object srcAsset, Object dstAsset)
			:this(assetDirPath, new Dictionary<Object, Object> { { srcAsset, dstAsset } })
		{
		}

		
		protected DependencyReplacer(IEnumerable<SerializedObject> dependencySources,
			Dictionary<Object, Object> replaceAssetPair)
		{
			DependencySources.AddRange(dependencySources);
			ReplaceAssetPair = new Dictionary<Object, Object>(replaceAssetPair);
		}

		//参照しているアセットを全て入れ替える
		public bool Replace()
		{
			bool isReplaced = false;
			foreach (var dependencySource in DependencySources)
			{
				if(CheckIgnoreType(dependencySource.targetObject.GetType()) ) continue;
				isReplaced |= Replace(dependencySource, ReplaceAssetPair);
			}

			if (TargetIsAsset && isReplaced)
			{
				AssetDatabase.Refresh();
			}
			return isReplaced;
		}
		
		bool CheckIgnoreType(Type type)
		{
			return IgnoreType.Contains(type);
		}

		//全てのSerializedPropertyのobjectReferenceValueを入れ替える
		bool Replace(SerializedObject serializedObject, Dictionary<Object, Object> replaceAssetPair)
		{
			serializedObject.Update();

			bool isReplaced = false;
			SerializedProperty it = serializedObject.GetIterator();
			while (it.Next(true))
			{
				if (it.propertyType == SerializedPropertyType.ObjectReference)
				{
					var obj = it.objectReferenceValue;
					if (obj != null)
					{
						Object reference = PrefabUtilityEx.GetPrefabAssetRootIfPrefabPart(obj);
						if (replaceAssetPair.TryGetValue(reference, out Object asset))
						{
							if (EnableDebugLog)
							{
								if (asset == null)
								{
									//アセットがnullになっている場合があるがエラーではない
									Debug.LogWarning(
										$"{reference.name} pare is null {serializedObject.targetObject.name} ... {it.propertyPath}",
										serializedObject.targetObject);
								}
								else
								{
									Debug.Log(
										$"{reference.name} -> {asset.name} in {serializedObject.targetObject.name} ... {it.propertyPath}",
										serializedObject.targetObject);
								}
							}
							it.objectReferenceValue = PrefabUtilityEx.GetPrefabAssetComponentIfPrefabPart(asset, obj.GetType());
							isReplaced = true;
						}
					}
				}
			}

			if (isReplaced)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(serializedObject.targetObject);
			}

			return isReplaced;
		}
    }
}
#endif
