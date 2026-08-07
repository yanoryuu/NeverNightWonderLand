using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //LegacyTextコンポーネントを使っているものを、全てTextMeshProを使ったものに入れ替える
    public class LegacyToTextMeshProConverter
    {
        LegacyToTextMeshProFontAssetSettings FontAssetSettings { get; }
        List<LegacyToTextMeshProTypeInfo> AllTextComponentTypes { get; }

        public LegacyToTextMeshProConverter(LegacyToTextMeshProFontAssetSettings fontAssetSettings,
            List<LegacyToTextMeshProTypeInfo> allTextComponentTypes)
        {
            FontAssetSettings = fontAssetSettings;
            AllTextComponentTypes = new (allTextComponentTypes);
        }

        //プレハブに対して変換処理を行う
        public void ConvertPrefab(GameObject prefabAsset)
        {
            if( !PrefabUtilityEx.IsPrefabAssetRoot(prefabAsset) )
            {
                Debug.LogError(prefabAsset.name + " is not prefab");
                return;
            }
            var allComponents = prefabAsset.GetComponentsInChildren<Component>(true);
            ConvertAll(allComponents);
            UnityEditor.EditorUtility.SetDirty(prefabAsset);
        }

        //プレハブに対して変換処理を行う
        public void ConvertScene(Scene scene)
        {
            List<Component> components = new ();
            foreach (var go in scene.GetAllGameObjectsInScene())
            {
                if (PrefabUtility.IsPartOfPrefabInstance(go)) continue;
                components.AddRange(go.GetComponents<Component>());
            }
            ConvertAll(components.ToArray());
        }

        void ConvertAll(Component[] allComponents)
        {
            allComponents = allComponents.Where(x => x != null).ToArray();
            //テキストを参照するコンポーネントを探す
            var targets = MakeTargets(allComponents);

            //テキストを参照する新しいコンポーネントを追加
            targets.ForEach(x => x.AddNewComponent());

            //全てのLegacyTextコンポーネントをTextMeshProに入れ替え
            foreach (var component in allComponents)
            {
                switch (component)
                {
                    case Text text:
                        SwapTextComponent(text);
                        break;
                    case UguiLocalizeTextSetting localizeTextSetting:
                        SwapLocalizeTextSettingComponent(localizeTextSetting);
                        break;
                }
            }

            //参照しているテキストコンポーネントを入れかえ
            targets.ForEach(x => x.ChangeTextComponent());
            
            //「テキストを参照するコンポーネント」が他のコンポーネントから参照されている場合、参照を入れ替える
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                ChangeReference(component, targets);
            }

            //古いコンポーネントを削除
            targets.ForEach(x => x.ClearOld());

        }

        List<LegacyToTextMeshProComponentSwapper> MakeTargets(Component[] components)
        {
            List<LegacyToTextMeshProComponentSwapper> targets = new ();
            foreach (var component in components)
            {
                var type = component.GetType();
                var componentTypes = AllTextComponentTypes.Find(x => x.OldType == type);
                if (componentTypes != null)
                {
                    targets.Add(new LegacyToTextMeshProComponentSwapper(component, componentTypes));
                }
            }
            return targets;
        }

        //LegacyTextコンポーネントをTextMeshProに入れ替え
        void SwapTextComponent(Text legacyText)
        {
            var go = legacyText.gameObject;
            var fontSetting = FontAssetSettings.FindSetting(legacyText.font);
            var textInfo = new LegacyToTextMeshProTextInfo(legacyText);
            
            //アウトラインがあるかどうか
            bool hasOutline = false;
            if (go.TryGetComponent(out Outline outline))
            {
                hasOutline = true;
                PrefabUtilityEx.RemoveComponentPrefabAssetOrNormalInstance(outline);
            }

            //ノベルテキスト用のコンポーネントがあるならそれを処理
            UguiNovelTextGenerator novelTextGenerator = go.GetComponent<UguiNovelTextGenerator>();
            float characterSpacing = 0;
            if (novelTextGenerator != null)
            {
                characterSpacing = novelTextGenerator.LetterSpaceSize;
            }
            if (legacyText is UguiNovelText novelText)
            {
                var textMeshProNovelText = go.AddComponent<TextMeshProNovelText>();
                textMeshProNovelText.RubyPrefab = fontSetting.RubyPrefab;
            }
            
            //古いコンポーネントを削除
            PrefabUtilityEx.RemoveComponentPrefabAssetOrNormalInstance(legacyText);
            if (novelTextGenerator != null)
            {
                PrefabUtilityEx.RemoveComponentPrefabAssetOrNormalInstance(novelTextGenerator);
            }
            
            //TextMeshProに入れ替え
            var textMeshProUGUI = go.AddComponent<TextMeshProUGUI>();
            textInfo.Apply(textMeshProUGUI);
            
            textMeshProUGUI.font = fontSetting.FontAsset;
            if (hasOutline)
            {
                textMeshProUGUI.fontMaterial = fontSetting.OutlineMaterial;
            }
            textMeshProUGUI.characterSpacing = characterSpacing;

            Debug.Log($"Change {legacyText.GetType().Name} -> {textMeshProUGUI.GetType().Name} in {go.GetHierarchyPath()}",go);
        }

        void SwapLocalizeTextSettingComponent(UguiLocalizeTextSetting localizeTextSetting)
        {
            var go = localizeTextSetting.gameObject;

            //新しいコンポーネントを追加
            UguiLocalizeTextSettingTMP localizeTextSettingTmp = go.AddComponent<UguiLocalizeTextSettingTMP>();

            foreach (var setting in localizeTextSetting.SettingList)
            {
                var settingTmp = new UguiLocalizeTextSettingTMP.Setting();
                settingTmp.language = setting.language;
                settingTmp.font = FontAssetSettings.FindSetting(setting.font).FontAsset;
                settingTmp.fontSize = setting.fontSize;
                settingTmp.lineSpacing = (setting.lineSpacing - 1.0f) * setting.fontSize;
                localizeTextSettingTmp.SettingList.Add(settingTmp);                
            }
            
            //古いコンポーネントを削除
            PrefabUtilityEx.RemoveComponentPrefabAssetOrNormalInstance(localizeTextSetting);

            Debug.Log(
                $"Change {nameof(UguiLocalizeTextSetting)} -> {nameof(UguiLocalizeTextSettingTMP) } in {go.GetHierarchyPath()}",
                go);
        }

        //「テキストを参照するコンポーネント」が他のコンポーネントから参照されている場合、参照を入れ替える
        void ChangeReference(Component component, List<LegacyToTextMeshProComponentSwapper> targets)
        {
            bool hasChanged = false;
            var serializedObject = new SerializedObject(component);
            foreach (var it in serializedObject.GetAllVisibleProperties())
            {
                if (it.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var objectReference = it.objectReferenceValue; 
                    if (objectReference == null) continue;
                    
                    var target = targets.Find(x => x.OldComponent == objectReference);
                    if(target!= null)
                    {
                        it.objectReferenceValue = target.NewComponent;
                        hasChanged = true;
                    }
                }
            }

            if (hasChanged)
            {
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"{component.GetType().Name} Component has changed the referenced component to a new component in {component.gameObject.GetHierarchyPath()}", component);
            }
        }
    }
}
