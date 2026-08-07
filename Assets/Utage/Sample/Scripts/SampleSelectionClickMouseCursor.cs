using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //選択肢クリックコマンド時のマウスカーソルを変更するサンプル
    public class SampleSelectionClickMouseCursor : MonoBehaviour
        , IPointerEnterHandler
        , IPointerExitHandler
        , IAdvClickEventCustom
    {
        public AdvGraphicBase AdvGraphic => this.GetComponentCacheInParent(ref advGraphic);
        AdvGraphicBase advGraphic;
        
        //カーソル管理コンポーネントを取得
        SampleSelectionClickMouseCursorManager CursorManager
        {
            get
            {
                if (cursorManager==null)
                {
                    var engine = AdvGraphic.Engine;
                    cursorManager = engine.GetComponentInChildren<SampleSelectionClickMouseCursorManager>(true);
                }
                return cursorManager;
            }
        }

        SampleSelectionClickMouseCursorManager cursorManager;
        
        //マウスが重なった時
        public void OnPointerEnter(PointerEventData eventData)
        {
            //カーソルのテクスチャを取得
            Texture2D texture = CursorManager.GetCursorTexture(this);
            
            //テクスチャが取得できなかったら何もしない(選択肢が有効ではなくなくなってる)
            if(texture==null) return;
            
            //中心点をテクスチャの中央に設定
            Vector2 hotSpot = new Vector2(texture.width / 2.0f, texture.height / 2.0f);
            Cursor.SetCursor(texture, hotSpot, CursorMode.ForceSoftware);
        }

        //マウスが離れた時
        public void OnPointerExit(PointerEventData eventData)
        {
            //カーソルを元に戻す
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        //クリックイベントが追加された時（SelectionClickコマンドが有効になったとき）に呼ばれる
        public void OnAddClickEvent()
        {
            CursorManager.OnAddClickEvent(this);
        }

        //クリックイベントが追加された時（SelectionClickコマンドが無効になったとき）に呼ばれる
        public void OnRemoveClickEvent()
        {
            CursorManager.OnRemoveClickEvent(this);
        }
    }
}