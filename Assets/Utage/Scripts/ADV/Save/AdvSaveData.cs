// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.IO;
using UtageExtensions;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace Utage
{

	/// <summary>
	/// セーブデータ
	/// </summary>
	[System.Serializable]
	public class AdvSaveData
	{
		public enum SaveDataType
		{
			Default,
			Quick,
			Auto,
		};

		/// <summary>
		/// セーブデータのファイルパス
		/// </summary>
		public string Path { get; private set; }

		public SaveDataType Type { get; private set; }

		/// <summary>
		/// セーブデータのタイトル
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// テクスチャ
		/// </summary>
		public Texture2D Texture
		{
			get { return texture; }
			set
			{
				texture = value;
				if (texture != null)
				{
					if (texture.wrapMode != TextureWrapMode.Clamp)
					{
						texture.wrapMode = TextureWrapMode.Clamp;
					}
				}
			}
		}
		Texture2D texture;

		/// <summary>
		/// 日付
		/// </summary>
		public System.DateTime Date { get; set; }

		/// <summary>
		/// セーブされているか
		/// </summary>
		public bool IsSaved { get { return !this.Buffer.IsEmpty; } }

		//ファイルバージョン
		public int FileVersion { get; private set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="path">セーブデータのファイルパス</param>
		public AdvSaveData(SaveDataType type, string path)
		{
			this.Type = type;
			this.Path = path;
			Clear();
		}

		/// <summary>
		/// クリア
		/// </summary>
		public void Clear()
		{
			this.Buffer = new BinaryBuffer();
			if (Texture != null) UnityEngine.Object.Destroy(Texture);
			Texture = null;
			FileVersion = -1;
			Title = "";
		}

		//セーブデータ内からパラメーターデータのみ読み込んで、独立したインスタンスとして返す
		public AdvParamManager ReadParam(AdvEngine engine)
		{
			AdvParamManager param = new AdvParamManager();
			param.InitDefaultAll(engine.DataManager.SettingDataManager.DefaultParam);
			Buffer.Overrirde(param.DefaultData);
			return param;
		}

		//セーブデータ内にパラメーターデータのみを上書きして書き込みする
		public void WriteParam(AdvParamManager paramManager)
		{
			Buffer.WriteBuffer(paramManager.DefaultData);
		}

		/// <summary>
		/// オートセーブデータからセーブデータを作成
		/// </summary>
		/// <param name="autoSave">オートセーブデータ</param>
		/// <param name="tex">セーブアイコン</param>
		public void SaveGameData(AdvSaveData autoSave, AdvEngine engine, Texture2D tex)
		{
			Clear();
			Buffer = autoSave.Buffer.Clone<BinaryBuffer>();
			Date = WrapperUnityVersion.DateTimeNow();
			Texture = tex;
			FileVersion = autoSave.FileVersion;
			Title = autoSave.Title;
		}

		/// <summary>
		/// ゲームのデータをセーブ
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		/// <param name="tex">セーブアイコン</param>
		public void UpdateAutoSaveData(AdvEngine engine, Texture2D tex, List<IBinaryIO> customSaveIoList, List<IBinaryIO> saveIoList)
		{
			Clear();
			//セーブ対象となる情報を設定
			List<IBinaryIO> ioList = new List<IBinaryIO>()
			{
				engine.ScenarioPlayer,
				engine.Param.DefaultData,
				engine.GraphicManager,
				engine.CameraManager,
				engine.SoundManager,
			};
			ioList.AddRange(customSaveIoList);
			ioList.AddRange(saveIoList);

			//バイナリデータを作成
			Profiler.BeginSample("MakeSaveDataBuffer");
			Buffer.MakeBuffer(ioList);
			Profiler.EndSample();

			Date = WrapperUnityVersion.DateTimeNow();
			Texture = tex;
			Title = engine.Page.SaveDataTitle;
		}

		/// <summary>
		/// ゲームのデータをロード
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		public void LoadGameData(AdvEngine engine, List<IBinaryIO> customSaveIoList, List<IBinaryIO> saveIoList)
		{
			Buffer.Overrirde(engine.Param.DefaultData);
			Buffer.Overrirde(engine.GraphicManager);
			Buffer.Overrirde(engine.CameraManager);
			Buffer.Overrirde(engine.SoundManager);

			Buffer.Overrirde(customSaveIoList);
			Buffer.Overrirde(saveIoList);
		}

		static readonly int MagicID = FileIOManager.ToMagicID('S', 'a', 'v', 'e');  //識別ID
		public const int Version = 10;   //ファイルバージョン
		public BinaryBuffer Buffer { get; protected set; } = new BinaryBuffer();

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader"></param>
		public void Read(BinaryReader reader)
		{
			Clear();
			int magicID = reader.ReadInt32();
			if (magicID != MagicID)
			{
				throw new System.Exception("Read File Id Error");
			}

			int version = reader.ReadInt32();
			if (version >= Version)
			{
				this.FileVersion = version;
				Date = new System.DateTime(reader.ReadInt64());
				int captureMemLen = reader.ReadInt32();
				if (captureMemLen > 0)
				{
					byte[] captureMem = reader.ReadBytes(captureMemLen);
					Texture2D tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
					tex.LoadImage(captureMem);
					Texture = tex;
				}
				else
				{
					Texture = null;
				}
				this.Title = reader.ReadString();

				Buffer.Read(reader);
			}
			else
			{
				Clear();
				throw new System.Exception(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public void Write(BinaryWriter writer)
		{
			Date = WrapperUnityVersion.DateTimeNow();
			writer.Write(MagicID);
			writer.Write(Version);
			writer.Write(Date.Ticks);
			if (Texture != null)
			{
				byte[] captureMem = Texture.EncodeToPNG();
				writer.WriteBuffer(captureMem);
			}
			else
			{
				writer.Write(0);
			}
			writer.Write(Title);
			Buffer.Write(writer);
		}
	}
	
}