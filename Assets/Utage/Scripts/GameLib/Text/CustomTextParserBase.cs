using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using UtageExtensions;

namespace Utage
{
	//宴のタグ構文解析とTextMeshProのタグ構文解析をして、
	//TextMeshPro形式のタグ付き文字列を生成するためのクラス
	public abstract class CustomTextParserBase : TextParser
	{
		public bool NoParse { get; set; }
		protected CustomTextParserBase(string text, bool isParseParamOnly = false)
			: base(text, isParseParamOnly)
		{
		}

		//TextMeshPro対応のタグ解析
		protected override bool ParseNovelTag(string name, string arg)
		{
			if (string.IsNullOrEmpty(name)) return false;
			
			//タグの解析を無視している最中
			if (NoParse)
			{
				//タグの解析無視終了
				if (IsNoParseTag(name))
				{
					NoParse = false;
					return true;
				}
				return false;
			}
			
			if(TryParseCustomTag(name, arg))
			{
				return true;
			}

			//宴独自のタグの解析
			return base.ParseNovelTag(name, arg);
		}

		protected abstract bool IsNoParseTag(string name);
		protected abstract bool TryParseCustomTag(string name, string arg);
	}
}
