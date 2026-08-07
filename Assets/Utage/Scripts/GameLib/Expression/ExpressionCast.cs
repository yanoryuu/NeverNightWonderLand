// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 文字列→数式の際に、object型をキャストするための処理
	/// </summary>	

	public class ExpressionCast
	{
		//intにキャストする
		public static int ToInt(object value)
		{
			if (value.GetType() == typeof(int)) return (int)value;
			else if (value.GetType() == typeof(float)) return (int)((float)value);
			else
			{
				throw new Exception("Cant cast :" + value.GetType() + " ToInt");
			}
		}
		//floatにキャストする
		public static float ToFloat(object value)
		{
			if (value.GetType() == typeof(float)) return (float)value;
			else if (value.GetType() == typeof(int)) return (float)((int)value);
			else
			{
				throw new Exception("Cant cast :" + value.GetType() + " ToFloat");
			}
		}
	}
}
