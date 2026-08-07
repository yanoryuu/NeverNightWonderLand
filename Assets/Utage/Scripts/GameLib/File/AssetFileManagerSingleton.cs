// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ファイル管理
	/// </summary>
	public partial class AssetFileManager : MonoBehaviour
	{
		//初期化が終わっているか
		internal static bool IsInitialized()
		{
			return true;
		}

		/// <summary>
		/// ロード設定を初期化（ゲーム起動直後にすること）
		/// </summary>
		static public void InitLoadTypeSetting(AssetFileManagerSettings.LoadType loadTypeSetting)
		{
			GetInstance().Settings.BootInit(loadTypeSetting);
		}

		/// <summary>
		/// エラー処理の設定
		/// </summary>
		/// <param name="callbackError">エラー時に呼ばれるコールバック</param>
		static public void InitError(Action<AssetFile> callbackError)
		{
			GetInstance().CallbackError = callbackError;
		}


		//エディタ上でロード対象のファイルが存在しているかチェックする処理をクリア
		static public void ClearCheckErrorInEditor()
		{
			var instance = GetInstance();
			if (instance != null)
			{
				instance.CheckerInEditor.Clear();
			}
		}

		//エディタ上でロード対象のファイルが存在しているかチェック（宴のインポート時に存在しないファイルがないかを主にチェックする）
		static public void CheckErrorInEditor(string rootPath, bool checkExt)
		{
			var instance = GetInstance();
			if (instance != null)
			{
				instance.CheckerInEditor.CheckAll(rootPath, checkExt);
			}
		}

		/// <summary>
		/// ファイル情報取得
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <returns>ファイル情報</returns>
		static public AssetFile GetFileCreateIfMissing(string path, IAssetFileSettingData settingData = null)
		{

			if (!IsEditorErrorCheck)
			{
				AssetFile file = GetInstance().AddSub(path, settingData);
				return file;
			}
			else
			{
				var instance = GetInstance();
				if (instance != null)
				{
					instance.AddFileCheckerInEditor(path, settingData);
				}
				return null;
			}
		}

		// サウンドファイル情報取得
		static public AssetFile GetSoundFileCreateIfMissing(string path, string key)
		{
			if (!IsEditorErrorCheck)
			{
				AssetFile file = GetInstance().FindFromSoundKey(path, key);
				if(file!=null) return file;
				return GetInstance().AddSub(path, null);
			}
			else
			{
				return null;
			}
		}


		/// <summary>
		/// ファイルのロード
		/// すぐ使うファイルに使用すること
		/// ロードの優先順位は一番高い
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		/// <returns>ファイル情報</returns>	
		static public AssetFile Load(string path, System.Object referenceObj)
		{
			return Load(GetFileCreateIfMissing(path), referenceObj);
		}
		/// <summary>
		/// ファイルのロード
		/// すぐ使うファイルに使用すること
		/// ロードの優先順位は一番高い
		/// </summary>
		/// <param name="file">ロードするファイル</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		/// <returns>ファイル情報</returns>	
		static public AssetFile Load(AssetFile file, System.Object referenceObj)
		{
			return GetInstance().LoadSub(file as AssetFileBase, referenceObj);
		}

		/// <summary>
		/// ファイルのロード
		/// すぐ使うファイルに使用すること
		/// ロードの優先順位は一番高い
		/// </summary>
		/// <param name="file">ロードするファイル</param>
		/// <param name="">ロード終了のコールバック</param>
		/// <returns>ファイル情報</returns>	
		static public void Load(AssetFile file, System.Action<AssetFile> onComplete)
		{
			GetInstance().LoadSub(file as AssetFileBase, onComplete);
		}

		/// <summary>
		/// ファイルの先行ロード
		/// もうすぐ使うファイルに使用すること
		/// ロードの優先順位は二番目に高い
		/// 事前にロードをかけてロード時間を短縮しておくのが主な用途
		/// </summary>
		/// <param name="path">ファイルパス</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		static public void Preload(string path, System.Object referenceObj)
		{
			Preload(GetFileCreateIfMissing(path), referenceObj);
		}

		/// <summary>
		/// ファイルの先行ロード
		/// もうすぐ使うファイルに使用すること
		/// ロードの優先順位は二番目に高い
		/// 事前にロードをかけてロード時間を短縮しておくのが主な用途
		/// </summary>
		/// <param name="file">先行ロードするファイル</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		static public void Preload(AssetFile file, System.Object referenceObj)
		{
			GetInstance().PreloadSub(file as AssetFileBase, referenceObj);
		}

		/// <summary>
		/// ファイルのバックグラウンドロード
		/// すぐには使わないが、そのうち使うであろうファイルに使用すること
		/// ロードの優先順位は三番目に高い
		/// 事前にロードをかけてロード時間を短縮しておくのが主な用途。
		/// </summary>
		/// <param name="file">ファイルパス</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		static public AssetFile BackGroundLoad(string path, System.Object referenceObj)
		{
			return BackGroundLoad(GetFileCreateIfMissing(path), referenceObj);
		}
		/// <summary>
		/// ファイルのバックグラウンドロード
		/// すぐには使わないが、そのうち使うであろうファイルに使用すること
		/// ロードの優先順位は三番目に高い
		/// 事前にロードをかけてロード時間を短縮しておくのが主な用途。
		/// </summary>
		/// <param name="file">バックグラウンドロードするファイル</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		static public AssetFile BackGroundLoad(AssetFile file, System.Object referenceObj)
		{
			return GetInstance().BackGroundLoadSub(file as AssetFileBase, referenceObj);
		}


		/// <summary>
		/// ファイルのダウンロードだけする
		/// ロードの優先順位は一番低い
		/// バックグラウンドでファイルのダウンロードをする。
		/// デバイスストレージに保存可能ならファイルをキャッシュしておく
		/// ロードしたアセットはメモリにもキャッシュにもしておくが
		/// メモリキャッシュはメモリが枯渇すると揮発するので、その場合は再ロードに時間がかかる
		/// </summary>
		/// <param name="path">パス</param>	
		static public void Download(string path)
		{
			Download(GetFileCreateIfMissing(path));
		}

		/// <summary>
		/// ファイルのダウンロードだけする
		/// ロードの優先順位は一番低い
		/// バックグラウンドでファイルのダウンロードをする。
		/// デバイスストレージに保存可能ならファイルをキャッシュしておく
		/// ロードしたアセットはメモリにもキャッシュにもしておくが
		/// メモリキャッシュはメモリが枯渇すると揮発するので、その場合は再ロードに時間がかかる
		/// </summary>
		/// <param name="file">ダウンロードするファイル</param>
		static public void Download(AssetFile file)
		{
			GetInstance().DownloadSub(file as AssetFileBase);
		}

		//ロード終了チェック
		public static bool IsLoadEnd()
		{
			return GetInstance().IsLoadEnd(AssetFileLoadPriority.Preload);
		}
		//ダウンロード終了チェック
		public static bool IsDownloadEnd()
		{
			return GetInstance().IsLoadEnd(AssetFileLoadPriority.DownloadOnly);
		}

		//ロード待ちのファイル数
		public static int CountLoading()
		{
			return GetInstance().CountLoading(AssetFileLoadPriority.Preload);
		}

		//ロード待ちのファイル数
		public static int CountDownloading()
		{
			return GetInstance().CountLoading(AssetFileLoadPriority.DownloadOnly);
		}

		//使用していないファイルをすべてアンロード
		public static void UnloadUnusedAll()
		{
			GetInstance().UnloadUnusedAssets(int.MaxValue);
		}

		//指定の数のファイルをすべてアンロード
		public static void UnloadUnused(int count)
		{
			GetInstance().UnloadUnusedAssets(count);
		}

		//拡張子を追加
		public static void AddAssetFileTypeExtensions(AssetFileType type, string[] extensions)
		{
			GetInstance().Settings.AddExtensions(type, extensions);
		}

		//static なアセットがあるか
		public static bool ContainsStaticAsset(UnityEngine.Object asset)
		{
			return GetInstance().StaticAssetManager.Contains(asset);
		}

		//CustomLoadManagerを取得
		public static CustomLoadManager GetCustomLoadManager()
		{
			return GetInstance().CustomLoadManager;
		}

		//ロードエラーコールバックを設定
		static public void SetLoadErrorCallBack(Action<AssetFile> callbackError)
		{
			GetInstance().callbackError = callbackError;
		}

		//ファイルのリロード
		static public void ReloadFile(AssetFile file)
		{
			GetInstance().ReloadFileSub(file as AssetFileBase);
		}

		/// <summary>
		/// エディタ上のエラーチェックのために起動してるか
		/// </summary>
		public static bool IsEditorErrorCheck
		{
			get { return isEditorErrorCheck; }
			set { isEditorErrorCheck = value; }
		}
		[RuntimeInitializeStaticField] static bool isEditorErrorCheck = false;
		[StaticField] static AssetFileManager instance;

		public static AssetFileManager GetInstance()
		{
			if (instance == null)
			{
				instance = WrapperFindObject.FindObjectOfType<AssetFileManager>();
				if (instance == null)
				{
					if (!isEditorErrorCheck)
					{
						Debug.Log("Not Found AssetFileManager in current scene");
					}
				}
			}
			return instance;
		}
	}
}
