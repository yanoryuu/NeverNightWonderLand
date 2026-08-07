// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


// ギャラリーを強制的に未開放にする（デバッグなどに）
namespace Utage
{
	[AddComponentMenu("Utage/ADV/Extra/AdvCloseGallery")]
	public class AdvCloseGallery : MonoBehaviour
	{
		//　ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine );
		[SerializeField]
		protected AdvEngine engine;

		//Cgギャラリーとシーンギャラリーを全部未開放に
		public void CloseAll()
		{
			CloseAllCgGallery();
			CloseAllSceneGallery();
		}

		//指定のラベルのイベントCGをギャラリーから削除
		public void RemoveCgGallery(string label)
		{
			AdvGallerySaveData save = Engine.SystemSaveData.GalleryData;
			save.RemoveCgLabel(label);
		}

		//Cgギャラリーを全部未開放に
		public void CloseAllCgGallery()
		{
			AdvGallerySaveData save = Engine.SystemSaveData.GalleryData;
			save.ClearCgLabels();
		}

		//指定のラベルの回想シーンをギャラリーから削除
		public void RemoveSceneGallery(string label)
		{
			AdvGallerySaveData save = Engine.SystemSaveData.GalleryData;
			save.RemoveSceneLabel(label);
		}

		//シーンギャラリーを全部未開放に
		public void CloseAllSceneGallery()
		{
			AdvGallerySaveData save = Engine.SystemSaveData.GalleryData;
			save.ClearSceneLabels();
		}

	}
}
