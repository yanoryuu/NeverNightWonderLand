// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// 章別のDLサンプル
	/// DLするかどうかでボタンを変える（実際には併用することはないと思われる）
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleChapterTitle")]
	public class SampleChapterTitle : MonoBehaviour
	{

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		public UtageUguiTitle title;
		public UtageUguiLoadWait loadWait;
		public UtageUguiMainGame mainGame;
		public List<string> chapterUrlList = new List<string>();
		public List<string> startLabel = new List<string>();

		public bool resetParam = true;
		public bool readSystemSaveData = true;
		public bool readAutoSaveDataParamOnly = false;

		//起動時に、AdvEnigne SystemSaveData IsAutoSaveOnQuitをオフにしてある必要があるので
		//ここで設定する
		public bool isAutoSaveOnQuit = true;

		void Start()
		{
		}


		public void OnClickDownLoadChpater(int chapterIndex)
		{
			StartCoroutine(LoadChaptersAsync(chapterIndex));
		}

		IEnumerator LoadChaptersAsync(int chapterIndex)
		{
			//追加シナリオがDLがされないうちにオートセーブされると、未DLの部分のシステムセーブがない状態で上書きされしまうので
			//デフォルトの「AdvEnigne SystemSaveData IsAutoSaveOnQuit」はオフにしてある必要がある。
			if (this.Engine.SystemSaveData.IsAutoSaveOnQuit)
			{
				Debug.LogError("Check Off AdvEnigne SystemSaveData IsAutoSaveOnQuit");
				//DL中はオートセーブを解除する
				this.Engine.SystemSaveData.IsAutoSaveOnQuit = false;
			}

			//今のパラメーターをバイナリデータとしてとっておく
			//パラメーターをリセットせずに章を追加ロードしたいときに
			byte[] bufferDefaultParam = null;
			byte[] bufferSystemParam = null;
			if (!resetParam)
			{
				bufferDefaultParam =
					BinaryUtil.BinaryWrite((writer) => engine.Param.Write(writer, AdvParamData.FileType.Default));
				bufferSystemParam =
					BinaryUtil.BinaryWrite((writer) => engine.Param.Write(writer, AdvParamData.FileType.System));
			}

			//指定した章よりも前の章はロードする必要がある
			for (int i = 0; i < chapterIndex + 1; ++i)
			{
				string url = chapterUrlList[i];
				//もう設定済みならロードしない
				if (this.Engine.ExitsChapter(url)) continue;

				//ロード自体はこれだけ
				//ただし、URLは
				// http://madnesslabo.net/Utage3Chapter/Windows/chapter2.chapter.asset
				//のように、Windowsなどのプラットフォーム別にフォルダわけなどを終えた絶対URLが必要
				yield return this.Engine.LoadChapterAsync(url);
			}

			//設定データを反映
			this.Engine.GraphicManager.Remake(this.Engine.DataManager.SettingDataManager.LayerSetting);

			//パラメーターをデフォルト値でリセット
			//これは場合によってはリセットしたくない場合もあるので、あえて外にだす
			this.Engine.Param.InitDefaultAll(this.Engine.DataManager.SettingDataManager.DefaultParam);

			//パラメーターの引継ぎ方法は以下のように、いろいろある
			//（ややこしいが、ゲーム起動時なのか、ゲームの最中なのか、そもそもチャプター機能をどう使うかを宴側からは制御できないのでこうなる）

			//その１。メモリ内にとってある場合
			//バイナリデータから読み取る
			if (!resetParam)
			{
				BinaryUtil.BinaryRead(bufferDefaultParam,
					(reader) => engine.Param.Read(reader, AdvParamData.FileType.Default));
				BinaryUtil.BinaryRead(bufferSystemParam,
					(reader) => engine.Param.Read(reader, AdvParamData.FileType.System));
			}

			//その２。オートセーブのパラメーターだけをロードする
			//同じやり方で任意のセーブファイルのパラメーターだけをロードするのも可能
			if (readAutoSaveDataParamOnly)
			{
				//オートセーブデータをロード
				this.Engine.SaveManager.ReadAutoSaveData();
				AdvSaveData autoSave = this.Engine.SaveManager.AutoSaveData;
				if (autoSave != null && autoSave.IsSaved)
				{
					autoSave.Buffer.Overrirde(this.Engine.Param.DefaultData);
				}
			}

			//その３。
			//システムセーブデータをロードする
			//ファイルからロードするので、事前に書き込みされてないとダメ
			//チャプターロードを使う場合は、システムセーブデータの読み込みがされないので
			//一度はこれを使う
			if (readSystemSaveData)
			{
				this.Engine.SystemSaveData.Init(this.Engine);
			}

			//システムセーブデータのオートセーブはここで設定する
			this.Engine.SystemSaveData.IsAutoSaveOnQuit = this.isAutoSaveOnQuit;


			//DL前にファイルサイズを取得する場合・・・

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
						//ファイル名などをキーにして、ファイルサイズを取得
						//ファイルサイズはUnityの公式機能では事前に取得できないため、
						//何らかの形で自前で実装が必要
						Debug.Log(file.FileName);
					}
				}
			}

			//リソースファイルのダウンロードを進めておく
			this.Engine.DataManager.DownloadAll();

			//ロード待ちのための画面遷移
			title.Close();
			loadWait.OpenOnChapter();
			loadWait.onClose.RemoveAllListeners();
			loadWait.onClose.AddListener(
				() =>
				{
					mainGame.Open();

					//StartGameはシステム系以外のパラメーターがリセットされてしまうので
					//パラメーターを引き継がない場合のみStartGame			
					if (resetParam && !readAutoSaveDataParamOnly)
					{
						this.Engine.StartGame(startLabel[chapterIndex]);
					}
					else
					{
						this.Engine.JumpScenario(startLabel[chapterIndex]);
					}
				}
			);
		}
	}
}
