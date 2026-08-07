// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using System.Collections.Generic;


namespace Utage
{
	/// <summary>
	/// 最小値と最大値を設定する
	/// </summary>
	[System.Serializable]
	public class MinMax<T>
	{
		public T Min
		{
			get { return min; }
			set { min = value; }
		}

		[SerializeField] T min;

		public T Max
		{
			get { return max; }
			set { max = value; }
		}

		[SerializeField] T max;
	}

	[System.Serializable]
	public class MinMaxFloat : MinMax<float>
	{
		public float RandomRange()
		{
			return Random.Range(Min, Max);
		}

		public float Clamp(float value)
		{
			return Mathf.Clamp(value, Min, Max);
		}

		public bool IsInnner(float v)
		{
			return (v >= Min) && (v <= Max);
		}

		public bool IsOver(float v)
		{
			return (v < Min) || (v > Max);
		}
	}


	[System.Serializable]
	public class MinMaxInt : MinMax<int>
	{
		public int RandomRange()
		{
			return Random.Range(Min, Max + 1);
		}

		public int Clamp(int value)
		{
			return Mathf.Clamp(value, Min, Max);
		}

		public bool IsInnner(int v)
		{
			return (v >= Min) && (v <= Max);
		}

		public bool IsOver(int v)
		{
			return (v < Min) || (v > Max);
		}
	}


}
