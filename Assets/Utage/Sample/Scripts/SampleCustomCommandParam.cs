// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	[AddComponentMenu("Utage/ADV/Examples/SampleCustomCommandParam")]
	public class SampleCustomCommandParam : AdvCustomCommandManager
	{
		public override void OnBootInit()
		{
			Utage.AdvCommandParser.OnCreateCustomCommandFromID += CreateCustomCommand;
		}
		void OnDestroy()
		{
			Utage.AdvCommandParser.OnCreateCustomCommandFromID -= CreateCustomCommand;
		}

		//AdvEnginのクリア処理のときに呼ばれる
		public override void OnClear()
		{
		}
 		
		//カスタムコマンドの作成用コールバック
		public void CreateCustomCommand(string id, StringGridRow row, AdvSettingDataManager dataManager, ref AdvCommand command )
		{
			switch (id)
			{
				case "SetParamTblCount":
					command = new AdvCommandParamTblKeyCount(row);
					break;
				case "SetParamTblCount2":
					command = new AdvCommandParamTblKeyCount2(row);
					break;
			}
		}
	}

	//指定の名前のテーブルの要素数を取得
	public class AdvCommandParamTblKeyCount : AdvCommand
	{
		public AdvCommandParamTblKeyCount(StringGridRow row)
			:base(row)
		{
			//
			this.paramName = ParseCell<string>(AdvColumnName.Arg1);
			this.tblName = ParseCell<string>(AdvColumnName.Arg2);
		}

		//コマンド実行
		public override void DoCommand(AdvEngine engine)
		{
			//指定の名前のテーブルを取得
			AdvParamStructTbl tbl;
			if (engine.Param.StructTbl.TryGetValue(tblName, out tbl))
			{
				//要素数を取得
				int count = tbl.Tbl.Count;

				//指定のパラメーターに設定
				if (!engine.Param.TrySetParameter(paramName, count))
				{
					Debug.LogError(paramName + " is not parameter name");
				}
			}
			else
			{
				Debug.LogError(tblName + " is not ParamTbl name");
			}
		}

		string paramName;
		string tblName;
	}

	//指定の名前のテーブル内の条件を満たすものの数を取得
	public class AdvCommandParamTblKeyCount2 : AdvCommand
	{
		public AdvCommandParamTblKeyCount2(StringGridRow row)
			: base(row)
		{
			//
			this.paramName = ParseCell<string>(AdvColumnName.Arg1);
			this.tblName = ParseCell<string>(AdvColumnName.Arg2);
			this.valueName = ParseCell<string>(AdvColumnName.Arg3);
			this.countValue = ParseCell<string>(AdvColumnName.Arg4);
		}

		//コマンド実行
		public override void DoCommand(AdvEngine engine)
		{
			//指定の名前のテーブルを取得
			AdvParamStructTbl tbl;
			if (engine.Param.StructTbl.TryGetValue(tblName, out tbl))
			{
				//テーブル内で指定の条件を満たす要素数を取得
				int count = 0;
				foreach (var paramStruct in tbl.Tbl.Values)
				{
					//テーブル内の指定の名前のパラメーター取得
					AdvParamData data;
					if (!paramStruct.Tbl.TryGetValue(valueName, out data))
					{
						Debug.LogError(valueName + " is not parameter name");
						return;
					}
					//指定の値（文字列として比較）だったらカウントアップ
					if (data.ParameterString == countValue)
					{
						++count;
					}
				}

				//指定のパラメーターに設定
				if (!engine.Param.TrySetParameter(paramName, count))
				{
					Debug.LogError(paramName + " is not parameter name");
				}
			}
			else
			{
				Debug.LogError(tblName + " is not ParamTbl name");
			}
		}

		string paramName;
		string tblName;

		string valueName;
		string countValue;
	}
}
