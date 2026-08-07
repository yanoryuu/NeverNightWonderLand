using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;

namespace Utage
{
	//宴のTextMeshPro対応をさらに独自カスタム
	public class UtageForTextMeshProCustomSample : MonoBehaviour
	{
		void Awake()
		{
			//テキスト解析処理をカスタム
			TextData.CreateCustomTextParser = CreateCustomTextParser;
			//ログテキスト作成をカスタム
			TextData.MakeCustomLogText = MakeCustomLogText;
		}

		void OnDestroy()
		{
			TextData.CreateCustomTextParser = null;
			TextData.MakeCustomLogText = null;
		}

		//テキスト解析をカスタム
		TextParser CreateCustomTextParser(string text)
		{
			//独自カスタムした
			return new TextMeshProTextParserCustomSample(text);
		}

		//ログテキスト作成をカスタム
		string MakeCustomLogText(string text)
		{
			return new TextMeshProTextParserCustomSample(text,true).NoneMetaString;
		}
	}
}
