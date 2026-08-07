#if UTAGE_URP
#if URP_17_OR_NEWER
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    public abstract class ScriptableRendererFeatureBase<T> : ScriptableRendererFeature
        where  T : ScriptableRenderPassBase
    {
        T RenderPass { get; set; }
        
        public override void Create()
        {
            RenderPass = CreatePass();
        }

        protected abstract T CreatePass();

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(RenderPass == null) return;

            if (!RenderPass.EnablePass()) return;
            
            // 反射プローブやプレビューカメラにエフェクトをレンダリングしないようにします。
            if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
                return;
            if (!RenderPass.IsActiveVolume()) return;

#if !URP_13_OR_NEWER
            RenderPass.Setup(renderer);
#endif
            renderer.EnqueuePass(RenderPass);
        }

#if URP_17_OR_NEWER
#elif URP_13_OR_NEWER
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            RenderPass.Setup(renderer);
        }
#endif
    }

    public abstract class ScriptableRendererFeatureSimpleShaderBase<T> : ScriptableRendererFeatureBase<T>
	    where T : ScriptableRenderPassBase
    {
        Shader Shader
        {
            get
            {
                if (shader == null)
                {
                    InitializeShader();
                }

                return shader;
            }
        }

        [SerializeField] Shader shader;

        protected virtual void InitializeShader()
        {
            if (shader == null)
            {
                shader = Shader.Find(ShaderPath);
            }
#if URP_17_OR_NEWER
            if (shaderGraph == null)
            {
                shaderGraph = Shader.Find(ShaderGraphPath);
            }
#endif
        }

        protected virtual Shader GetShader()
        {
#if URP_17_OR_NEWER
            if(RenderGraphUtil.EnableRenderGraph())
            {
                return ShaderGraph;
            }
            else
            {
                return Shader;
            }
#else
            return Shader; 
#endif
        }

        protected abstract string ShaderPath { get; }

        public virtual void Reset()
        {
            InitializeShader();
        }

#if URP_17_OR_NEWER
        protected Shader ShaderGraph
        {
            get
            {
                if (shaderGraph == null)
                {
                    InitializeShader();
                }
                return shaderGraph;
            }
        }

        [SerializeField] Shader shaderGraph;
        protected abstract string ShaderGraphPath { get; }
#endif
    }

}
#endif
