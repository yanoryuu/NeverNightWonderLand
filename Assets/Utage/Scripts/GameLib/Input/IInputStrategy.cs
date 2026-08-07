using UnityEngine;

namespace Utage
{
	//インプット処理切り替え用のインターフェース
	public interface IInputStrategy
	{
		//マウス座標を取得
		public Vector3 GetMousePosition();

		//GUIを閉じる入力があったか
		//デフォルトでは右クリック
		public bool IsInputGuiClose();

		//強制スキップ入力があったか
		//デフォルトでは、Ctrlキー
		public bool IsInputForceSkip();

		//バックログを開く入力があったか
		//デフォルトでは、マウスホイールを上に入力
		public bool IsInputOpenBackLog();

		//バックログを閉じる入力があったか
		//デフォルトでは、マウスホイールを下に入力
		public bool IsInputCloseBackLog();

		//マウスによる次へ読み進める入力があったか
		//デフォルトでは、マウスホイールを下に入力
		public bool IsInputNexByMouse();

		//次へ読み進める入力があったか
		//デフォルトでは、リターンキー
		public bool IsInputNextButton();

		//ゲーム終了ダイアログを開く入力があったか
		//デフォルトではEscapeキー
		public bool IsInputExit();

		//デバッグ用のポーズ入力（その0）があったか
		//デフォルトでは、マウス左ボタンを押した瞬間
		public bool IsInputDebugPause0();

		//デバッグ用のポーズ入力（その1）があったか
		//デフォルトでは、マウス左ボタンが押されていたのを離した瞬間（クリック直後を想定）
		public bool IsInputDebugPause1();
	}
}
