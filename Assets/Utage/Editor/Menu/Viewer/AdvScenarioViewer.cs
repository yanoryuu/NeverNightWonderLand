// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	//宴のビューワー表示ウィンドウ
	public class AdvScenarioViewer : CustomEditorWindow
	{
		void OnEnable()
		{
			//シーン変更で描画をアップデートする
			this.autoRepaintOnSceneChange = true;
			//スクロールを有効にする
			this.isEnableScroll = true;
		}

		AdvEngine Engine
		{
			get
			{
				if (engine == null)
				{
					this.engine = WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngine>();
				}
				return engine;
			}
		}
		AdvEngine engine;
		
		
		protected override void OnGUISub()
		{
			var advEngine = Engine;
			if (advEngine == null)
			{
				UtageEditorToolKit.BoldLabel("Not found AdvEngine");
			}
			else
			{
				OnGuiCurrentScenario();
			}
		}

		[SerializeField]
		bool foldOutScenario = true;
		//現在のページデータを描画
		void OnGuiCurrentScenario()
		{
			UpdateCurrentCommand();
			UtageEditorToolKit.FoldoutGroup(ref this.foldOutScenario, "Scenario", () =>
			{
				if (Engine.Page != null && Engine.Page.CurrentData != null)
				{
					OnScnearioLabelData(Engine.Page.CurrentData.ScenarioLabelData);
				}
			});
		}

		//「現在のコマンド」の更新処理
		AdvCommand currentCommand = null;
		bool isChangeCurrentCommandTrigger;
		void UpdateCurrentCommand()
		{
			if (currentCommand != Engine.ScenarioPlayer.MainThread.CurrentCommand)
			{
				currentCommand = Engine.ScenarioPlayer.MainThread.CurrentCommand;
				if (currentCommand != null)
				{
					isChangeCurrentCommandTrigger = true;
				}
			}
		}


		float scrollTopY;
		Vector2 scrollPosition2;
		//シナリオラベルデータを表示
		void OnScnearioLabelData(AdvScenarioLabelData data)
		{
			if (data == null)
			{
				UtageEditorToolKit.BoldLabel("Not found scenario data");
			}
			else
			{
				//エディタ上のビュワーで表示するラベル
				string viewerLabel = "*" + data.ScenarioLabel + "   " +  data.FileLabel;
				if (currentCommand != null && currentCommand.RowData!=null)
				{
					viewerLabel += " : " + currentCommand.RowData.RowIndex;
				}
				GUILayout.Label(viewerLabel);
				
				//位置を記憶しておく
				if (Event.current.type == EventType.Repaint)
				{
					Rect rect = GUILayoutUtility.GetLastRect();
					scrollTopY = rect.y + rect.height;
				}

				this.scrollPosition2 = EditorGUILayout.BeginScrollView(this.scrollPosition2);
				data.PageDataList.ForEach(x => OnGuiPage(x));
				EditorGUILayout.EndScrollView();
			}
		}
		//ページデータを描画
		void OnGuiPage(AdvScenarioPageData page)
		{
			if (page == null)
			{
				UtageEditorToolKit.BoldLabel("Not found page data");
			}
			else
			{
				EditorGUILayout.BeginVertical(WindowStyle);
				page.CommandList.ForEach(x => OnGuiCommand(x));
				EditorGUILayout.EndVertical();
			}
		}

		//コマンドの表示
		void OnGuiCommand(AdvCommand command )
		{
			StringGridRow row = command.RowData;

			Color color = GUI.color;
			bool isCurrentCommand = Engine.ScenarioPlayer.MainThread.IsCurrentCommand(command);
			//現在のコマンドなら色を変える
			if (isCurrentCommand)
			{
				GUI.color = Color.red;
			}
			//テキストは別表示

			EditorGUILayout.BeginVertical(BoxStyle);
			{
				string text = "";
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.Label(command.CommandLabel, GUILayout.Width(200));
					if (row == null || row.Strings == null)
					{
//						GUILayout.Label("", BoxStyle);
					}
					else
					{
						int commandIndex;
						if (!row.Grid.TryGetColumnIndex(AdvColumnName.Command.ToString(), out commandIndex))
						{
							commandIndex = -1;
						}

						int textIndex;
						if (!row.Grid.TryGetColumnIndex(AdvColumnName.Text.ToString(), out textIndex))
						{
							textIndex = -1;
						}
						if (textIndex > 0 && textIndex< row.Strings.Length)
						{
							text = row.Strings[textIndex];
						}

						bool isEmpty = true;
						for (int i = 0; i < row.Strings.Length; ++i)
						{
							if (i != textIndex && i != commandIndex && !string.IsNullOrEmpty(row.Strings[i])) isEmpty = false;
						}
						if (isEmpty)
						{
						}
						else
						{
							for (int i = 0; i < row.Strings.Length; ++i)
							{
								if (i != textIndex && i != commandIndex)
								{
									GUILayout.Label(row.Strings[i], BoxStyle);
								}
							}
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				if (!string.IsNullOrEmpty(text) && (command is AdvCommandText || command is AdvCommandSelection) )
				{
					GUILayout.Label(text);
				}
			}
			EditorGUILayout.EndVertical();

			//現在のコマンド
			if (isCurrentCommand)
			{
				//コマンドが変わったときにオートスクロール
				if (Event.current.type == EventType.Repaint && isChangeCurrentCommandTrigger)
				{
					Rect rect = GUILayoutUtility.GetLastRect();
					float minY = Mathf.Max(0, rect.y - this.position.height + rect.height + 50) + scrollTopY;
					float maxY = rect.y - 50;
					this.scrollPosition2.y = Mathf.Clamp(this.scrollPosition.y, minY, maxY);
					isChangeCurrentCommandTrigger = false;
				}
				GUI.color = color;
			}
		}
	}
}
