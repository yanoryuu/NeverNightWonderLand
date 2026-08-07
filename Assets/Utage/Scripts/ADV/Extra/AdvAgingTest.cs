// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	/// <summary>
	/// エージングテスト。選択肢などを自動入力する
	/// </summary>
	[AddComponentMenu("Utage/ADV/Extra/AdvAgingTest")]
	public class AdvAgingTest : MonoBehaviour
	{
		//選択肢の選び方
		public enum Type
		{
			Random,		//ランダム
			DepthFirst,	//深さ優先
		}
		[SerializeField]
		Type type = Type.Random;
		public Type SelectType
		{
			get { return type; }
			set { type = value; }
		}

		//自動選択しない選択肢のジャンプ先ラベルのリスト
		public List<string> ExcludedAutoSelectJumpLabels => excludedAutoSelectJumpLabels;
		[SerializeField] List<string> excludedAutoSelectJumpLabels = new();

		//無効化フラグ
		[SerializeField]
		bool disable = false;
		public bool Disable
		{
			get { return disable; }
			set { disable = value; }
		}

		[System.Flags]
		public enum SkipFlags
		{
			Voice = 0x1<<0,
			Movie = 0x1 << 1,
		}
		[SerializeField,EnumFlags]
		SkipFlags skipFilter = 0;

		public SkipFlags SkipFilter
		{
			get { return skipFilter; }
			set { skipFilter = value; }
		}

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField]
		protected AdvEngine engine;

		public float waitTime = 1.0f;
		float time;

		public bool clearOnEnd = true;

		//選択した選択肢情報を記憶
		private Dictionary<AdvScenarioPageData, int> SelectedDictionary { get; }= new ();

		void Awake()
		{
			Engine.SelectionManager.OnBeginWaitInput.AddListener(OnBeginWaitInput);
			Engine.SelectionManager.OnUpdateWaitInput.AddListener(OnUpdateWaitInput);

			Engine.ScenarioPlayer.OnBeginCommand.AddListener(OnBeginCommand);
			Engine.ScenarioPlayer.OnUpdatePreWaitingCommand.AddListener(OnUpdatePreWaitingCommand);
			Engine.ScenarioPlayer.OnEndScenario.AddListener(OnEndScenario);
		}

		//選択肢待ち開始
		void OnBeginWaitInput(AdvSelectionManager selection)
		{
			time = -Time.deltaTime;
		}

		//選択肢待機中
		void OnUpdateWaitInput(AdvSelectionManager selection)
		{
			if (Disable) return;

			time += Time.deltaTime;
			if (time >= waitTime)
			{
				for(int i = 0; i < 100; ++i)
				{
					int index = GetIndex(selection);
					if (CheckAutoSelect(selection,index))
					{
						selection.SelectWithTotalIndex(index);
						return;
					}
				}
				selection.SelectWithTotalIndex(GetIndex(selection));

			}
		}

		//選択肢待ち開始
		void OnBeginCommand(AdvCommand command)
		{
			time = -Time.deltaTime;
		}

		//コマンド待機中
		void OnUpdatePreWaitingCommand(AdvCommand command)
		{
			if (Disable) return;
			if (!IsWaitInputCommand(command)) return;

			time += Time.deltaTime;
			if (time >= waitTime)
			{
				switch (command)
				{
					case AdvCommandWaitInput:
						Engine.UiManager.IsInputTrig = true;
						break;
					case AdvCommandWaitCustom:
						Engine.UiManager.IsInputTrigCustom = true;
						break;
					case AdvCommandSendMessage:
						Engine.ScenarioPlayer.SendMessageTarget.SafeSendMessage("OnAgingInput", command);
						break;
					case AdvCommandVideo:
						Engine.UiManager.IsInputTrig = true;
						break;
					case AdvCommandText:
					{
						if (Engine.SoundManager.IsPlayingVoice())
						{
							Engine.Page.InputSendMessage();
						}

						break;
					}
				}
			}
		}

		void OnEndScenario(AdvScenarioPlayer player)
		{
			if (clearOnEnd)
			{
				this.SelectedDictionary.Clear();
			}
		}


		bool IsWaitInputCommand(AdvCommand command)
		{
			switch (command)
			{
				case AdvCommandWaitInput:
				case AdvCommandWaitCustom:
				case AdvCommandSendMessage:
					return true;
				case AdvCommandVideo:
					return (skipFilter & SkipFlags.Movie) == SkipFlags.Movie;
				case AdvCommandText:
					return (skipFilter & SkipFlags.Voice) == SkipFlags.Voice;
				default:
					return false;
			}
		}


		//選択するインデックス取得
		int GetIndex(AdvSelectionManager selection)
		{
			switch (type)
			{
				case Type.DepthFirst:
					//深さ優先（チュートリアルなど、網羅的に選択する場合に）
					return GetIndexDepthFirst(selection);
				default:
					//ランダム
					return UnityEngine.Random.Range(0, selection.TotalCount);
			}
		}

		//深さ優先の場合にインデックスを取得（チュートリアルなど、網羅的に選択する場合に）
		int GetIndexDepthFirst(AdvSelectionManager selection)
		{
			int index;
			if (!SelectedDictionary.TryGetValue(Engine.Page.CurrentData, out index))
			{
				index = 0;
				SelectedDictionary.Add(Engine.Page.CurrentData, index);
			}
			else
			{
				if (index + 1 < selection.TotalCount)
				{
					++index;
				}
				SelectedDictionary[Engine.Page.CurrentData] = index;
			}
			return index;
		}

		//自動選択可能なインデックスかチェック
		//除外リストにある等で自動選択が不可能な場合はfalseを返す
		private bool CheckAutoSelect(AdvSelectionManager selection, int index)
		{
			if (ExcludedAutoSelectJumpLabels.Count <= 0) return true;
			if (index >= selection.Selections.Count) return false;

			var selected = selection.Selections[index];
			if (ExcludedAutoSelectJumpLabels.Contains(selected.JumpLabel))
			{
				return false;
			}
			return true;
		}
	}
}
