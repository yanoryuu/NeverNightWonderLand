// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	[AddComponentMenu("Utage/ADV/Examples/SampleCustomCommand")]
	public class SampleCustomCommand : AdvCustomCommandManager
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
				//既存のコマンドを改造コマンドに変えたい場合は、IDで判別
				//コメントアウトを解除すれば、テキスト表示がデバッグログ出力のみに変わる
//				case AdvCommandParser.IdText:
//					command = new SampleCustomAdvCommandText(row);
//					break;
				//新しい名前のコマンドも作れる
				case "DebugLog":
					command = new SampleAdvCommandDebugLog(row);
					break;
			}
		}
	}

	//Textの内容をデバッグログで出力するカスタムコマンド
	public class SampleAdvCommandDebugLog : AdvCommand
	{
		public SampleAdvCommandDebugLog(StringGridRow row)
			:base(row)
		{
			//コンストラクタでParseすると、インポート時にエラーがでる

			//「Text」列の文字列を取得
			this.log = ParseCell<string>(AdvColumnName.Text);
		}

		//コマンド実行
		public override void DoCommand(AdvEngine engine)
		{
			Debug.Log(log);
		}

		string log;
	}

	/// テキスト表示コマンドを書き換える。
	/// あくまでサンプルとして、ログを出力するだけに
	internal class SampleCustomAdvCommandText : AdvCommand
	{
		public SampleCustomAdvCommandText(StringGridRow row)
			: base(row)
		{
			//「Text」列の文字列を取得
			this.log = ParseCell<string>(AdvColumnName.Text);
		}

		//コマンド実行
		public override void DoCommand(AdvEngine engine)
		{
			Debug.Log(log);
		}

		string log;
	}
}
