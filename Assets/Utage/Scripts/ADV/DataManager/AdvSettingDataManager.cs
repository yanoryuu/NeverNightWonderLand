// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// シナリオデータの管理
	/// </summary>
	public class AdvSettingDataManager
	{
		//インポートされた章データ
		public AdvImportScenarios ImportedScenarios { get; set; }

		/// <summary>
		/// 基本設定データ
		/// </summary>
		public AdvBootSetting BootSetting { get { return this.bootSetting; } }
		AdvBootSetting bootSetting = new AdvBootSetting();

		/// <summary>
		/// キャラクターテクスチャ設定
		/// </summary>
		public AdvCharacterSetting CharacterSetting { get { return this.characterSetting; } }
		AdvCharacterSetting characterSetting = new AdvCharacterSetting();

		/// <summary>
		/// テクスチャ設定
		/// </summary>
		public AdvTextureSetting TextureSetting { get { return this.textureSetting; } }
		AdvTextureSetting textureSetting = new AdvTextureSetting();

		/// <summary>
		/// サウンドファイル設定
		/// </summary>
		public AdvSoundSetting SoundSetting { get { return this.soundSetting; } }
		AdvSoundSetting soundSetting = new AdvSoundSetting();

		/// <summary>
		/// レイヤー設定
		/// </summary>
		public AdvLayerSetting LayerSetting { get { return this.layerSetting; } }
		AdvLayerSetting layerSetting = new AdvLayerSetting();

		/// <summary>
		/// パラメーター設定
		/// </summary>
		public AdvParamManager DefaultParam { get { return this.defaultParam; } }
		AdvParamManager defaultParam = new AdvParamManager();

		/// <summary>
		/// シーン回想設定
		/// </summary>
		public AdvSceneGallerySetting SceneGallerySetting { get { return this.sceneGallerySetting; } }
		AdvSceneGallerySetting sceneGallerySetting = new AdvSceneGallerySetting();

		/// <summary>
		/// ローカライズ設定
		/// </summary>
		public AdvLocalizeSetting LocalizeSetting { get { return this.localizeSetting; } }
		AdvLocalizeSetting localizeSetting = new AdvLocalizeSetting();

		/// <summary>
		/// アニメーション設定
		/// </summary>
		public AdvAnimationSetting AnimationSetting { get { return this.animationSetting; } }
		AdvAnimationSetting animationSetting = new AdvAnimationSetting();


		/// <summary>
		/// 目パチ設定
		/// </summary>
		public AdvEyeBlinkSetting EyeBlinkSetting { get { return this.eyeBlinkSetting; } }
		AdvEyeBlinkSetting eyeBlinkSetting = new AdvEyeBlinkSetting();
		
		/// <summary>
		/// リップシンク設定
		/// </summary>
		public AdvLipSynchSetting LipSynchSetting { get { return this.lipSynchSetting; } }
		AdvLipSynchSetting lipSynchSetting = new AdvLipSynchSetting();

		/// <summary>
		/// パーティクル設定
		/// </summary>
		public AdvParticleSetting ParticleSetting { get { return this.advParticleSetting; } }
		AdvParticleSetting advParticleSetting = new AdvParticleSetting();

		/// <summary>
		/// VidoClip設定
		/// </summary>
		public AdvVideoSetting VideoSetting { get { return this.videoSetting; } }
		AdvVideoSetting videoSetting = new AdvVideoSetting();
		
		//カスタム設定
		public AdvCustomDataManager CustomDataManager { get; } = new();


		List<IAdvSetting> SettingDataList
		{
			get
			{
				if (settingDataList == null)
				{
					settingDataList = new List<IAdvSetting>();
					settingDataList.Add(LayerSetting);
					settingDataList.Add(CharacterSetting);
					settingDataList.Add(TextureSetting);
					settingDataList.Add(SoundSetting);
					settingDataList.Add(DefaultParam);
					settingDataList.Add(SceneGallerySetting);
					settingDataList.Add(LocalizeSetting);
					settingDataList.Add(AnimationSetting);
					settingDataList.Add(EyeBlinkSetting);
					settingDataList.Add(LipSynchSetting);
					settingDataList.Add(ParticleSetting);
				}
				return settingDataList;
			}
		}
		List<IAdvSetting> settingDataList = null;

		/// <summary>
		/// 起動時の初期化
		/// </summary>
		/// <param name="rootDirResource">ルートディレクトリのリソース</param>
		public void BootInit(string rootDirResource, AdvDataManager dataManager = null)
		{
			BootSetting.BootInit(rootDirResource, dataManager);
			CustomDataManager.BootInit(this);
			if (this.ImportedScenarios != null)
			{
				foreach (AdvChapterData chapter in this.ImportedScenarios.Chapters)
				{
					chapter.BootInit(this);
				}
			}
			//Layerシートがプロジェクトに1枚も存在しない場合でも、デフォルトレイヤーを補完する
			LayerSetting.InitDefaultLayers();
		}

		/// <summary>
		/// 全リソースをバックグラウンドでダウンロード
		/// </summary>
		internal void DownloadAll()
		{
			SettingDataList.ForEach(x => x.DownloadAll());
			CustomDataManager.DownloadAll();
		}

	}
}
