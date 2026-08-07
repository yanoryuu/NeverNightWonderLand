// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// パラメーター設定
	/// </summary>
	public class AdvParamStructTbl
	{
		public Dictionary<string,AdvParamStruct> Tbl { get{return tbl; }}
		Dictionary<string,AdvParamStruct> tbl = new Dictionary<string, AdvParamStruct> ();

		//通常のパラメーターとしてデータ追加
		public void AddSingle(StringGrid grid)
		{
			const string SingleKey = "";
			AdvParamStruct data;
			if(	!Tbl.TryGetValue(SingleKey,out data))
			{
				data = new AdvParamStruct();
				Tbl.Add(SingleKey,data);
			}			
			data.AddData(grid);
		}

		//構造体のパラメーターテーブルとしてデータ解析
		public void AddTbl(StringGrid grid)
		{
			if (grid.Rows.Count < 3) {
				Debug.LogError(grid.Name + " is not Param Sheet");
				return;
			}

			StringGridRow row0 = grid.Rows[0];
			StringGridRow row1 = grid.Rows[1];
			StringGridRow row2 = grid.Rows[2];

			AdvParamStruct header = new AdvParamStruct(row0, row1, row2);

			for (int i =3; i < grid.Rows.Count; ++i)
			{
				StringGridRow row = grid.Rows[i];
				if (row.IsEmptyOrCommantOut) continue;
				AdvParamStruct data = new AdvParamStruct(header, row);
				string key = row.Strings[0];
				if (Tbl.ContainsKey(key))
				{
					row.ToErrorString(key + " is already contains ");
				}
				else
				{
					Tbl.Add(key, data);
				}
			}
		}

		//中身を全てコピー
		internal AdvParamStructTbl Clone()
		{
			AdvParamStructTbl clone = new AdvParamStructTbl();
			foreach (var item in Tbl)
			{
				clone.Tbl.Add(item.Key, item.Value.Clone());
			}
			return clone;
		}

		/// <summary>
		/// システムデータ以外の値をデフォルト値で初期化
		/// </summary>
		internal void InitDefaultNormal(AdvParamStructTbl src)
		{
			foreach (var keyValue in src.Tbl)
			{
				AdvParamStruct data;
				if (Tbl.TryGetValue(keyValue.Key, out data))
				{
					data.InitDefaultNormal(keyValue.Value);
				}
				else
				{
					Debug.LogError("Param: " + keyValue.Key + "  is not found in default param");
				}
			}
		}

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer, AdvParamData.FileType fileType)
		{
			Profiler.BeginSample(" Write StructTbl");
			writer.Write(Version);
			writer.Write(Tbl.Count);
			foreach (var keyValue in Tbl)
			{
				writer.Write(keyValue.Key);
				writer.WriteBuffer((x) => keyValue.Value.Write(x, fileType));
			}
			Profiler.EndSample();
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader, AdvParamData.FileType fileType)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string key = reader.ReadString();
				if (Tbl.ContainsKey(key))
				{
					reader.ReadBuffer((x) => Tbl[key].Read(x,fileType));
				}
				else
				{
					//セーブされていたが、パラメーター設定から消えているので読み込まない
					reader.SkipBuffer();
				}
			}
		}
	}
}