// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// ADV用SendMessageコマンドから送られたメッセージを受け取る処理のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/UtageRecieveMessageSample")]
	public class UtageRecieveMessageSample : MonoBehaviour
	{
		public AdvEngine engine;
		public InputField inputFiled;

		void Awake()
		{
			if (inputFiled != null) inputFiled.gameObject.SetActive(false);
		}

		//SendMessageコマンドが実行されたタイミング
		void OnDoCommand(AdvCommandSendMessage command)
		{
			switch (command.Name)
			{
				case "DebugLog":
					DebugLog(command);
					break;
				case "InputFileld":
					InputFileld(command);
					break;
				case "AutoLoad":
					AutoLoad(command);
					break;
				default:
					Debug.LogError("Unknown Message:" + command.Name);
					break;
			}
		}

		//SendMessageコマンドの処理待ちタイミング
		void OnWait(AdvCommandSendMessage command)
		{
			switch (command.Name)
			{
				case "InputFileld":
					//インプットフィールドが有効な間は待機
					command.IsWait = inputFiled.gameObject.activeSelf;
					break;
				case "AutoLoad":
					command.IsWait = true;
					break;
				default:
					command.IsWait = false;
					break;
			}
		}

		//SendMessageコマンドの処理待ちタイミング
		void OnAgingInput(AdvCommandSendMessage command)
		{
			switch (command.Name)
			{
				case "InputFileld":
					inputFiled.gameObject.SetActive(false);
					break;
				default:
					break;
			}
		}

		//デバッグログを出力
		void DebugLog(AdvCommandSendMessage command)
		{
			Debug.Log(command.Arg2);
		}

		//設定された入力フィールドを有効化
		void InputFileld(AdvCommandSendMessage command)
		{
			inputFiled.gameObject.SetActive(true);
			inputFiled.onEndEdit.RemoveAllListeners();
			inputFiled.onEndEdit.AddListener((string text) => OnEndEditInputFiled(command.Arg2, text));
		}

		//入力終了。入力されたテキストを宴のパラメーターに設定する
		void OnEndEditInputFiled(string paramName, string text)
		{
			if (!engine.Param.TrySetParameter(paramName, text))
			{
				Debug.LogError(paramName + "is not found");
			}

			inputFiled.gameObject.SetActive(false);
		}

		//オートロード
		void AutoLoad(AdvCommandSendMessage command)
		{
			Debug.Log("AutoLoad");
			StartCoroutine(CoAutoLoadSub());
		}

		IEnumerator CoAutoLoadSub()
		{
			//終了処理
			engine.ScenarioPlayer.IsReservedEndScenario = true;
			//終了処理は1フレームかかるので遅らせる
			yield return null;

			//オートセーブデータをロード
			engine.SaveManager.ReadAutoSaveData();

			if (engine.SaveManager.AutoSaveData == null || !engine.SaveManager.AutoSaveData.IsSaved)
			{
				//オートセーブデータのロード失敗
				Debug.LogError("AutoLoad is not yet load");
			}
			else
			{
				//オートセーブデータでゲーム開始
				engine.OpenLoadGame(engine.SaveManager.AutoSaveData);
			}
		}
	}
}
