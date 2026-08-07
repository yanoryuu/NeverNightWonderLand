// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Networking;
#else
#define LEGACY_WWW
#endif

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//WWWの拡張クラス
	public class WWWEx
	{
		public enum Type
		{
			Default,
			Cache,
		};


		//ロードするURL
		public string Url { get; private set; }

		//アセットバンドルのハッシュ値
		public Hash128 AssetBundleHash { get; private set; }

		//アセットバンドルのバージョン値
		public int AssetBundleVersion { get; private set; }

		//ロードタイプ
		public Type LoadType { get; private set; }

		//ロード失敗したときのリトライ回数
		public int RetryCount { get; set; }
		//ロードの進捗がなかったときのタイムアウト時間
		public float TimeOut { get; set; }

		//ロードの進捗
		public float Progress { get; private set; }

		//ロード中の進捗を取得するために
		public Action<WWWEx> OnUpdate { get; set; }

		//デバッグログを無視するか
		public bool IgnoreDebugLog { get; set; }

#if false
		//Byteを記録するか
		public bool StoreBytes { get; set; }

		//記録されたBytes
		public byte[] Bytes { get; set; }

#else
		//ローカルにDLしたものを書き込む（アセットバンドルの手動キャッシュなどのために）
		public bool WriteLocal { get; set; }
		public string WritePath { get; set; }
		public FileIOManager IoManager { get; set; }
#endif

		//通常のWWWロード
		public WWWEx(string url)
		{
			this.LoadType = Type.Default;
			InitSub(url);
		}

		//キャッシュからのロード
		public WWWEx(string url, Hash128 assetBundleHash)
		{
			this.AssetBundleHash = assetBundleHash;
			this.LoadType = Type.Cache;
			InitSub(url);
		}

		//キャッシュからのロード
		public WWWEx(string url, int assetBundleVersion)
		{
			this.AssetBundleVersion = assetBundleVersion;
			this.LoadType = Type.Cache;
			InitSub(url);
		}

		void InitSub(string url)
		{
			this.Url = url;
			this.RetryCount = 5;
			this.TimeOut = 5;
			this.Progress = 0;
		}

#if LEGACY_WWW
		///WWWを使ったアセットバンドルのダウンロードのみの処理（キャッシュに入れるだけ）
		public IEnumerator DownLoadAssetBundleAsync(Action onComplete, Action onFailed)
		{
			yield return LoadAsync(
				//OnComplete
				(www) =>
				{
					www.assetBundle.Unload(true);
					onComplete();
				},
				//OnFailed
				(www) =>
				{
					//失敗
					onFailed();
				}
				);
		}

		///WWWを使ったアセットバンドルのロード処理（キャッシュからロードまたはダウンロード）
		public IEnumerator LoadFromCacheOrDownloadAssetBundleAsync(Action<AssetBundle> onComplete, Action onFailed)
		{
			yield return LoadAsync(
				//OnComplete
				(www) =>
				{
					if (www.assetBundle == null)
					{
						if (!IgnoreDebugLog) Debug.LogError(www.url + " is not assetBundle");
						onFailed();
					}
					else
					{
						onComplete(www.assetBundle);
					}
				},
				//OnFailed
				(www) =>
				{
					//失敗
					onFailed();
				}
				);
		}

		///WWWを作成
		WWW CreateWWW()
		{
			switch (LoadType)
			{
				case Type.Cache:
					if (AssetBundleHash.isValid)
					{
						return WWW.LoadFromCacheOrDownload(Url, AssetBundleHash);
					}
					else
					{
						return WWW.LoadFromCacheOrDownload(Url, AssetBundleVersion);
					}
				default:
					return new WWW(Url);
			}
		}


		///WWWを使ったロード処理
		IEnumerator LoadAsync(Action<WWW> onComplete, Action<WWW> onFailed = null)
		{
			return LoadAsync(
				//OnComplete
				(www) =>
				{
					onComplete(www);
				},
				//OnFailed
				(www) =>
				{
					if(!IgnoreDebugLog) Debug.LogError("WWW load error " + www.url + "\n" + www.error);
					if (onFailed != null) onFailed(www);
				},
				//OnTimeOut);
				(www) =>
				{
					if (!IgnoreDebugLog) Debug.LogError("WWW timeout " + www.url);
					if (onFailed != null) onFailed(www);
				}
				);
		}


		///WWWを使ったロード処理
		IEnumerator LoadAsync(Action<WWW> onComplete, Action<WWW> onFailed, Action<WWW> onTimeOut)
		{
			return LoadAsyncSub(onComplete, onFailed, onTimeOut, RetryCount);
		}

		///WWWを使ったロード処理
		IEnumerator LoadAsyncSub(Action<WWW> onComplete, Action<WWW> onFailed, Action<WWW> onTimeOut, int retryCount)
		{
#if !UTAGE_DISABLE_CACHING
			if (LoadType == Type.Cache)
			{
				while (!Caching.ready) yield return null;
			}
#endif
			bool retry = false;
			//WWWでダウンロード
			using (WWW www = CreateWWW())
			{
				float time = 0;
				bool isTimeOut = false;
				this.Progress = 0;
				//ロード待ち
				while (!www.isDone && !isTimeOut)
				{
					//タイムアウトチェック
					if (0 < TimeOut)
					{
						if (Progress == www.progress)
						{
							time += Time.unscaledDeltaTime;
							if (time >= TimeOut)
							{
								isTimeOut = true;
							}
						}
						else
						{
							time = 0;
						}
					}
					Progress = www.progress;
					if (OnUpdate != null) OnUpdate(this);
					yield return null;
				}
				if (isTimeOut)
				{
					//タイムアウト
					if (retryCount <= 0)
					{
						if (onTimeOut != null) onTimeOut(www);
					}
					else
					{
						retry = true;
					}
				}
				else if (!string.IsNullOrEmpty(www.error))
				{
					//ロードエラー
					if (retryCount <= 0)
					{
						if (onFailed != null) onFailed(www);
					}
					else
					{
						retry = true;
					}
				}
				else
				{
					Progress = www.progress;
					if (WriteLocal)
					{
						IoManager.CreateDirectory(FilePathUtil.GetDirectoryPath(WritePath) + "/");
						IoManager.Write(WritePath, www.bytes);
					}
					else
					if (OnUpdate != null) OnUpdate(this);
					//ロード終了
					if (onComplete != null) onComplete(www);
				}
			}

			//リトライするなら再帰で呼び出す
			if (retry)
			{
				yield return LoadAsyncSub(onComplete, onFailed, onTimeOut, retryCount - 1);
			}
		}


		///アセットバンドルをロード
		IEnumerator LoadAssetBundleAsync(Action<WWW, AssetBundle> onComplete, Action<WWW> onFailed)
		{
			return LoadAsync(
				//OnComplete
				(www) =>
				{
					AssetBundle assetBundle = www.assetBundle;
					if (assetBundle != null)
					{
						//成功！
						if (onComplete != null) onComplete(www, assetBundle);
					}
					else
					{
						//失敗
						if (!IgnoreDebugLog) Debug.LogError(www.url + " is not assetBundle");
						if (onFailed != null) onFailed(www);
					}
				},
				//OnFailed
				(www) =>
				{
					//失敗
					if (onFailed != null) onFailed(www);
				}
				);
		}

		///アセットバンドルのメインアセットをロード
		IEnumerator LoadAssetBundleMainAssetAsync<T>(bool unloadAllLoadedObjects, Action<WWW, T> onComplete, Action<WWW> onFailed) where T : UnityEngine.Object
		{
			return LoadAssetBundleAsync(
				//OnComplete
				(www, assetBundle) =>
				{
					T mainAsset = assetBundle.mainAsset as T;
					if (mainAsset != null)
					{
						//成功！
						if (onComplete != null) onComplete(www, mainAsset);
					}
					else
					{
						//失敗
						if (!IgnoreDebugLog) Debug.LogError(www.url + " is not AssetBundle of " + typeof(T).Name);
						if (onFailed != null) onFailed(www);
					}
					mainAsset = null;
					assetBundle.Unload(unloadAllLoadedObjects);
				},
				//OnFailed
				(www) =>
				{
					//失敗
					if (onFailed != null) onFailed(www);
				}
				);
		}
#else

		///WWWを使ったアセットバンドルのダウンロードのみの処理（キャッシュに入れるだけ）
		public IEnumerator DownLoadAssetBundleAsync(Action onComplete, Action onFailed)
		{
			yield return LoadAsync(
				//OnComplete
				(www) =>
				{
//					www.assetBundle.Unload(true);
					onComplete();
				},
				//OnFailed
				(www) =>
				{
					//失敗
					onFailed();
				}
				);
		}

		///WWWを使ったアセットバンドルのロード処理（キャッシュからロードまたはダウンロード）
		public IEnumerator LoadFromCacheOrDownloadAssetBundleAsync(Action<AssetBundle> onComplete, Action onFailed)
		{
			yield return LoadAssetBundleAsync(
				(www, assetBundle) =>
				{
					onComplete(assetBundle);
				},
				(www)=>
				{
					onFailed();
				}
				);
		}

		///WWWを使ったロード処理
		IEnumerator LoadAsync(Action<UnityWebRequest> onComplete, Action<UnityWebRequest> onFailed = null)
		{
			return LoadAsync(
				//OnComplete
				(www) =>
				{
					onComplete(www);
				},
				//OnFailed
				(www) =>
				{
					if (!IgnoreDebugLog) Debug.LogError("WWW load error " + www.url + "\n" + www.error);
					if (onFailed != null) onFailed(www);
				},
				//OnTimeOut);
				(www) =>
				{
					if (!IgnoreDebugLog) Debug.LogError("WWW timeout " + www.url);
					if (onFailed != null) onFailed(www);
				}
				);
		}

		///WWWを使ったロード処理
		IEnumerator LoadAsync(Action<UnityWebRequest> onComplete, Action<UnityWebRequest> onFailed, Action<UnityWebRequest> onTimeOut)
		{
			return LoadAsyncSub(onComplete, onFailed, onTimeOut, RetryCount);
		}

		///WWWを使ったロード処理
		IEnumerator LoadAsyncSub(Action<UnityWebRequest> onComplete, Action<UnityWebRequest> onFailed, Action<UnityWebRequest> onTimeOut, int retryCount)
		{
#if !UTAGE_DISABLE_CACHING && !UNITY_2022_1_OR_NEWER
			if (LoadType == Type.Cache)
			{
				while (!Caching.ready) yield return null;
			}
#endif
			bool retry = false;

			//UnityWebRequestでダウンロード
			using (UnityWebRequest uwr = CreateWebRequest())
			{
				//タイムアウトチェック
				if (0 < TimeOut)
				{
					uwr.timeout = (int)TimeOut;
				}

				UnityWebRequestAsyncOperation request = uwr.SendWebRequest();
				float time = 0;
				bool isTimeOut = false;
				this.Progress = 0;
				//ロード待ち
				while (!request.isDone && !isTimeOut)
				{
					//タイムアウトチェック
					if (0 < TimeOut)
					{
						if (Progress == request.progress)
						{
							time += Time.unscaledDeltaTime;
							if (time >= TimeOut)
							{
								isTimeOut = true;
							}
						}
						else
						{
							time = 0;
						}
					}
					Progress = request.progress;
					if (OnUpdate != null) OnUpdate(this);
					yield return null;
				}

				if (isTimeOut || uwr.error == "Request timeout")
				{
					//タイムアウト
					if (retryCount <= 0)
					{
						if (onTimeOut != null) onTimeOut(uwr);
					}
					else
					{
						retry = true;
					}
				}
				else if ( WrapperUnityVersion.WebRequestResultError(uwr))
				{
					//ロードエラー
					if (retryCount <= 0)
					{
						if (onFailed != null) onFailed(uwr);
					}
					else
					{
						retry = true;
					}
				}
				else
				{
					Progress = request.progress;
					if (OnUpdate != null) OnUpdate(this);
					if (onComplete != null) onComplete(uwr);
				}
			}

			//リトライするなら再帰で呼び出す
			if (retry)
			{
				yield return LoadAsyncSub(onComplete, onFailed, onTimeOut, retryCount - 1);
			}
		}

		///WWWを作成
		UnityWebRequest CreateWebRequest()
		{
			switch (LoadType)
			{
				case Type.Cache:
					if (AssetBundleHash.isValid)
					{
						return UnityWebRequestAssetBundle.GetAssetBundle(Url, AssetBundleHash);
					}
					else
					{
						return UnityWebRequestAssetBundle.GetAssetBundle(Url, (uint)AssetBundleVersion);
					}
				default:
					if (WriteLocal)
					{
						return UnityWebRequest.Get(Url);
					}
					else
					{
						return UnityWebRequestAssetBundle.GetAssetBundle(Url);
					}
			}
		}



		///アセットバンドルをロード
		IEnumerator LoadAssetBundleAsync(Action<UnityWebRequest, AssetBundle> onComplete, Action<UnityWebRequest> onFailed)
		{
			return LoadAsync(
				//OnComplete
				(www) =>
				{
					AssetBundle assetBundle = null;
					if (WriteLocal)
					{
						IoManager.CreateDirectory(FilePathUtil.GetDirectoryPath(WritePath) + "/");
						IoManager.Write(WritePath, www.downloadHandler.data);
						assetBundle = AssetBundle.LoadFromFile(WritePath);
					}
					else
					{
						assetBundle = DownloadHandlerAssetBundle.GetContent(www);
					}
					if (assetBundle != null)
					{
						//成功！
						if (onComplete != null) onComplete(www, assetBundle);
					}
					else
					{
						//失敗
						if (!IgnoreDebugLog) Debug.LogError(www.url + " is not assetBundle");
						if (onFailed != null) onFailed(www);
					}
				},
				//OnFailed
				(www) =>
				{
					//失敗
					if (onFailed != null) onFailed(www);
				}
				);
		}

#endif
		///アセットバンドルのメインアセットをロード
		public IEnumerator LoadAssetBundleByNameAsync<T>(string assetName, bool unloadAllLoadedObjects, Action<T> onComplete, Action onFailed) where T : UnityEngine.Object
		{
			AssetBundle assetBundle = null;
			yield return LoadAssetBundleAsync(
				//OnComplete
				(_www, _assetBundle) =>
				{
					assetBundle = _assetBundle;
				},
				//OnFailed
				(_www) =>
				{
					//失敗
					if (onFailed != null) onFailed();
				}
				);

			if (assetBundle == null) yield break;

			AssetBundleRequest request = assetBundle.LoadAssetAsync<T>(assetName);
			while (!request.isDone)
			{
				yield return null;
			}
			T asset = request.asset as T;
			if (asset == null)
			{
				//失敗
				if (!IgnoreDebugLog) Debug.LogError(Url + "  " + assetName + " is not AssetBundle of " + typeof(T).Name);
				if (onFailed != null) onFailed();
			}
			else
			{
				//成功！
				if (onComplete != null) onComplete(asset);
			}
			asset = null;
			request = null;
			assetBundle.Unload(unloadAllLoadedObjects);
		}


		///アセットバンドルのメインアセットをロード
		public IEnumerator LoadAssetBundleAllAsync<T>(bool unloadAllLoadedObjects, Action<T[]> onComplete, Action onFailed) where T : UnityEngine.Object
		{
			AssetBundle assetBundle = null;
			yield return LoadAssetBundleAsync(
				//OnComplete
				(_www, _assetBundle) =>
				{
					assetBundle = _assetBundle;
				},
				//OnFailed
				(_www) =>
				{
					//失敗
					if (onFailed != null) onFailed();
				}
				);

			if (assetBundle == null) yield break;

			AssetBundleRequest request = assetBundle.LoadAllAssetsAsync<T>();
			while (!request.isDone)
			{
				yield return null;
			}
			T[] assets = request.allAssets as T[];
			if (assets == null || assets.Length <= 0)
			{
				//失敗
				if (!IgnoreDebugLog) Debug.LogError(Url + "  " + " is not AssetBundle of " + typeof(T).Name);
				if (onFailed != null) onFailed();
			}
			else
			{
				//成功！
				if (onComplete != null) onComplete(assets);
			}
			assets = null;
			request = null;
			assetBundle.Unload(unloadAllLoadedObjects);
		}


	}
}
