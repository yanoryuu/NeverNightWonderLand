// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UTAGE_URP

using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace Utage.RenderPipeline.Urp
{
    //URP関係のユーティリティ
    public static class UrpUtil
    {
        public static UniversalRenderPipelineAsset GetCurrentRendererPipeLine()
        {
#if URP_17_OR_NEWER
            var pipeline = QualitySettings.renderPipeline;
            if(pipeline == null)
            {
                pipeline = GraphicsSettings.defaultRenderPipeline;
            }
            return pipeline as UniversalRenderPipelineAsset; 
#else
            return GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset; 
#endif
        }
    }
}
#endif
