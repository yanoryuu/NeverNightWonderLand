// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// if分岐のデータクラス
	/// </summary>
	internal class AdvIfData
	{
		/// <summary>
		/// 入れ子の親
		/// </summary>
		public AdvIfData Parent { get; private set; }

		/// <summary>
		/// スキップ中（条件判定がfalse）か
		/// </summary>
		public bool IsSkpping { get { return isSkpping; } set { isSkpping = value; } }
		bool isSkpping = false;         //スキップ中か

		//親のIfがスキップ済みかどうかチェック
		public bool IsParantSkipping
		{
			get
			{
				if (Parent == null) return false;

				return Parent.IsSkpping || Parent.IsParantSkipping;
			}
		}

		//if文がtrueになったか
		public bool IsIf { get; set; }

		internal AdvIfData(AdvIfData parent)
		{
			Parent = parent;
		}

		/// <summary>
		/// if文の開始
		/// </summary>
		/// <param name="param">判定に使う数値パラメーター</param>
		/// <param name="exp">判定式</param>
		internal void BeginIf(AdvParamManager param, ExpressionParser exp)
		{
			IsIf = param.CalcExpressionBoolean(exp);
			isSkpping = !IsIf;
		}

		/// <summary>
		/// else if文の開始
		/// </summary>
		/// <param name="param">判定に使う数値パラメーター</param>
		/// <param name="exp">判定式</param>
		internal void ElseIf(AdvParamManager param, ExpressionParser exp)
		{
			if (!IsIf)
			{
				IsIf = param.CalcExpressionBoolean(exp);
				isSkpping = !IsIf;
			}
			else
			{
				isSkpping = true;
			}
		}

		/// <summary>
		/// else文の開始
		/// </summary>
		internal void Else()
		{
			if (!IsIf)
			{
				IsIf = true;
				isSkpping = false;
			}
			else
			{
				isSkpping = true;
			}
		}

		/// <summary>
		/// if系処理の終了
		/// </summary>
		internal void EndIf()
		{
			isSkpping = false;
		}
	};
}