using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
	//シナリオ割り込みをするためのコンポ―ネント
	//シナリオ割り込みは、今のシナリオ実行を強制的に中断して、指定のシナリオラベルにジャンプする
	//ただし、強制的中断による副作用は未検証
    public class AdvInterruptScenario : MonoBehaviour, IAdvSaveData
    {
	    // ADVエンジン
		public AdvEngine AdvEngine => this.GetAdvEngineCacheFindIfMissing(ref advEngine);
		[SerializeField] protected AdvEngine advEngine;

		//シナリオ割り込みが発生したとき（指定のラベルにジャンプする前）に呼ばれるイベント
		public UnityEvent OnInterruptScenario => onInterruptScenario;
		[SerializeField] UnityEvent onInterruptScenario = new();

		//割り込む前のシナリオへの復帰処理の開始時に呼ばれるイベント
		public UnityEvent OnStartResumeScenario => onStartResumeScenario;
		[SerializeField] UnityEvent onStartResumeScenario = new();

		//割り込む前のシナリオへの復帰処理の終了時に呼ばれるイベント
		public UnityEvent OnEndResumeScenario => onEndResumeScenario;
		[SerializeField] UnityEvent onEndResumeScenario = new();

		//Resume時にパラメーターを再ロードするかどうか
		//通常はfalseで、パラメーターだけは割り込みシナリオで変化したほうのものを優先する
		public bool ReloadParamOnResume
		{
			get => reloadParamOnResume;
			set => reloadParamOnResume = value;
		}
		[SerializeField] bool reloadParamOnResume = false;
		
		public bool Resuming { get; private set; } = false;

		//ジャンプ前のセーブデータをバイナリデータとしてとっておく
		protected byte[] BufferSaveData { get; set; }

		//割り込みシナリオ中か
		//再開中か、呼び出し元へ戻るデータがある場合はtrue
		//それ以外の細かい調整が必要な場合は、プロジェクトごとに拡張
		public bool InterruptingScenario()
		{
			return Resuming || (BufferSaveData != null && BufferSaveData.Length > 0);
		}

		//今のシナリオの進行を中断し、テキスト以外の表示は継続したまま指定のラベルのシナリオにジャンプして割り込ませる
		public void InterruptScenario(string label)
		{
			//再開のために今のセーブデータをバッファに保存
			BufferSaveData = BinaryUtil.BinaryWrite((writer)=> AdvEngine.SaveManager.CurrentAutoSaveData.Write(writer));
			
			OnInterruptScenario.Invoke();
			
			//今のシナリオを中断
			AdvEngine.ScenarioPlayer.ForceBreakCurrentPage();
			//指定のシナリオラベルを開始
			AdvEngine.JumpScenario(label);
		}

		//中断時点のシナリオに戻す（中断したページの冒頭のセーブデータをロードして再開）
		public void ResumeScenario()
		{
			if(BufferSaveData == null || BufferSaveData.Length == 0)
			{
				//中断したセーブデータがない場合はエラー
				Debug.LogError("BufferSaveData is null. Call InterruptScenario before ResumeScenario.");
				return;
			}
			StartResumeScenario();

			byte[] bufferParam = null;
			//サブシナリオで変化したパラメーターを取得
			if (!ReloadParamOnResume)
			{
				bufferParam = BinaryUtil.BinaryWrite((writer) => AdvEngine.Param.Write(writer, AdvParamData.FileType.Default));
			}
			//再開のためにとっておいたバッファからセーブデータを復元
			var saveData = new AdvSaveData(AdvSaveData.SaveDataType.Quick, "");
			BinaryUtil.BinaryRead(BufferSaveData,saveData.Read);
			AdvEngine.ScenarioPlayer.OnBeginReadSaveData.AddListener(OnBeginReadSaveData);

			//今のスレッドの強制的中断フラグを立てておく
			AdvEngine.ScenarioPlayer.MainThread.IsForceBreaking = true;
			//セーブデータから復元
			AdvEngine.OpenLoadGame(saveData);
//			Debug.LogWarning("ResumeScenario called. BufferSaveData loaded.");
			return;
			
			//再開用のセーブデータのロード直後の処理
			void OnBeginReadSaveData(AdvScenarioPlayer player)
			{
				if (!ReloadParamOnResume && bufferParam != null)
				{
					
					//シナリオ再開前にパラメーターだけ、サブシナリオで変化したものに戻す
					BinaryUtil.BinaryRead(bufferParam,
						(reader) => AdvEngine.Param.Read(reader, AdvParamData.FileType.Default));
				}
				//今のスレッドの強制的中断フラグを解除
				AdvEngine.ScenarioPlayer.MainThread.IsForceBreaking = false;
				
//				Debug.Log("ResumeScenario: BufferParam loaded.");
				BufferSaveData = null;
				bufferParam = null;
				AdvEngine.ScenarioPlayer.OnBeginReadSaveData.RemoveListener(OnBeginReadSaveData);
				EndResumeScenario();
			}
		}
		//ResumeScenarioをAdvCommandSendMessageByNameコマンドで呼ぶためもの
		void ResumeScenario(AdvCommandSendMessageByName command)
		{
//			Debug.Log("ResumeScenario: AdvCommandSendMessageByName.");
			ResumeScenario();
		}

		//中断したシナリオに戻る処理を開始
		void StartResumeScenario()
		{
			Resuming = true;
			OnStartResumeScenario.Invoke();
		}
		
		//中断したシナリオに戻る処理を終了
		void EndResumeScenario()
		{
			OnEndResumeScenario.Invoke();
			Resuming = false;
		}

		//中断したシナリオに戻るためのセーブデータをクリアする
		void ClearResumeScenarioDataSub()
		{
			BufferSaveData = null;
		}
		//ClearResumeScenarioDataをAdvCommandSendMessageByNameコマンドで呼ぶためもの
		void ClearResumeScenarioData(AdvCommandSendMessageByName command)
		{
			ClearResumeScenarioDataSub();
		}
		
		//セーブデータ処理（割り込みシナリオ中にセーブしても、再開できるようにする）
		
		public string SaveKey => "InterruptScenario";
		const int Version = 0;

		public void OnClear()
		{
			//クリア処理をまたいでデータが必要になるので、ここでは何もしない
		}

		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.WriteBuffer(BufferSaveData ?? Array.Empty<byte>());
		}

		public void OnRead(BinaryReader reader)
		{
			//バージョンチェック
			int version = reader.ReadInt32();
			if (version == Version)
			{
				byte[] buffer = reader.ReadBuffer();
				if (!Resuming)
				{
					//再開中は何もしない
					BufferSaveData = buffer;
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				BufferSaveData = null;
			}
		}
    }
}
