// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


// ギャラリーを強制解放する（デバッグなどに）
namespace Utage
{
	[AddComponentMenu("Utage/ADV/Extra/AdvOpenGallery")]
	public class AdvOpenGallery : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine );
		[SerializeField]
		protected AdvEngine engine;

		//シーンギャラリーとCgギャラリーを全部解放
		public void OpenAll()
		{
			OpenAllSceneGallery();
			OpenAllCgGallery();
		}

		//シーンギャラリーを全部解放
		public void OpenAllSceneGallery()
		{
			AdvGallerySaveData save = Engine.SystemSaveData.GalleryData;
			List<AdvSceneGallerySettingData> list =　Engine.DataManager.SettingDataManager.SceneGallerySetting.List;
			foreach (AdvSceneGallerySettingData item in list)
			{
				save.AddSceneLabel(item.ScenarioLabel);
			}
		}

		//Cgギャラリーを全部解放
		public void OpenAllCgGallery()
		{
			AdvGallerySaveData save = Engine.SystemSaveData.GalleryData;
			List<AdvTextureSettingData> list = Engine.DataManager.SettingDataManager.TextureSetting.List;
			foreach (AdvTextureSettingData item in list)
			{
				if (item.TextureType != AdvTextureSettingData.Type.Event) continue;
				if (string.IsNullOrEmpty(item.ThumbnailPath)) continue;

				save.AddCgLabel(item.Key);
			}
		}
	}
}
