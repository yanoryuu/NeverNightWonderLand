#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace Utage.RenderPipeline.Urp
{
	public static class RenderGraphUtil
	{
		static MaterialPropertyBlock SharedPropertyBlock { get; } = new MaterialPropertyBlock();
		// マテリアルの追加プロパティを設定するために使用するstaticな共通インスタンスを取得
		//（Allocを避けるためにstaticな一時バッファとして使用する）
		public static MaterialPropertyBlock GetSharedPropertyBlock()
		{
			var propertyBlock = SharedPropertyBlock;
			propertyBlock.Clear();
			return propertyBlock;
		}
		
		//activeColorTextureをコピーするためのTextureHandleを作成
		public static TextureHandle CreateRenderGraphTextureFromActiveColor(RenderGraph renderGraph,
			ContextContainer frameData,
			string textureName = "_ActiveColorCopy",
			bool clear = false, FilterMode filterMode = FilterMode.Bilinear,
			TextureWrapMode wrapMode = TextureWrapMode.Clamp)
		{
			var textureHandle = frameData.Get<UniversalResourceData>().activeColorTexture;
			if (!textureHandle.IsValid())
			{
				Debug.LogError("colorTextureHandle is invalid");
			}
			return CreateRenderGraphTexture(renderGraph,textureHandle, textureName, clear, filterMode, wrapMode);
		}

		//指定のテクスチャハンドルをコピーするためのTextureHandleを作成
		public static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph,
			TextureHandle textureHandle,
			string textureName,
			bool clear = false, FilterMode filterMode = FilterMode.Bilinear,
			TextureWrapMode wrapMode = TextureWrapMode.Clamp)
		{
			var textureDesc = renderGraph.GetTextureDesc(textureHandle);
			return CreateRenderGraphTexture(renderGraph, textureDesc, textureName, clear, filterMode, wrapMode);
		}

		//textureDescをコピーするためのTextureHandleを作成
		public static TextureHandle CreateRenderGraphTexture(RenderGraph renderGraph,
			TextureDesc textureDesc,
			string textureName,
			bool clear = false, FilterMode filterMode = FilterMode.Bilinear,
			TextureWrapMode wrapMode = TextureWrapMode.Clamp)
		{
			// MSAA テクスチャの「desc.bindMS = true」でない限り、サンプリングにバインドされる前に解決パスが挿入されます。
			// Blit等で使用するデフォルト シェーダーは MSAA ターゲットをサンプリングすることを想定していないため、「bindMS = false」のままにします。
			// カメラ ターゲットで MSAA が有効になっている場合でも、コピー カラー パスの前に MSAA 解決が行われますが、
			// この変更により、メインパスの前に不必要な MSAA 解決が回避されます。
			textureDesc.bindTextureMS = false;
			textureDesc.msaaSamples = MSAASamples.None;
			// 深度バッファは使用しない
			textureDesc.depthBufferBits = DepthBits.None;

			textureDesc.name = textureName;
			textureDesc.clearBuffer = clear;
			textureDesc.filterMode = filterMode;
			textureDesc.wrapMode = wrapMode;
			return renderGraph.CreateTexture(textureDesc);
		}

		// カメラの画面をサンプリングするための一時的なカラーコピーテクスチャの作成に使用する、RenderTextureDescriptor（RenderTextureの作成に必要な情報）を作成
		public static RenderTextureDescriptor GetCameraTargetTextureDescriptor(UniversalCameraData cameraData)
		{
			return GetTextureDescriptor(cameraData.cameraTargetDescriptor);
		}

		// 一時的なカラーコピーテクスチャの作成に使用する、RenderTextureDescriptor（RenderTextureの作成に必要な情報）を作成
		public static RenderTextureDescriptor GetTextureDescriptor(RenderTextureDescriptor desc)
		{
			// MSAA テクスチャの「desc.bindMS = true」でない限り、サンプリングにバインドされる前に解決パスが挿入されます。
			// Blit等で使用するデフォルト シェーダーは MSAA ターゲットをサンプリングすることを想定していないため、「bindMS = false」のままにします。
			// カメラ ターゲットで MSAA が有効になっている場合でも、コピー カラー パスの前に MSAA 解決が行われますが、
			// この変更により、メインパスの前に不必要な MSAA 解決が回避されます。
			desc.msaaSamples = 1;
			// これにより、この例のメイン パスでは深度バッファが使用されないため、現在の記述子に関連付けられた深度バッファのコピーが回避されます。
			desc.depthBufferBits = (int)DepthBits.None;

			return desc;
		}
		
		//現在、RenderGraphが有効かどうかチェックする
		public static bool EnableRenderGraph()
		{
#if UNITY_6000_4_OR_NEWER
			return true;
#else
			return !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;
#endif
		}
	}
}
#endif
#endif
