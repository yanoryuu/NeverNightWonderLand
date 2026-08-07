// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if false
namespace Utage
{
	//コンバートしたファイル（アセットバンドルや独自符号ファイル）の情報
	public class ConvertFileInfo
	{
		//アセットバンドル名
		public string Name { get; protected set; }
		//バージョン
		public int Version { get; protected set; }
		//ハッシュ値
		public Hash128 Hash { get; protected set; }
		//アセットバンドルのハッシュ値
		public string[] AllDependencies { get; protected set; }
		public string[] DirectDependencies { get; protected set; }

		//ロードする際のパス
		public string RuntimeLoadPath { get { return FilePathUtil.Combine(List.DirectoryPath,Name); } }
		ConvertFileInfoDictionary List { get; set; }

		//アセットバンドルマニフェストから作成
		public ConvertFileInfo(string name, AssetBundleManifest manifest, ConvertFileInfoDictionary list)
		{
			this.Name = name;
			this.Version = 0;
			this.Hash = manifest.GetAssetBundleHash(name);
			this.AllDependencies = manifest.GetAllDependencies(name);
			this.DirectDependencies = manifest.GetDirectDependencies(name);
			this.List = list;
		}

		//独自定義のファイルの情報作成
		public ConvertFileInfo(string name, ConvertFileInfoDictionary list)
		{
			this.Name = name;
			this.Version = 0;
			this.AllDependencies = new string[0];
			this.DirectDependencies = new string[0];
			this.List = list;
		}

		public ConvertFileInfo(BinaryReader reader, ConvertFileInfoDictionary list)
		{
			this.List = list;
			Read(reader);
		}

		public bool VersionUp(AssetBundleManifest manifest)
		{
			Hash128 oladHash = Hash;
			Hash128 newHash = manifest.GetAssetBundleHash(Name);
			if(oladHash.Equals(newHash) )
			{
				return false;
			}
			else
			{
				this.Version++;
				this.Hash = newHash;
				this.AllDependencies = manifest.GetAllDependencies(Name);
				this.DirectDependencies = manifest.GetDirectDependencies(Name);
				Debug.Log("AssetBundle" + Name + " is uped to Version " + Version );
				return true;
			}
		}

		public void VersionUp()
		{
			this.Version++;
		}


		const int BinaryVersion = 0;
		public void Write(BinaryWriter writer)
		{
			writer.Write(BinaryVersion);
			writer.Write(Name);
			writer.Write(Version);
			writer.Write(Hash.ToString());

			writer.Write(AllDependencies.Length);
			foreach( var item in AllDependencies )
			{
				writer.Write(item);
			}

			writer.Write(DirectDependencies.Length);
			foreach (var item in DirectDependencies)
			{
				writer.Write(item);
			}
		}

