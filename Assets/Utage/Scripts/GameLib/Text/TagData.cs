// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	//構文解析した際のタグデータ
	//TextMeshProのタグを使う場合などに、いったんこちらの中間データにしてから
	//TextMeshProのタグなどを生成したり、宴の独自のタグのTextMeshProのタグに変換する
	public class TagData : IParsedTextData
	{
		public string TagString { get; private set; }
		public string TagName { get; private set; }
		public string TagArg { get; private set; }
		public bool IgnoreTagString { get; private set; }
		
		//タグによって表示される文字数（絵文字などに使用）
		public int CountCharacter { get; set; } = 0;

		public TagData(string str, string name, string arg)
		{
			TagString = str;
			TagName = name;
			TagArg = arg;
		}
		public TagData(string str, string name, string arg, bool ignoreTagString)
			: this(str, name, arg)
		{
			this.IgnoreTagString = ignoreTagString;
		}
	};
}
