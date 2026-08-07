// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Utage
{
	//サブルーチンの情報
	public class SubRoutineInfo
	{
		public string ReturnLabel { get; set; }		//戻り先のラベル
		public int ReturnPageNo { get; set; }		//戻り先の
		public AdvCommand ReturnCommand { get; set; }       //戻り先の

		internal string JumpLabel { get; private set; }      //指定されているジャンプ先のシナリオラベル
		internal string CalledLabel { get; private set; }		//呼び出し元のシナリオラベル
		//呼び出しサブルーチンコマンドのインデックス
		//（同一シナリオラベル内でのサブルーチンがいつかある場合、何番目のサブルーチンコマンドか）
		internal int CalledSubroutineCommandIndex { get; private set; }

		public SubRoutineInfo( AdvEngine engine, string jumpLabel, string calledLabel, int calledSubroutineCommandIndex)
		{
			this.JumpLabel = jumpLabel;
			this.CalledLabel = calledLabel;
			this.CalledSubroutineCommandIndex = calledSubroutineCommandIndex;
			InitReturnInfo (engine);
		}

		public SubRoutineInfo(AdvEngine engine, BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version == Version)
			{
				this.JumpLabel = reader.ReadString();
				this.CalledLabel = reader.ReadString();
				this.CalledSubroutineCommandIndex = reader.ReadInt32();
				InitReturnInfo(engine);
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		const int Version = 0;
		internal void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(this.JumpLabel);
			writer.Write(this.CalledLabel);
			writer.Write(this.CalledSubroutineCommandIndex);
		}

		void InitReturnInfo(AdvEngine engine)
		{
			if (!string.IsNullOrEmpty (JumpLabel))
			{
				this.ReturnLabel = JumpLabel;
				this.ReturnPageNo = 0;
				this.ReturnCommand = null;
			}
			else
			{
				engine.DataManager.SetSubroutineRetunInfo(CalledLabel,CalledSubroutineCommandIndex, this );
			}
		}
	}


	/// <summary>
	/// ジャンプのマネージャー
	/// </summary>
	public class AdvJumpManager
	{
		//ジャンプ先のラベル名
		public string Label{ get; private set; }

		//サブルーチンの復帰先の情報
		public SubRoutineInfo SubRoutineReturnInfo { get; private set; }

		//サブルーチンのコールスタック
		public Stack<SubRoutineInfo> SubRoutineCallStack { get { return subRoutineCallStack; } }
		Stack<SubRoutineInfo> subRoutineCallStack = new Stack<SubRoutineInfo>();

		class RandomInfo
		{
			public AdvCommand command;
			public float rate;
			public RandomInfo(AdvCommand command, float rate)
			{
				this.command = command;
				this.rate = rate;
			}
		}

		List<RandomInfo> randomInfoList = new List<RandomInfo>();

		//ジャンプ先が登録されているか
		public bool IsReserved
		{
			get { return !string.IsNullOrEmpty(Label) || SubRoutineReturnInfo != null; }
		}

		//ジャンプ先のラベルを登録
		public void RegistoreLabel(string jumpLabel)
		{
			this.Label = jumpLabel;
		}

		//サブルーチンを登録
		public void RegistoreSubroutine(string label, SubRoutineInfo calledInfo) 
		{
			this.Label = label;
			subRoutineCallStack.Push(calledInfo);
		}

		//サブルーチンを終了して、元のページの次のページに戻る
		public void EndSubroutine()
		{
			if (IsSubroutine)
			{
				this.SubRoutineReturnInfo = subRoutineCallStack.Pop();
			}
			else
			{
				Debug.LogErrorFormat("Failed to terminate the subroutine.Please call the subroutine with 'JumpSubRoutine'.");
			}
		}

		//サブルーチン中かどうかを判定
		public bool IsSubroutine
		{
			get { return subRoutineCallStack.Count > 0; }
		}

		//ランダムジャンプのラベルを登録
		public void AddRandom(AdvCommand command, float rate)
		{
			randomInfoList.Add(new RandomInfo(command, rate));
		}

		//ジャンプしたときにクリアする
		public void ClearOnJump()
		{
			Label = "";
			SubRoutineReturnInfo = null;
			randomInfoList.Clear();
		}

		//全てクリアする
		public void Clear()
		{
			ClearOnJump();
			subRoutineCallStack.Clear();
		}

		//サブルーチンから抜ける
		public void ExitSubRoutine()
		{
			//サブルーチンのコールスタックをすべてクリアする
			subRoutineCallStack.Clear();
		}

		//実行するランダムコマンドを取得
		public AdvCommand GetRandomJumpCommand()
		{
			//各要素の合計値を計算
			float sum = 0;
			randomInfoList.ForEach(item => sum += item.rate);
			if (sum <= 0)
			{
				//合計値が0以下。つまりランダムジャンプは無効
				return null;
			}
			else
			{
				//ランダム値を計算
				float rand = Random.Range(0, sum);

				//ランダムジャンプ先のラベルを取得
				foreach (RandomInfo info in randomInfoList)
				{
					rand -= info.rate;
					if (rand <= 0)
					{
						return info.command;
					}
				}
				return null;
			}
		}

		const int Version = 0;
		//バイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(subRoutineCallStack.Count);
			foreach (var item in subRoutineCallStack)
			{
				item.Write(writer);
			}
		}
		//バイナリ読み込み
		public void Read(AdvEngine engine, BinaryReader reader)
		{
			this.Clear();
			if (reader.BaseStream.Length <= 0) return;

			int version = reader.ReadInt32();
			if (version == Version)
			{
				int count = reader.ReadInt32();
				SubRoutineInfo[] array = new SubRoutineInfo[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = new SubRoutineInfo(engine, reader);
				}
				for (int i = count - 1; i >= 0; --i)
				{
					subRoutineCallStack.Push(array[i]);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}
