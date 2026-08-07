// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UtageExtensions;

namespace Utage
{
	//指定のオブジェクトをSerializedObject単位で依存関係を解析するためのクラス
    public class DependencyParser
    {
	    //シーン内の全てのコンポーネントのうち、指定のアセットを参照しているものを取得
	    //includePrefabInstanceがプレハブインスタンスを対象にするか。falseの場合、参照している場合は警告をして対象にしない
	    public static IEnumerable<SerializedObject> GetDependencySourceComponentsInScene(Scene scene, List<Object> targets, bool includePrefabInstance)
	    {
		    foreach (Component component in scene.GetComponentsInScene<Component>(true))
		    {
			    if (component == null) continue;
			    foreach (var item in new DependencyParser(component).GetDependencySources(targets, false))
			    {
				    if (!includePrefabInstance && PrefabUtility.IsPartOfPrefabInstance(component))
				    {
					    //プレハブインスタンスが参照している場合は警告をして対象にしない
					    Debug.LogWarning("Contains Prefab Instance reference. " + component.name, component);
				    }
				    else
				    {
					    yield return item;
				    }
			    }
		    }
	    }

	    public static IEnumerable<SerializedObject> GetDependencySourceAssetsInProject(string dir, List<Object> targets)
	    {
		    foreach (var asset in AssetDataBaseEx.GetAllAssets(dir))
		    {
			    if (asset == null) continue;
			    //シーンは除外
			    if (asset is SceneAsset) continue;

			    //参照元自身は除外
			    if (targets.Contains(asset)) continue;
			    
			    foreach (var item in new DependencyParser(asset).GetDependencySources(targets, true))
			    {
				    yield return item;
			    }
		    }
	    }

	    protected List<SerializedObject> SerializedObjectList { get; } = new List<SerializedObject>();
	    public DependencyParser(Component component) : this(new SerializedObject(component))
	    {
	    }

	    public DependencyParser(SerializedObject obj)
	    {
		    SerializedObjectList.Add(obj);
	    }

	    public DependencyParser(UnityEngine.Object asset)
	    {
		    if (!AssetDataBaseEx.IsAsset(asset))
		    {
			    Debug.LogError($"{asset.name} is not asset",asset );
			    return;
		    }
		    if (PrefabUtilityEx.IsPrefabAssetRoot(asset))
		    {
			    if (asset is GameObject go)
			    {
				    foreach (Component component in go.GetComponentsInChildren<Component>(true))
				    {
					    SerializedObjectList.Add(new SerializedObject(component));
				    }
			    }
		    }
		    else
		    {
			    SerializedObjectList.Add(new SerializedObject(asset));
		    }
	    }

	    //指定のUnityEngine.Objectを参照しているSerializedObject（コンポーネントかScriptableObject）を取得
	    public IEnumerable<SerializedObject> GetDependencySources(List<Object> targets, bool searchInAsset)
	    {
		    //参照先がアセットの場合は、そのアセットも解析するという場合に
		    //targetsは除外するめにHashSetに入れる
		    HashSet<UnityEngine.Object> parsedAssets = new(targets);
		    foreach (var dependencySource in GetDependencySources(targets, parsedAssets, searchInAsset))
		    {
			    yield return dependencySource;
		    }
	    }

	    IEnumerable<SerializedObject> GetDependencySources(List<Object> targets,
		    HashSet<UnityEngine.Object> parsedAssets, bool searchInAsset)
	    {
		    foreach (var serializedObject in SerializedObjectList)
		    {
			    SerializedProperty it = serializedObject.GetIterator();
			    while (it.Next(true))
			    {
				    if (it.propertyType == SerializedPropertyType.ObjectReference)
				    {
					    var obj = it.objectReferenceValue;
					    if (obj == null) continue;
					    if (CheckDependency(obj, targets))
					    {
						    yield return serializedObject;
						    break;
					    }

					    //対象がアセットの場合はさらにチェックが必要
					    if (searchInAsset)
					    {
						    if (AssetDataBaseEx.IsAsset(obj))
						    {
							    var nextAsset = PrefabUtilityEx.GetPrefabAssetRootIfPrefabPart(obj);
							    if (parsedAssets.Add(nextAsset))
							    {
								    //参照オブジェクトがアセットの場合はさらに入れ子で解析
								    foreach (var dependency in new DependencyParser(nextAsset).GetDependencySources(
									             targets, parsedAssets, searchInAsset))
								    {
									    yield return dependency;
								    }
							    }
						    }
					    }
				    }
			    }
		    }
	    }
	    
	    //指定のオブジェクトが、targets内のアセットを参照しているか
	    bool CheckDependency(Object obj, List<Object> targets)
	    {
		    if (obj == null) return false;
		    var refAsset = PrefabUtilityEx.GetPrefabAssetRootIfPrefabPart(obj);
		    return targets.Contains(refAsset);
	    }

	    public void DebugLogDependency(bool searchInAsset)
	    {
		    //参照先がアセットの場合は、そのアセットも解析するという場合に
		    //targetsは除外するめにHashSetに入れる
		    HashSet<UnityEngine.Object> parsedAssets = new();
		    DebugLogDependency(parsedAssets, searchInAsset);
	    }

	    void DebugLogDependency(HashSet<UnityEngine.Object> parsedAssets, bool searchInAsset)
	    {
		    foreach (var serializedObject in SerializedObjectList)
		    {
			    SerializedProperty it = serializedObject.GetIterator();
			    while (it.Next(true))
			    {
				    if (it.propertyType == SerializedPropertyType.ObjectReference)
				    {
					    var obj = it.objectReferenceValue;
					    if (obj == null)
					    {
						    Debug.Log(serializedObject.targetObject.name + " " + it.propertyPath + " : null");
					    }
					    else
					    {
						    Debug.Log(serializedObject.targetObject.name + " " + it.propertyPath + " : " + obj.name + " : type(" + obj.GetType().Name + ")");

						    //対象がアセットの場合はさらにチェックが必要
						    if (searchInAsset)
						    {
							    if (AssetDataBaseEx.IsAsset(obj))
							    {
								    var nextAsset = PrefabUtilityEx.GetPrefabAssetRootIfPrefabPart(obj);
								    if (parsedAssets.Add(nextAsset))
								    {
									    //参照オブジェクトがアセットの場合はさらに入れ子で解析
									    new DependencyParser(nextAsset).DebugLogDependency(parsedAssets, searchInAsset);
								    }
							    }
						    }
					    }
				    }
			    }
		    }
	    }

    }
}
#endif
