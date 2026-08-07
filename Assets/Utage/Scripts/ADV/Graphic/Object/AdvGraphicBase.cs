// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// グラフィックオブジェクトのデータ
	/// </summary>
	public abstract class AdvGraphicBase : MonoBehaviour
	{
		public AdvGraphicObject ParentObject { get; set; }

		public AdvGraphicLayer Layer { get { return ParentObject.Layer; } }
		public AdvEngine Engine { get { return Layer.Manager.Engine; } }
		protected float PixelsToUnits { get { return Layer.Manager.PixelsToUnits; } }

		protected AdvGraphicInfo LastResource { get { return ParentObject.LastResource; } }

		//********初期化********//
		public virtual void Init(AdvGraphicObject parentObject)
		{
			ParentObject = parentObject;
		}

		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public abstract bool CheckFailedCrossFade(AdvGraphicInfo graphic);

		//********描画時のリソース変更********//
		public abstract void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime);

		//********コマンド特有の引数を適用********//
		public virtual void SetCommandArg(AdvCommand command)
		{
		}
		
		public virtual void ChangeAnimationState(string animationState, float fadeTime)
		{
		}

		//拡大縮小の設定
		public abstract void Scale(AdvGraphicInfo graphic);

		//配置
		public abstract void Alignment(Utage.Alignment alignment, AdvGraphicInfo graphic);

		//上下左右の反転
		public virtual void Flip(bool flipX, bool flipY)
		{
			if (!flipX && !flipY)
				return;
			UguiFlip flip = this.GetComponent<UguiFlip>();
			//フリップ設定を消してもう一度（順番が狂うので）
			if (flip != null)
			{
				flip.RemoveComponentMySelf();
			}
			flip = this.gameObject.AddComponent<UguiFlip>();
			flip.FlipX = flipX;
			flip.FlipY = flipY;
		}

		//エフェクト用の色が変化したとき
		public virtual void OnEffectColorsChange(AdvEffectColor color)
		{
			UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
			if (graphic != null)
			{
				graphic.color = color.MulColor;
			}
		}

		//文字列指定でのパターンチェンジ（キーフレームアニメーションに使う）
		public virtual void ChangePattern(string pattern)
		{
		}

		//ルール画像つきのフェードコンポーネントの初期化のみ行う
		public virtual IAnimationRuleFade BeginRuleFade(AdvEngine engine, AdvTransitionArgs data)
		{
			UguiTransition transition = this.gameObject.AddComponent<UguiTransition>();
			transition.BeginRuleFade(
				engine.EffectManager.FindRuleTexture(data.TextureName),
				data.Vague,
				false);
			return transition;
		}

		//ルール画像つきのフェードイン（オブジェクト単位にかけるのでテクスチャ描き込み効果なし）
		public virtual void RuleFadeIn(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			UguiTransition transition = this.gameObject.AddComponent<UguiTransition>();
			transition.UnscaledTime = Engine.Time.Unscaled;
			transition.RuleFadeIn(
				engine.EffectManager.FindRuleTexture(data.TextureName),
				data.Vague,
				false,
				data.GetSkippedTime(engine),
				() =>
				{
					transition.RemoveComponentMySelf(false);
					if (onComplete != null) onComplete();
				});
		}
		//ルール画像つきのフェードアウト（オブジェクト単位にかけるのでテクスチャ描き込み効果なし）
		public virtual void RuleFadeOut(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			UguiTransition transition = this.gameObject.AddComponent<UguiTransition>();
			transition.UnscaledTime = Engine.Time.Unscaled;
			transition.RuleFadeOut(
				engine.EffectManager.FindRuleTexture(data.TextureName),
				data.Vague,
				false,
				data.GetSkippedTime(engine),
				() =>
				{
					transition.RemoveComponentMySelf(false);
					if (onComplete != null) onComplete();
				});
		}
		//ルール画像つきのフェードのスキップ
		public virtual void SkipRuleFade()
		{
			UguiTransition transition = this.gameObject.GetComponent<UguiTransition>();
			if (transition == null)
			{
				Debug.LogError("Not found UguiTransition on SkipRuleFade");
				return;
			}
			transition.SKipRuleFade();
		}

		public virtual void Read(BinaryReader reader)
		{
		}

		public virtual void Write(BinaryWriter writer)
		{
		}
	}
}

