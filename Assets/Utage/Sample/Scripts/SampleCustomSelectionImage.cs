// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;

namespace Utage
{
	/// 選択肢用のカスタム処理をするサンプル
	/// 選択肢プレハブに設定する
	/// ・カスタム処理（指定したセル名の判定式（Bool型のParamなど）の結果によって、ボタンの画像を差し替える）
	public class SampleCustomSelectionImage : MonoBehaviour
	{
		//画像を差し替える対象のImage（選択肢ボタンの背景などを指定）
		[SerializeField] Image targetImage;

		//Boolのパラメーター名（条件式の判定に使用するパラメーターを指定）
		[SerializeField] string paramName = "";

		//条件式がtrueのときに設定する画像
		[SerializeField] Sprite spriteOnTrue;
		//条件式がfalseのときに設定する画像
		[SerializeField] Sprite spriteOnFalse;

		//AdvEngineを取得
		AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref advEngine);
		AdvEngine advEngine;


		//最初の1フレーム時の初期化
		void Start()
		{

			if (targetImage == null)
			{
				Debug.LogWarning("targetImage is not set", this);
				return;
			}

			//カスタム処理
			InitCustomOperation();
		}

		//カスタム処理（指定したセル名の判定式の結果によってボタン画像を差し替える）
		void InitCustomOperation()
		{
			if (string.IsNullOrEmpty(paramName)) return;


			var flag = Engine.Param.GetParameterBoolean(paramName);
			//結果によって画像を差し替える
			targetImage.sprite = flag ? spriteOnTrue : spriteOnFalse;
		}
	}
}