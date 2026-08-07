using UnityEngine;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
    //選択肢クリックコマンド時のマウスが重なっときなどに色を変化するサンプル
    public class SampleSelectionClickColor : MonoBehaviour
        , IPointerEnterHandler
        , IPointerExitHandler
        , IAdvClickEventCustom
    {
        public AdvGraphicBase AdvGraphic => this.GetComponentCacheInParent(ref advGraphic);
        AdvGraphicBase advGraphic;
        
        public AdvGraphicObject AdvGraphicObject => AdvGraphic.ParentObject;
        AdvEffectColor EffectColor => AdvGraphicObject.GetComponent<AdvEffectColor>();
        
        //マウスが重なった時
        public void OnPointerEnter(PointerEventData eventData)
        {
            //色を変える
            SetColor(Color.magenta);
        }

        //マウスが離れた時
        public void OnPointerExit(PointerEventData eventData)
        {
            //色を戻す
            SetColor(Color.white);
        }

        //クリックイベントが追加された時（SelectionClickコマンドが有効になったとき）に呼ばれる
        public void OnAddClickEvent()
        {
            //何か処理があれば行う
        }

        //クリックイベントが終わった時（SelectionClickコマンドが無効になったとき）に呼ばれる
        public void OnRemoveClickEvent()
        {
            //色を戻す
            SetColor(Color.white);
        }
        
        //色を設定
        void SetColor(Color color)
        {
            EffectColor.ScriptColor = color;
        }
    }
}