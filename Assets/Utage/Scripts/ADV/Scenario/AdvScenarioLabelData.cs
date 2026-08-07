// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// シナリオラベルで区切られたデータ
	/// </summary>
	public class AdvScenarioLabelData
	{
		//ページデータ
		public List<AdvScenarioPageData> PageDataList{ get; private set; }

		//シナリオラベル
		public string ScenarioLabel { get; private set; }
		//次のデータ
		public AdvScenarioLabelData Next { get; internal set; }


		public int PageNum
		{
			get { return PageDataList.Count; }
		}

		//セーブポイントが設定されているか
		public bool IsSavePoint
		{
			get
			{
				return (scenarioLabelCommand == null) ? false : scenarioLabelCommand.Type == AdvCommandScenarioLabel.ScenarioLabelType.SavePoint;
			}
		}

		//セーブのタイトルが設定されているか
		public string SaveTitle
		{
			get
			{
				return (scenarioLabelCommand == null) ? "" : scenarioLabelCommand.Title;
			}
		}

		//コマンドのリスト
		public List<AdvCommand> CommandList { get; set; }

		AdvCommandScenarioLabel scenarioLabelCommand;

		//コンストラクタ
		internal AdvScenarioLabelData(string scenarioLabel, AdvCommandScenarioLabel scenarioLabelCommand, List<AdvCommand> commandList)
		{
			this.ScenarioLabel = scenarioLabel;
			this.scenarioLabelCommand = scenarioLabelCommand;
			this.CommandList = commandList;
			this.PageDataList = new List<AdvScenarioPageData>();
			if (CommandList.Count <= 0) return;

			int commandIndex = 0;
			do
			{
				int begin = commandIndex;
				int end = GetPageEndCommandIndex(begin);
				//ページデータ追加
				PageDataList.Add(new AdvScenarioPageData(this, PageDataList.Count, CommandList.GetRange(begin, end - begin+1)));
				commandIndex = end+1;
			} while (commandIndex < CommandList.Count);

			this.PageDataList.ForEach(x => x.Init());
		}

		//ページ終了処理コマンドのインデックスを探す
		int GetPageEndCommandIndex(int begin)
		{
			for (int i = begin; i < CommandList.Count; ++i)
			{
				if (CommandList[i] is AdvCommandEndPage)
				{
					return i;
				}
				//ページ区切り系のコマンド
				if (CommandList[i].IsTypePageEnd())
				{
					for (int j = i+1; j < CommandList.Count; ++j)
					{
						if (CommandList[j].IsTypePage())
						{
							break;
						}
						if (CommandList[j] is AdvCommandEndPage)
						{
							return j;
						}
					}
					return i;
				}
			}
			return CommandList.Count-1;
		}
		//データのダウンロード
		public void Download(AdvDataManager dataManager)
		{
			PageDataList.ForEach( (item)=>item.Download(dataManager) );
		}

		//ファイルセットに追加
		public void AddToFileSet(HashSet<AssetFile> fileSet)
		{
			foreach (var page in PageDataList)
			{
				page.AddToFileSet(fileSet);
			}
		}

		//ページデータの取得
		public AdvScenarioPageData GetPageData(int page)
		{
			return (page < PageDataList.Count) ? PageDataList[page] : null;
		}

		//エラー文字列
		public string ToErrorString(string str, string gridName)
		{
			if (scenarioLabelCommand!=null)
			{
				return scenarioLabelCommand.RowData.ToErrorString(str);
			}
			else
			{
				return str + " "+ gridName;
			}
		}

		//サブルーチンコマンドのシナリオラベル内のインデックスを取得
		internal int CountSubroutineCommandIndex(AdvCommand command)
		{
			int index = 0;
			foreach (AdvScenarioPageData page in PageDataList)
			{
				foreach (AdvCommand cmd in page.CommandList)
				{
					System.Type type = cmd.GetType();
					if (type == typeof(AdvCommandJumpSubroutine) || type == typeof(AdvCommandJumpSubroutineRandom))
					{
						if (cmd == command)
						{
							return index;
						}
						else
						{
							++index;
						}
					}
				}
			}
			Debug.LogError("Not found Subroutine Command");
			return -1;
		}

		//サブルーチンの帰り先を見つけて情報を設定
		internal bool TrySetSubroutineRetunInfo(int subroutineCommandIndex, SubRoutineInfo info)
		{
			info.ReturnLabel = ScenarioLabel;

			AdvCommand calledCommand=null;
			int index = 0;
			foreach(AdvScenarioPageData page in PageDataList)
			{
				foreach (AdvCommand cmd in page.CommandList)
				{
					//呼び出し元のコマンドを探す
					System.Type type = cmd.GetType();
					if (calledCommand == null)
					{
						if (type == typeof(AdvCommandJumpSubroutine) || type == typeof(AdvCommandJumpSubroutineRandom))
						{
							if (index == subroutineCommandIndex)
							{
								calledCommand = cmd;
							}
							else
							{
								++index;
							}
						}
					}
					else
					{
						//呼び出しもとは見つかってるので、飛び先のコマンドを見つける
						if (calledCommand.GetType() == typeof(AdvCommandJumpSubroutine))
						{
							//呼び出し元のコマンドの次のコマンド
							info.ReturnPageNo = page.PageNo;
							info.ReturnCommand = cmd;
							return true;
						}
						if (calledCommand.GetType() == typeof(AdvCommandJumpSubroutineRandom))
						{
							if (type != typeof(AdvCommandJumpSubroutineRandom) && type != typeof(AdvCommandJumpSubroutineRandom))
							{
								//ランダムサブルーチンが終わったところ
								info.ReturnPageNo = page.PageNo;
								info.ReturnCommand = cmd;
								return true;
							}
						}
					}
				}
			}
			return false;
		}





		//指定のページ移行のファイルをプリロード
		internal HashSet<AssetFile> MakePreloadFileListSub(AdvDataManager dataManager, int page, int maxFilePreload, int preloadDeep)
		{
			AdvScenarioLabelData data = this;
			HashSet<AssetFile> fileSet = new HashSet<AssetFile>();
			do
			{
				for (int j = page; j < data.PageNum; ++j)
				{
					data.GetPageData(j).AddToFileSet(fileSet);
					if (fileSet.Count >= maxFilePreload)
					{
						return fileSet;
					}
				}
				//ジャンプなどがあるので、このページでいったん先読みの区切り
				if (data.IsEndPreLoad())
				{
					//ジャンプ先もプリロードする
					data.PreloadDeep(dataManager, page, fileSet, maxFilePreload, preloadDeep);
					break;
				}
				if(data==this)
				{
					//現在のシナリオラベルデータの場合は、条件つきジャンプ先もプリロードする
					if (dataManager.Engine.ScenarioPlayer.PreloadDeepJumpIf)
					{
						data.PreloadDeep(dataManager, fileSet, maxFilePreload, preloadDeep);
					}
				}
				page = 0;
				data = data.Next;
			} while (data != null);
			return fileSet;
		}

		//ファイルのプリロードを終わらせるべきか
		bool IsEndPreLoad()
		{
			if(CommandList.Count<=0) return false;

			//シナリオ分岐系のコマンドだったら、プリロードは終了
			AdvCommand lastCommand = CommandList[CommandList.Count-1];
			if (lastCommand is AdvCommandPageControler)
			{
				if (CommandList.Count - 2 < 0) return false;
				lastCommand = CommandList[CommandList.Count - 2];
			}
			if( lastCommand is AdvCommandEndScenario  ) return true;
			if( lastCommand is AdvCommandSelectionEnd ) return true;
			if( lastCommand is AdvCommandSelectionClickEnd ) return true;
			if( lastCommand is AdvCommandJumpRandomEnd ) return true;

			//自動分岐は条件式を考慮する
			if( (lastCommand is AdvCommandJump) || 
				(lastCommand is AdvCommandJumpSubroutine) ||
				(lastCommand is AdvCommandJumpSubroutineRandom )
				)
			{
				if( lastCommand.IsEmptyCell( AdvColumnName.Arg2 ) )
				{
					return true;
				}
			}
			return false;
		}

		//ジャンプ先のリソースファイルをファイルセットに追加
		void PreloadDeep( AdvDataManager dataManager, int startPage, HashSet<AssetFile> fileSet, int maxFilePreload, int deepLevel)
		{
			if (fileSet.Count >= maxFilePreload) return;
			if (deepLevel <= 0) return;

			for (int page = startPage; page < this.PageNum; ++page)
			{
				GetPageData(page).GetJumpScenarioLabelDataList(dataManager).ForEach(
					x =>
					{
						if (x != null)
						{
							x.PreloadDeep(dataManager, fileSet, maxFilePreload, deepLevel);
						}
					});
			}
		}


		//最初のページ（とその自動ジャンプ先）のリソースファイルをファイルセットに追加
		void PreloadDeep(AdvDataManager dataManager, HashSet<AssetFile> fileSet, int maxFilePreload, int deepLevel)
		{
			//フラグを考慮しないので、循環参照による無限参照を回避、階層は5階層までしか拾わない
			if (deepLevel <= 0) return;
			--deepLevel;

			if (PageNum <= 0) return;
			if (fileSet.Count >= maxFilePreload) return;

			//最初のページのみプリロード
			GetPageData(0).AddToFileSet(fileSet);
			if (fileSet.Count >= maxFilePreload) return;

			//最初のページに自動分岐があれば、さらにその先をプリロード
			GetPageData(0).GetAutoJumpLabels(dataManager).ForEach(
					x =>
					{
						if (x != null)
						{
							x.PreloadDeep(dataManager, fileSet, maxFilePreload, deepLevel);
						}
					});
		}

#if UNITY_EDITOR

		//エディタ上のビュワーで表示するラベル
		public string FileLabel
		{
			get
			{
				foreach (AdvScenarioPageData page in PageDataList)
				{
					foreach (AdvCommand command in page.CommandList)
					{
						if (command.RowData != null && command.RowData.Grid != null)
						{
							string name = command.RowData.Grid.Name;
							int index = name.LastIndexOf("/");
							return name.Substring(index,name.Length-index);
						}
					}
				}
				return "Unknown";
			}
		}
#endif
		
		//全コマンドのエラーをチェックする（コマンド単体でエラーチェックできない構文チェックなど）
		public void CheckErrorCommands(AdvSettingDataManager dataManager)
		{
			//if-else系のコマンドの構文エラーをチェックする
			var elseIf = new AdvScenarioDataIfElse(this);
			elseIf.CheckError(dataManager);
		}

		//シナリオラベル開始時に呼ぶ処理
		public void OnStartScenarioLabel(AdvEngine engine)
		{
			this.scenarioLabelCommand?.OnStartScenarioLabel(engine);
		}
	}
}
