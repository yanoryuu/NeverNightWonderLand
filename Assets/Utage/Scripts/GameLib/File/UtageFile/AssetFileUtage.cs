// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// 宴が扱うデフォルトのファイルクラス。
	/// カスタムしない限りはこれが使われる
	/// </summary>
	internal class AssetFileUtage : AssetFileBase
	{
		//実際にロードするパス
		protected string LoadPath { get; set; }
		//アセットバンドル
		protected AssetBundle AssetBundle { get; set; }

		public AssetFileUtage(AssetFileManager assetFileManager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
			: base(assetFileManager, fileInfo, settingData)
		{
			this.LoadPath = ParseLoadPath();
		}

		public override bool CheckCacheOrLocal()
		{
			if (this.FileInfo.StrageType == AssetFileStrageType.Server)
			{
#if UTAGE_DISABLE_CACHING
				return false;
#elif UNITY_2022_1_OR_NEWER
				//Unity2022から、WebGLでUnityEngine.Cachingが使用不可能に
				return false;
#else
				if (LoadPath == null || FileInfo == null || FileInfo.AssetBundleInfo == null)
				{
					return false;
				}
				else
				{
					return Caching.IsVersionCached(LoadPath, FileInfo.AssetBundleInfo.Hash);
				}
#endif
			}
			else
			{
				return true;
			}
		}

		//ロード
		public override IEnumerator LoadAsync(Action onComplete, Action onFailed)
		{
			this.IsLoadEnd = false;
			this.IsLoadError = false;
			//通常のロード
			yield return LoadAsyncSub(LoadPath,
				//成功
				() =>
				{
					//DLのみの場合は、ロードが終わったわけではない
					if (Priority != AssetFileLoadPriority.DownloadOnly)
					{
						this.IsLoadEnd = true;
					}
					onComplete();
				},
				//失敗
				() =>
				{
					this.IsLoadError = true;
					onFailed();
				}
				);
		}


		//ロード
		IEnumerator LoadAsyncSub(string path, Action onComplete, Action onFailed)
		{
			switch (FileInfo.StrageType)
			{
				case AssetFileStrageType.Resources:
					//Resoucesからのロード
					if (FileManager.EnableResourcesLoadAsync)
					{
						//非同期
						yield return LoadResourceAsync(path, onComplete, onFailed);
					}
					else
					{
						//同期
						LoadResource(path, onComplete, onFailed);
					}
					break;
				default:
					{
						yield return LoadAssetBundleAsync(path,onComplete, onFailed);
					}
					break;
			}
		}


		Type GetResourceType()
		{
			switch (FileType)
			{
				case AssetFileType.Text:        //テキスト
					return typeof(TextAsset);
				case AssetFileType.Texture:     //テクスチャ
					return typeof(Texture2D);
				case AssetFileType.Sound:       //サウンド
					return typeof(AudioClip);
				case AssetFileType.UnityObject:     //Unityオブジェクト（プレハブとか）
				default:
					return typeof(UnityEngine.Object);
			}
		}
		//ロードエラーを設定
		void SetLoadError(string errorMsg)
		{
			this.LoadErrorMsg = errorMsg + " : load from " + LoadPath;
			IsLoadError = true;
		}


		//同期ロード処理（Resourcesから）
		void LoadResource(string loadPath, Action onComplete, Action onFailed)
		{
			loadPath = FilePathUtil.GetPathWithoutExtension(loadPath);

			UnityEngine.Object asset = Resources.Load(loadPath, GetResourceType());
			LoadAsset(asset, onComplete, onFailed);
		}

		//非同期ロード（Resourcesから）
		IEnumerator LoadResourceAsync(string loadPath, Action onComplete, Action onFailed)
		{
			loadPath = FilePathUtil.GetPathWithoutExtension(loadPath);
			ResourceRequest request = Resources.LoadAsync(loadPath, GetResourceType());
			while (!request.isDone)
			{
				yield return null;
			}
			LoadAsset(request.asset, onComplete, onFailed);
		}
		//ロードアセットバンドル
		IEnumerator LoadAssetBundleAsync(string path, Action onComplete, Action onFailed)
		{
			WWWEx wwwEx = MakeWWWEx(path);
			wwwEx.RetryCount = FileManager.AutoRetryCountOnDonwloadError;
			wwwEx.TimeOut = FileManager.TimeOutDownload;

			this.AssetBundle = null;
			if (Priority == AssetFileLoadPriority.DownloadOnly)
			{
				yield return wwwEx.DownLoadAssetBundleAsync(onComplete, onFailed);
			}
			else
			{
				AssetBundle assetBundle = null;
				yield return wwwEx.LoadFromCacheOrDownloadAssetBundleAsync(
					(x)=>
					{
						assetBundle = x;
					}, 
					onFailed);
				if (assetBundle != null)
				{
					yield return LoadAssetBundleAsync(assetBundle, onComplete, onFailed);
				}
			}
		}

		WWWEx MakeWWWEx(string path)
		{
			if (this.FileInfo.AssetBundleInfo == null )
			{
				return new WWWEx(path);
			}
			else if (!this.FileInfo.AssetBundleInfo.CacheLoad)
			{
				return new WWWEx(path);
			}
			else if (this.FileInfo.AssetBundleInfo.Hash.isValid)
			{
				return new WWWEx(path, this.FileInfo.AssetBundleInfo.Hash);
			}
			else
			{
				return new WWWEx(path, this.FileInfo.AssetBundleInfo.Version);
			}
		}

		//アセットバンドルのロード
		//実際のアセットをロードする
		//宴の場合は1アセットバンドル＝1アセットなのでそれに合わせたロードを
		IEnumerator LoadAssetBundleAsync( AssetBundle assetBundle, Action onComplete, Action onFailed)
		{
			AssetBundleRequest request = assetBundle.LoadAllAssetsAsync(GetResourceType());
			while (!request.isDone)
			{
				yield return null;
			}
			UnityEngine.Object[] assets = request.allAssets;
			if (assets == null || assets.Length <= 0)
			{
				SetLoadError("AssetBundleType Error");
				assetBundle.Unload(true);
				onFailed();
			}
			else
			{
				LoadAsset(assets[0], onComplete, onFailed);
				assets = null;
				request = null;
				//アセットバンドルを保持して、assetBundle.Unload(true)を呼ぶ
				if (FileType == AssetFileType.UnityObject && FileManager.UnloadUnusedType == AssetFileManager.UnloadType.NoneAndUnloadAssetBundleTrue)
				{
					this.AssetBundle = assetBundle;
				}
				else
				{
					assetBundle.Unload(false);
				}
			}
		}

		void LoadAsset(UnityEngine.Object asset, Action onComplete, Action onFailed)
		{
			if (asset == null)
			{
				SetLoadError("LoadResource Error");
				onFailed();
				return;
			}

			switch (FileType)
			{
				case AssetFileType.Text:      //テキスト
					Text = asset as TextAsset;
					if (null == Text)
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.Texture:     //テクスチャ
					Texture = asset as Texture2D;
					if (null == Texture)
					{
						SetLoadError("LoadResource Error");
					}
					break;
				case AssetFileType.Sound:       //サウンド
					Sound = asset as AudioClip;
					if (null == Sound)
					{
						SetLoadError("LoadResource Error");
					}
					else
					{
						//アセットバンドルの場合、
						//preloadAudioDataがfalseならLoadAudioDataが必要
						if (!Sound.preloadAudioData && FileInfo.StrageType != AssetFileStrageType.Resources)
						{
							Sound.LoadAudioData();
						}
					}					break;
				default:
				case AssetFileType.UnityObject:     //Unityオブジェクト（プレハブとか）
					UnityObject = asset;
					if (null == UnityObject)
					{
						SetLoadError("LoadResource Error");
					}
					break;
			}
			if (IsLoadError)
			{
				onFailed();
			}
			else
			{
				onComplete();
			}
		}

		/// <summary>
		/// リソースをアンロードして、メモリを解放
		/// </summary>
		public override void Unload()
		{
			switch (FileType)
			{
				case AssetFileType.Text:        //テキスト
					Resources.UnloadAsset(Text);
					break;
				case AssetFileType.Texture:     //テクスチャ
					Resources.UnloadAsset(Texture);
					break;
				case AssetFileType.Sound:       //サウンド
					Resources.UnloadAsset(Sound);
					break;
				case AssetFileType.UnityObject:     //Unityオブジェクト
					break;
				default:
					break;
			}
			Text = null;
			Texture = null;
			Sound = null;
			UnityObject = null;
			if(AssetBundle!=null)
			{
				AssetBundle.Unload(true);
				AssetBundle = null;
			}


			IsLoadEnd = false;
			Priority = AssetFileLoadPriority.DownloadOnly;
		}
	}
}
