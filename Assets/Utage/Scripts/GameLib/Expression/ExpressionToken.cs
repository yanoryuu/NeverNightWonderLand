// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 文字列→数式の解析をする際のトークンクラス
	/// </summary>	

	public class ExpressionToken
	{
		//演算子
		const string Lpa = "(";			//　左括弧
		const string Rpa = ")";			//　右括弧
		const string Comma = ",";		//　カンマ区切り

		const string Not = "!";			// 否定

		const string Prod = "*";		//　乗算
		const string Div = "/";			//　除算
		const string Mod = "%";			//　剰余算

		public const string Plus = "+";		//　加算
		public const string Minus = "-";		//　減算

		const string GreaterEq = ">=";	//　>=　以上
		const string LessEq = "<=";		//　<=　以下
		const string Greater = ">";		//　>　より大きい
		const string Less = "<";		//　<　より小さい・未満

		const string EqEq = "==";		// 等しい
		const string NotEq = "!=";		// 異なる

		const string And = "&&";		// && 比較論理積
		const string Or = "||";			// || 比較論理和

		const string Eq = "=";			//　代入
		const string PlusEq = "+=";		//　加算代入
		const string MinusEq = "-=";	//　減算代入
		const string ProdEq = "*=";		//　乗算代入
		const string DivEq = "/=";		//　除算代入
		const string ModEq = "%=";		//　剰余算代入

		static public readonly ExpressionToken LpaToken = new ExpressionToken(Lpa, false, ExpressionToken.TokenType.Lpa, 0);
		static public readonly ExpressionToken RpaToken = new ExpressionToken(Rpa, false, ExpressionToken.TokenType.Rpa, 0);
		static public readonly ExpressionToken CommaToken = new ExpressionToken(Comma, false, ExpressionToken.TokenType.Comma, 0);

		//単演算子の＋
		static public readonly ExpressionToken UniPlus = new ExpressionToken(Plus, false, ExpressionToken.TokenType.Unary, 1);
		//単演算子の-
		static public readonly ExpressionToken UniMinus = new ExpressionToken(Minus, false, ExpressionToken.TokenType.Unary, 1);

		//　全演算子データ
		static readonly ExpressionToken[] OperatorArray = 
		{
			LpaToken,
			RpaToken,
			CommaToken,
		
			new ExpressionToken( GreaterEq, false, ExpressionToken.TokenType.Binary, 4 ),
			new ExpressionToken( LessEq, false, ExpressionToken.TokenType.Binary, 4 ),
			new ExpressionToken( Greater, false, ExpressionToken.TokenType.Binary, 4 ),
			new ExpressionToken( Less, false, ExpressionToken.TokenType.Binary, 4 ),

			new ExpressionToken( EqEq, false, ExpressionToken.TokenType.Binary, 5 ),
			new ExpressionToken( NotEq, false, ExpressionToken.TokenType.Binary, 5 ),
		
			new ExpressionToken( And, false, ExpressionToken.TokenType.Binary, 6 ),
		
			new ExpressionToken( Or, false, ExpressionToken.TokenType.Binary, 7 ),
		
			new ExpressionToken( Eq, false, ExpressionToken.TokenType.Substitution, 8 ),
			new ExpressionToken( PlusEq, false, ExpressionToken.TokenType.Substitution, 8 ),
			new ExpressionToken( MinusEq, false, ExpressionToken.TokenType.Substitution, 8 ),
			new ExpressionToken( ProdEq, false, ExpressionToken.TokenType.Substitution, 8 ),
			new ExpressionToken( DivEq, false, ExpressionToken.TokenType.Substitution, 8 ),
			new ExpressionToken( ModEq, false, ExpressionToken.TokenType.Substitution, 8 ),
		
			new ExpressionToken( Not, false, ExpressionToken.TokenType.Unary, 1 ),
		
			new ExpressionToken( Prod, false, ExpressionToken.TokenType.Binary, 2 ),
			new ExpressionToken( Div, false, ExpressionToken.TokenType.Binary, 2 ),
			new ExpressionToken( Mod, false, ExpressionToken.TokenType.Binary, 2 ),
		
			new ExpressionToken( Plus, false, ExpressionToken.TokenType.Binary, 3 ),
			new ExpressionToken( Minus, false, ExpressionToken.TokenType.Binary, 3 ),
		};

		public enum TokenType
		{
			Lpa,			//　タイプ・左括弧
			Rpa,			//　タイプ・右括弧
			Comma,			//　タイプ・カンマ
			Unary,			//　タイプ・単項演算子
			Binary,			//　タイプ・二項演算子
			Substitution,	//　タイプ・代入演算子
			Number,			//　タイプ・値
			Value,			//　タイプ・変数
			Function,		//　タイプ・組み込み関数
		};

		/// <summary>
		/// 名前
		/// </summary>
		public string Name{get{return name;}}
		string name;

		bool isAlphabet;

		/// <summary>
		/// トークンのタイプ
		/// </summary>
		public TokenType Type{get{return type;}}
		TokenType type;

		/// <summary>
		/// 演算の優先順
		/// </summary>
		public int Priority { get { return priority; } }
		int priority;

		/// <summary>
		/// 値
		/// </summary>
		public object Variable { get { return variable; } set { variable = value; } }
		object variable;

		/// <summary>
		/// 関数の引数の数
		/// </summary>
		public int NumFunctionArg { get { return numFunctionArg; } }
		int numFunctionArg;

		public ExpressionToken(string name, bool isAlphabet, TokenType type, int priority, object variable )
		{
			Create(name, isAlphabet, type, priority, variable);
		}
		public ExpressionToken(string name, bool isAlphabet, TokenType type, int priority )
		{
			Create(name, isAlphabet, type, priority,null);
		}
		void Create(string name, bool isAlphabet, TokenType type, int priority, object variable)
		{
			this.name = name;
			this.isAlphabet = isAlphabet;
			this.type = type;
			this.priority = priority;
			this.variable = variable;
		}

		//区切り文字かチェック
		static public bool CheckSeparator(char c)
		{
			if (char.IsWhiteSpace(c) || c == ',') return true;

			switch (c)
			{
				case ',':
				case '+':
				case '-':
				case '*':
				case '%':
				case '/':
				case '>':
				case '<':
				case '&':
				case '|':
				case '!':
				case '=':
				case '(':
				case ')':
					return true;
				default:
					return false;
			}
		}

		//名前から演算子を検索
		static public ExpressionToken FindOperator(string exp, int index)
		{
			foreach (ExpressionToken operatior in OperatorArray)
			{
				if (operatior.isAlphabet) continue;
				if (operatior.name.Length > exp.Length - index) continue;

				if (exp.IndexOf(operatior.name, index, operatior.name.Length) == index)
				{
					return operatior;
				}
			}
			return null;
		}

		//名前からトークン作成
		static public ExpressionToken CreateToken(string name)
		{
			if (name.Length == 0)
			{
				Debug.LogError(" Token is enmpty");
			}

			int i;
			if (int.TryParse(name, out i))
			{
				//intとして追加
				return new ExpressionToken(name, false, ExpressionToken.TokenType.Number, 0, i);
			}
			float f;
			if (WrapperUnityVersion.TryParseFloatGlobal(name, out f))
			{
				//floatとして追加
				return new ExpressionToken(name, false, ExpressionToken.TokenType.Number, 0, f);
			}
			bool b;
			if (bool.TryParse(name, out b))
			{
				//boolとして追加
				return new ExpressionToken(name, false, ExpressionToken.TokenType.Number, 0, b);
			}
			string str;
			if (TryParseString(name, out str))
			{
				//stringとして追加
				return new ExpressionToken(name, false, ExpressionToken.TokenType.Number, 0, str);
			}

			ExpressionToken token;
			if (TryParseFunction(name,out token))
			{
				//関数として追加
				return token;
			}

			//変数として追加
			return new ExpressionToken(name, false, ExpressionToken.TokenType.Value, 0);
		}

		static bool TryParseString(string str, out string outStr)
		{
			outStr = "";
			if (string.IsNullOrEmpty(str)) return false;
			if (str.Length < 2) return false;
			if (str[0] == '"' && str[str.Length-1] == '"')
			{
				outStr = str.Substring(1, str.Length - 2);
				return true;
			}

			return false;
		}

		// 代入演算
		static public ExpressionToken OperateSubstition(ExpressionToken value1, ExpressionToken token, ExpressionToken value2, System.Func<string, object, bool> callbackSetValue)
		{
			value1.variable = CalcSubstition(value1.variable, token, value2.variable);
			//変数なので外部の代入処理
			if (value1.type == ExpressionToken.TokenType.Value )
			{
				if (!callbackSetValue(value1.name, value1.variable))
				{
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperateSubstition, token.name,value1.variable));
				}
			}
			return value1;
		}

		// 代入演算用の計算
		static object CalcSubstition(object value1, ExpressionToken token, object value2)
		{
			if (token.name == Eq) return value2;
			if( value1 is int )
			{
				if (value2 is int) return CalcSubstitionSub((int)value1,token,(int)value2);
				else if (value2 is float) return CalcSubstitionSub((int)value1, token, (float)value2);
				else if (value2 is string) return CalcSubstitionSub((int)value1, token, (string)value2);
			}
			else if( value1 is float )
			{
				if (value2 is int) return CalcSubstitionSub((float)value1, token, (int)value2);
				else if (value2 is float) return CalcSubstitionSub((float)value1, token, (float)value2);
				else if (value2 is string) return CalcSubstitionSub((float)value1, token, (string)value2);
			}
			else if (value1 is string)
			{
				if (value2 is int) return CalcSubstitionSub((string)value1, token, (int)value2);
				else if (value2 is float) return CalcSubstitionSub((string)value1, token, (float)value2);
				else if (value2 is string) return CalcSubstitionSub((string)value1, token, (string)value2);
			}
			throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
		}

		// 代入演算用の計算
		//(Genericで四足演算ができないので、コピペコード･･････)
		static object CalcSubstitionSub(int value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case PlusEq:	return (value1 + value2);
				case MinusEq:	return (value1 - value2);
				case ProdEq:	return (value1 * value2);
				case DivEq:		return (value1 / value2);
				case ModEq:		return (value1 % value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(int value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case PlusEq:	return (value1 + value2);
				case MinusEq:	return (value1 - value2);
				case ProdEq:	return (value1 * value2);
				case DivEq:		return (value1 / value2);
				case ModEq:		return (value1 % value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(float value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case PlusEq:	return (value1 + value2);
				case MinusEq:	return (value1 - value2);
				case ProdEq:	return (value1 * value2);
				case DivEq:		return (value1 / value2);
				case ModEq:		return (value1 % value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(float value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case PlusEq:	return (value1 + value2);
				case MinusEq:	return (value1 - value2);
				case ProdEq:	return (value1 * value2);
				case DivEq:		return (value1 / value2);
				case ModEq:		return (value1 % value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(string value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case PlusEq: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(string value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case PlusEq: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(string value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case PlusEq: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(int value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case PlusEq: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcSubstitionSub(float value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case PlusEq: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}

		// 単項演算
		static public ExpressionToken OperateUnary(ExpressionToken value, ExpressionToken token)
		{
			return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, CalcUnary(value.variable, token));
		}
		// 単項演算の計算
		static object CalcUnary(object value, ExpressionToken token)
		{
			switch (token.name)
			{
				case Not:		// !
					if (value is bool) return !(bool)value;
					else
					{
						throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
					}
				case Plus:		// +
					if (value is float) return value;
					else if (value is int) return value;
					else
					{
						throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
					}
				case Minus:		// -
					if (value is float) return -(float)value;
					else if (value is int) return -(int)value;
					else
					{
						throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
					}
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}

		// 二項演算
		static public ExpressionToken OperateBinary(ExpressionToken value1, ExpressionToken token, ExpressionToken value2)
		{
			return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, CalcBinary(value1.variable, token, value2.variable));
		}

		// 二項演算の計算
		static object CalcBinary(object value1, ExpressionToken token, object value2)
		{
			switch (token.name)
			{
				case Prod:		// *
				case Div:		// /
				case Mod:		// %
				case Plus:		// +
				case Minus:		// -
				case Greater:	// >
				case Less:		// <
				case GreaterEq:	// >=
				case LessEq:	// <=
					return CalcBinaryNumber(value1, token, value2);
				case EqEq:		// ==
				case NotEq:		// !=
					return CalcBinaryEq(value1, token, value2);
				case And:		// &&
				case Or:		// ||
					return CalcBinaryAndOr(value1, token, value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}

		// 二項演算の計算(int,floatなどの数値計算・比較)
		static object CalcBinaryNumber(object value1, ExpressionToken token, object value2)
		{
			if (value1 is int)
			{
				if (value2 is int) return CalcBinaryNumberSub((int)value1, token, (int)value2);
				else if (value2 is float) return CalcBinaryNumberSub((int)value1, token, (float)value2);
				else if (value2 is string) return CalcBinaryNumberSub((int)value1, token, (string)value2);
			}
			else if (value1 is float)
			{
				if (value2 is int) return CalcBinaryNumberSub((float)value1, token, (int)value2);
				else if (value2 is float) return CalcBinaryNumberSub((float)value1, token, (float)value2);
				else if (value2 is string) return CalcBinaryNumberSub((float)value1, token, (string)value2);
			}
			else if (value1 is string)
			{
				if (value2 is int) return CalcBinaryNumberSub((string)value1, token, (int)value2);
				else if (value2 is float) return CalcBinaryNumberSub((string)value1, token, (float)value2);
				else if (value2 is string) return CalcBinaryNumberSub((string)value1, token, (string)value2);
			}
			throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
		}
		// 二項演算の計算(int,floatなどの数値計算・比較)
		//(Genericで四足演算ができないので、コピペコード･･････)
		static object CalcBinaryNumberSub(int value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case Prod:		return (value1 * value2);
				case Div:		return (value1 / value2);
				case Mod:		return (value1 % value2);
				case Plus:		return (value1 + value2);
				case Minus:		return (value1 - value2);
				case Greater:	return (value1 > value2);
				case Less:		return (value1 < value2);
				case GreaterEq:	return (value1 >= value2);
				case LessEq:	return (value1 <= value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(int value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case Prod: return (value1 * value2);
				case Div: return (value1 / value2);
				case Mod: return (value1 % value2);
				case Plus: return (value1 + value2);
				case Minus: return (value1 - value2);
				case Greater: return (value1 > value2);
				case Less: return (value1 < value2);
				case GreaterEq: return (value1 >= value2);
				case LessEq: return (value1 <= value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(float value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case Prod: return (value1 * value2);
				case Div: return (value1 / value2);
				case Mod: return (value1 % value2);
				case Plus: return (value1 + value2);
				case Minus: return (value1 - value2);
				case Greater: return (value1 > value2);
				case Less: return (value1 < value2);
				case GreaterEq: return (value1 >= value2);
				case LessEq: return (value1 <= value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(float value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case Prod: return (value1 * value2);
				case Div: return (value1 / value2);
				case Mod: return (value1 % value2);
				case Plus: return (value1 + value2);
				case Minus: return (value1 - value2);
				case Greater: return (value1 > value2);
				case Less: return (value1 < value2);
				case GreaterEq: return (value1 >= value2);
				case LessEq: return (value1 <= value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(string value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(string value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(string value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(string value1, ExpressionToken token, bool value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(float value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(int value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryNumberSub(bool value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case Plus: return (value1 + value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}


		// 二項演算の計算(==や!=などの比較演算)
		static object CalcBinaryEq(object value1, ExpressionToken token, object value2)
		{
			if (value1 is int)
			{
				if (value2 is int) return CalcBinaryEqSub((int)value1, token, (int)value2);
				else if (value2 is float) return CalcBinaryEqSub((int)value1, token, (float)value2);
			}
			else if (value1 is float)
			{
				if (value2 is int) return CalcBinaryEqSub((float)value1, token, (int)value2);
				else if (value2 is float) return CalcBinaryEqSub((float)value1, token, (float)value2);
			}
			else if (value1 is bool)
			{
				if (value2 is bool) return CalcBinaryEqSub((bool)value1, token, (bool)value2);
			}
			else if (value1 is string)
			{
				if (value2 is string) return CalcBinaryEqSub((string)value1, token, (string)value2);
			}
			throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
		}
		// 二項演算の計算(int,floatなどの数値計算・比較)
		//(Genericで四足演算ができないので、コピペコード･･････)
		static object CalcBinaryEqSub(int value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case EqEq:	return (value1 == value2);
				case NotEq: return (value1 != value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryEqSub(int value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case EqEq: return (value1 == value2);
				case NotEq: return (value1 != value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryEqSub(float value1, ExpressionToken token, int value2)
		{
			switch (token.name)
			{
				case EqEq: return (value1 == value2);
				case NotEq: return (value1 != value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryEqSub(float value1, ExpressionToken token, float value2)
		{
			switch (token.name)
			{
				case EqEq: return (value1 == value2);
				case NotEq: return (value1 != value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryEqSub(bool value1, ExpressionToken token, bool value2)
		{
			switch (token.name)
			{
				case EqEq: return (value1 == value2);
				case NotEq: return (value1 != value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}
		static object CalcBinaryEqSub(string value1, ExpressionToken token, string value2)
		{
			switch (token.name)
			{
				case EqEq: return (value1 == value2);
				case NotEq: return (value1 != value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}


		// 二項演算の計算(&&や||などの論理式)
		static object CalcBinaryAndOr(object value1, ExpressionToken token, object value2)
		{
			if (value1 is bool)
			{
				if (value2 is bool) return CalcBinaryAndOrSub((bool)value1, token, (bool)value2);
			}
			throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
		}
		// 二項演算の計算(&&や||などの論理式)
		static object CalcBinaryAndOrSub(bool value1, ExpressionToken token, bool value2)
		{
			switch (token.name)
			{
				case And: return (value1 && value2);
				case Or: return (value1 || value2);
				default:
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpressionOperator, token.name));
			}
		}


		const string FuncRandom = "Random";
		const string FuncRandomF = "RandomF";
		const string FuncCeil = "Ceil";
		const string FuncCeilToInt = "CeilToInt";
		const string FuncFloor = "Floor";
		const string FuncFloorToInt = "FloorToInt";
		//関数名であればトークン作成
		static bool TryParseFunction(string name, out ExpressionToken token)
		{
			switch (name)
			{
				case FuncRandom:
				case FuncRandomF:
					token = new ExpressionToken(name, false, ExpressionToken.TokenType.Function, 0);
					token.numFunctionArg = 2;
					return true;
				case FuncCeil:
				case FuncCeilToInt:
				case FuncFloor:
				case FuncFloorToInt:
					token = new ExpressionToken(name, false, ExpressionToken.TokenType.Function, 0);
					token.numFunctionArg = 1;
					return true;
				default:
					token = null;
					return false;
			}
		}
		
		//　関数演算
		static public ExpressionToken OperateFunction(ExpressionToken token, ExpressionToken[] args)
		{
			switch (token.name)
			{
				case FuncRandom:
					{
						int random = UnityEngine.Random.Range(ExpressionCast.ToInt(args[0].variable), ExpressionCast.ToInt(args[1].variable)+1);
						return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, random);
					}
				case FuncRandomF:
					{
						float random = UnityEngine.Random.Range(ExpressionCast.ToFloat(args[0].variable), ExpressionCast.ToFloat(args[1].variable));
						return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, random);
					}
				case FuncCeil:
					{
						return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, Mathf.Ceil(ExpressionCast.ToFloat(args[0].variable)));
					}
				case FuncCeilToInt:
					{
						return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, Mathf.CeilToInt(ExpressionCast.ToFloat(args[0].variable)));
					}
				case FuncFloor:
					{
						return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, Mathf.Floor(ExpressionCast.ToFloat(args[0].variable)));
					}
				case FuncFloorToInt:
					{
						return new ExpressionToken("", false, ExpressionToken.TokenType.Number, 0, Mathf.FloorToInt(ExpressionCast.ToFloat(args[0].variable)));
					}
				default:
					throw new Exception("Unkonw Function :" + token.name);
			}
		}

		public bool IsValueType
		{
			get
			{
				switch (Type)
				{
					case TokenType.Number:
					case TokenType.Value:
						return true;
					default:
						return false;
				}
			}
		}
	};
}