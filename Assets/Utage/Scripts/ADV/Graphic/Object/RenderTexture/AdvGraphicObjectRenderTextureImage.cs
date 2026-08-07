// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// テクスチャに描きこんだものを描画する
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectRenderTextureImage")]
	public class AdvGraphicObjectRenderTextureImage : AdvGraphicObjectUguiBase, IAdvCrossFadeImageObject
	{
		protected override Material Material { get { return RawImage.material; } set { RawImage.material = value; } }
		public AdvRenderTextureSpace RenderTextureSpace { get; private set; }

		UguiCrossFadeRawImage CrossFade { get; set; }

		//前フレームのテクスチャを使ってクロスフェード処理を行う
		RenderTexture copyTemporary;
		void ReleaseTemporary()
		{
			if (this.copyTemporary != null)
			{
				RenderTexture.ReleaseTemporary(this.copyTemporary);
				this.copyTemporary = null;
			}
			if (CrossFade != null)
			{
				CrossFade.RemoveComponentMySelf();
				CrossFade = null;
			}
		}

		RawImage RawImage { get; set; }


		void OnDestroy()
		{
			if (this.copyTemporary != null)
			{
				RenderTexture.ReleaseTemporary(this.copyTemporary);
				this.copyTemporary = null;
			}
		}

		//初期化処理
		protected override void AddGraphicComponentOnInit()
		{
		}

		//初期化
		public void Init(AdvRenderTextureSpace renderTextureSpace)
		{
			this.RenderTextureSpace = renderTextureSpace;
			this.RawImage = this.gameObject.GetComponentCreateIfMissing<RawImage>();
			if (renderTextureSpace.RenderTextureType == AdvRenderTextureMode.Image)
			{
				this.Material = new Material(ShaderManager.DrawByRenderTexture);
			}
			this.RawImage.texture = RenderTextureSpace.RenderTexture;
			this.RawImage.SetNativeSize();
			this.RawImage.rectTransform.localScale = Vector3.one;

			//			this.fadeTimer = this.gameObject.AddComponent<Timer>();
			//			this.fadeTimer.AutoDestroy = false;
		}

		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
		{
			return false;
		}

		//********描画時のリソース変更********//
		public override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
		{
			//既に描画されている場合は、クロスフェード用のイメージを作成
			bool crossFade = TryCreateCrossFadeImage(fadeTime, graphic);
			if (!crossFade)
			{
				ReleaseTemporary();
			}
			//新しくリソースを設定
			RawImage.texture = RenderTextureSpace.RenderTexture;
			var setting = RenderTextureSpace.Setting;
			RawImage.rectTransform.SetWidth(setting.RenderTextureSize.x/setting.RenderTextureScale);
			RawImage.rectTransform.SetHeight(setting.RenderTextureSize.y/setting.RenderTextureScale);
			if (!crossFade && LastResource == null)
			{
				ParentObject.FadeIn(fadeTime);
			}
		}

		//ルール画像つきのフェードコンポーネントの初期化のみ行う
		public override IAnimationRuleFade BeginRuleFade(AdvEngine engine, AdvTransitionArgs data)
		{
			UguiTransition transition = this.gameObject.AddComponent<UguiTransition>();
			transition.BeginRuleFade(
				engine.EffectManager.FindRuleTexture(data.TextureName),
				data.Vague,
				RenderTextureSpace.RenderTextureType == AdvRenderTextureMode.Image);
			return transition;
		}

		//ルール画像つきのフェードイン
		public override void RuleFadeIn(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			UguiTransition transition = this.gameObject.AddComponent<UguiTransition>();
			transition.UnscaledTime = Engine.Time.Unscaled;
			transition.RuleFadeIn(
				engine.EffectManager.FindRuleTexture(data.TextureName),
				data.Vague,
				RenderTextureSpace.RenderTextureType == AdvRenderTextureMode.Image,
				data.GetSkippedTime(engine),
				() =>
				{
					transition.RemoveComponentMySelf(false);
					if (onComplete != null) onComplete();
				});
		}

		//ルール画像つきのフェードアウト
		public override void RuleFadeOut(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			UguiTransition transition = this.gameObject.AddComponent<UguiTransition>();
			transition.UnscaledTime = Engine.Time.Unscaled;
			transition.RuleFadeOut(
				engine.EffectManager.FindRuleTexture(data.TextureName),
				data.Vague,
				RenderTextureSpace.RenderTextureType == AdvRenderTextureMode.Image,
				data.GetSkippedTime(engine),
				() =>
				{
					transition.RemoveComponentMySelf(false);
					RawImage.SetAlpha(0);
					if (onComplete != null) onComplete();
				});
		}

		//クロスフェード用のイメージを作成
		protected bool TryCreateCrossFadeImage(float time, AdvGraphicInfo graphic)
		{
			if (LastResource == null) return false;

			if (RawImage.texture == null) return false;

			if (!RenderTextureSpace.HasRendered) return false;

			//前フレームのテクスチャを使ってクロスフェード処理を行う
			ReleaseTemporary();
			Material material = this.Material;
			this.copyTemporary = RenderTextureSpace.RenderTexture.CreateCopyTemporary(0);
			CrossFade= this.gameObject.AddComponent<UguiCrossFadeRawImage>();
			CrossFade.Timer.Unscaled = Engine.Time.Unscaled;
			CrossFade.Material = material;
			CrossFade.CrossFade(
				copyTemporary,
				time,
				() =>
				{
					//テクスチャを破棄
					ReleaseTemporary();
				});
			return true;
		}

		public bool IsCrossFading
		{
			get
			{
				if (CrossFade == null) return false;
				return true;
			}
		}

		public void RestartCrossFade(float fadeTime, Action onComplete)
		{
			if (CrossFade == null)
			{
				Debug.LogError("CrossFadeComponent is not found",this);
				return;
			}

			CrossFade.Restart(
					fadeTime,
					() =>
					{
						ReleaseTemporary();
						onComplete();
					});
		}
		public void SkipCrossFade()
		{
			if (CrossFade == null)
			{
				Debug.LogError("CrossFadeComponent is not found",this);
				return;
			}
			CrossFade.Timer.SkipToEnd();
		}
	}
}
