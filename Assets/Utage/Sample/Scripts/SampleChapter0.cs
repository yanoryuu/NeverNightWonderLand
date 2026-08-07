// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// チャプター0で全共通設定（共通リソースやマクロや、パラメーター設定）を定義し、
	/// チャプター1～は個別にロードする場合のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleChapter0")]
	public class SampleChapter0 : MonoBehaviour
	{

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		public UtageUguiTitle title;
		public UtageUguiLoadWait loadWait;
		public UtageUguiMainGame mainGame;
		public List<string> chapterUrlList = new List<string>();
		public List<string> startLabel = new List<string>();

		void Start()
		{
		}


		public void OnClickDownLoadChpater(int chapterIndex)
		{
			StartCoroutine(LoadChaptersAsync(chapterIndex));
		}

		IEnumerator LoadChaptersAsync(int chapterIndex)
		{
			string url = chapterUrlList[chapterIndex];
			//もう設定済みならロードしない
			if (!this.Engine.ExitsChapter(url))
			{
				yield return this.Engine.LoadChapterAsync(url);
			}

			//設定データを反映
			this.Engine.GraphicManager.Remake(this.Engine.DataManager.SettingDataManager.LayerSetting);


			//DL前にファイルサイズを取得する場合・・・
			int sumSize = 0; //合計ファイルサイズ
			//ファイルのセット
			var fileSet = this.Engine.DataManager.GetAllFileSet();
			foreach (var file in fileSet)
			{
				AssetFileBase assetFile = file as AssetFileBase;
				if (assetFile == null)
				{
					Debug.LogError("Not Support Type");
					continue;
				}
				else
				{
					//DL済みかチェック
					if (!assetFile.CheckCacheOrLocal())
					{
						//未DLのファイルのサイズを加算
						sumSize += assetFile.FileInfo.AssetBundleInfo.Size;

						//！注
						//AssetBundleInfo.Sizeは、デフォルトでは0．
						//宴が使っている、Unity公式のアセットバンドルマニフェストにはファイルサイズの情報がないため、
						//SampleCustomAssetBundleLoad.csを参考に
						//事前に独自で、アセットバンドルのサイズを設定しておく必要がある。
					}
				}
			}

			Debug.Log("DownLoad Size = 0");

			//シナリオ内で使用するファイルのみロード
			//Textureシートなどで定義だけされているものはDLしない
			this.Engine.DataManager.DownloadAllFileUsed();

			//ロード待ちのための画面遷移
			title.Close();
			loadWait.OpenOnChapter();
			loadWait.onClose.RemoveAllListeners();
			loadWait.onClose.AddListener(
				() =>
				{
					mainGame.Open();

					this.Engine.JumpScenario(startLabel[chapterIndex]);
				}
			);
		}
	}
}
