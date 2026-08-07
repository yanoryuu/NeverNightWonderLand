// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 文字列→数式のための解析用クラス
	/// </summary>	

	public class ExpressionParser
	{
		/// <summary>
		/// 元の数式
		/// </summary>
		public string Exp { get { return this.exp; } }
		string exp;

		/// <summary>
		/// エラーメッセージ
		/// </summary>
		public string ErrorMsg { get { return this.errorMsg; } }
		string errorMsg = null;
		void AddErrorMsg(string msg)
		{
			if (string.IsNullOrEmpty(errorMsg)) errorMsg = "";
			else errorMsg += "\n";

			errorMsg += msg;
		}

		//数式から解析したトークン
		List<ExpressionToken> tokens;

		/// <summary>
		/// 数式の文字列から数式を作成
		/// 変数名があるかのチェック関数を使い、数式にエラーがある場合は
		/// ErrorMsgにエラーがでるので、それをチェックすること
		/// </summary>
		/// <param name="exp">数式の文字列</param>
		/// <param name="callbackGetValue">変数名から数値を取得するためのコールバック</param>
		/// <param name="callbackCheckSetValue">変数名の数値を代入するチェックをするためのコールバック</param>
		/// <param name="isBoolean">論理式の場合のみtrueを設定。エラーチェックに使う</param>
		public ExpressionParser(string exp, System.Func<string, object> callbackGetValue, System.Func<string, object, bool> callbackCheckSetValue, bool isBoolean )
		{
			Create(exp, callbackGetValue, callbackCheckSetValue, isBoolean);
		}
		public ExpressionParser(string exp, System.Func<string, object> callbackGetValue, System.Func<string, object, bool> callbackCheckSetValue )
		{
			Create(exp, callbackGetValue, callbackCheckSetValue, false);
		}
		void Create(string exp, System.Func<string, object> callbackGetValue, System.Func<string, object, bool> callbackCheckSetValue, bool isBoolean )
		{
			this.exp = exp;
			//逆ポーランド式へ変換
			tokens = ToReversePolishNotation(exp);
			if (string.IsNullOrEmpty(ErrorMsg))
			{
				//計算してみてエラーがないかチェック
				if (isBoolean)
				{
					CalcExpBoolean(callbackGetValue, callbackCheckSetValue);
				}
				else
				{
					CalcExp(callbackGetValue, callbackCheckSetValue);
				}
			}
		}

		/// <summary>
		/// 数式の文字列を解析して、計算結果を返す
		/// </summary>
		/// <param name="exp">数式の文字列</param>
		/// <param name="callbackGetValue">変数名から数値を取得するためのコールバック</param>
		/// <param name="callbackSetValue">変数名の数値を代入するためのコールバック</param>
		/// <returns>計算結果</returns>
		public object CalcExp(System.Func<string, object> callbackGetValue, System.Func<string, object, bool> callbackSetValue)
		{
			bool isError = false;
			//変数を値に変換
			foreach (ExpressionToken token in tokens)
			{
				if (token.Type == ExpressionToken.TokenType.Value)
				{
					object obj = callbackGetValue(token.Name);
					if (obj == null)
					{
						AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpUnknownParameter, token.Name));
						isError = true;
					}
					else
					{
						token.Variable = obj;
					}
				}
			}
			if (!isError)
			{
				//計算
				object ret = Calc(callbackSetValue);
				return ret;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 数式の文字列を解析して、計算結果を返す
		/// </summary>
		/// <param name="exp">数式の文字列</param>
		/// <param name="callbackGetValue">変数名から数値を取得するためのコールバック</param>
		/// <param name="callbackSetValue">変数名の数値を代入するためのコールバック</param>
		/// <returns>計算結果</returns>
		public bool CalcExpBoolean(System.Func<string, object> callbackGetValue, System.Func<string, object, bool> callbackSetValue)
		{
			object obj = CalcExp(callbackGetValue, callbackSetValue);
			if (obj != null)
			{
				if (obj.GetType() == typeof(bool))
				{
					return (bool)obj;
				}
			}
			AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpResultNotBool));
			return false;
		}

		// 演算式の結果を計算
		object Calc(System.Func<string, object, bool> callbackSetValue )
		{
			try
			{
				///逆ポーランド式の演算
				Stack<ExpressionToken> values = new Stack<ExpressionToken>();
				ExpressionToken value1;
				ExpressionToken value2;
				foreach (ExpressionToken token in tokens)
				{
					switch (token.Type)
					{
						case ExpressionToken.TokenType.Substitution:	//代入演算
							value2 = values.Pop();
							value1 = values.Pop();
							values.Push(ExpressionToken.OperateSubstition(value1, token, value2, callbackSetValue));
							break;
						case ExpressionToken.TokenType.Unary:			//単項演算
							values.Push(ExpressionToken.OperateUnary(values.Pop(), token));
							break;
						case ExpressionToken.TokenType.Binary:			//二項演算
							value2 = values.Pop();
							value1 = values.Pop();
							values.Push(ExpressionToken.OperateBinary(value1, token, value2));
							break;
						case ExpressionToken.TokenType.Number:
						case ExpressionToken.TokenType.Value:
							values.Push(token);
							break;
						case ExpressionToken.TokenType.Function:		//関数
							{
								int num = token.NumFunctionArg;
								ExpressionToken[] args = new ExpressionToken[num];
								for (int i = 0; i < num; ++i)
								{
									args[num-i-1] = values.Pop();
								}
								values.Push(ExpressionToken.OperateFunction(token, args));
							}
							break;
						default:
							break;
					}
				}
				if (values.Count != 1)
				{
					throw new Exception(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpIllegal));
				}
				return values.Peek().Variable;
			}
			catch(Exception e)
			{
				Debug.LogError(e.Message + e.StackTrace );
				AddErrorMsg(e.Message);
				return null;
			}
		}

		// 式を逆ポーランド記法に変換
		List<ExpressionToken> ToReversePolishNotation(string exp)
		{
			List<ExpressionToken> tokens = SplitToken(exp);	//式を演算要素別に分解
			if (!CheckTokenCount(tokens))
			{
				AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.ExpIllegal));
			}
			return ToReversePolishNotationSub(tokens);	//逆ポーランド記法に変換
		}

		// 式を演算要素別に分解
		static List<ExpressionToken> SplitToken(string exp)
		{
			List<ExpressionToken> tokens = new List<ExpressionToken>();  //演算式

			tokens.Add(ExpressionToken.LpaToken);
			int index = 0;
			string strToken = "";			//直前の文字列
			while (index < exp.Length)
			{
				char c = exp[index];

				bool isSkipped=false;
				const char StringSeparator = '\"';
				const char ArraySeparator0 = '[';
				const char ArraySeparator1 = ']';
				switch(c)
				{
					case StringSeparator:
						SkipGroup(StringSeparator, StringSeparator, ref strToken, exp, ref index);
						isSkipped = true;
						tokens.Add(ExpressionToken.CreateToken(strToken));
						strToken = "";
						break;
					case ArraySeparator0:
						SkipGroup(ArraySeparator0, ArraySeparator1, ref strToken, exp, ref index);
						isSkipped = true;
						break;
					default:
						break;
				}
				if (isSkipped)
				{
					continue;
				}

				if (char.IsWhiteSpace(c))
				{
					//空白・区切り文字なので、直前の文字列をトークンとして分割
					if (!string.IsNullOrEmpty(strToken))
					{
						tokens.Add(ExpressionToken.CreateToken(strToken));
					}
					strToken = "";
					index++;
					continue;
				}

				//演算子が来たかチェック
				ExpressionToken operatior = ExpressionToken.FindOperator(exp, index);
				if (operatior == null)
				{
					//演算子はなかった。一文字シフト
					strToken += c;
					index++;
				}
				else
				{
					//演算子発見・直前の文字列をトークンとして分割
					if (!string.IsNullOrEmpty(strToken))
					{
						var token = ExpressionToken.CreateToken(strToken);
						tokens.Add(token);
					}

					bool isValueLastToken = tokens.Count > 0 && tokens[tokens.Count - 1].IsValueType;
					//演算子をトークンとして追加
					if (!isValueLastToken && operatior.Name == ExpressionToken.Minus)
					{
						//単項演算子のマイナスとして登録（二項演算子ではなく）
						tokens.Add(ExpressionToken.UniMinus);
					}
					else if (!isValueLastToken && operatior.Name == ExpressionToken.Plus)
					{
						//単項演算子のプラスとして登録（二項演算子ではなく）
						tokens.Add(ExpressionToken.UniPlus);
					}
					else
					{
						//見つかった演算子を追加
						tokens.Add(operatior);
					}
					strToken = "";
					index += operatior.Name.Length;
				}
			}
			if (!string.IsNullOrEmpty(strToken))
			{
				tokens.Add(ExpressionToken.CreateToken(strToken));
			}
			tokens.Add(ExpressionToken.RpaToken);
			return tokens;
		}

		static bool SkipGroup(char begin, char end, ref string strToken, string exp, ref int index)
		{
			strToken += begin;
			index++;
			while (index < exp.Length)
			{
				char c = exp[index];
				//区切り文字がくるまでシフト
				if (c != end)
				{
					strToken += c;
				}
				else
				{
					if (strToken[strToken.Length - 1] == '\\')
					{
						//区切り文字だけど、直前に\がある
						strToken = strToken.Remove(strToken.Length-1) + c;
					}
					else
					{
						strToken += c;
						index++;
						return true;
					}
				}
				index++;
			}
			return false;
		}

		//空白文字による分割、ただし文字列定義中であれば例外
		static bool CheckStringSeparate(char c, string strToken)
		{
			if (strToken.Length > 0 && strToken[0] == '\"')
			{
				return false;
			}
			else
			{
				return true;
			}
		}



		///演算可能な要素数が矛盾がないかチェック
		bool CheckTokenCount(List<ExpressionToken> tokenArray)
		{
			int expCount = 0;
			foreach (ExpressionToken token in tokenArray)
			{
				switch (token.Type)
				{
					case ExpressionToken.TokenType.Binary:
					case ExpressionToken.TokenType.Substitution:
						expCount--;
						break;
					case ExpressionToken.TokenType.Value:
					case ExpressionToken.TokenType.Number:
						expCount++;
						break;
					case ExpressionToken.TokenType.Function:
						expCount += (1-token.NumFunctionArg);
						break;
					default:
						break;
				}
			}
			if (expCount != 1)
			{
				Debug.LogError(expCount);
			}
			return (expCount == 1);
		}

		//逆ポーランド記法に変換
		List<ExpressionToken> ToReversePolishNotationSub(List<ExpressionToken> tokens)
		{
			List<ExpressionToken> expList = new List<ExpressionToken>();  //返還後のリスト
			//式を逆ポーランド記法に変換
			Stack<ExpressionToken> tmpStack = new Stack<ExpressionToken>();  //演算子用のスタック
			foreach (ExpressionToken token in tokens)
			{
				try
				{
					switch (token.Type)
					{
						case ExpressionToken.TokenType.Lpa:	//左括弧
							tmpStack.Push(token);
							break;
						case ExpressionToken.TokenType.Rpa:	//右括弧
							{
								while (tmpStack.Count != 0)
								{
									ExpressionToken last = tmpStack.Peek();
									if (ExpressionToken.TokenType.Lpa == last.Type)
									{
										tmpStack.Pop();
										break;
									}
									expList.Add(tmpStack.Pop());
								}
							}
							break;
						case ExpressionToken.TokenType.Binary:	//演算子
						case ExpressionToken.TokenType.Unary:
						case ExpressionToken.TokenType.Substitution:
						case ExpressionToken.TokenType.Function:
							{
								ExpressionToken last = tmpStack.Peek();
								while (tmpStack.Count != 0 && (token.Priority > last.Priority))
								{
									if (ExpressionToken.TokenType.Lpa == last.Type)
									{
										break;
									}
									expList.Add(last);
									tmpStack.Pop();
									last = tmpStack.Peek();
								}
								tmpStack.Push(token);
							}
							break;
						case ExpressionToken.TokenType.Number:	//変数
						case ExpressionToken.TokenType.Value:	//値
							expList.Add(token);
							break;
						case ExpressionToken.TokenType.Comma:	//カンマ
							// スタックのトップのトークンが左括弧になるまで
							// スタック上の演算子を出力キューにポップし続ける
							while (true)
							{
								ExpressionToken last = tmpStack.Peek();
								if (ExpressionToken.TokenType.Lpa == last.Type)
								{
									break;
								}
								expList.Add(tmpStack.Pop());
							}
							break;
						default:
							AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.UnknownType,token.Type.ToString()));
							break;
					}
				}
				catch (System.Exception e)
				{
					AddErrorMsg(e.ToString());
				}
			}
			return expList;
		}
	}
}
