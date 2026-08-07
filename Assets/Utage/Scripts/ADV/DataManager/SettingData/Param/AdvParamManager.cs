// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UtageExtensions;
using UnityEngine.Profiling;

namespace Utage
{
	[System.Serializable]
	public class AdvParamManager : AdvSettingBase
	{
		public const string DefaultSheetName = "Param";

		const string KeyPattern = @"(.+)\[(.+)\]\.(.+)";
		static readonly Regex KeyRegix = new Regex(KeyPattern, RegexOptions.IgnorePatternWhitespace);
		internal static bool ParseKey(string key, out string structName, out string indexKey, out string valueKey)
		{
			structName = indexKey = valueKey = "";
			if (!key.Contains("[")) return false;
			Match match = KeyRegix.Match(key);
			if (!match.Success) return false;

			structName = match.Groups[1].Value + "{}";
			indexKey = match.Groups[2].Value;
			valueKey = match.Groups[3].Value;
			return true;
		}
		public bool IsInit { get; protected set; }

		public Dictionary<string, AdvParamStructTbl> StructTbl { get { return structTbl; } }
		Dictionary<string, AdvParamStructTbl> structTbl = new Dictionary<string, AdvParamStructTbl>();

		//システム系のパラメーターが変化したか（セーブに使う）
		public bool HasChangedSystemParam { get; set; }

		//デフォルトパラメーター
		public AdvParamManager DefaultParameter { get; set; }

		protected AdvEngine AdvEngine { get; set; }

		/// <summary>
		/// キーからパラメータを取得
		/// </summary>
		bool TryGetParamData(string key, out AdvParamData data)
		{
			data = null;
			string structName, indexKey, valueKey;
			if (!ParseKey(key, out structName, out indexKey, out valueKey))
			{
				AdvParamStruct def = GetDefault();
				if (def == null) return false;
				return def.Tbl.TryGetValue(key, out data);
			}
			else
			{
				AdvParamStruct paramStruct;
				if (!TryGetParamTbl(structName, indexKey, out paramStruct))
				{
					return false;
				}
				return paramStruct.Tbl.TryGetValue(valueKey, out data);
			}
		}

		/// <summary>
		/// パラメータテーブルを取得
		/// </summary>
		public bool TryGetParamTbl(string structName, string indexKey, out AdvParamStruct paramStruct)
		{
			paramStruct = null;
			if (!StructTbl.ContainsKey(structName)) return false;

			if (!StructTbl[structName].Tbl.ContainsKey(indexKey)) return false;

			paramStruct = StructTbl[structName].Tbl[indexKey];
			return true;
		}

		public AdvParamStruct GetDefault()
		{
			if (!StructTbl.ContainsKey(DefaultSheetName))
			{
				return null;
			}
			return StructTbl[DefaultSheetName].Tbl[""];
		}

		protected override void OnParseGrid(StringGrid grid)
		{
			if (GridList.Count == 0)
			{
				Debug.LogError("Old Version Reimport Excel Scenario Data");
				return;
			}

			string sheetName = grid.SheetName;
			AdvParamStructTbl data;
			if (!StructTbl.TryGetValue(sheetName, out data))
			{
				data = new AdvParamStructTbl();
				StructTbl.Add(sheetName, data);
			}

			if (sheetName == DefaultSheetName)
			{
				data.AddSingle(grid);
			}
			else
			{
				data.AddTbl(grid);
			}
		}

		/// <summary>
		/// システムデータ含めてデフォルト値で初期化
		/// </summary>
		public void InitDefaultAll(AdvParamManager src)
		{
			this.DefaultParameter = src;
			this.StructTbl.Clear();
			foreach (var keyValue in src.StructTbl)
			{
				StructTbl.Add(keyValue.Key, keyValue.Value.Clone());
			}
			IsInit = true;
		}

		/// <summary>
		/// システムデータ以外の値をデフォルト値で初期化
		/// </summary>
		/// <param name="advParamSetting"></param>
		public void InitDefaultNormal(AdvParamManager src)
		{
			foreach (var keyValue in src.StructTbl)
			{
				AdvParamStructTbl data;
				if (StructTbl.TryGetValue(keyValue.Key, out data))
				{
					data.InitDefaultNormal(keyValue.Value);
				}
				else
				{
					Debug.LogError("Param: " + keyValue.Key + "  is not found in default param");
				}
			}
		}

		public void SetAdvEngine(AdvEngine advEngine)
		{
			AdvEngine = advEngine;
		}

		public int GetParameterInt(string key)
		{
			AdvParamData data = SafeFindParamData(key);
			if (data==null)
			{
				return 0;
			}
			return data.IntValue;
		}

		public void SetParameterInt(string key, int value)
		{
			SetParameter<int>(key, value);
		}

