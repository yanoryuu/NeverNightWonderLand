// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// 独自のアセットバンドルロードをするサンプル
	/// </summary>
	[AddComponentMenu("Utage/Sample/SampleCustomAssetBundleLoad")]
	public class SampleCustomAssetBundleLoad : MonoBehaviour
	{
		/// <summary>開始フレームで自動でADVエンジンを起動する</summary>
		[SerializeField]
		string startScenario = "";

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField]
		AdvEngine engine;


		[System.Serializable]
		public class SampleAssetBundleVersionInfo
		{
			public string resourcePath; //宴内で管理するリソースのロードパス
			public string url;          //実際のアセットバンドルのURL
			public int version;         //アセットバンドルのバージョン
			public int size;			//アセットバンドルのサイズ
		}

		//アセットバンドルのURLと、宴がロードするパスとを関連付けるためのリスト
		//ただし、宴のファイルマネージャーは1アセットバンドル＝1アセットとなっているので
		//1つのアセットバンドル内に、複数のファイルを入れる構成ではロードできない
		//その場合は、CustomLoadManagerを使ってロード・アンロードの処理のコールバックを登録すること
		List<SampleAssetBundleVersionInfo> assetBundleList = new List<SampleAssetBundleVersionInfo>()
		{
			//以下は、あくまでサンプル
			//もちろん実際は、ハードコーディングしないで、
			//何らかの命名規則に従って処理するか、
			//アセットバンドル作成ツールが作成するであろうファイルリストを使うことになる
			//デフォルトの宴ではAssetBundleManifestを利用している

			//シナリオのファイルパスと、そのアセットバンドルが置いてあるURL・バージョンを関連付ける
			new SampleAssetBundleVersionInfo()
			{
				resourcePath = @"Sample.scenarios.asset",
				url = @"http://madnesslabo.net/Utage3CustomLoad/Windows/sample.scenarios.asset",
				version = 0,
				size = 128,
			},
			

			//テクスチャやサウンドのファイルパスと、そのアセットバンドルが置いてあるURL・バージョンを関連付ける
			new SampleAssetBundleVersionInfo()
			{
				resourcePath = @"Texture/Character/Utako/utako.png",
				url = @"http://madnesslabo.net/Utage3CustomLoad/Windows/texture/character/utako/utako.asset",
				version = 0,
				size = 256,
			},


			//同様に、全ての必要なリソースリストを作る
			new SampleAssetBundleVersionInfo()
			{
				resourcePath = @"Texture/BG/TutorialBg1.png",
				url = @"http://madnesslabo.net/Utage3Download/Sample/Windows/texture/bg/tutorialbg1.asset",
				version = 0,
				size = 512,
			},
			new SampleAssetBundleVersionInfo()
			{
				resourcePath = @"Sound/BGM/MainTheme.wav",
				url = @"http://madnesslabo.net/Utage3Download/Sample/Windows/sound/bgm/maintheme.asset",
				version = 0,
				size = 1024,
			},
		};

		AdvImportScenarios Scenarios { get; set; }

		void Awake()
		{
			StartCoroutine(LoadEngineAsync());
		}

		//エンジンをロード
		IEnumerator LoadEngineAsync()
		{
			//シナリオやリソースのロードのまえに
			//アセットバンドルのファイルリストの初期化が必要
			//
			//・宴が渡すファイルパスと、
			//・実際にアセットバンドルが置いてあるサーバーのURL、
			//・アセットバンドルのバージョン
			//これらの情報を設定する
			foreach (var versionInfo in assetBundleList)
			{
				AssetFileManager.GetInstance().AssetBundleInfoManager.AddAssetBundleInfo(versionInfo.resourcePath, versionInfo.url, versionInfo.version, versionInfo.size);
			}

			//開始ラベルを登録しておく
			if (!string.IsNullOrEmpty(startScenario))
			{
				Engine.StartScenarioLabel = startScenario;
			}

			//ロードファイルタイプをサーバーに
			AssetFileManager.InitLoadTypeSetting(AssetFileManagerSettings.LoadType.Server);

			//シナリオのロード
			yield return LoadScenariosAsync("Sample.scenarios.asset");

			if (this.Scenarios == null)
			{
				Debug.LogError("Scenarios is Blank. Please set .scenarios Asset", this);
				yield break;
			}

			//シナリオとルートパスを指定して、エンジン起動
			//カスタムしてスクリプトを書くときは、最終的にここにくればよい
			Engine.BootFromExportData(this.Scenarios, "");

			//自動再生
			StartEngine();
		}

		//シナリオをロードする
		IEnumerator LoadScenariosAsync(string url)
		{
			AssetFile file = AssetFileManager.Load(url, this);
			while (!file.IsLoadEnd) yield return null;

			AdvImportScenarios scenarios = file.UnityObject as AdvImportScenarios;
			if (scenarios == null)
			{
				Debug.LogError(url + " is  not scenario file");
				yield break;
			}
			this.Scenarios = scenarios;
		}


		//シナリオ開始
		void StartEngine()
		{
			StartCoroutine(CoPlayEngine());
		}

		IEnumerator CoPlayEngine()
		{
			//初期化（シナリオのDLなど）を待つ
			while (Engine.IsWaitBootLoading) yield return null;

			if (string.IsNullOrEmpty(startScenario))
			{
				Engine.StartGame();
			}
			else
			{
				Engine.StartGame(startScenario);
			}
		}
	}
}
