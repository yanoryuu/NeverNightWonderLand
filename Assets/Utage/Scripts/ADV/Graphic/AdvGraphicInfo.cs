// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

namespace Utage
{

	/// <summary>
	/// グラフィック情報クラス
	/// </summary>
	public class AdvGraphicInfo : IAssetFileSettingData
	{
		public const string TypeCharacter = "Character";
		public const string TypeTexture = "Texture";
		public const string TypeParticle = "Particle";
		public const string TypeCapture = "Capture";
		public const string TypeVideo = "Video";


		//独自オブジェクトを作成するためのコールバック
		//独自にカスタムしたい、ファイルタイプのオブジェクトの型だけ作成すればいい
		public delegate void CreateCustom(string fileType, ref Type type);

		[field: RuntimeInitializeStaticField] public static CreateCustom CallbackCreateCustom;

		/// <summary>
		/// 文字列の条件式を計算するコールバック
		/// </summary> 
		[field: RuntimeInitializeStaticField] public static Func<string, bool> CallbackExpression;

		public const string FileType2D = "2D";
		public const string FileTypeAvatar = "Avatar";
		public const string FileTypeDicing = "Dicing";
		public const string FileTypeVideo = "Video";
		public const string FileType2DPrefab = "2DPrefab";
		public const string FileTypeParticle = "Particle";
		public const string FileType3D = "3D";
		public const string FileType3DPrefab = "3DPrefab";
		public const string FileTypeCustom = "Custom";
		public const string FileTypeCustom2D = "Custom2D";
		public const string FileTypeOverridePrefab = "OverridePrefab";

		public string DataType { get; protected set; }
		int Index { get; set; }
		public string Key { get; protected set; }
		public string FileType { get; protected set; }

		public StringGridRow RowData { get; protected set; }
		public IAdvSettingData SettingData { get; protected set; }

		public string FileName { get; protected set; }

		AssetFile file;
		public AssetFile File
		{
			get { return file; }
			set { file = value; }
		}

		public Vector2 Pivot { get; private set; }
		public bool EnablePivot0 { get; private set; }
		public Vector2 Pivot0 { get; private set; }


		public Vector3 Scale { get; private set; }
		public Vector3 Position { get; private set; }

		//サブファイル名
		public string SubFileName { get; private set; }

		//アニメーションステート
		public string AnimationState { get; private set; }
		//アニメーションデータ
		public AdvAnimationData AnimationData { get; private set; }

		//目パチデータのラベル
		public AdvEyeBlinkData EyeBlinkData { get; set; }

		//口パクデータ
		public AdvLipSynchData LipSynchData { get; private set; }

		//ループするか
		public bool Loop { get; private set; }

		//条件式の判定
		public bool CheckConditionalExpression
		{
			get
			{
				if (null == CallbackExpression)
				{
					Debug.LogError("GraphicInfo CallbackExpression is nul");
					return false;
				}
				else
				{
					return CallbackExpression(ConditionalExpression);
				}
			}
		}

		//条件式
		public string ConditionalExpression { get; private set; }

		//テクスチャ書き込み設定
		public AdvRenderTextureSetting RenderTextureSetting { get { return renderTextureSetting; } }
		AdvRenderTextureSetting renderTextureSetting = new AdvRenderTextureSetting();
		
		public AdvFaceIconInfo Icon{ get; private set; }

