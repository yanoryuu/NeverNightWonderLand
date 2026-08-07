// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{
	// SortingOrderのカスタム設定を行うためのインターフェース
	public interface IAdvGraphicEventSortingOrder
	{
		//描画順の設定
		public void SetSortingOrder(int sortingOrder);
	}
}
