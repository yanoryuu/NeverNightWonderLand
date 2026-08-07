// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Utage
{

    /// <summary>
    /// フェード切り替え機能つきのスプライト表示
    /// </summary>
    [AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectAvatar")]
    public class AdvGraphicObjectAvatar : AdvGraphicObjectUguiBase
    {
		protected Timer FadeTimer { get; set; }

		//アバター描画コンポーネント
		protected AvatarImage Avatar { get; set; }
		//目パチ
		protected EyeBlinkAvatar EyeBlink { get; set; }
		//口パク
		protected LipSynchAvatar LipSynch { get; set; }

		//アニメーション
		protected AdvAnimationPlayer Animation { get; set; }

		protected CanvasGroup Group { get; set; }
		
		//オプション表示切替用の情報
		protected class AvatarOptionInfo
		{
			public string paranName;
			public string optionName;
		};
		protected List<AvatarOptionInfo> AvatarOptionInfoList
		{
			get { return avatarOptionInfoList; }
		}
		private List<AvatarOptionInfo> avatarOptionInfoList = new List<AvatarOptionInfo>();

		//初期化処理
		protected override void AddGraphicComponentOnInit()
		{
			Avatar = this.gameObject.AddComponent< AvatarImage>();
			Avatar.OnPostRefresh.AddListener(OnPostRefresh);
			this.EyeBlink = this.gameObject.AddComponent<EyeBlinkAvatar>();
			this.EyeBlink.UnscaledTime = Engine.Time.Unscaled;

			this.LipSynch = this.gameObject.AddComponent<LipSynchAvatar>();
			this.LipSynch.UnscaledTime = Engine.Time.Unscaled;

			this.Animation = this.gameObject.AddComponent<AdvAnimationPlayer>();

			this.Group = this.gameObject.AddComponent<CanvasGroup>();

			this.FadeTimer = this.gameObject.AddComponent<Timer>();
			this.FadeTimer.AutoDestroy = false;
		}

		protected override Material Material { get { return Avatar.Material; } set { Avatar.Material = value; } }

		//エフェクト用の色が変化したとき
		public override void OnEffectColorsChange(AdvEffectColor color)
		{
			Graphic[] graphics = GetComponentsInChildren<Graphic>();
			foreach (Graphic graphic in graphics )
			{
				if (graphic != null)
				{
					graphic.color = color.MulColor;
				}
			}
		}

		//目パチなどのために
		protected virtual void OnPostRefresh()
		{
			if (!this.ParentObject.EnableRenderTexture)
			{
				OnEffectColorsChange(this.ParentObject.EffectColor);
			}
		}

		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
		{
			AvatarData avatarData = graphic.File.UnityObject as AvatarData;
			return Avatar.AvatarData != avatarData;
		}

		//********描画時のリソース変更********//
		public override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
		{
			Avatar.Material = graphic.RenderTextureSetting.GetRenderMaterialIfEnable(Avatar.Material);

			//既に描画されている場合は、クロスフェード用のイメージを作成
//			bool crossFade = TryCreateCrossFadeImage(fadeTime);
			//新しくリソースを設定
			AvatarData avatarData = graphic.File.UnityObject as AvatarData;
			Avatar.AvatarData = avatarData;
			Avatar.CachedRectTransform.sizeDelta = avatarData.Size;
			Avatar.AvatarPattern.SetPattern(graphic.RowData);
			InitAvatarOptionInfoList(avatarData);

			//目パチを設定
			SetEyeBlinkSync(graphic.EyeBlinkData);
			//口パクを設定
			SetLipSynch(graphic.LipSynchData);
			//アニメーションを設定
			SetAnimation(graphic.AnimationData);

			if (LastResource == null)
			{
				ParentObject.FadeIn(fadeTime);
			}
		}

		//上下左右の反転
		public override void Flip(bool flipX, bool flipY)
		{
			Avatar.Flip(flipX, flipY);
		}
		
		//アニメーションを設定
		protected void SetAnimation(AdvAnimationData data)
		{
			Animation.Cancel();
			if (data != null)
			{
				Animation.Play(data.Clip,Engine.Page.SkippedSpeed);
			}
		}

		//目パチを設定
		protected void SetEyeBlinkSync(AdvEyeBlinkData data)
		{
			if (data == null)
			{
				EyeBlink.AnimationData = new MiniAnimationData();
			}
			else
			{
				EyeBlink.IntervalTime.Min = data.IntervalMin;
				EyeBlink.IntervalTime.Max = data.IntervalMax;
				EyeBlink.RandomDoubleEyeBlink = data.RandomDoubleEyeBlink;
				EyeBlink.EyeTag = data.Tag;
				EyeBlink.AnimationData = data.AnimationData;
			}
		}

		//口パクを設定
		protected void SetLipSynch(AdvLipSynchData data)
		{
			LipSynch.Cancel();
			if (data == null)
			{
				LipSynch.AnimationData = new MiniAnimationData();
			}
			else
			{
				LipSynch.Type = data.Type;
				LipSynch.Interval = data.Interval;
				LipSynch.ScaleVoiceVolume = data.ScaleVoiceVolume;
				LipSynch.LipTag = data.Tag;
				LipSynch.AnimationData = data.AnimationData;
				LipSynch.Play();
			}
		}

		protected virtual void Update()
		{
			UpdateAvatarOption();
		}

		protected virtual void InitAvatarOptionInfoList(AvatarData avatarData)
		{
			AvatarOptionInfoList.Clear();
			foreach (var option in avatarData.GetAllOptionPatterns())
			{
				string paramName = avatarData.name + "_" + option;
				object param;
				if( !Engine.Param.TryGetParameter(paramName, out param) ) continue;
				AvatarOptionInfoList.Add( 
					new AvatarOptionInfo()
					{
						optionName = option,
						paranName = paramName,
					});
			}
		}

		//アバターのオプションを取得
		protected virtual void UpdateAvatarOption()
		{
			foreach (var info in AvatarOptionInfoList)
			{
				bool enable = Engine.Param.GetParameterBoolean(info.paranName);
				Avatar.AvatarPattern.SetOptionEnable(info.optionName,enable);
			}
		}
    }
}
