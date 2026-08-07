// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// パーティクル設定（ラベルとの対応）
	/// </summary>
	public class AdvParticleSettingData : AdvSettingDictinoayItemBase
	{
		//グラフィックの情報
		public AdvGraphicInfo Graphic { get { return this.graphic; } }
		AdvGraphicInfo graphic;


		/// <summary>
		/// StringGridの一行からデータ初期化
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			if (row.IsEmptyOrCommantOut) return false;

			this.RowData = row;
			string key = AdvParser.ParseCell<string>(row, AdvColumnName.Label);
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}
			else
			{
				InitKey(key);
				this.graphic = new AdvGraphicInfo(AdvGraphicInfo.TypeParticle, 0, key , row, this);
				return true;
			}
		}

		public void BootInit(AdvSettingDataManager dataManager)
		{
			Graphic.BootInit((fileName,fileType) => FileNameToPath(fileName, fileType, dataManager.BootSetting), dataManager);
		}

		string FileNameToPath(string fileName, string fileType, AdvBootSetting settingData)
		{
			return settingData.ParticleDirInfo.FileNameToPath(fileName);
		}
	}


	/// <summary>
	/// パーティクルの設定
	/// </summary>
	public class AdvParticleSetting : AdvSettingDataDictinoayBase<AdvParticleSettingData>
	{
		/// <summary>
		/// 起動時の初期化
		/// </summary>
		public override void BootInit(AdvSettingDataManager dataManager)
		{
			foreach (AdvParticleSettingData data in List)
			{
				data.BootInit(dataManager);
			}
		}

		/// <summary>
		/// 全てのリソースをダウンロード
		/// </summary>
		public override void DownloadAll()
		{
			foreach (AdvParticleSettingData data in List)
			{
				AssetFileManager.Download(data.Graphic.File);
			}
		}

		//ラベルからデータを取得
		AdvParticleSettingData FindData(string label)
		{
			AdvParticleSettingData data;
			if (!Dictionary.TryGetValue(label, out data))
			{
				return null;
			}
			else
			{
				return data;
			}
		}

		/// <summary>
		/// ラベルからグラフィック情報を取得
		/// </summary>
		/// <param name="label">ラベル</param>
		/// <returns>グラフィック情報</returns>
		public AdvGraphicInfo LabelToGraphic(string label)
		{
			AdvParticleSettingData data = FindData(label);
			if (data == null)
			{
				Debug.LogError("Not found " + label + " in Particle sheet");
				return null;
			}
			return data.Graphic;
		}
	}
}