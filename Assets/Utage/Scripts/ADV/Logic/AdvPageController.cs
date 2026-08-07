// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	public enum AdvPageControllerType
	{
		InputBrPage,    //入力待ち後、改ページ
		KeepText,		//テキストを表示したままに
		Next,			//次のコマンドを
		Input,			//入力待ち
		Br,				//次のコマンド実行後、改行して次のテキスト表示
		InputBr,        //入力待ちと次のコマンド実行後、改行して次のテキスト表示
		BrPage,			//自動で改ページして次のコマンド		
	};

	/// <summary>
	/// ページ制御
	/// </summary>
	public class AdvPageController
	{
		//ページの末端か
		public static bool IsPageEndType( AdvPageControllerType type )
		{
			switch (type)
			{
				case AdvPageControllerType.InputBrPage:
				case AdvPageControllerType.BrPage:
					return true;
				default:
					return false;
			}
		}

		//入力待ちするか
		public static bool IsWaitInputType(AdvPageControllerType type)
		{
			switch (type)
			{
				case AdvPageControllerType.InputBrPage:
				case AdvPageControllerType.Input:
				case AdvPageControllerType.InputBr:
					return true;
				default:
					return false;
			}
		}

		//次のテキストを改行するか
		public static bool IsBrType(AdvPageControllerType type)
		{
			switch (type)
			{
				case AdvPageControllerType.Br:
				case AdvPageControllerType.InputBr:
					return true;
				default:
					return false;
			}
		}

		//テキスト表示を続けたままにするか
		public bool IsKeepText { get; private set; }

		//入力待ちする
		public bool IsWaitInput{ get; private set; }

		//次のテキストは改行して始める
		public bool IsBr { get; private set; }

		//ページコントロールフラグの更新
		public void Update(AdvPageControllerType type)
		{
			IsKeepText = !IsPageEndType(type);
			IsWaitInput = IsWaitInputType(type);
			IsBr = IsBrType(type);
		}

		public void Clear()
		{
			IsKeepText = false;
			IsWaitInput = false;
			IsBr = false;
		}
	}
}
