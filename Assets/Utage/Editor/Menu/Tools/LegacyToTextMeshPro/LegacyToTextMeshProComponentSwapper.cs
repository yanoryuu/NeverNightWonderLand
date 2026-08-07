using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //Legacyテキストを参照しているコンポーネントを新しいコンポーネントに入れ替え
    //基本手的なプロパティ値をコピーし
    //参照しているテキストコンポーネントをTextMeshPro系に入れ替え
    public class LegacyToTextMeshProComponentSwapper
    {
        public GameObject TargetGameObject { get; }
        public Component OldComponent { get; }
        List<ReferenceTextTarget> ReferenceTextTargets { get; } = new();
        public Component NewComponent { get; private set; }
        public LegacyToTextMeshProTypeInfo TypeInfo { get; }

        class ReferenceTextTarget
        {
            Text LegacyText { get; }
            public string PropertyPath { get; }
            public GameObject GameObject { get; }
            
            public ReferenceTextTarget(string propertyPath, Text legacyText)
            {
                PropertyPath = propertyPath;
                LegacyText = legacyText;
                GameObject = LegacyText == null ? null : LegacyText.gameObject;
            }

            //新しくTextMeshProを参照するプロパティに設定する対象かどうかをチェック
            public bool IsTarget(Component component, string targetPropertyPath)
            {
                bool ComparePropertyPath(string path)
                {
                    return String.Compare(path, targetPropertyPath, StringComparison.OrdinalIgnoreCase) == 0;
                }
                
                //命名規則にしたがって、TextMesh用のプロパティパスから、LegacyText用のプロパティパスを探す
                if(ComparePropertyPath(PropertyPath+"TMP"))
                {
                    return true;
                }
                if (ComparePropertyPath("TextMeshPro" + PropertyPath))
                {
                    return true;
                }

                if (ComparePropertyPath(PropertyPath + "Pro"))
                {
                    return true;
                }

                //命名規則の例外
                if (component.GetType() == typeof(AdvUguiBacklogTMP))
                {
                    if (ComparePropertyPath("textMeshProLog" + PropertyPath))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public LegacyToTextMeshProComponentSwapper(Component oldComponent, LegacyToTextMeshProTypeInfo typeInfo)
        {
            OldComponent = oldComponent;
            TypeInfo = typeInfo;
            TargetGameObject = OldComponent.gameObject;
            InitReferenceTextTargets(OldComponent);
        }

        void InitReferenceTextTargets(Component component)
        {
            ReferenceTextTargets.Clear();
            var serializedObject = new SerializedObject(component);
            foreach (var it in serializedObject.GetAllVisibleProperties())
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var fieldInfo = it.GetFieldInfo();
                    if (fieldInfo == null)
                    {
                        Debug.LogWarning($" {NewComponent.GetType()} {it.propertyPath} is not found fieldInfo",
                            NewComponent);
                        continue;
                    }
                    System.Type fieldType = fieldInfo.FieldType;
                    if (typeof(Text).IsAssignableFrom(fieldType))
                    {
                        ReferenceTextTargets.Add(new ReferenceTextTarget(it.propertyPath, it.objectReferenceValue as Text));
                    }
                }
            }
        }

        //新しいコンポーネントを追加する
        public void AddNewComponent()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(OldComponent))
            {
                var json = EditorJsonUtility.ToJson(OldComponent);
                //コンポーネントを追加
                var component = OldComponent.gameObject.AddComponent(TypeInfo.NewType);
                NewComponent = component;
                //基本的なプロパティ値をJSONでコピー
                EditorJsonUtility.FromJsonOverwrite(json, component);
            }
            else
            {
                //コンポーネントを追加
                var json = JsonUtility.ToJson(OldComponent);
                var component = OldComponent.gameObject.AddComponent(TypeInfo.NewType);
                NewComponent = component;
                //基本的なプロパティ値をJSONでコピー
                JsonUtility.FromJsonOverwrite(json, component);
            }
            Debug.Log($"Change {NewComponent.GetType().Name} to {OldComponent.GetType().Name} in {TargetGameObject.GetHierarchyPath()}", TargetGameObject);
        }
        
        //設定されているテキストコンポーネントを入れかえ
        public void ChangeTextComponent()
        {
            if( ReferenceTextTargets.Count <= 0) return;

            bool changed = false;
            var serializedObject = new SerializedObject(NewComponent);
            foreach (var it in serializedObject.GetAllVisibleProperties())
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                var fieldInfo = it.GetFieldInfo();
                if (fieldInfo == null)
                {
                    Debug.LogWarning($" {NewComponent.GetType()} {it.propertyPath} is not found fieldInfo", NewComponent);
                    continue;
                }
                System.Type fieldType = fieldInfo.FieldType;

                if (typeof(Text).IsAssignableFrom(fieldType))
                {
                    //参照をクリア
                    it.objectReferenceValue = null;
                    changed = true;
                }
                else if (typeof(TextMeshProUGUI).IsAssignableFrom(fieldType) ||
                         typeof(TextMeshProNovelText).IsAssignableFrom(fieldType))
                {
                    //
                    var target = FindReferenceTextTarget(NewComponent,it);
                    if (target == null)
                    {
                        continue;
                    }
                    
                    //元のコンポーネントでも参照が設定されてない
                    if(target.GameObject == null)
                    {
                        continue;
                    }

                    if (!target.GameObject.TryGetComponent(fieldType, out var textComponent))
                    {
                        //新しいTextMeshProComponentがないのでエラー出力
                        Debug.LogError($"{target.GameObject.GetHierarchyPath()} has no {fieldType}", target.GameObject);
                        continue;
                    }

                    //新しいテキストコンポーネントがあるので、そこに設定
                    it.objectReferenceValue = textComponent;
                    changed = true;
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            ReferenceTextTarget FindReferenceTextTarget(Component component, SerializedProperty property)
            {
                if( ReferenceTextTargets.Count == 1 ) return ReferenceTextTargets[0];
                var path = property.propertyPath;
                foreach (var target in ReferenceTextTargets)
                {
                    if (target.IsTarget(component, path)) return target;
                }
                
                //見つからなかった
                string errorMsg = $"{component.GetType()} .{path} is not found text reference\n";
                foreach (var target in ReferenceTextTargets)
                {
                    errorMsg += target.PropertyPath + "\n";
                }
                Debug.LogError(errorMsg, component);
                return null;
            }
        }

        //古いコンポーネントを消す
        public void ClearOld()
        {
            PrefabUtilityEx.RemoveComponentPrefabAssetOrNormalInstance(OldComponent);
        }
    }
}
