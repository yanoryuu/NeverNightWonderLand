// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// ファイル管理
	/// </summary>
	[AddComponentMenu("Utage/Lib/File/AssetFileManager")]
	[RequireComponent(typeof(StaticAssetManager))]
//	[RequireComponent(typeof(ConvertFileListManager))]
	public partial class AssetFileManager : MonoBehaviour
	{
		public FileIOManager FileIOManager
		{
			get => this.GetComponentCache<FileIOManager>(ref fileIOManager);
			set => fileIOManager = value;
		}
		[SerializeField,UnityEngine.Serialization.FormerlySerializedAs("fileIOManger")]
		FileIOManager fileIOManager;

		public bool EnableResourcesLoadAsync
		{
			get => enableResourcesLoadAsync;
			set => enableResourcesLoadAsync = value;
		}
		[SerializeField]
		bool enableResourcesLoadAsync = true;

		//ダウンロードエラー時に、自動でリトライする回数
		public float TimeOutDownload
		{
			get => timeOutDownload;
			set => timeOutDownload = value;
		}
		[SerializeField]
		float timeOutDownload = 10;                 //タイムアウト時間

		//ダウンロードエラー時に、自動でリトライする回数
		public int AutoRetryCountOnDonwloadError
		{
			get => autoRetryCountOnDonwloadError;
			set => autoRetryCountOnDonwloadError = value;
		}
		[SerializeField]
		int autoRetryCountOnDonwloadError = 5;

		[SerializeField]
		int loadFileMax = 5;                    //同時にロードするファイルの最大数

		// ロード済みファイルの最大数（この数を超えたら、未使用ファイルをアンロードする）
		public int MaxFilesOnMemory
		{
			get => rangeOfFilesOnMemory.Max;
			set => rangeOfFilesOnMemory.Max = value;
		}

		// 未使用ファイルをアンロードするときに、ロード済みファイル数がこの数以下になるようにする。
		// 使用中ファイルはアンロードしないので、アンロード後もロード済みファイル数がこの数以上になることはある
		public int MinFilesOnMemory
		{
			get => rangeOfFilesOnMemory.Min;
			set => rangeOfFilesOnMemory.Min = value;
		}
		[SerializeField,MinMax(0,100)]
		MinMaxInt rangeOfFilesOnMemory = new MinMaxInt() { Min = 10, Max = 20 };


		//アンロード時の処理タイプ
		public enum UnloadType
		{
			None,						//特に何もしない
			UnloadUnusedAsset,			//対象のアセットがアンロードされたらUnloadUnusedAsset
			UnloadUnusedAssetAlways,    //常にUnloadUnusedAsset
			NoneAndUnloadAssetBundleTrue,   //アセットバンドルがある場合,UnloadUnusedAssetはせず、AssetBundle.Unload(true)を呼ぶ
		};
		public UnloadType UnloadUnusedType
		{
			get => unloadType;
			set => unloadType = value;
		}
		[SerializeField] UnloadType unloadType = UnloadType.UnloadUnusedAsset;
		
		//UnloadUnusedAssetsを呼ぶ閾値
		public int UnloadUnusedAssetsThreshold
		{
			get => unloadUnusedAssetsThreshold;
			set => unloadUnusedAssetsThreshold = value;
		}
		[SerializeField] int unloadUnusedAssetsThreshold = 0;

		//NoneAndUnloadAssetBundleTrue時に非アセットバンドルのUnloadUnusedAssetsを行う
		public bool UnloadUnusedAssetsOnAssetBundleTrue
		{
			get => unloadUnusedAssetsOnAssetBundleTrue;
			set => unloadUnusedAssetsOnAssetBundleTrue = value;
		}
		[SerializeField] bool unloadUnusedAssetsOnAssetBundleTrue = false;

		bool unloadingUnusedAssets;	//現在、UnloadUnusedAssetsを呼び出しているかどうか
		bool forceUnloadUnusedAssets; //閾値を無視して強制的にUnloadUnusedAssetsを呼ぶ
		int pendingUnloadCount; //UnloadUnusedAssets呼び出しまでの累積カウント

		[SerializeField]
		internal bool loadLegacySoundExt = false;							//昔の形式のサウンドの拡張子を変更する呼び出しをする際に

		[SerializeField]
		internal bool isOutPutDebugLog = false;								//ダウンロードのログをコンソールに出力する
		[SerializeField]
		internal bool isDebugCacheFileName = false;							//キャッシュファイルパスをデバッグモードにする（隠蔽せずに公開する）
		[SerializeField]
		internal bool isDebugBootDeleteChacheTextAndBinary = false;					//起動時に、テキストやバイナリのキャッシュを削除する
		[SerializeField]
		internal bool isDebugBootDeleteChacheAll = false;							//起動時に、キャッシュファイルを全て消す

		public AssetFileManagerSettings Settings
		{
			get => settings;
			set => settings = value;
		}
		[SerializeField]
		AssetFileManagerSettings settings;


		public AssetBundleInfoManager AssetBundleInfoManager
		{
			get => this.GetComponentCacheCreateIfMissing(ref assetBundleInfoManager);
			set => assetBundleInfoManager = value;
		}
		[SerializeField]
		AssetBundleInfoManager assetBundleInfoManager;

		[SerializeField]
		AssetFileDummyOnLoadError dummyFiles = new AssetFileDummyOnLoadError();

		List<AssetFileBase> loadingFileList = new List<AssetFileBase>();		//ロード中ファイルリスト
		List<AssetFileBase> loadWaitFileList = new List<AssetFileBase>();		//ロード待ちファイルリスト
		List<AssetFileBase> usingFileList = new List<AssetFileBase>();			//使用中のファイルリスト
		public Dictionary<string, AssetFileBase> FileTbl => fileTbl;
		Dictionary<string, AssetFileBase> fileTbl= new Dictionary<string, AssetFileBase>(); //管理中のファイルリスト

		//カスタムロードのマネージャー
		CustomLoadManager CustomLoadManager { get { return this.GetComponentCacheCreateIfMissing<CustomLoadManager>(ref customLoadManager); } }
		CustomLoadManager customLoadManager;

		//動的にロードしないリソースのリスト
		StaticAssetManager StaticAssetManager { get { return this.GetComponentCacheCreateIfMissing<StaticAssetManager>(ref staticAssetManager); } }
		StaticAssetManager staticAssetManager;

		//ファイルの存在チェックのために使う
		AssetFileManagerCheckerInEditor CheckerInEditor { get { return checkerInEditor; } }
		AssetFileManagerCheckerInEditor checkerInEditor = new AssetFileManagerCheckerInEditor();

		Action<AssetFile> callbackError;

		public Action<AssetFile> CallbackError
		{
			get { return callbackError ??( callbackError = CallbackFileLoadError); }
			set { callbackError = value; }
		}

        bool isWaitingRetry;

        // ロードエラー時のデフォルトコールバック
        void CallbackFileLoadError(AssetFile file)
		{
            AssetFileBase errorFile =  file as AssetFileBase;
			string errorMsg = file.LoadErrorMsg + "\n" + file.FileName;
			Debug.LogError(errorMsg);

            if (SystemUi.GetInstance() != null)
            {
                if (isWaitingRetry)
                {
					StartCoroutine(CoWaitRetry(errorFile));
                }
                else
                {
                    isWaitingRetry = true;
                    //リロードを促すダイアログを表示
                    SystemUi.GetInstance().OpenDialog1Button(
                        errorMsg, LanguageSystemText.LocalizeText(SystemText.Retry),
                        () =>
                        {
                            isWaitingRetry = false;
                            ReloadFileSub(errorFile);
                        }
                        );
                }
            }
            else
            {
                ReloadFileSub(errorFile);
            }
        }

		//リトライダイアログからの応答を待つ
        IEnumerator CoWaitRetry(AssetFileBase file)
        {
            while (isWaitingRetry)
            {
                yield return null;
            }
            ReloadFileSub(file);
        }

        void Awake()
		{
			if (null == instance)
			{
				instance = this;
			}
		}

		///＊＊＊以下、ファイル追加・ロード処理＊＊＊///

		//サウンドファイルを指定のキーから探す
		AssetFileBase FindFromSoundKey(string path, string key)
		{
			if (string.IsNullOrEmpty(key)) return null;

//			string path2 = GetPathChangedSoundExt(path);
			AssetFileBase file = FindFromTbl(path, "", null);
			if (file != null) return file;

			foreach ( var keyValue in fileTbl )
			{
				file = keyValue.Value;
				IAssetFileSoundSettingDataKey setting = file.SettingData as IAssetFileSoundSettingDataKey;
				if (setting != null && setting.Key == key)
				{
					return file;
				}
			}
			return null;
		}

		// 管理ファイルを追加
		AssetFileBase AddSub(string path, IAssetFileSettingData settingData)
		{
			//管理テーブルにあるなら、そこから
			string path2 = GetPathChangedSoundExt(path);
			AssetFileBase file = FindFromTbl(path, path2,settingData);
			if (file != null) return file;
/*			if (path.Contains(" "))
			{
				Debug.LogWarning("[" + path + "] contains white space");
			}
*/			AssetBundleInfo assetBundleInfo = FindAssetBundleInfo(path,path2);
			AssetFileInfo fileInfo = new AssetFileInfo(path, settings, assetBundleInfo);

			//staticなアセットにあるなら、そこから
			file = StaticAssetManager.FindAssetFile(this, fileInfo, settingData);
			if (file == null)
			{
				//カスタムロードなアセットにあるなら、そこから
				file = CustomLoadManager.Find(this, fileInfo, settingData);
				if (file == null)
				{
					//宴形式の通常ファイルロード
					file = new AssetFileUtage(this, fileInfo, settingData);
				}
			}
			fileTbl.Add(path, file);
			return file;
		}

		void AddFileCheckerInEditor(string path, IAssetFileSettingData settingData)
		{
			//staticなアセットにあるなら、そこから
			if (StaticAssetManager.Contains(path)) return;
			CheckerInEditor.AddFile(path, settingData);
		}


		string GetPathChangedSoundExt(string path)
		{
			if (!loadLegacySoundExt) return "";
			string ext = FilePathUtil.GetExtension(path);
			if (string.Compare(ext, ".ogg", true) == 0)
			{
				return FilePathUtil.ChangeExtension(path, ".mp3");
			}
			else if (string.Compare(ext, ".mp3", true) == 0)
			{
				return FilePathUtil.ChangeExtension(path, ".ogg");
			}
			return "";
		}

		AssetFileBase FindFromTbl(string path, string path2, IAssetFileSettingData settingData)
		{
			AssetFileBase file;
			//管理テーブルにあるなら、そこから
			if (fileTbl.TryGetValue(path, out file))
			{
				return file;
			}
			if (string.IsNullOrEmpty(path2))
			{
				return null;
			}
			if (fileTbl.TryGetValue(path2, out file))
			{
				Debug.Log("find from path2  = " + path2);
				return file;
			}
			return null;
		}

		AssetBundleInfo FindAssetBundleInfo(string path, string path2)
		{
			AssetBundleInfo assetBundleInfo = AssetBundleInfoManager.FindAssetBundleInfo(path);
			//管理テーブルにあるなら、そこから
			if (assetBundleInfo!=null)
			{
				return assetBundleInfo;
			}
			if (string.IsNullOrEmpty(path2))
			{
				return null;
			}
			assetBundleInfo = AssetBundleInfoManager.FindAssetBundleInfo(path2);
			if (assetBundleInfo!=null)
			{
				Debug.Log("find from path2  = " + path2);
				return assetBundleInfo;
			}
			return null;
		}

		// ダウンロード
		void DownloadSub(AssetFileBase file)
		{
			if (file.CheckCacheOrLocal())
			{
				return;
			}
			file.ReadyToLoad(AssetFileLoadPriority.DownloadOnly, null);
			AddLoadFile(file);
		}

		// プリロード
		void PreloadSub(AssetFileBase file, System.Object referenceObj)
		{
			AddUseList(file);
			file.ReadyToLoad(AssetFileLoadPriority.Preload, referenceObj);
			AddLoadFile(file);
		}
		//	ファイルのバックグランドロード
		AssetFile BackGroundLoadSub(AssetFileBase file, System.Object referenceObj)
		{
			AddUseList(file);
			file.ReadyToLoad(AssetFileLoadPriority.BackGround, referenceObj);
			AddLoadFile(file);
			return file;
		}
		
		// 通常のロード
		AssetFile LoadSub(AssetFileBase file, System.Object referenceObj)
		{
			AddUseList(file);
			file.ReadyToLoad(AssetFileLoadPriority.Default, referenceObj);
			AddLoadFile(file);
			return file;
		}

		//使用中のファイルリストとして設定
		void AddUseList(AssetFileBase file)
		{
			if (!usingFileList.Contains (file)) 
			{
				usingFileList.Add (file);
			}
		}

		// コールバックつきのロード
		void LoadSub(AssetFileBase file, System.Action<AssetFile> onComplete)
		{
			StartCoroutine(CoLoadWait(file, onComplete));
		}

		// コールバックつきのロードコルーチン
		IEnumerator CoLoadWait(AssetFileBase file, System.Action<AssetFile> onComplete)
		{
			if (file.IsLoadEnd)
			{
				onComplete(file);
			}
			else
			{
				LoadSub(file, this);
				while (!file.IsLoadEnd) yield return null;
				onComplete(file);
			}
		}

		//ロード待ちリストに追加
		void AddLoadFile(AssetFileBase file)
		{
			TryAddLoadingFileList(file);
		}

		//ロード待ちリストを追加
		bool TryAddLoadingFileList(AssetFileBase file)
		{
			if (file.IsLoadEnd) return false;
			if (loadingFileList.Contains(file))
			{
				return false;
			}

			if (loadingFileList.Count < loadFileMax)
			{
				loadingFileList.Add(file);
				if (isOutPutDebugLog) Debug.Log("Load Start :" + file.FileName);
				StartCoroutine(LoadAsync(file));
				return true;
			}
			else if (!loadWaitFileList.Contains(file))
			{
				loadWaitFileList.Add(file);
				return false;
			}
			else
			{
				return false;
			}
		}

		IEnumerator LoadAsync(AssetFileBase file)
		{
			yield return file.LoadAsync(
					//ロード成功
					() =>
					{
						if (isOutPutDebugLog) Debug.Log("Load End :" + file.FileName);
						loadingFileList.Remove(file);
						LoadNextFile();
					},
					//ロード失敗
					() =>
					{
						//ロード失敗
						if (dummyFiles.isEnable)
						{
							//ダミーファイルをロード
							if (dummyFiles.outputErrorLog)
							{
								Debug.LogError("Load Failed. Dummy file loaded:" + file.FileName + "\n" + file.LoadErrorMsg);
							}
							file.LoadDummy(dummyFiles);
							loadingFileList.Remove(file);
							LoadNextFile();
						}
						else
						{
							Debug.LogError("Load Failed :" + file.FileName + "\n" + file.LoadErrorMsg);
							//ロード失敗処理
							if (CallbackError != null)
							{
								CallbackError(file);
							}
						}
					}
				);
		}


		//ファイルリロード
		void ReloadFileSub(AssetFileBase file)
		{
			StartCoroutine(ReloadFileSubAsync(file));
		}

		//ファイルリロード(無限ループよけに1フレーム遅らせる)
		IEnumerator ReloadFileSubAsync(AssetFileBase file)
		{
			yield return null;
			yield return StartCoroutine(LoadAsync(file));
		}

		//ロード待ちのファイルをロードする
		void LoadNextFile()
		{
			AssetFileBase next = null;
			foreach (AssetFileBase file in loadWaitFileList)
			{
				if (next == null)
				{
					next = file;
				}
				else
				{
					if (file.Priority < next.Priority)
					{
						next = file;
					}
				}
			}
			if (next != null)
			{
				if (next.IsLoadEnd)
				{
					//ダミーファイルなどによって既にロード済み
					loadWaitFileList.Remove(next);
				}
				else if (TryAddLoadingFileList(next))
				{
					loadWaitFileList.Remove(next);
				}
				else
				{
					Debug.LogError("Failed To Load file " + next.FileName);
				}
			}
		}

		///＊＊＊以下、未使用ファイルを管理してロードとメモリ効率を高める処理＊＊＊///

		void LateUpdate()
		{
			int totalLoaded = GetTotalOnMemoryFileCount ();
			if (totalLoaded > MaxFilesOnMemory)
			{
				UnloadUnusedFileList(totalLoaded - MinFilesOnMemory);
			}
		}


		void OnDestroy()
		{
			forceUnloadUnusedAssets = true;
			UnloadUnusedFileList(Int32.MaxValue);
			instance = null;
		}

		//メモリを消費しているファイルの数
		int GetTotalOnMemoryFileCount()
		{
			int count = loadingFileList.Count;
			foreach( var file in usingFileList )
			{
				if (file.IgnoreUnload || !file.IsLoadEnd)
					continue;

				++count;
			}
			return count;
		}

		//指定の数のシステムメモリにプールされてる未使用ファイルをアンロードして、メモリを解放
		protected virtual void UnloadUnusedFileList(int count)
		{
			if (usingFileList.Count <= 0 || count <= 0)
			{
				return;
			}

			//指定個数だけアンロード
			int unloadUnusedCount = 0;
			List<AssetFileBase> newList = new List<AssetFileBase>();
			foreach (AssetFileBase file in usingFileList)
			{
				//ロード中だったり、参照がのこっているのは対象としない
				if (count <= 0 || file.IgnoreUnload || !file.IsLoadEnd || file.ReferenceCount > 0)
				{
					newList.Add(file);
				}
				else
				{
					if (isOutPutDebugLog) Debug.Log("Unload " + file.FileName);
					file.Unload();
					--count;
					if (NeedUnloadUnusedAssets(file))
					{
						++unloadUnusedCount;
					}
				}
			}
			UnloadUnusedAssets(unloadUnusedCount);
			usingFileList = newList;
		}
		

		//UnloadUnusedAssetsが必要なファイルかどうか
		bool NeedUnloadUnusedAssets(AssetFileBase file)
		{
			//UnityObject以外のファイルはUnloadUnusedAssets呼ぶ必要ない
			if (file.FileType != AssetFileType.UnityObject) return false;

			//NoneAndUnloadAssetBundleTrue時は、非アセットバンドルのみ対象
			if (unloadType == UnloadType.NoneAndUnloadAssetBundleTrue)
			{
				if (!unloadUnusedAssetsOnAssetBundleTrue) return false;
				if (file.FileInfo?.AssetBundleInfo != null) return false;
			}

			return true;
		}

		//未使用リソースをすべて解放するUnloadUnusedAssetsを呼ぶ
		//重いのでなるべく呼ばないために本当に必要か色々チェック
		protected virtual void UnloadUnusedAssets(int count)
		{
			switch (unloadType)
			{
				//必要な数が0以下ならしない
				case UnloadType.UnloadUnusedAsset:
					if (count <= 0) return;
					break;
				//問答無用で毎回
				case UnloadType.UnloadUnusedAssetAlways:
					break;
				//非アセットバンドルのUnloadUnusedAssetsが有効なら、カウントがある場合のみ実行
				case UnloadType.NoneAndUnloadAssetBundleTrue:
					if (!unloadUnusedAssetsOnAssetBundleTrue || count <= 0) return;
					break;
				//アンロードしない
				case UnloadType.None:
				default:
					return;
			}

			//閾値に達するまでUnloadUnusedAssetsの呼び出しを遅延させる
			pendingUnloadCount += count;
			if (!forceUnloadUnusedAssets && pendingUnloadCount < unloadUnusedAssetsThreshold) return;

			//解放中なら二重解放はしない
			if (unloadingUnusedAssets) return;
			//動いてないならコルーチン回せない
			if (!this.gameObject.activeInHierarchy) return;

			StartCoroutine(UnloadUnusedAssetsAsync());
		}
		IEnumerator UnloadUnusedAssetsAsync()
		{
			if (isOutPutDebugLog) Debug.Log("UnloadUnusedAssets");
			unloadingUnusedAssets = true;
			pendingUnloadCount = 0;
			yield return Resources.UnloadUnusedAssets();
			unloadingUnusedAssets = false;
		}


		///＊＊＊以下、外部から値を取得するための処理＊＊＊///

		//ロード終了チェック
		bool IsLoadEnd( AssetFileLoadPriority priority )
		{
			foreach (var file in loadingFileList)
			{
				if( file.Priority <= priority )
				{
					if (!file.IsLoadEnd) return false;
				}
			}
			foreach (var file in loadWaitFileList)
			{
				if (file.Priority <= priority)
				{
					if (!file.IsLoadEnd) return false;
				}
			}
			return true;
		}

		//ロード中のファイル数
		int CountLoading(AssetFileLoadPriority priority)
		{
			int count = 0;
			foreach (var file in loadingFileList)
			{
				if (file.Priority <= priority)
				{
					if (!file.IsLoadEnd)
					{
						++count;
					}
				}
			}
			foreach (var file in loadWaitFileList)
			{
				if (file.Priority <= priority)
				{
					if (!file.IsLoadEnd)
					{
						++count;
					}
				}
			}
			return count;
		}
	}
}