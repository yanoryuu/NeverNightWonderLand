// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UtageExtensions;

namespace Utage
{
	/// 選択肢用のカスタム処理をするサンプル
	/// 選択肢プレハブに設定する
	/// ・カスタムテキスト（選択肢のテキスト本文以外に好感度などのパラメ―ターのテキストを表示する）
	/// ・カスタム処理（指定したセル名の判定式が満たされる場合に、選択肢を表示するが入力不可にする）
	public class SampleCustomSelection : MonoBehaviour
	{
		//選択肢のコンポーネント
		public AdvUguiSelection Selection => this.GetComponentCache(ref selection);
		AdvUguiSelection selection;

		//カスタムテキストの設定
		[SerializeField] GameObject customTextRoot;  //カスタムテキストの表示オブジェクトのルート（背景などある場合を想定）
		[SerializeField] TextMeshProUGUI customTextPro; //カスタムテキストの表示テキスト
		[SerializeField] Text customText; //カスタムテキストの表示テキスト
		[SerializeField] string customTextParamCellName;  //カスタムテキストで使用するパラメータ名を設定するセル名
		
		//カスタム処理で使うセル名
		[SerializeField] string customOperationCellName = "";
		//入力を無効にする時の色
		[SerializeField] Color disableInputColor = Color.gray;

		//AdvEngineを取得
		AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref advEngine);
		AdvEngine advEngine;


		//最初の1フレーム時の初期化
		void Start()
		{
			if (Selection == null)
			{
				Debug.LogWarning("Selection is not found",this);
				return;
			}
			
			//選択肢のデータを取得
			var data = Selection.Data;
			//Selectionコマンドの行データ
			var rowData = data.RowData;

			//カスタムテキストの初期化
			InitCustomText(rowData);

			//カスタム処理
			InitCustomOperation(rowData);
		}
		
		//カスタムテキストの初期化（選択肢のテキスト本文以外に好感度などのパラメ―ターのテキストを表示する）
		void InitCustomText(StringGridRow rowData)
		{
			//カスタムテキストの表示オブジェクトが設定されてない場合は処理しない
			if(customTextRoot==null) return;
			if(customText==null && customTextPro == null) return;
			
			//カスタムテキストのセル名が設定されてない場合は処理しない
			if(string.IsNullOrEmpty(customTextParamCellName)) return;

			//カスタム処理のパラメータを取得
			if (!rowData.TryParseCell(customTextParamCellName, out string paramName))
			{
				//指定の名前のセルが見つからないか空の場合は処理しない
				return;
			}

			//カスタムテキストのパラメータを取得
			if (!Engine.Param.ExistParameter(paramName) )
			{
				//パラメータが見つからない場合はエラーログを出力して処理しない
				Debug.LogError(rowData.ToErrorString($"{paramName} is not found)"));
				return;
			}
			
			//パラメーターを取得
			int param = Engine.Param.GetParameterInt(paramName);
			//カスタムテキストの表示
			customTextRoot.SetActive(true);
			//カスタムテキストの設定。ここではパラメーターをそのままテキストとして表示
			string text = param.ToString();
			TextComponentWrapper.SetText(customText, customTextPro, text);
		}


		//カスタム処理（指定したセル名の判定式が満たされる場合に、選択肢を表示するが入力不可にする）
		void InitCustomOperation(StringGridRow rowData)
		{
			//カスタム処理のセル名が設定されてない場合は処理しない
			if(string.IsNullOrEmpty(customOperationCellName)) return;
			
			//カスタム処理のパラメータを取得
			if (!rowData.TryParseCell(customOperationCellName, out string customOperation))
			{
				//指定の名前のセルが見つからないか空の場合は処理しない
				return;
			}
			
			//カスタム処理の実行
			
			//指定のセル名に設定されているテキストから、条件式を作成
			var expressionBoolean = Engine.Param.CreateExpressionBoolean(customOperation);
			if (!string.IsNullOrEmpty(expressionBoolean.ErrorMsg))
			{
				//条件式の記述エラー
				Debug.LogError(rowData.ToErrorString(expressionBoolean.ErrorMsg));
				return;
			}
			
			//条件式の評価
			if (Engine.Param.CalcExpressionBoolean(expressionBoolean))
			{
				//条件に当てはまる場合の処理
				
				//ボタンの入力を無効にする
				this.GetComponentInChildren<Button>().interactable = false;
				
				//全てのUIの色を指定の色にする（グレーアウト処理）
				foreach (var graphic in this.GetComponentsInChildren<Graphic>())
				{
					//テキストは除外(テキストカラーは変えない)
					if(graphic is TextMeshProUGUI ) continue;
					if (graphic is Text) continue;
					
					graphic.color = disableInputColor;
				}
			}
		}
	}
}
