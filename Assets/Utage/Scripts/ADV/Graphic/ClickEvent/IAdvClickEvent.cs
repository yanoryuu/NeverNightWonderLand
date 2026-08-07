// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Utage
{

	/// <summary>
	/// グラフィックオブジェクトのデータ
	/// </summary>
	public interface IAdvClickEvent
	{
		GameObject gameObject { get; }

		/// <summary>
		/// クリックイベントを設定
		/// </summary>
		void AddClickEvent(bool isPolygon, StringGridRow row, UnityAction<BaseEventData> action);

		/// <summary>
		/// クリックイベントを削除
		/// </summary>
		void RemoveClickEvent();
	}
}
