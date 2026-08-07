// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.IO;

namespace Utage
{

	/// <summary>
	/// IF分岐のマネージャー
	/// </summary>
	internal class AdvIfManager
	{

		//処理中のif文
		AdvIfData Current { get; set; }

		//セーブデータのロード直後
		bool SaveDataStart { get; set; }
		public bool OldSaveDataStart { get; set; }

		// クリア
		public void ResetOnJump()
		{
			if (!SaveDataStart)
			{
				Current = null;
				OldSaveDataStart = false;
			}
			SaveDataStart = false;
		}

		/// <summary>
		/// if文の開始
		/// </summary>
		/// <param name="param">判定に使う数値パラメーター</param>
		/// <param name="exp">判定式</param>
		public void BeginIf(AdvParamManager param, ExpressionParser exp)
		{
			OldSaveDataStart = false;
			Current = new AdvIfData(Current);

			if (Current.IsParantSkipping)
			{
				//親がスキップ中なので、このIf構造は全てスキップ
//				Debug.Log("このIf構造は　親がスキップ中なので、スキップされます");
				Current.IsSkpping = true;
			}
			else
			{
				Current.BeginIf(param, exp);
			}
		}

		/// <summary>
		/// else if文の開始
		/// </summary>
		/// <param name="param">判定に使う数値パラメーター</param>
		/// <param name="exp">判定式</param>
		public void ElseIf(AdvParamManager param, ExpressionParser exp)
		{
			if (Current == null)
			{
				//Currentがないのはエラー
				if (!OldSaveDataStart) Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.ElseIf, exp));
				Current = new AdvIfData(Current);
			}
			if (!Current.IsParantSkipping)
			{
				Current.ElseIf(param, exp);
			}
		}

		/// <summary>
		/// else文の開始
		/// </summary>
		public void Else()
		{
			if (Current == null)
			{
				//Currentがないのはエラー
				if (!OldSaveDataStart) Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.Else));
				Current = new AdvIfData(Current);
			}
			if (!Current.IsParantSkipping)
			{
				Current.Else();
			}
		}

		/// <summary>
		/// if文の終了
		/// </summary>
		public void EndIf()
		{
			if (Current == null)
			{
				//セーブデータ復帰直後ではないなら、Curretがないのはエラー
				if (!OldSaveDataStart) Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.EndIf));
				Current = new AdvIfData(Current);
			}
			if (!Current.IsParantSkipping)
			{
				Current.EndIf();
			}
			Current = Current.Parent;
		}

		/// <summary>
		/// if分岐によるスキップをするか
		/// </summary>
		/// <param name="command">コマンドデータ</param>
		/// <returns>スキップする場合はtrue。しない場合はfalse</returns>
		public bool CheckSkip(AdvCommand command)
		{
			if (command == null)
			{
				return false;
			}

			if (Current == null)
			{
				//現在のIfデータなし。スキップしない
				return false;
			}
			else if (Current.IsSkpping && !command.IsIfCommand)
			{
				//現在のifデータ内でスキップ中なら、If系コマンド以外はスキップ
				return true;
			}
			else
			{
				return false;
			}
		}

		const int Version = 0;
		//バイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			int count = 0;
			var ifData = Current;
			while (ifData != null)
			{
				++count;
				ifData = ifData.Parent;
			}
			writer.Write(count);
			ifData = Current;
			while (ifData != null)
			{
				writer.Write(ifData.IsSkpping);
				writer.Write(ifData.IsIf);
				ifData = ifData.Parent;
			}
		}
		//バイナリ読み込み
		public void Read(BinaryReader reader)
		{
			SaveDataStart = true;
			OldSaveDataStart = false;
			int version = reader.ReadInt32();
			if (0 <= version && version <= Version)
			{
				Current = null;
				int count = reader.ReadInt32();
				for (int i = 0; i < count; ++i)
				{
					Current = new AdvIfData(Current);
				}
				var ifData = Current;
				while (ifData != null)
				{
					ifData.IsSkpping = reader.ReadBoolean();
					ifData.IsIf = reader.ReadBoolean();
					ifData = ifData.Parent;
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
		//バイナリ読み込み
		public void ReadOld()
		{
			SaveDataStart = true;
			OldSaveDataStart = true;
		}
	}
}
