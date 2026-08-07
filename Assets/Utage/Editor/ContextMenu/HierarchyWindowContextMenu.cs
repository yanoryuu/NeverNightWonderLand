// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage
{
    //ヒエラルキーWindow内で、GameObjectの作成などをサポート
    public class HierarchyWindowContextMenu
    {
        const string RootPath = "GameObject/Utage/";

        //サウンドグループの作成（互換性のために）
        [MenuItem(RootPath + "SoundGroups", false)]
        public static void CreateSoundGroups()
        {
            var soundManager = SoundManager.GetInstance();
            if (soundManager == null)
            {
                Debug.Log("Not found SoundManager");
                return;
            }
            
            // アンドゥ操作をまとめるためのグループ
            using (new EditorUndoGroupScope(nameof(CreateSoundGroups)))
            {
                //GUID指定で、もとになるプレハブをロード
                GameObject prefab = AssetDataBaseEx.LoadAssetByGuid<GameObject>("704fa02cf417cb84a8b1e361d7368fd6");
                //プレハブからインスタンスを作成
                var go = Object.Instantiate(prefab.gameObject, soundManager.transform);
                go.name = prefab.name;
                InitPrefabInstance(go);
                //子オブジェクトのみを子に
                List<GameObject> newObjects = (from Transform child in go.transform select child.gameObject).ToList();
                go.transform.MoveAllChildren(go.transform.parent);
                
                //オーディオミキサーの設定
                var audioMixer = soundManager.AudioMixer;
                if (audioMixer != null)
                {
                    //オーディオミキサーが設定されていたら、それをグループに設定
                    Tuple<string, string>[] groupNames =
                            { new("Bgm", "BGM"), new("Se", "SE"), new("Voice", "Voice"), new("Ambience", "Ambience") }
                        ;
                    foreach (var groupName in groupNames)
                    {
                        var child = soundManager.transform.Find(groupName.Item1);
                        if (child == null)
                        {
                            Debug.LogError("Not found " + groupName.Item1 + " in " + soundManager.name, soundManager);
                            continue;
                        }

                        if (child.TryGetComponent(out SoundGroup soundGroup))
                        {
                            var audioMixerGroup = audioMixer.FindMatchingGroups(groupName.Item2);
                            if (audioMixerGroup.Length == 1)
                            {
                                soundGroup.AudioMixerGroup = audioMixerGroup[0];
                            }
                        }
                    }
                }
                Object.DestroyImmediate(go);
                // オブジェクト作成操作
                for (int i = 0; i < newObjects.Count; i++)
                {
                    Undo.RegisterCreatedObjectUndo(newObjects[i], "Create New Object" + i);
                }
            }
        }

        //指定のパスのプレハブUIをロードして、シーン内に新規オブジェクトとして追加
        public static void CreateGameObjectToHierarchy(string prefabPath, string undoActionName)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"{prefabPath}にUIプレハブが存在しません");
                return;
            }

            var go = AddPrefabToSelectedChild(prefab);

            //作成したゲームオブジェクトを選択状態にする
            Selection.activeGameObject = go;
            //Undoに登録
            Undo.RegisterCreatedObjectUndo(go, undoActionName);
        }

        public static GameObject AddPrefabToSelectedChild(GameObject prefab)
        {
            var go = (Selection.activeGameObject != null)
                ? Object.Instantiate(prefab.gameObject, Selection.activeGameObject.transform)
                : Object.Instantiate(prefab.gameObject);
            go.name = prefab.name;
            InitPrefabInstance(go);
            return go;
        }

        public static void InitPrefabInstance(GameObject go)
        {
            var t = go.transform;
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one;
            t.rotation = Quaternion.identity;
            if (go.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
    }
}
