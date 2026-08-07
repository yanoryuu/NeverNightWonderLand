// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UTAGE_URP_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;
using UnityEngine.Rendering;
using Utage;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage.RenderPipeline.Urp
{
    //URPのプロジェクト設定のユーティリティ
    public static class UrpProjectSettingsUtil
    {
        const string RendererDataListPropertyName = "m_RendererDataList";
        const string DefaultRendererIndexPropertyName = "m_DefaultRendererIndex";

        public static int SetRenderer(ScriptableRendererData renderer)
        {
            if (renderer == null)
            {
                Debug.LogError("Not found ScriptableRendererData");
                return -1;
            }

            UniversalRenderPipelineAsset urpAsset = UrpUtil.GetCurrentRendererPipeLine();
            if (urpAsset == null)
            {
                Debug.LogError("Not found UniversalRenderPipelineAsset");
                return -1;
            }

            //アクセス方法がないのでSerializedObjectを使ってアクセス
            var serializedObject = new SerializedObject(urpAsset);
            var rendererDataList = serializedObject.FindProperty(RendererDataListPropertyName);
            //RendererDataListにScriptableRendererDataを追加
            if (rendererDataList == null)
            {
                Debug.LogError($"Not found {RendererDataListPropertyName}");
                return -1;
            }
            
            //既に設定済みだったら、そのインデックスを返す
            for (int i = 0; i< rendererDataList.arraySize; ++i)
            {
                var elementAtIndex = rendererDataList.GetArrayElementAtIndex(i);
                if (elementAtIndex.objectReferenceValue == renderer)
                {
                    return i;
                }
            }

            //未設定なら追加
            int rendererIndex = rendererDataList.arraySize;
            rendererDataList.arraySize++;
            var newElement = rendererDataList.GetArrayElementAtIndex(rendererIndex);
            newElement.objectReferenceValue = renderer;

            // 変更を適用
            serializedObject.ApplyModifiedProperties();
            return rendererIndex;
        }

        //現在のプロジェクトのデフォルトのRendererDataとそのインデックスを取得
        public static (ScriptableRendererData,int) GetDefaultRendererData()
        {
            UniversalRenderPipelineAsset urpAsset = UrpUtil.GetCurrentRendererPipeLine();
            if (urpAsset == null)
            {
                Debug.LogError("Not found UniversalRenderPipelineAsset");
                return (null,-1);
            }

            return GetDefaultRendererData(urpAsset);
        }
        
        //URPアセットからデフォルトのRendererDataとそのインデックスを取得
        public static (ScriptableRendererData,int) GetDefaultRendererData(UniversalRenderPipelineAsset urpAsset)
        {

            //アクセス方法がないのでSerializedObjectを使ってアクセス
            var serializedObject = new SerializedObject(urpAsset);
            var rendererDataList = serializedObject.FindProperty(RendererDataListPropertyName);
            if (rendererDataList == null)
            {
                Debug.LogError($"Not found {RendererDataListPropertyName}");
                return (null, -1);
            }

            var defaultIndex = serializedObject.FindProperty(DefaultRendererIndexPropertyName);
            if (defaultIndex == null)
            {
                Debug.LogError($"Not found {DefaultRendererIndexPropertyName}");
                return (null, -1);
            }

            var index = defaultIndex.intValue;
            if (index < 0)
            {
                index = 0;
            }
            if(index >=rendererDataList.arraySize)
            {
                Debug.LogError("Invalid m_DefaultRendererIndex");
                return (null, -1);
            }
            var elementAtIndex = rendererDataList.GetArrayElementAtIndex(index);
            return (elementAtIndex.objectReferenceValue as ScriptableRendererData, index);
        }
    }
}
#endif