		public float GetParameterFloat(string key)
		{
			AdvParamData data = SafeFindParamData(key);
			if (data==null)
			{
				return 0;
			}
			return data.FloatValue;
		}

		public void SetParameterFloat(string key, float value)
		{
			SetParameter<float>(key, value);
		}

		public bool GetParameterBoolean(string key)
		{
			AdvParamData data = SafeFindParamData(key);
			if (data==null)
			{
				return false;
			}
			return data.BoolValue;
		}

		public void SetParameterBoolean(string key, bool value)
		{
			SetParameter<bool>(key, value);
		}

		public string GetParameterString(string key)
		{
			AdvParamData data = SafeFindParamData(key);
			if (data==null)
			{
				return "";
			}
			return data.StringValue;
		}

		public void SetParameterString(string key, string value)
		{
			SetParameter<string>(key, value);
		}
		
		AdvParamData SafeFindParamData(string key)
		{
			AdvParamData data;
			if (!TryGetParamData(key, out data))
			{
				Debug.LogErrorFormat("{0} is not found in parameter",key);
				return null;
			}
			return data;
		}

		public T GetParameter<T>(string key)
		{
			return (T)GetParameter(key);
		}

		public void SetParameter<T>(string key, T value)
		{
			SetParameter(key, (object)value);
		}

		/// <summary>
		/// 値の代入
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		public void SetParameter(string key, object parameter)
		{
			if (!TrySetParameter(key, parameter))
			{
				Debug.LogError(key + " is not parameter name");
			}
		}

		/// <summary>
		/// 値の代入を試みる
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		/// <returns>代入に成功したらtrue。指定の名前の数値なかったらfalse</returns>
		public bool TrySetParameter(string key, object parameter)
		{
			AdvParamData data;
			if (!CheckSetParameterSub(key, parameter, out data))
			{
				return false;
			}

			data.Parameter = parameter;
			if (data.SaveFileType == AdvParamData.FileType.System)
			{
				HasChangedSystemParam = true;
			}

			if (AdvEngine != null)
			{
				AdvEngine.ParameterEventTrigger.CallEventChangeParameter(data);
			}
			return true;
		}

		/// <summary>
		/// 値の取得を試みる
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		/// <returns>成功したらtrue。指定の名前の数値なかったらfalse</returns>
		public bool TryGetParameter(string key, out object parameter)
		{
			AdvParamData data;
			if (TryGetParamData(key, out data))
			{
				parameter = data.Parameter;
				return true;
			}
			parameter = null;
			return false;
		}

		/// <summary>
		/// 値の代入が可能かチェックする。実際には代入しない
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <param name="parameter">値</param>
		/// <returns>代入に成功したらtrue。指定の名前の数値なかったらfalse</returns>
		public bool CheckSetParameter(string key, object parameter)
		{
			AdvParamData data;
			return CheckSetParameterSub(key, parameter, out data);
		}
		
