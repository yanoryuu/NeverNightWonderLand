// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Utage
{
	/// <summary>
	/// iTweenのタイプ
	/// </summary>
	public enum iTweenType
	{
		ColorFrom,
		ColorTo,
		MoveAdd,			//MoveByと同じ
		MoveBy,				//相対位置移動
		MoveFrom,			//指定の位置から移動
		MoveTo,				//指定の位置へ移動
		PunchPosition,
		PunchRotation,
		PunchScale,
		RotateAdd,
		RotateBy,
		RotateFrom,
		RotateTo,
		ScaleAdd,
		ScaleBy,
		ScaleFrom,
		ScaleTo,
		ShakePosition,
		ShakeRotation,
		ShakeScale,
		Stop,
		Max,
	};

	/// <summary>
	/// 文字列で書かれたiTweenを解析してiTween命令を発行
	/// iTweenのドキュメントは　 http://itween.pixelplacement.com
	/// EaseTypeのデモは		 http://www.robertpenner.com/easing/easing_demo.html
	/// </summary>
	public class iTweenData
	{
		public const string Time = "time";
		public const string Delay = "delay";
		public const string Speed = "speed";        //　MoveToなどのように、移動量が決まっている場合は、timeの代わりにスピード指定が可能
		public const string X = "x";
		public const string Y = "y";
		public const string Z = "z";
		public const string Color = "color";
		public const string R = "r";
		public const string G = "g";
		public const string B = "b";
		public const string A = "a";
		public const string Alpha = "alpha";
		public const string Islocal = "islocal";
		public const string EaseType = "easeType";
		public const string LoopType = "loopType";

		iTweenType type;
		public iTweenType Type
		{
			get { return type; }
		}

		public iTween.LoopType Loop
		{
			get { return loopType; }
		}
		iTween.LoopType loopType;

		public int LoopCount
		{
			get { return loopCount; }
		}
		int loopCount;

		public Dictionary<string,object> HashObjects
		{
			get { return hashObjects; }
		}
		Dictionary<string, object> hashObjects = new Dictionary<string, object>();

		public object[] MakeHashArray()
		{
			List<object> hashArray = new List<object>();
			foreach (var keyValue in HashObjects)
			{
				hashArray.Add(keyValue.Key);
				hashArray.Add(keyValue.Value);
			}
			return hashArray.ToArray();
		}

		/// <summary>
		/// エラーメッセージ（コンストラクタで初期化した際にエラーがあった場合に、エラーメッセージが入る）
		/// </summary>
		public string ErrorMsg { get { return errorMsg; } }
		string errorMsg = "";

		/// <summary>
		/// セーブのためにとっておく
		/// </summary>
		string strType;
		string strArg;
		string strEaseType;
		string strLoopType;

		//文字列をキーにして値を返すコールバック（変数処理のため）
		[RuntimeInitializeStaticField] public static System.Func<string, object> CallbackGetValue;

		//ダイナミック(変数が仕込まれていて、実行ごとに結果がかわる)
		public bool IsDynamic { get { return this.isDynamic; } }
		bool isDynamic;


		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="type">Tweenのタイプ</param>
		/// <param name="arg">文字列で定義される、Tweenの引数</param>
		/// <param name="easeType">補完方法</param>
		/// <param name="loopType">ループの方法</param>
		public iTweenData(string type, string arg, string easeType, string loopType)
		{
			Init(type, arg, easeType, loopType );
		}
		public iTweenData(iTweenType type, string arg)
		{
			Init(type.ToString(), arg, "", "");
		}

		/// <summary>
		/// 再初期化
		/// </summary>
		public void ReInit()
		{
			if (isDynamic)
			{
				HashObjects.Clear();
				Init(strType, strArg, strEaseType, strLoopType);
			}
		}


		//初期化
		void Init(string type, string arg, string easeType, string loopType)
		{
			this.strType = type;
			this.strArg = arg;
			this.strEaseType = easeType;
			this.strLoopType = loopType;
			
			ParseParameters(type, arg);
			if (!string.IsNullOrEmpty(easeType))
			{
				HashObjects.Add(EaseType, easeType);
			}
			if (!string.IsNullOrEmpty(loopType))
			{
				try
				{
					ParseLoopType(loopType);
					HashObjects.Add(LoopType, this.loopType);
				}
				catch (System.Exception e)
				{
					AddErrorMsg(loopType + "は、LoopTypeとして解析できません。");
					AddErrorMsg(e.Message);
				}
			}
		}

		void ParseParameters(string type, string arg)
		{
			try
			{
				this.type = (iTweenType)System.Enum.Parse(typeof(iTweenType), type);
				if (this.type == iTweenType.Stop)
				{
					return;
				}
				else
				{
					char[] separator = { ' ', '=' };
					string[] args = arg.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
					if (args.Length % 2 != 0 || args.Length <= 0)
					{
						AddErrorMsg(arg + "内が、「パラメーター名=値」 の形式で書かれていません。");
					}
					else
					{
						for (int i = 0; i < args.Length / 2; ++i)
						{
							string name = args[i * 2];
							var obj = ParseValue(this.type, name, args[i * 2 + 1], ref isDynamic);
							if (obj == null)
							{
								AddErrorMsg($"{name}={args[i * 2 + 1]}　はTweenとして解析できません。");
							}
							HashObjects.Add(name, obj);
						}
					}
				}
			}
			catch (System.Exception e)
			{
				AddErrorMsg(arg + "内が、「パラメーター名=値」 の形式で書かれていません。");
				AddErrorMsg(e.Message);
			}
		}

		//エラーメッセージを追加
		void AddErrorMsg(string msg)
		{
			if (!string.IsNullOrEmpty(errorMsg))
			{
				errorMsg += "\n";
			}
			errorMsg += msg;
		}

		//ループ型を解析
		void ParseLoopType(string loopTypeStr)
		{
			loopType = iTween.LoopType.none;
			loopCount = 0;
			char[] separator = { ' ', '=' };
			string[] args = loopTypeStr.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
			if( args.Length == 2 )
			{
				loopType = (iTween.LoopType)System.Enum.Parse(typeof(iTween.LoopType), args[0]);
				loopCount = int.Parse(args[1]);
			}
			else
			{
				throw new System.Exception();
			}
		}

		/// <summary>
		/// 無限ループしてるか
		/// </summary>
		public bool IsEndlessLoop { get { return (loopType != iTween.LoopType.none) && ( loopCount <= 0 ); } }

		// 回数制限付きのループ
		public bool IsCountLoop { get { return (loopType != iTween.LoopType.none) && ( loopCount > 0 ); } }

		//ローカル座標系のサポートしているか
		public bool IsSupportLocal
		{
			get
			{
				string[] args = ArgTbl[(int)this.Type];
				foreach (var item in args)
				{
					if (item == Islocal)
					{
						return true;
					}
				}
				return false;
			}
		}
			
		//ローカル座標系の操作か？
		//2D、3Dの関係上切りかえる必要が出てきやすいので
		public bool IsLocal
		{
			get
			{
				if (HashObjects.ContainsKey(Islocal))
				{
					return (bool)HashObjects[Islocal];
				}
				else
				{
					if (IsSupportLocal)
					{
						return false;
					}
					else
					{
						Debug.LogError("Not Support Local type");
						return false;
					}
				}
			}
			set
			{
				HashObjects[Islocal] = value;
			}
		}

		/// <summary>
		/// セーブデータ用のバイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public void Write(BinaryWriter writer)
		{
			if (!IsEndlessLoop)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.TweenWrite));
			}

			writer.Write(strType);
			writer.Write(strArg);
			writer.Write(strEaseType);
			writer.Write(strLoopType);
		}

		/// <summary>
		/// セーブデータ用のバイナリ読みこみ
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public iTweenData(BinaryReader reader)
		{
			string strType= reader.ReadString();
			string strArg = reader.ReadString();
			string strEaseType = reader.ReadString();
			string strLoopType = reader.ReadString();
			Init(strType, strArg, strEaseType, strLoopType);
		}

		//itweenのタイプごとに、対応する引数名を定義
		static readonly string[][] ArgTbl = new string[(int)iTweenType.Max][]
		{
			new string[]{Time,Delay,Color,Alpha,R,G,B,A},		//ColorFrom,
			new string[]{Time,Delay,Color,Alpha,R,G,B,A},		//ColorTo,
			new string[]{Time,Delay,X,Y,Z,Speed},				//MoveAdd,
			new string[]{Time,Delay,X,Y,Z,Speed},				//MoveBy,
			new string[]{Time,Delay,X,Y,Z,Speed,Islocal},		//MoveFrom,
			new string[]{Time,Delay,X,Y,Z,Speed,Islocal},		//MoveTo,
			new string[]{Time,Delay,X,Y,Z},						//PunchPosition,
			new string[]{Time,Delay,X,Y,Z},						//PunchRotation,
			new string[]{Time,Delay,X,Y,Z},						//PunchScale,
			new string[]{Time,Delay,X,Y,Z,Speed},				//RotateAdd,
			new string[]{Time,Delay,X,Y,Z,Speed},				//RotateBy,
			new string[]{Time,Delay,X,Y,Z,Speed,Islocal},		//RotateFrom,
			new string[]{Time,Delay,X,Y,Z,Speed,Islocal},		//RotateTo,
			new string[]{Time,Delay,X,Y,Z,Speed},				//ScaleAdd,
			new string[]{Time,Delay,X,Y,Z,Speed},				//ScaleBy,
			new string[]{Time,Delay,X,Y,Z,Speed},				//ScaleFrom,
			new string[]{Time,Delay,X,Y,Z,Speed},				//ScaleTo,
			new string[]{Time,Delay,X,Y,Z,Islocal},				//ShakePosition,
			new string[]{Time,Delay,X,Y,Z},						//ShakeRotation,
			new string[]{Time,Delay,X,Y,Z},						//ShakeScale,
			new string[]{},										//Stop,
		};

		/// <summary>
		/// itweenの引数の値を文字列から解析
		/// </summary>
		/// <param name="type">itweenのタイプ</param>
		/// <param name="name">引数の名前</param>
		/// <param name="valueString">値の文字列</param>
		/// <returns>値</returns>
		static object ParseValue(iTweenType type, string name, string valueString, ref bool isDynamic )
		{
			object paramValue = null;
			if (CallbackGetValue != null)
			{
				paramValue = CallbackGetValue(valueString);
				isDynamic = true;
			}
			if (CheckArg(type, name))
			{
				switch (name)
				{
					case Time:
					case Delay:
					case Speed:
					case Alpha:
					case R:
					case G:
					case B:
					case A:
					case X:
					case Y:
					case Z:
						if (paramValue != null )
						{
							return (float)paramValue;
						}
						else
						{
							return WrapperUnityVersion.ParseFloatGlobal(valueString);
						}
					case Islocal: 
						if (paramValue != null )
						{
							return (bool)paramValue;
						}
						else
						{
							return bool.Parse(valueString);
						}
					case Color:
						return ColorUtil.ParseColor(valueString);
					default:
						return null;
				}
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 引数名とitweenのタイプが対応しているかチェック
		/// </summary>
		/// <param name="type">itweenのタイプ</param>
		/// <param name="name">引数名</param>
		/// <returns></returns>
		static bool CheckArg(iTweenType type, string name)
		{
			return (System.Array.IndexOf(ArgTbl[(int)type], name) >= 0);
		}
	
		public static bool IsPostionType(iTweenType type)
		{
			switch (type)
			{
				case iTweenType.MoveAdd:
				case iTweenType.MoveBy:
				case iTweenType.MoveFrom:
				case iTweenType.MoveTo:
				case iTweenType.PunchPosition:
				case iTweenType.ShakePosition:
					return true;
				default:
					return false;
			}
		}
	}
}
