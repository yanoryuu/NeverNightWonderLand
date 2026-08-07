using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utage;

namespace Utage
{

	public class SampleSubScenario : MonoBehaviour
	{
		// ADVエンジン
		public AdvEngine AdvEngine
		{
			get { return advEngine; }
		}

		[SerializeField] protected AdvEngine advEngine;

		// UI処理
		public UtageUguiMainGame UiMainGame
		{
			get { return uiMainGame; }
		}

		[SerializeField] protected UtageUguiMainGame uiMainGame;

		//サブシナリオを再生中かどうか
		public bool IsPlayingSubScenario { get; private set; }

		//今のセーブデータバイナリデータとしてとっておく
		private byte[] BufferSaveData { get; set; }

		//サブシナリオのパラメーターをバイナリデータとしてとっておく
		private byte[] BufferParam { get; set; }

		bool IsAutoSave { get; set; }

		private Coroutine CoroutineJumpScenarioAsync { get; set; }

		//現在のシナリオを中断して、指定のラベルのサブシナリオを開始
		//サブシナリオが終了したら自動的に本シナリオを再開
		public void JumpSubScenario(string label)
		{
			CoroutineJumpScenarioAsync = StartCoroutine(JumpScenarioAsync(label));
		}

		//本シナリオの再開処理をやめる
		//サブシナリオをそのまま本シナリオとして続行する場合などに呼ぶ
		public void CancelResumeMainScenario(AdvCommandSendMessageByName command)
		{
			if (IsPlayingSubScenario)
			{
				IsPlayingSubScenario = false;
				StopCoroutine(CoroutineJumpScenarioAsync);
				CoroutineJumpScenarioAsync = null;
				ResumeSaveSetting();
			}
		}

		IEnumerator JumpScenarioAsync(string label)
		{
			IsPlayingSubScenario = true;
			OnStartSubScenario();
			//指定のシナリオラベルを開始
			AdvEngine.JumpScenario(label);
			while (!AdvEngine.IsEndOrPauseScenario)
			{
				yield return null;
			}

			OnEndSubScenario();
			IsPlayingSubScenario = false;
			CoroutineJumpScenarioAsync = null;
		}

		//サブシナリオの開始処理
		void OnStartSubScenario()
		{
			//再開のために今のセーブデータをバッファに保存
			//		BufferSaveData = BinaryUtil.BinaryWrite((writer)=> AdvEngine.SaveManager.CurrentAutoSaveData.Write(writer));
			//セーブを無効化
			AdvEngine.SaveManager.Type = AdvSaveManager.SaveType.Disable;
			IsAutoSave = AdvEngine.SaveManager.IsAutoSave;
			AdvEngine.SaveManager.IsAutoSave = false;

			//今の表示等をクリア
			AdvEngine.ClearOnEnd();

			//サブシナリオ中にUIを変える
			//プロジェクトごとに違うと思われる

			//例として・・・
			//UIアップデート無効（EndScenarioでタイトルに戻るのを防ぐ）
			if (UiMainGame != null) UiMainGame.enabled = true;
			//ほかにセーブボタンの非表示とか、その他のUI制御を・・・

		}

		//サブシナリオの終了処理
		void OnEndSubScenario()
		{
			//サブシナリオで変化したパラメーターを取得
			BufferParam =
				BinaryUtil.BinaryWrite((writer) => AdvEngine.Param.Write(writer, AdvParamData.FileType.Default));

			//再開のためにとっておいたセーブデータをバッファから復元
			//		BinaryUtil.BinaryRead(BufferSaveData, (reader) => AdvEngine.SaveManager.CurrentAutoSaveData.Read(reader));

			ResumeSaveSetting();

			//セーブデータから本シナリオを再開
			AdvEngine.ScenarioPlayer.OnBeginReadSaveData.AddListener(OnBeginReadSaveData);
			AdvEngine.OpenLoadGame(AdvEngine.SaveManager.CurrentAutoSaveData);

			//サブシナリオ中に変えたUIを元に戻す
			//プロジェクトごとに違うと思われる

			//例として・・・
			//UIアップデート無効を解除
			if (UiMainGame != null) UiMainGame.enabled = true;
			//セーブボタンの再表示とか、その他のUI制御を・・・
		}


		//再開用のセーブデータのロード直後の処理
		void OnBeginReadSaveData(AdvScenarioPlayer player)
		{
			//シナリオ再開前にパラメーターだけ、サブシナリオで変化したものに戻す
			BinaryUtil.BinaryRead(BufferParam, (reader) => AdvEngine.Param.Read(reader, AdvParamData.FileType.Default));
			AdvEngine.ScenarioPlayer.OnBeginReadSaveData.RemoveListener(OnBeginReadSaveData);
		}

		//セーブの設定を元に戻す
		void ResumeSaveSetting()
		{
			AdvEngine.SaveManager.Type = AdvSaveManager.SaveType.Default;
			AdvEngine.SaveManager.IsAutoSave = IsAutoSave;
		}
	}
}
