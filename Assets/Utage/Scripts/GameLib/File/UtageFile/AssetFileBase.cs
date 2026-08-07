// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	//ファイルマネージャーで扱うファイルの基底クラス
	public abstract class AssetFileBase : AssetFile
	{
		public AssetFileBase(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
		{
			this.FileManager = mangager;
			this.FileInfo = fileInfo;
			this.FileType = fileInfo.FileType;
			this.SettingData = settingData;
			this.Priority = AssetFileLoadPriority.DownloadOnly;
		}
		protected AssetFileManager FileManager { get; private set; }
		public AssetFileInfo FileInfo { get; private set; }

		public virtual string FileName { get { return FileInfo.FileName; } }

		public IAssetFileSettingData SettingData { get; private set; }

		public virtual AssetFileType FileType { get; protected set; }

		/// <summary>関連ファイルも含めてすべてロード終了したか</summary>
		public bool IsLoadEnd { get; protected set; }

		/// <summary>ロードエラーしたか</summary>
		public bool IsLoadError { get; protected set; }

		/// <summary>ロードエラーメッセージ</summary>
		public string LoadErrorMsg { get; protected set; }

		/// <summary>ロードしたTextAsset</summary>
		public TextAsset Text{ get; protected set; }

		/// <summary>ロードしたテクスチャ</summary>
		public Texture2D Texture { get; protected set; }

		/// <summary>ロードしたサウンド</summary>
		public AudioClip Sound { get; protected set; }

		/// <summary>ロードしたUnityオブジェクト</summary>
		public UnityEngine.Object UnityObject { get; protected set; }

		//ロードの優先順
		protected internal AssetFileLoadPriority Priority { get; protected set; }

		//アンロードを無視する（ダミーファイルやStaticAsset用）
		protected internal bool IgnoreUnload { get; protected set; }

		//参照オブジェクト
		protected HashSet<System.Object> referenceSet = new HashSet<object>();

		//参照ブジェクトの数
		internal int ReferenceCount
		{
			get
			{
				if (referenceSet.Contains(null))
				{
					referenceSet.RemoveWhere(s => s == null);
					Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.FileReferecedIsNull));
				}
				return referenceSet.Count;
			}
		}


		/// <summary>
		/// ロードの準備開始
		/// </summary>
		/// <param name="loadPriority">ロードの優先順</param>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		/// <returns></returns>
		internal virtual void ReadyToLoad(AssetFileLoadPriority loadPriority, System.Object referenceObj)
		{
			//ロードプライオリティの反映
			if (loadPriority < this.Priority)
			{
				this.Priority = loadPriority;
			}
			Use(referenceObj);
		}

		/// <summary>
		/// そのオブジェクトで使用する（参照を設定する）
		/// </summary>
		/// <param name="referenceObj">ファイルを参照するオブジェクト</param>
		public virtual void Use(System.Object referenceObj)
		{
			if (null != referenceObj)
			{
				referenceSet.Add(referenceObj);
			}
		}

		/// <summary>
		/// そのオブジェクトから未使用にする（参照を解放する）
		/// </summary>
		/// <param name="referenceObj">ファイルの参照を解除するオブジェクト</param>
		public virtual void Unuse(System.Object referenceObj)
		{
			if (null != referenceObj)
			{
				referenceSet.Remove(referenceObj);
			}
		}

		/// <summary>
		/// 参照用コンポーネントの追加
		/// </summary>
		/// <param name="go">参照コンポーネントを追加するGameObject</param>
		public virtual void AddReferenceComponent(GameObject go)
		{
			AssetFileReference fileReference = go.AddComponent<AssetFileReference>();
			fileReference.Init(this);
		}

		//ダミーファイルのロード
		internal void LoadDummy(AssetFileDummyOnLoadError dummyFiles)
		{
			IgnoreUnload = true;
			IsLoadEnd = true;
			IsLoadError = false;
			switch (FileType)
			{
				case AssetFileType.Text:        //テキスト
					Text = dummyFiles.text;
					break;
				case AssetFileType.Texture:     //テクスチャ
					Texture = dummyFiles.texture;
					break;
				case AssetFileType.Sound:       //サウンド
					Sound = dummyFiles.sound;
					break;
				case AssetFileType.UnityObject:     //Unityオブジェクト（プレハブとか）
					this.UnityObject = dummyFiles.asset;
					break;
				default:
					break;
			}
		}

		//実際にロードするパスを設定
		protected virtual string ParseLoadPath()
		{
			switch (FileInfo.StrageType)
			{
				case AssetFileStrageType.Server:
				case AssetFileStrageType.StreamingAssets:
					{
						if (this.FileInfo.AssetBundleInfo == null)
						{
							Debug.LogError("Not found in assetbundle list " + FileName);
							return FilePathUtil.EncodeUrl(FileName);
						}
						string url = this.FileInfo.AssetBundleInfo.Url;
						url = FilePathUtil.ToCacheClearUrl(url);
						return FilePathUtil.EncodeUrl(url);
					}
				case AssetFileStrageType.Resources:
				default:
					return FileName;
			}
		}

		//ローカルまたはキャッシュあるか（つまりサーバーからDLする必要があるか）
		public abstract bool CheckCacheOrLocal();
		//ロード処理
		public abstract IEnumerator LoadAsync(System.Action onComplete, System.Action onFailed);
		//アンロード処理
		public abstract void Unload();
	}
}
