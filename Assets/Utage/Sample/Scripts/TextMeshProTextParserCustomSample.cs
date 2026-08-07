using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;

namespace Utage
{
	//宴のTextMeshPro対応のタグ解析をさらにカスタムするクラス
	public class TextMeshProTextParserCustomSample : TextMeshProTextParser
	{
		public TextMeshProTextParserCustomSample(string text, bool isParseParamOnly = false)
			: base(text, isParseParamOnly)
		{
		}
		//タグの解析。解析方法によってここをoverrideして処理
		protected override bool TryParseTag(int index, out int endIndex, out string tagName, out string tagArg)
		{
			bool result = base.TryParseTag(index, out endIndex, out tagName, out tagArg);
			if (result) return true;

			string name0 ="";
			string arg0 = "";
			
			//タグの区切り記号として { と } を追加する処理
			endIndex = ParserUtil.ParseTag(
				'{', '}',ParserUtil.DefaultTagSeparator,
				OriginalText, index, 
				(name,arg)=>
				{
					bool ret = ParseTag(name, arg);
					if (ret)
					{
						name0 = name;
						arg0 = arg;
					}
					return ret;
				});
			tagName =name0;
			tagArg = arg0;
			return endIndex != index;
		}
		
		//カスタムタグ
		static readonly List<string> CustomTagTbl = new List<string>()
		{
			"offset","/offset",
		};

		//特に処理の必要のない基本タグか
		protected override bool IsDefaultTag(string name)
		{
			//特に処理の必要のないTextMeshProの基本タグ
			if (base.IsDefaultTag(name))
			{
				return true;
			}
			//特に処理の必要のないカスタムタグ
			if (CustomTagTbl.Contains(name))
			{
				return true;
			}
			return false;
		}
	}
}
