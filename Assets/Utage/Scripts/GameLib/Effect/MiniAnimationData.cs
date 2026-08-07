// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//主にパラパラアニメを想定した、ちょっとしたアニメーションのためのデータ
	[System.Serializable]
	public class MiniAnimationData
	{
		[System.Serializable]
		public class Data
		{
			//表示時間
			public float Duration { get { return duration; } }
			[SerializeField]
			float duration;


			//名前
			[SerializeField]
			string name;


			public Data(float duration, string name)
			{
				this.duration = duration;
				this.name = name;
			}

			//命名規則などを踏まえた名前を
			public string ComvertName(string originalName)
			{
				switch (ParseNamigType())
				{
					case NamingType.Suffix:
						return originalName + GetSffixName();
					case NamingType.Swap:
						return GetSwapName(originalName);
					default:
						return name;
				}
			}

			//命名規則などを踏まえた名前を
			public string ComvertNameSimple()
			{
				switch (ParseNamigType())
				{
					case NamingType.Suffix:
						return GetSffixName();
					default:
						return name;
				}
			}

			//サフィックス（接尾辞）としての名前を取得
			public string GetSffixName()
			{
				return name.Substring(1);
			}

			//()で囲まれた部分をスワップ
			public string GetSwapName(string originalName)
			{
				if (originalName.Length < 2) return originalName;
				int index = originalName.IndexOf('(');
				if (index < 0) return originalName;

				return originalName.Substring(0, index) + name.Substring(1);
			}

			NamingType ParseNamigType()
			{
				if (name.Length < 2) return NamingType.Default;

				if (name[0] != '*') return NamingType.Default;

				if (name[1] == '(' && name[name.Length-1] == ')') return NamingType.Swap;
				return NamingType.Suffix;
			}
			enum NamingType
			{
				Default,
				Suffix,
				Swap,
			}
		};

		public List<Data> DataList { get { return dataList; } }
		[SerializeField]
		List<Data> dataList = new List<Data>();

		internal bool TryParse(StringGridRow row, int index)
		{
			try
			{
				DataList.Clear();
				while (index+1 < row.Strings.Length)
				{
					if (row.IsEmptyCell(index) && row.IsEmptyCell(index + 1))
					{
						break;
					}
					string str = row.ParseCell<string>(index++);
					float time = row.ParseCell<float>(index++);
					DataList.Add(new Data(time, str));
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}