// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using UnityEngine;


namespace Utage
{
    public class AdvScenarioDataIfElse
    {
	    private AdvScenarioLabelData ScenarioLabelData { get; }
	    public AdvScenarioDataIfElse(AdvScenarioLabelData scenarioLabelData)
	    {
		    ScenarioLabelData = scenarioLabelData;
	    }
	    
        //if-else系のコマンドの構文エラーをチェックする
		public void CheckError(AdvSettingDataManager dataManager)
		{
			List<IfElseScope> scopeList = new List<IfElseScope>();
			IfElseScope currentScope = null;
			
			foreach (AdvScenarioPageData page in ScenarioLabelData.PageDataList)
			{
				foreach ( var command  in page.CommandList)
				{
					if (command is IAdvCommandIfElse commandIfElse)
					{
						switch (commandIfElse)
						{
							case IAdvCommandIf commandIf:
								currentScope = new IfElseScope(commandIf,currentScope);
								scopeList.Add(currentScope);
								break;
							case IAdvCommandElseIf commandElseIf:
							case IAdvCommandElse commandElse:
							case IAdvCommandEndIf commandEndIf:
								if (currentScope == null)
								{
									//ifが未設定でIf-else系コマンドがある。
									//最初にIfコマンドを設定してください。
									Debug.LogError(commandIfElse.ToErrorString("Syntax error in if-else commands.Set the if command first.") );
								}
								else
								{
									currentScope.AddCommand(commandIfElse);
									if(commandIfElse is IAdvCommandEndIf)
									{
										currentScope = currentScope.Parent;
									}
								}
								break;
							default:
								//IAdvCommandIfElseを継承しているが、未知のクラス
								Debug.LogError(commandIfElse.ToErrorString($"{commandIfElse.GetType()} is an unknown class that inherits from {nameof(IAdvCommandIfElse)}."));
								break;
								
						}
					}
				}
			}

			foreach (var block in scopeList)
			{
				block.CheckError();
			}
		}
		
		//if-elseスコープ
		//if-else系のコマンドの構文エラーチェックに使う
		class IfElseScope
		{
			//入れ子になっている場合、親のif-elseスコープ
			public IfElseScope Parent { get; }
			List<IAdvCommandIfElse> CommandList { get; } = new List<IAdvCommandIfElse>();

			public IfElseScope(IAdvCommandIf commandIf, IfElseScope parent)
			{
				Parent = parent;
				AddCommand(commandIf);
			}

			public void AddCommand(IAdvCommandIfElse command)
			{
				CommandList.Add(command);
			}

			public void CheckError()
			{
				int countIf = 0;
				int countElseIf = 0;
				int countElse = 0;
				int countEndif = 0;
				foreach (var command in CommandList)
				{
					switch (command)
					{
						case IAdvCommandIf commandIf:
							++countIf;
							break;
						case IAdvCommandElseIf commandElseIf:
							++countElseIf;
							if (countElse >= 1)
							{
								//おなじif-elseスコープ内で、Elseコマンドの後にElseIfコマンドを使ってはいけません。。
								Debug.LogError(commandElseIf.ToErrorString(
									$"Syntax error in if-else commands. Do not use an ElseIf command after an Else command within the same if-else scope."));
							}
							break;
						case IAdvCommandElse commandElse:
							++countElse;
							if (countElse != 1)
							{
								//Elseコマンドが同じif-elseスコープで複数設定されています。
								Debug.LogError(commandElse.ToErrorString(
									$"Syntax error in if-else commands. Else commands are set multiple times in the same if-else scope."));
							}
							break;
						case IAdvCommandEndIf commandEndIf:
							++countEndif;
							break;
						default:
							//IAdvCommandIfElseを継承しているが、未知のクラス
							Debug.LogError(command.ToErrorString(
								$"{command.GetType()} is an unknown class that inherits from {nameof(IAdvCommandIfElse)}."));
							break;
					}
				}

				//EndIfコマンドがない
				if (countEndif <= 0)
				{
					var command = CommandList[0];
					Debug.LogError(command.ToErrorString(
						$"Syntax error in if-else commands. An If command must end with an EndIf command."));
				}
			}
		}

    }
}
