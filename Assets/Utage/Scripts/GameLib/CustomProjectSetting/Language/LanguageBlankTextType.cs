// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	// 表示言語切り替え用のクラス
	public enum LanguageBlankTextType
	{
		SwapDefaultLanguage,	//データがない場合でも基本言語に切り替え
		NoBlankText,			//データがない場合は想定しない。エラーを出す。
		AllowBlankText,			//データがない場合は想定しないが、空テキストを許す。
	}
}
