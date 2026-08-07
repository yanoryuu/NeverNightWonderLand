// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Utage
{
	public class SampleSelectionClick2DPrefab : MonoBehaviour
			, IAdvClickEvent
			, IPointerClickHandler
			, IPointerEnterHandler
			, IPointerExitHandler
	{
		//マウスカーソルのテクスチャ
		[SerializeField] Texture2D cursorIcon;

		//SetClickコマンドの行データ（拡張が必要な時に）
		StringGridRow Row { get; set; }
		//クリック時のコールバック
		UnityAction<BaseEventData> action { get; set; }

		void Awake()
		{
			//コライダー(当たり判定)を無効にする
			SetEnableCollider(false);
		}

		//SelectionClickコマンドが実行されたので、クリックイベントを設定する
		public virtual void AddClickEvent(bool isPolygon, StringGridRow row, UnityAction<BaseEventData> action)
		{
			this.Row = row;
			this.action = action;
			//コライダー(当たり判定)を有効にする
			SetEnableCollider(true);
		}

		//SelectionClickコマンドが終了したので、クリックイベントを解放する
		public virtual void RemoveClickEvent()
		{
			this.Row = null;
			this.action = null;
			//コライダー(当たり判定)を無効にする
			SetEnableCollider(false);
		}
		
		//コライダー(当たり判定)の有効無効を設定する
		void SetEnableCollider(bool enableCollider)
		{
			foreach (var col in this.GetComponentsInChildren<Collider2D>(true))
			{
				col.enabled = enableCollider;
			}
		}
		
		//クリックされたとき
		public void OnPointerClick(PointerEventData eventData)
		{
			action?.Invoke(eventData);
		}

		//マウスが重なった時
		public void OnPointerEnter(PointerEventData eventData)
		{
			if(cursorIcon==null) return;
            
			//中心点をテクスチャの中央に設定
			Vector2 hotSpot = new Vector2(cursorIcon.width / 2.0f, cursorIcon.height / 2.0f);
			Cursor.SetCursor(cursorIcon, hotSpot, CursorMode.ForceSoftware);
		}

		//マウスが離れた時
		public void OnPointerExit(PointerEventData eventData)
		{
			if(cursorIcon==null) return;
			
			//カーソルを元に戻す
			Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		}
	}
}

