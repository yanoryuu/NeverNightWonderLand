// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
namespace Utage
{
	/// メッセージウィドウに対しての表示文字数チェック処理（文字溢れがないかなど）のための共通インターフェース
	public interface IAdvMessageWindowCaracterCountChecker
	{
		GameObject gameObject { get; }

		//表示文字数チェック開始（今設定されているテキストを返す）
		string StartCheckCaracterCount();

		//指定テキストに対する表示文字数チェック
		bool TryCheckCaracterCount(string text, out int count, out string errorString);

		//Startで設定されていたテキストに戻す
		void EndCheckCaracterCount(string text);
	}
}
