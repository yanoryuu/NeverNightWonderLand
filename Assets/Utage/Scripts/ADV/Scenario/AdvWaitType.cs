// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace Utage
{
	//コマンド待機のタイプ
	public enum AdvCommandWaitType
	{
		Default,        	//エフェクトコマンド自体を即座に終了待ちをする（自分自身と、Addで設定したコマンドの終了待ちをする）
		PageWait,           //改ページ後に、エフェクト終了待ちをする
		InputWait,          //クリック入力後に、エフェクト終了待ちをする
		Add,                //このコマンドではエフェクト終了待ちをしないが、Waitが設定されたエフェクトコマンドか、改ページのタイミングで終了待ちをする
		NoWait,             //エフェクトの終了待ちをしない
		Skippable,         	//スキップ可能なDefault
		PageWaitSkippable,  //スキップ可能なPageWait。
		InputWaitSkippable, //スキップ可能なInputWait。
		AddSkippable,       //スキップ可能なAdd
		SkippableOnWaitThread,//スレッドの終了待ち状態のときだけスキップ可能
		SkipOnInput,		//入力待ちの終わりでスキップさせて終了
		SkipOnBrPage,		//改ページ時にスキップさせて終了
	};

	public static class AdvCommandWaitTypeExtensions
	{
		//入力ウェイト中にスキップできるか
		public static bool IsSkippableInput(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.Skippable:
				case AdvCommandWaitType.InputWaitSkippable:
				case AdvCommandWaitType.AddSkippable:
					return true;
				default:
					return false;
			}
		}

		//コマンドウェイト中にスキップできるか
		public static bool IsSkippableCommand(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.Skippable:
				case AdvCommandWaitType.AddSkippable:
					return true;
				default:
					return false;
			}
		}

		//スレッド待ち中のコマンドウェイト中にスキップできるか
		public static bool IsSkippableCommandOnWaitThread(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.SkippableOnWaitThread:
				case AdvCommandWaitType.AddSkippable:
					return true;
				default:
					return false;
			}
		}

		
		public static bool IsSkippable(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.Skippable:
				case AdvCommandWaitType.InputWaitSkippable:
				case AdvCommandWaitType.PageWaitSkippable:
				case AdvCommandWaitType.AddSkippable:
					return true;
				default:
					return false;
			}
		}

		//待機コマンドで一緒に待機の対象となるタイプか？
		public static bool IsWaitingCommandType(this AdvCommandWaitType target )
		{
			//タイプによって終了を待つ
			switch (target)
			{
				case AdvCommandWaitType.Default:
				case AdvCommandWaitType.Add:
				case AdvCommandWaitType.AddSkippable:
				case AdvCommandWaitType.Skippable:
				case AdvCommandWaitType.SkippableOnWaitThread:
					return true;
				default:
					return false;
			}
		}

		//改行入力などを入力前にするエフェクトの終了待ちの対象となるタイプか？
		public static bool IsWaitingInputType(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.Add:
				case AdvCommandWaitType.InputWait:
				case AdvCommandWaitType.AddSkippable:
				case AdvCommandWaitType.InputWaitSkippable:
					return true;
				default:
					return false;
			}
		}

		//改ページ待ち時点でのエフェクトの終了待ちの対象となるタイプか？
		public static bool IsWaitingPageEndEffect(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.Add:
				case AdvCommandWaitType.InputWait:
				case AdvCommandWaitType.PageWait:
				case AdvCommandWaitType.AddSkippable:
				case AdvCommandWaitType.InputWaitSkippable:
				case AdvCommandWaitType.PageWaitSkippable:
					return true;
				default:
					return false;
			}
		}

		//スレッド終了待ち時点でのエフェクトの終了待ちの対象となるタイプか？
		public static bool IsWaitingOnThreadType(this AdvCommandWaitType target )
		{
			switch (target)
			{
				case AdvCommandWaitType.Default:
				case AdvCommandWaitType.Add:
				case AdvCommandWaitType.AddSkippable:
				case AdvCommandWaitType.Skippable:
				case AdvCommandWaitType.SkippableOnWaitThread:
					return true;
				default:
					return false;
			}
		}
	}
}