		//指定のキーのパラメーターが存在するかチェック
		public bool ExistParameter(string key)
		{
			if (TryGetParamData(key, out var data))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 値の取得
		/// </summary>
		/// <param name="key">値の名前</param>
		/// <returns>数値</returns>
		public object GetParameter(string key)
		{
			object parameter;
			if (TryGetParameter(key, out parameter))
			{
				return parameter;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 文字列で書かれた数式から数式を作成
		/// </summary>
		/// <param name="exp">文字列で書かれた数式</param>
		/// <returns>数式</returns>
		public ExpressionParser CreateExpression(string exp)
		{
			return new ExpressionParser(exp, GetParameter, CheckSetParameter);
		}


		/// <summary>
		/// 文字列で書かれた数式を計算して結果を返す
		/// ただし、パラメーターに代入はしない
		/// </summary>
		/// <param name="exp">文字列で書かれた数式</param>
		/// <returns>計算結果</returns>
		public object CalcExpressionNotSetParam(string exp)
		{
			ExpressionParser expression = CreateExpression(exp);
			if (string.IsNullOrEmpty(expression.ErrorMsg))
			{
				return expression.CalcExp(GetParameter, CheckSetParameter);
			}
			else
			{
				throw new System.Exception(expression.ErrorMsg);
			}
		}

		/// <summary>
		/// 数式を計算して結果を返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public object CalcExpression(ExpressionParser exp)
		{
			return exp.CalcExp(GetParameter, TrySetParameter);
		}


		/// <summary>
		/// 数式を計算してfloatを返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public float CalcExpressionFloat(ExpressionParser exp)
		{
			object obj = exp.CalcExp(GetParameter, TrySetParameter);
			if (obj.GetType() == typeof(int))
			{
				return (float)(int)obj;
			}
			else if (obj.GetType() == typeof(float))
			{
				return (float)obj;
			}
			else
			{
				Debug.LogError("Float Cast error : " + exp.Exp);
				return 0;
			}
		}

		/// <summary>
		/// 数式を計算してintを返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public int CalcExpressionInt(ExpressionParser exp)
		{
			object obj = exp.CalcExp(GetParameter, TrySetParameter);
			if (obj.GetType() == typeof(int))
			{
				return (int)obj;
			}
			else if (obj.GetType() == typeof(float))
			{
				return (int)(float)obj;
			}
			else
			{
				Debug.LogError("Int Cast error : " + exp.Exp);
				return 0;
			}
		}

		/// <summary>
		/// 文字列で書かれた論理式から数式を作成
		/// </summary>
		/// <param name="exp">文字列で書かれた論理式</param>
		/// <returns>数式</returns>
		public ExpressionParser CreateExpressionBoolean(string exp)
		{
			return new ExpressionParser(exp, GetParameter, CheckSetParameter, true);
		}

		/// <summary>
		/// 論理式を計算して結果を返す
		/// </summary>
		/// <param name="exp">数式</param>
		/// <returns>計算結果</returns>
		public bool CalcExpressionBoolean(ExpressionParser exp)
		{
			bool ret = exp.CalcExpBoolean(GetParameter, TrySetParameter);
			if (!string.IsNullOrEmpty(exp.ErrorMsg))
			{
				Debug.LogError(exp.ErrorMsg);
			}
			return ret;
		}

		/// <summary>
		/// 文字列で書かれた論理式を計算して結果を返す
		/// </summary>
		/// <param name="exp">文字列で書かれた論理式</param>
		/// <returns>計算結果</returns>
		public bool CalcExpressionBoolean(string exp)
		{
			return CalcExpressionBoolean(CreateExpressionBoolean(exp));
		}


		/// <summary>
		/// 値の代入を試みる
		/// </summary>
		bool CheckSetParameterSub(string key, object parameter, out AdvParamData data)
		{
			if (!TryGetParamData(key, out data))
			{
				return false;
			}
			if (data.SaveFileType == AdvParamData.FileType.Const)
			{
				return false;
			}

			///bool値のキャストは気をつける
			if (data.Type == AdvParamData.ParamType.Bool || parameter is bool)
			{
				if (data.Type != AdvParamData.ParamType.Bool || !(parameter is bool))
				{
					return false;
				}
			}
			///string値のキャストは気をつける
			if (parameter is string)
			{
				if (data.Type != AdvParamData.ParamType.String)
				{
					return false;
				}
			}
			if (data.Type == AdvParamData.ParamType.String)
			{
				if (parameter is bool)
				{
					return false;
				}
			}
			return true;
		}

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer, AdvParamData.FileType fileType)
		{
			Profiler.BeginSample(" Write " + fileType.ToString());
			writer.Write(Version);
			writer.Write(StructTbl.Count);
			foreach (var keyValue in StructTbl)
			{
				writer.Write(keyValue.Key);
				writer.WriteBuffer((x) => keyValue.Value.Write(x, fileType));
			}
			Profiler.EndSample();
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader, AdvParamData.FileType fileType)
		{
			//基本的なパラメーターをデフォルト値でリセット（システムデータ以外）
			if (fileType == AdvParamData.FileType.Default)
			{
				this.InitDefaultNormal(DefaultParameter);
			}

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
				if (StructTbl.ContainsKey(key))
				{
					reader.ReadBuffer((x) => StructTbl[key].Read(x, fileType));
				}
				else
				{
					//セーブされていたが、パラメーター設定から消えているので読み込まない
					reader.SkipBuffer();
				}
			}
		}

		//システムセーブデータ用のIO
		public class IoInerface : IBinaryIO
		{
			public IoInerface(AdvParamManager param, AdvParamData.FileType fileType)
			{
				Param = param;
				FileType = fileType;
			}

			AdvParamData.FileType FileType { get; set; }
			AdvParamManager Param { get; set; }

			//データのキー
			public string SaveKey { get { return "ParamManagerIoInerface"; } }

			//書き込み
			public void OnWrite(BinaryWriter writer)
			{
				Param.Write(writer, FileType);
			}

			//読み込み
			public void OnRead(BinaryReader reader)
			{
				Param.Read(reader, FileType);
			}
		}

		public IoInerface SystemData
		{
			get
			{
				if (systemData == null)
				{
					systemData = new IoInerface(this, AdvParamData.FileType.System);
				}
				return systemData;
			}
		}
		IoInerface systemData;

		public IoInerface DefaultData
		{
			get
			{
				if (defaultData == null)
				{
					defaultData = new IoInerface(this, AdvParamData.FileType.Default);
				}
				return defaultData;
			}
		}
		IoInerface defaultData;
	}
}
