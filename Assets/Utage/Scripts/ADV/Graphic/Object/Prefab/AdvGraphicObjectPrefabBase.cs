// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// プレハブオブジェクト表示のスーパークラス
	/// </summary>
	public abstract class AdvGraphicObjectPrefabBase : AdvGraphicBase
	{
		protected GameObject currentObject;

		Animator animator;

		string AnimationStateName { get; set; }

		//初期化処理
		public override void Init(AdvGraphicObject parentObject)
		{
			AnimationStateName = "";
			base.Init(parentObject);
		}

		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public override bool CheckFailedCrossFade(AdvGraphicInfo grapic)
		{
			//ファイルが同じで、アニメーションステート名が違う場合、アニメーション変更だけのパターン変更とみなす
			if (IsOnlyChangeAnimationState(grapic)) return false;
			
			//それ以外は、クロスフェード失敗で、オブジェクトを作り直す流れに
			return true;
		}

		//********描画時のリソース変更********//
		public override void ChangeResourceOnDraw(AdvGraphicInfo grapic, float fadeTime)
		{
			//新しくリソースを設定
			if ( !IsOnlyChangeAnimationState(grapic))
			{
				currentObject = GameObject.Instantiate(grapic.File.UnityObject) as GameObject;
				Vector3 localPostion = currentObject.transform.localPosition;
				Vector3 localEulerAngles = currentObject.transform.localEulerAngles;
				Vector3 localScale = currentObject.transform.localScale;
				currentObject.transform.SetParent(this.transform);
				currentObject.transform.localPosition = localPostion;
				currentObject.transform.localScale = localScale;
				currentObject.transform.localEulerAngles = localEulerAngles;
				currentObject.ChangeLayerDeep(this.gameObject.layer);
				currentObject.gameObject.SetActive(true);

				animator = this.GetComponentInChildren<Animator>();
				ChangeResourceOnDrawSub(grapic);
			}

			if (LastResource == null)
			{
				ParentObject.FadeIn(fadeTime);
			}
		}

		//アニメーションステートの変更だけ行いたい場合
		protected bool IsOnlyChangeAnimationState(AdvGraphicInfo grapic)
		{
			if (LastResource == null) return false;
			if (LastResource == grapic) return true;
			//ファイルが同じで、アニメーションステート名が違う場合、アニメーション変更だけのパターン変更とみなす
			if (LastResource.File == grapic.File && LastResource.AnimationState != grapic.AnimationState) return true;
			return false;
		}

		//********描画時のリソース変更********//
		protected abstract void ChangeResourceOnDrawSub(AdvGraphicInfo grapic);
		//		{
		//			this.sprite = currentObject.GetComponent<SpriteRenderer>();
		//		}

		//拡大縮小の設定
		public override void Scale(AdvGraphicInfo graphic)
		{
			this.transform.localScale = graphic.Scale * Layer.Manager.PixelsToUnits;
		}

		//配置
		public override void Alignment(Utage.Alignment alignment, AdvGraphicInfo graphic)
		{
			this.transform.localPosition = graphic.Position;
		}

		//上下左右の反転
		public override void Flip(bool flipX, bool flipY)
		{
		}

		//********描画時の引数適用********//
		public override void SetCommandArg(AdvCommand command)
		{
			string stateName = GetAnimationStateName(command);
			float fadeTime = command.ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);

			if (stateName.IsNullOrEmpty())
			{
				SetAnimationState(stateName);
				return;
			}
			else
			{
				//カンマ区切りで複数ステート指定可能にする
				var states = StringUtil.SplitByComma(stateName);
				foreach (var state in states)
				{
					SetAnimationState(state);
				}
				AnimationStateName = stateName;
			}
			return;

			void SetAnimationState(string state)
			{
				if (!string.IsNullOrEmpty(state) && !IsAnimationState(state) && !IsAnimatorParameter(state))
				{
					if (command is AdvCommandSprite)
					{
						return;
					}
					else
					{
						//					Debug.LogError(stateName + " is not Animation State");
						//Live2Dなどの場合エラーとは限らないので、エラーメッセージは出さない
						return;
					}
				}

				ChangeAnimationState(state, fadeTime);
			}
		}

		public override void ChangeAnimationState(string animationName, float fadeTime)
		{
			AnimationStateName = animationName;
			if (!string.IsNullOrEmpty(animationName))
			{
				if (animator)
				{
					if (IsAnimationState(animationName))
					{
						//Stateを変更
						animator.CrossFadeInFixedTime(animationName, fadeTime);
					}
					else if(IsAnimatorParameter(animationName))
					{
						//Triggerパラメータを設定（Animatorの遷移に任せる）
						animator.SetTrigger(animationName);
					}
				}
				else
				{
					//レガシーな手法
					Animation ani = GetComponentInChildren<Animation>();
					if (ani != null)
					{
						ani.CrossFade(animationName, fadeTime);
					}
				}
			}
		}

		//Animationステート名を取得
		string GetAnimationStateName(AdvCommand command)
		{
			if (!string.IsNullOrEmpty(LastResource.AnimationState))
			{
				return LastResource.AnimationState;
			}
			else
			{
				//パターン指定が、ステート名の場合
				string pattern = AdvCharacterInfo.ParsePatternOnly(command);
				return pattern;
			}
		}

		bool IsAnimationState(string patternName)
		{
			if (string.IsNullOrEmpty(patternName)) return false;
			if (animator)
			{
				int count = animator.layerCount;
				for(int i = 0; i < count; ++i)
				{
					if (animator.HasState(i, Animator.StringToHash(patternName)))
					{
						return true;
					}
				}
			}
			else
			{
				Animation ani = GetComponentInChildren<Animation>();
				if (ani != null)
				{
					foreach (AnimationState state in ani)
					{
						if (state.name == patternName)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		bool IsAnimatorParameter(string paramName)
		{
			if (string.IsNullOrEmpty(paramName)) return false;
			if (animator == null) return false;

			foreach (var param in animator.parameters)
			{
				if (param.name == paramName)
					return true;
			}
			return false;
		}

		//ルール画像つきのフェードコンポーネントの初期化のみ行う
		public override IAnimationRuleFade BeginRuleFade(AdvEngine engine, AdvTransitionArgs data)
		{
			Debug.LogError(this.gameObject.name + " is not support RuleFade", this.gameObject);
			return null;
		}

		public override void RuleFadeIn(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			Debug.LogError(this.gameObject.name + " is not support RuleFadeIn", this.gameObject);
			if (onComplete != null) onComplete();
		}

		//ルール画像つきのフェードアウト（オブジェクト単位にかけるのでテクスチャ描き込み効果なし）
		public override void RuleFadeOut(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			Debug.LogError(this.gameObject.name + " is not support RuleFadeOut", this.gameObject);
			if (onComplete != null) onComplete();
		}


		enum SaveType
		{
			Animator,
			Other,
		};
		const int Version = 1;
		public override void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			if (animator != null)
			{
				writer.Write(SaveType.Animator.ToString());
				int count = animator.layerCount;
				writer.Write(count);
				for(int i = 0; i < count; ++i)
				{
					AnimatorStateInfo info =  animator.IsInTransition(i) ?
						//状態遷移中なら次の状態を
						animator.GetNextAnimatorStateInfo(i):
						//そうでない今の状態を
						animator.GetCurrentAnimatorStateInfo(i);
					writer.Write(info.fullPathHash);
					writer.Write(info.normalizedTime);
				}
			}
			else
			{
				writer.Write(SaveType.Other.ToString());
				writer.Write(AnimationStateName);
			}
		}

		public override void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			string typeName = reader.ReadString();
			SaveType type = (SaveType)System.Enum.Parse(typeof(SaveType), typeName);
			switch (type)
			{
				case SaveType.Animator:
					{
						int count = reader.ReadInt32();
						for (int i = 0; i < count; ++i)
						{
							int stateNameHash = reader.ReadInt32();
							int layer = i;
							float normalizedTime = reader.ReadSingle();
							animator.Play(stateNameHash, layer, normalizedTime);
						}
					}
					break;
				case SaveType.Other:
				default:
					string stateName = reader.ReadString();
					ChangeAnimationState(stateName, 0);
					break;
			}
		}
	}
}
