using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;

namespace Utage
{
	//TextMeshProに、自動インデントをするための設定
	[Serializable]
	public class TextMeshProAutoIndentSettings
	{
		[Serializable]
		public class IndentData
		{
			//字下げの対象になる先頭文字列
			public string Prefix => prefix;
			[SerializeField] string prefix = "「";
			
			//字下げする際のタグ
			public string IndentTag => indentTag;
			[SerializeField] string indentTag = "<indent=1.0em>";
		}
		
		public List<IndentData> IndentDataList => indentDataList;
		[SerializeField] List<IndentData> indentDataList = new ();

		public bool EnableSettings => IndentDataList.Count > 0;
	}
}
