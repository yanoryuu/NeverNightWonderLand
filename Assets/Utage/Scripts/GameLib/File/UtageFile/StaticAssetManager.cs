// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// 動的にロードしないで、常に保持しつづけるアセットの管理
	/// 3Dモデルや、BGM（DLするとストリーム再生できない）など
	/// アセットバンドル化したくないオブジェクトを中心に
	/// </summary>
	[AddComponentMenu("Utage/Lib/File/StaticAssetManager")]
	public class StaticAssetManager : MonoBehaviour
	{
		[SerializeField] List<StaticAsset> assets = new List<StaticAsset>();
		
		public List<StaticAsset> Assets => assets;

		public AssetFileBase FindAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
		{
			if (Assets == null) return null;
			string assetName = FilePathUtil.GetFileNameWithoutExtension(fileInfo.FileName);
			StaticAsset asset = Find(assetName);
			if (asset == null) return null;

			return new StaticAssetFile(asset, mangager, fileInfo, settingData);
		}

		public bool Contains(Object asset)
		{
			foreach( StaticAsset item in Assets )
			{
				if( item.Asset == asset ) return true;
			}
			return false;
		}

		public bool Contains(string path)
		{
			string assetName = FilePathUtil.GetFileNameWithoutExtension(path);
			StaticAsset asset = Find(assetName);
			return (asset != null);
		}

		StaticAsset Find(string assetName)
		{
			return Assets.Find((x) =>
			{
				if (x.Asset==null)
				{
					//アセットが破壊されている
					Debug.LogWarning($"Missing Asset in {nameof(StaticAssetManager)}", this);
					return false;
				}

				return x.Asset.name == assetName;
			});
		}

	}

	//動的にロードしないアセットの情報
	[System.Serializable]
	public class StaticAsset
	{
		[SerializeField]
		Object asset=null;
		public Object Asset
		{
			get { return asset; }
			set { asset = value; }
		}
	}

	//動的にロードしないアセットをロードファイルのように扱うためのクラス
	public class StaticAssetFile : AssetFileBase
	{
		public StaticAsset Asset { get; protected set; }

		public StaticAssetFile(StaticAsset asset, AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
			: base(mangager, fileInfo, settingData)
		{
			this.Asset = asset;
			this.Text = Asset.Asset as TextAsset;
			this.Texture = Asset.Asset as Texture2D;
			this.Sound = Asset.Asset as AudioClip;
			this.UnityObject = Asset.Asset;
			this.IsLoadEnd = true;
			this.IgnoreUnload = true;

			if (Texture != null)
			{
				FileType = AssetFileType.Texture;
			}
			else if (Sound != null)
			{
				FileType = AssetFileType.Sound;
			}
			else if (UnityObject != null)
			{
				FileType = AssetFileType.UnityObject;
			}
		}

		public override bool CheckCacheOrLocal()
		{
			return true;
		}

		public override IEnumerator LoadAsync(System.Action onComplete, System.Action onFailed)
		{
			onComplete();
			yield break;
		}
		public override void Unload() { }
	}
}