		public void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version == BinaryVersion)
			{
				Name = reader.ReadString();
				Version = reader.ReadInt32();
				Hash = Hash128.Parse(reader.ReadString());

				AllDependencies = new string[reader.ReadInt32()];
				for (int i = 0; i < AllDependencies.Length; ++i)
				{
					AllDependencies[i] = reader.ReadString();
				}

				DirectDependencies = new string[reader.ReadInt32()];
				for (int i = 0; i < DirectDependencies.Length; ++i)
				{
					DirectDependencies[i] = reader.ReadString();
				}
			}
			else
			{
				throw new System.Exception(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		//ログデータを書き込み
		public void AppendLogString(StringBuilder builder)
		{
			builder.Append("" + Name + "\t");
			builder.Append("Version=" + Version);
			builder.Append("\n");
		}

		//アセットバンドルのログデータを書き込み
		public void AppendAssetBundleLogString(StringBuilder builder)
		{
			builder.Append(""+Name+ "\t");
			builder.Append("Version=" + Version + "\t");
			builder.Append("Hash=" + Hash.ToString() + "\t");
			builder.Append("AllDependencies=");
			foreach (var item in AllDependencies)
			{
				builder.Append(item+",");
			}
			builder.Append("\t");

			builder.Append("DirectDependencies=");
			foreach (var item in DirectDependencies)
			{
				builder.Append(item + ",");
			}
			builder.Append("\n");
		}
	};
	public class ConvertFileInfoDictionary : Dictionary<string, ConvertFileInfo>
	{
		public ConvertFileList FileList { get; protected set; }
		public string Key { get; protected set; }
		public string DirectoryPath { get { return FileList.DirectoryPath; } }

		public ConvertFileInfoDictionary( ConvertFileList fileList, string key )
		{
			FileList = fileList;
			Key = key;
		}

		const int Version = 0;
		//データをバイナリに書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(Count);
			foreach (ConvertFileInfo info in Values)
			{
				info.Write(writer);
			}
		}

		public void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version == Version)
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; ++i)
				{
					ConvertFileInfo info = new ConvertFileInfo(reader, this);
					this.Add(info.Name, info);
				}
			}
			else
			{
				throw new System.Exception(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		//ログデータを書き込み
		public void AppendLogString(StringBuilder builder)
		{
			foreach (ConvertFileInfo info in Values)
			{
				info.AppendLogString(builder);
			}
		}

		//アセットバンドルのログデータを書き込み
		public void AppendAssetBundleLogString(StringBuilder builder)
		{
			foreach (ConvertFileInfo info in Values)
			{
				info.AppendAssetBundleLogString(builder);
			}
		}
	}

	//コンバートしたファイル（アセットバンドルや独自符号ファイル）のリスト
	public class ConvertFileList
	{
		//プラットフォームごとのアセットバンドルリスト
		public Dictionary<string, ConvertFileInfoDictionary> FileLists { get; protected set; }

		public string FilePath { get; protected set; }
		public string DirectoryPath { get; protected set; }

		public ConvertFileList(string filePath)
		{
			FilePath = filePath;
			DirectoryPath = FilePathUtil.GetDirectoryPath(filePath);
			FileLists = new Dictionary<string, ConvertFileInfoDictionary>();
		}

		//ファイルのパスから、ファイル情報を取得
		public bool TryGetValue(string filePath, AssetFileEncodeType encodeType, out ConvertFileInfo info)
		{
			info = null;
			switch (encodeType)
			{
				case AssetFileEncodeType.AlreadyEncoded:
					if (!FilePathUtil.IsUnderDirectory(filePath,DirectoryPath) ) return false;
					string fileKey = FilePathUtil.RemoveDirectory(filePath, DirectoryPath);
					foreach( var files in FileLists.Values )
					{
						if (files.TryGetValue(fileKey, out info))
						{
							return true;
						}
					}
					return false;
				case AssetFileEncodeType.AssetBundle:
					string assetName = FilePathUtil.GetFileNameWithoutExtension(filePath).ToLower();
					string keyPlatform = AssetBundleHelper.RuntimeAssetBundleTarget().ToString();
					ConvertFileInfoDictionary infoList;
					if (FileLists.TryGetValue(keyPlatform, out infoList))
					{ 
						if (infoList.TryGetValue(assetName, out info))
						{
							return true;
						}
					}
					return false;
				default:
					return false;
			}
		}

#if UNITY_EDITOR

		//データをバージョンアップする（Eidtor上のみ使用可能）
		public int EditorVersionUpAssetBundle(AssetBundleManifest manifest, UnityEditor.BuildTarget buildTarget)
		{
			int count = 0;
			string buildTargetKey = AssetBundleHelper.BuildTargetToBuildTargetFlag(buildTarget).ToString();
			ConvertFileInfoDictionary oldInfoList;
			FileLists.TryGetValue(buildTargetKey, out oldInfoList);

			ConvertFileInfoDictionary newInfoList = new ConvertFileInfoDictionary(this,buildTargetKey);
			foreach(string assetBundleName in manifest.GetAllAssetBundles() )
			{
				ConvertFileInfo info;
				if (oldInfoList != null && oldInfoList.TryGetValue(assetBundleName, out info))
				{
					if (info.VersionUp(manifest)) ++count;
				}
				else
				{
					info = new ConvertFileInfo(assetBundleName, manifest, newInfoList);
					++count;
				}
				newInfoList.Add(info.Name,info);
			}
			FileLists.Remove(newInfoList.Key);
			FileLists.Add(newInfoList.Key, newInfoList);
			return count;
		}

		//独自定義ファイルのバージョンアップ用情報
		public class CusomFileVersionUpInfo
		{
			public string Name { get; protected set; }
			public bool IsVersionUp { get; protected set; }

			public CusomFileVersionUpInfo(string name, bool isVersionUp)
			{
				this.Name = name;
				this.IsVersionUp = isVersionUp;
			}
		}

		//独自定義ファイルをバージョンアップする。バージョンアップチェックはしない（Eidtor上のみ使用可能）
		public void EditorVersionUp(string key, List<CusomFileVersionUpInfo> cusomFileVersionUpInfoList)
		{
			ConvertFileInfoDictionary oldInfoList;
			FileLists.TryGetValue(key, out oldInfoList);
			ConvertFileInfoDictionary newInfoList = new ConvertFileInfoDictionary(this, key);
			foreach (var versionUpInfo in cusomFileVersionUpInfoList)
			{
				ConvertFileInfo info;
				if (oldInfoList !=null && oldInfoList.TryGetValue(versionUpInfo.Name, out info))
				{
					if (versionUpInfo.IsVersionUp) info.VersionUp();
				}
				else
				{
					info = new ConvertFileInfo(versionUpInfo.Name, newInfoList);
				}
				if (newInfoList.ContainsKey(info.Name))
				{
					Debug.LogError(info.Name + " is already contains ");
					continue;
				}
				newInfoList.Add(info.Name, info);
			}
			FileLists.Remove(key);
			FileLists.Add(key, newInfoList);
		}
#endif

		static readonly int MagicID = FileIOManager.ToMagicID('c', 'n', 'f', 'l');	//識別ID

		const int Version = 0;
		//データをバイナリに書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(MagicID);
			writer.Write(Version);
			writer.Write(FileLists.Count);
			foreach (string key in FileLists.Keys)
			{
				writer.Write(key);
				FileLists[key].Write(writer);
			}
		}

		//バイナリからデータを上書き・追加
		public void Read(BinaryReader reader)
		{
			int magicID = reader.ReadInt32();
			if (magicID != MagicID)
			{
				throw new System.Exception("Read File Error " + magicID );
			}

			FileLists.Clear();
			int version = reader.ReadInt32();
			if (version == Version)
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; ++i )
				{
					string key = reader.ReadString();
					ConvertFileInfoDictionary assetBundleDictionary = new ConvertFileInfoDictionary(this, key);
					assetBundleDictionary.Read(reader);
					FileLists.Add(assetBundleDictionary.Key, assetBundleDictionary);
				}
			}
			else
			{
				throw new System.Exception(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		//ログデータの文字列を作成
		public string ToLogString(bool isAssetBundle)
		{
			StringBuilder builder = new StringBuilder();
			foreach (ConvertFileInfoDictionary item in FileLists.Values)
			{
				builder.AppendLine(item.Key);
				if (isAssetBundle)
				{
					item.AppendAssetBundleLogString(builder);
				}
				else
				{
					item.AppendLogString(builder);
				}
			}
			return builder.ToString();
		}
	};
}
#endif
