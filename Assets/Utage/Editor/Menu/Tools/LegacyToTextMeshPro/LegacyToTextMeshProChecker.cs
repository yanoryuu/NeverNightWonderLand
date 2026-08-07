
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage
{
    //LegacyTextコンポーネントを使っているかのチェッカー
    public class LegacyToTextMeshProChecker 
    {
        LegacyToTextMeshProFontAssetSettings FontAssetSettings { get; }
        List<LegacyToTextMeshProTypeInfo> AllTextComponentTypes { get; }
        public LegacyToTextMeshProChecker(LegacyToTextMeshProFontAssetSettings fontAssetSettings, List<LegacyToTextMeshProTypeInfo> allTextComponentTypes)
        {
            FontAssetSettings = fontAssetSettings;
            AllTextComponentTypes = allTextComponentTypes;
        }
        
        //指定のプレハブアセット内をチェックする
        public void CheckInPrefabAsset(GameObject prefabAsset)
        {
            int count = CheckComponents(prefabAsset.GetComponentsInChildren<Component>(true));
            if (count > 0)
            {
                Debug.LogWarning($"{count} {nameof(Text)} component(s) found in prefabAsset ({prefabAsset.name })", prefabAsset);
            }
            else
            {
                Debug.Log($"Succeeded. {nameof(Text)} component is not found in prefabAsset ({prefabAsset.name})", prefabAsset);
            }

            //プレハブアセットに依存するフォントをチェック
            CheckDependencies(prefabAsset);
        }

        //指定のシーン内をチェックする
        public void CheckInScene(SceneAsset sceneAsset)
        {
            var scene = EditorSceneManagerEx.OpenSceneAsset(sceneAsset);
            int count = CheckComponents(scene.GetComponentsInScene<Component>(true));
            if (count > 0)
            {
                Debug.LogWarning($"{count} {nameof(Text)} component(s) found in Scene ({scene.name})",
                    sceneAsset);
            }
            else
            {
                Debug.Log($"Succeeded. {nameof(Text)} component is not found in Scene ({scene.name})",
                    sceneAsset);
            }
            
            //シーンアセットに依存するフォントをチェック
            CheckDependencies(sceneAsset);
        }

        void CheckDependencies(Object targetAsset)
        {
            var targetDependencies = EditorUtility.CollectDependencies(new[] { targetAsset });
            List<Object> dependencies = new List<Object>(targetDependencies);
            foreach (var targetDependency in targetDependencies)
            {
                if (targetDependency is TMP_FontAsset fontAsset)
                {
                    var fontAssetDependencies = EditorUtility.CollectDependencies(new[] { fontAsset });
                    foreach (var fontAssetDependency in fontAssetDependencies)
                    {
                        //TMP_FontAssetが使用しているLegacyFontはチェックから除外
                        if(fontAssetDependency is Font legacyFont)
                        {
                            dependencies.Remove(legacyFont);
                        }
                    }
                }
            }

            foreach (var dependency in dependencies)
            {
                //LegacyFontが使われているかチェック
                if (dependency is Font font)
                {
                    if (FontAssetSettings.FontAssetSettings.Find(x => x.LegacyFontAsset == font) != null)
                    {
                        Debug.LogWarning($"FontAsset ({dependency}) is used in {targetAsset.name}. Please check it.", targetAsset);
                    }
                    else
                    {
                        Debug.LogError($"Unknown FontAsset ({dependency}) is used in {targetAsset.name}. Please check it.", targetAsset);
                    }
                }
            }
        }

        int CheckComponents(IEnumerable<Component> components)
        {
            int count = 0;
            foreach (var component in components)
            {
                if (!CheckComponent(component))
                {
                    count++;
                }
            }
            return count;
        }

        bool CheckComponent(Component component)
        {
            if (component == null) return true;
            bool result = true;
            switch (component)
            {
                case Text:
                    Debug.LogWarning(
                        $"{component.gameObject.GetHierarchyPath()}  {nameof(Text)} component is used. Please replace to TextMeshPro.",
                        component);
                    result = false;
                    break;
                case UguiLocalizeTextSetting:
                    Debug.LogWarning(
                        $"{component.gameObject.GetHierarchyPath()}  {nameof(UguiLocalizeTextSetting)} component is used. Please replace to {nameof(UguiLocalizeTextSettingTMP)}.",
                        component);
                    result = false;
                    break;
            }
            var type = component.GetType();
            var componentTypes = AllTextComponentTypes.Find(x => x.OldType == type);
            if (componentTypes != null)
            {
                Debug.LogWarning(
                    $"{component.gameObject.GetHierarchyPath()}  {type.Name} component is used.",
                    component);
                result = false;                
            }

            return result;
        }
    }
}