		public AdvGraphicInfo(string dataType, int index, string key, StringGridRow row, IAdvSettingData advSettindData)
		{
			this.DataType = dataType;
			this.Index = index;
			this.Key = key;
			this.SettingData = advSettindData;
			this.RowData = row;

			switch (DataType)
			{
				case AdvGraphicInfo.TypeParticle:
					this.FileType = AdvGraphicInfo.FileTypeParticle;
					break;
				default:
					this.FileType = AdvParser.ParseCellOptional<string>(row, AdvColumnName.FileType, "");
					break;
			}

			this.FileName = AdvParser.ParseCell<string>(row, AdvColumnName.FileName);
			try
			{
				this.Pivot = ParserUtil.ParsePivotOptional(AdvParser.ParseCellOptional<string>(row, AdvColumnName.Pivot, ""), new Vector2(0.5f, 0.5f));
			}
			catch (System.Exception e)
			{
				Debug.LogError(row.ToErrorString(e.Message));
			}

			try
			{
				string pivot0String = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Pivot0, "");
				this.EnablePivot0 = !string.IsNullOrEmpty(pivot0String) && pivot0String != "NoUse";
				if (this.EnablePivot0)
				{
					this.Pivot0 = ParserUtil.ParsePivotOptional(pivot0String,new Vector2(0.5f, 0.5f));
				}
				else
				{
					this.Pivot0 = new Vector2(0.5f, 0.5f);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError(row.ToErrorString(e.Message));
			}

			try
			{
				this.Scale = ParserUtil.ParseScale3DOptional(AdvParser.ParseCellOptional<string>(row, AdvColumnName.Scale, ""), Vector3.one);
			}
			catch (System.Exception e)
			{
				Debug.LogError(row.ToErrorString(e.Message));
			}

			Vector3 pos;
			pos.x = AdvParser.ParseCellOptional<float>(row, AdvColumnName.X, 0);
			pos.y = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Y, 0);
			pos.z = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Z, 0);
			this.Position = pos;

			this.SubFileName = AdvParser.ParseCellOptional<string>(row, AdvColumnName.SubFileName, "");
			this.AnimationState = AdvParser.ParseCellOptional<string>(row, AdvColumnName.AnimationState, "");

