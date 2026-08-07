// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura


namespace Utage
{

	// クリックイベントのカスタムインターフェース
	//SelectionClickコマンドで使用するオブジェクトのクリックイベントをカスタムするためのインターフェース
	public interface IAdvClickEventCustom
	{
		//クリックイベントが追加された
		void OnAddClickEvent();
		//クリックイベントが削除された
		void OnRemoveClickEvent();
	}
}
