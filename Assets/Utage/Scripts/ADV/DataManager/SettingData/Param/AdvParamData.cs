// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// パラメーターのデータ
	/// </summary>	
	public class AdvParamData
	{
		/// <summary>
		/// キー
		/// </summary>
		public string Key { get { return key; } }
		string key;

		/// <summary>
		/// 型
		/// </summary>
		public enum ParamType
		{
			/// <summary>bool</summary>
			Bool,
			/// <summary>float</summary>
			Float,
			/// <summary>int</summary>
			Int,
			/// <summary>string</summary>
			String,
		};

		/// <summary>
		/// 型
		/// </summary>
		public ParamType Type { get { return type; } }
		ParamType type;
		
		/// 値(ボクシングが発生するので、型がわかっているならなるべく使わないこと)
		public object Parameter
		{
			get
			{
				return GetValueWithBoxing();
			}
			set
			{
				SetValueWithBoxing(value);
			}
		}
		
		//ボクシングの発生しないBool値のget,set
		//ボクシング除けのためにobjectで汎用化させるのを廃止し値型を扱えるように
		public bool BoolValue
		{
			get
			{
				if (Type != ParamType.Bool)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not Bool type. This type is {1} ",Key,Type);
					return false;
				}
				return boolValue;
			}
			set
			{
				if (Type != ParamType.Bool)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not Bool type. This type is {1} ",Key,Type);
					return;
				}
				boolValue = value;
			}
		}
		bool boolValue;

		//ボクシングの発生しないFloat値のget,set
		//ボクシング除けのためにobjectで汎用化させるのを廃止し値型を扱えるように
		public float FloatValue
		{
			get
			{
				if (Type != ParamType.Float)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not Float type. This type is {1} ",Key,Type);
					return 0;
				}
				return floatValue;
			}
			set
			{
				if (Type != ParamType.Float)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not Float type. This type is {1} ",Key,Type);
					return;
				}
				floatValue = value;
			}
		}
		float floatValue;

		//ボクシングの発生しないFloat値のget,set
		//ボクシング除けのためにobjectで汎用化させるのを廃止し値型を扱えるように
		public int IntValue
		{
			get
			{
				if (Type != ParamType.Int)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not Int type. This type is {1} ",Key,Type);
					return 0;
				}
				return intValue;
			}
			set
			{
				if (Type != ParamType.Int)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not Int type. This type is {1} ",Key,Type);
					return;
				}
				intValue = value;
			}
		}
		int intValue;

		//ボクシングの発生しないString値のget,set
		//ボクシング除けのためにobjectで汎用化させるのを廃止し値型を扱えるように
		public string StringValue
		{
			get
			{
				if (Type != ParamType.String)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not String type. This type is {1} ",Key,Type);
					return "";
				}
				return stringValue;
			}
			set
			{
				if (Type != ParamType.String)
				{
					Debug.LogErrorFormat("Parameter [{0}] is not String type. This type is {1} ",Key,Type);
					return;
				}
				stringValue = value;
			}
		}
		string stringValue;

		/// <summary>
		/// ファイルタイプ
		/// </summary>
		public enum FileType
		{
			/// <summary>通常</summary>
			Default,
			/// <summary>システムセーブデータ</summary>
			System,
			/// <summary>Const（一定の値。代入やセーブロードの対象にならない）</summary>
			Const,
		};

		/// <summary>
		/// 型
		/// </summary>
		public FileType SaveFileType { get { return this.fileType; } }
		FileType fileType;

		public bool TryParse(string name, string type, string fileType)
		{
			this.key = name;
			if (!ParserUtil.TryParaseEnum<ParamType>(type, out this.type))
			{
				Debug.LogError(type + " is not ParamType");
				return false;
			}
			if (string.IsNullOrEmpty(fileType))
			{
				this.fileType = FileType.Default;
			}
			else
			{
				if (!ParserUtil.TryParaseEnum<FileType>(fileType, out this.fileType))
				{
					Debug.LogError(fileType + " is not FileType");
					return false;
				}
			}
			return true;
		}

		public bool TryParse(AdvParamData src, string value)
		{
			this.key = src.Key;
			this.type = src.Type;
			this.fileType = src.SaveFileType;
			try
			{
				ParseParameterString(value);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool TryParse(StringGridRow row)
		{
			string key = AdvParser.ParseCell<string>(row, AdvColumnName.Label);
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}
			else
			{
				this.key = key;
				this.type = AdvParser.ParseCell<ParamType>(row, AdvColumnName.Type);
				this.fileType = AdvParser.ParseCellOptional<FileType>(row, AdvColumnName.FileType, FileType.Default);
				try
				{
					var parameterString = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Value, "");
					ParseParameterString(parameterString);
					return true;
				}
				catch
				{
					return false;
				}
			}
		}

		public void Copy(AdvParamData src)
		{
			this.key = src.Key;
			this.type = src.type;
			this.fileType = src.fileType;
			switch (Type)
			{
				case ParamType.Bool:
					this.boolValue = src.boolValue;
					break;
				case ParamType.Float:
					this.floatValue = src.floatValue;
					break;
				case ParamType.Int:
					this.intValue = src.intValue;
					break;
				case ParamType.String:
					this.stringValue = src.stringValue;
					break;
			}
		}

		//ボクシングが発生する値の取得
		object GetValueWithBoxing()
		{
			switch (Type)
			{
				case ParamType.Bool:
					return this.boolValue;
				case ParamType.Float:
					return this.floatValue;
				case ParamType.Int:
					return this.intValue;
				case ParamType.String:
					return this.stringValue;
				default:
					Debug.LogErrorFormat("Unknown Type {0}", Type);
					return this.stringValue;
			}
		}

		void SetValueWithBoxing(object value)
		{
			switch (Type)
			{
				case ParamType.Bool:
					this.boolValue = (bool) value;
					break;
				case ParamType.Float:
					this.floatValue = ExpressionCast.ToFloat(value);
					break;
				case ParamType.Int:
					this.intValue = ExpressionCast.ToInt(value);
					break;
				case ParamType.String:
					this.stringValue = (string) value;
					break;
				default:
					Debug.LogErrorFormat("Unknown Type {0}", Type);
					break;
			}
		}

		void ParseParameterString(string parameterString)
		{
			switch (Type)
			{
				case ParamType.Bool:
					boolValue = bool.Parse(parameterString);
					break;
				case ParamType.Float:
					floatValue = WrapperUnityVersion.ParseFloatGlobal(parameterString);
					break;
				case ParamType.Int:
					intValue = int.Parse(parameterString);
					break;
				case ParamType.String:
					stringValue = parameterString;
					break;
				default:
					Debug.LogErrorFormat("Unknown Type {0}", Type);
					break;
			}
		}

		public string ParameterString
		{
			get
			{
				switch (Type)
				{
					case ParamType.Bool:
						return this.boolValue.ToString();
					case ParamType.Float:
						return WrapperUnityVersion.ToStringFloatGlobal(this.floatValue);
					case ParamType.Int:
						return this.intValue.ToString();
					case ParamType.String:
						return this.stringValue.ToString();
					default:
						Debug.LogErrorFormat("Unknown Type {0}", Type);
						return this.stringValue.ToString();
				}
			}
		}

		public void CopySaveData(AdvParamData src)
		{
			if (this.key != src.Key) Debug.LogError(src.key + "is different name of Saved param");
			if (this.type != src.type) Debug.LogError(src.type + "is different type of Saved param");
			if (this.fileType != src.fileType) Debug.LogError(src.fileType + "is different fileType of Saved param");
			this.Copy(src);
		}


		//セーブデータ用の読み込み
		public void Read(string paramString)
		{
			ParseParameterString(paramString);
		}
	}
}