			this.ConditionalExpression = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Conditional, "");
			this.RenderTextureSetting.Parse(row);

			this.Loop = row.ParseCellOptional<bool>("Loop", true);

			//アイコンファイルの設定
			this.Icon = new AdvFaceIconInfo(row);
		}

		//例外的に動的に生成する場合。（主にキャプチャー画面を描画するオブジェクトためになど
		public AdvGraphicInfo(string dataType, string key, string fileType)
		{
			this.DataType = dataType;
			this.Key = key;
			this.FileType = fileType;
			this.FileName = "";
			this.Pivot = new Vector2(0.5f, 0.5f);
			this.EnablePivot0 = false;
			this.Pivot0 = new Vector2(0.5f, 0.5f);
			this.Scale = Vector3.one;

			this.Position = Vector3.zero;
			this.ConditionalExpression = "";
			this.SubFileName = "";
			this.AnimationState = "";
			this.Loop = true;
		}

		//起動時の初期化
		public void BootInit(Func<string, string, string> FileNameToPath, AdvSettingDataManager dataManager)
		{
			File = AssetFileManager.GetFileCreateIfMissing(FileNameToPath(FileName, FileType), this);
			//アニメーションの設定
			string animationLabel = AdvParser.ParseCellOptional<string>(RowData, AdvColumnName.Animation, "");
			if (!string.IsNullOrEmpty(animationLabel))
			{
				this.AnimationData = dataManager.AnimationSetting.Find(animationLabel);
				if (this.AnimationData == null)
				{
					Debug.LogError(this.RowData.ToErrorString("Animation [ " + animationLabel + " ] is not found"));
				}
			}

			//目パチの設定
			string eyeBlinkLabel = AdvParser.ParseCellOptional<string>(RowData, AdvColumnName.EyeBlink, "");
			if (!string.IsNullOrEmpty(eyeBlinkLabel))
			{
				AdvEyeBlinkData data;
				if (dataManager.EyeBlinkSetting.Dictionary.TryGetValue(eyeBlinkLabel, out data))
				{
					this.EyeBlinkData = data;
				}
				else
				{
					Debug.LogError(this.RowData.ToErrorString("EyeBlinkLabel [ " + eyeBlinkLabel + " ] is not found"));
				}
			}

			//口パクの設定
			string lipSynchLabel = AdvParser.ParseCellOptional<string>(RowData, AdvColumnName.LipSynch, "");
			if (!string.IsNullOrEmpty(lipSynchLabel))
			{
				AdvLipSynchData data;
				if (dataManager.LipSynchSetting.Dictionary.TryGetValue(lipSynchLabel, out data))
				{
					this.LipSynchData = data;
				}
				else
				{
					Debug.LogError(this.RowData.ToErrorString("LipSynchLabel [ " + lipSynchLabel + " ] is not found"));
				}
			}
			
			//アイコンの設定
			Icon.BootInit((fileName) => dataManager.BootSetting.CharacterDirInfo.FileNameToPath(fileName) );

		}

		//IAdvGraphicObjectがAddComponentされたプレハブをリソースに持つかチェック
		internal bool TryGetAdvGraphicObjectPrefab(out GameObject prefab)
		{
			prefab = null;
			if (File == null)
				return false;
			if (File.FileType != AssetFileType.UnityObject) return false;

			GameObject obj = File.UnityObject as GameObject;
			if (obj == null) return false;

			if (obj.GetComponent<AdvGraphicObject>() == null) return false;

			prefab = obj;
			return true;
		}

		//FileType列の文字列から、Addするコンポーネントの型を取得
		internal Type GetComponentType()
		{
			if (CallbackCreateCustom != null)
			{
				Type type = null;
				CallbackCreateCustom(this.FileType, ref type);
				if (type != null) return type;
			}

			switch (this.FileType)
			{
				case FileType3D:
				case FileType3DPrefab:
					return typeof(AdvGraphicObject3DPrefab);
				case FileTypeParticle:
					return typeof(AdvGraphicObjectParticle);
				case FileType2DPrefab:
					return typeof(AdvGraphicObject2DPrefab);
				case FileTypeCustom:
					return typeof(AdvGraphicObjectCustom);

				case FileTypeAvatar:
					return typeof(AdvGraphicObjectAvatar);
				case FileTypeDicing:
					return typeof(AdvGraphicObjectDicing);
				case FileTypeVideo:
#if UNITY_5_6_OR_NEWER
					return typeof(AdvGraphicObjectVideo);
#else
					Debug.LogErrorFormat("FileType :{0} is not support Unity5.5. Please upgrade Unity5.6 or newer ", FileTypeVideo);
					return typeof(AdvGraphicObjectVideo);
#endif
				case FileTypeCustom2D:
					return typeof(AdvGraphicObjectCustom2D);
				case FileType2D:
				default:
					return typeof(AdvGraphicObjectRawImage);
			}
		}
		
		//宴のコンポーネントを継承したプレハブかどうか
		internal bool IsOverridePrefab()
		{
			switch (FileType)
			{
				case FileTypeOverridePrefab:
					return true;
				default:
					return false;
			}
		}


		//コンポーネントの種別がUI系かどうか
		internal bool IsUguiComponentType
		{
			get
			{
				switch (FileType)
				{
					case FileTypeOverridePrefab:
						return true;
					default:
						return GetComponentType().IsSubclassOf(typeof(AdvGraphicObjectUguiBase));
				}
			}
		}

		//クロスフェード可能な素材か比較
		internal bool CheckFailedCrossFade(AdvGraphicInfo target)
		{
			if (target.FileType != this.FileType) return true;
			if (!target.EnablePivot0 && target.Pivot0 != this.Pivot0) return true;
			return false;
		}


		const int SaveVersion = 0;

		//セーブデータ用のバイナリ書き込み
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(SaveVersion);
			writer.Write(DataType);
			writer.Write(Key);
			writer.Write(Index);
		}

		//セーブデータ用のバイナリ読み込み
		public static AdvGraphicInfo ReadGraphicInfo(AdvEngine engine, BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > SaveVersion)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return null;
			}

			string dataType = reader.ReadString();
			string key = reader.ReadString();
			int index = reader.ReadInt32();

			AdvGraphicInfoList list;
			switch (dataType)
			{
				case TypeCharacter:
					list = engine.DataManager.SettingDataManager.CharacterSetting.KeyToGraphicInfo(key);
					break;
				case TypeParticle:
					return engine.DataManager.SettingDataManager.ParticleSetting.LabelToGraphic(key);
				case TypeTexture:
					list = engine.DataManager.SettingDataManager.TextureSetting.LabelToGraphic(key);
					break;
				case TypeCapture:
					Debug.LogError("Caputure image not support on save");
					return null;
				default:
					return new AdvGraphicInfo(dataType, key, AdvGraphicInfo.FileType2D);
			}

			if (list != null && index < list.InfoList.Count)
			{
				return list.InfoList[index];
			}
			return null;
		}
	}
}
