using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //選択肢クリックコマンド時のマウスカーソルを変更する際の全体制御用のサンプル
    public class SampleSelectionClickMouseCursorManager : MonoBehaviour
    {
        AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] AdvEngine engine;
        
        //マウスカーソルのテクスチャ
        [SerializeField] Texture2D cursorIcon;
        //マウスカーソルのテクスチャ（選択済み）
        [SerializeField] Texture2D cursorIconSelected;

        void Awake()
        {
            Engine.GraphicManager.OnInitGraphicObject.AddListener(OnInitGraphicObject);
        }
        
        
        //グラフィックオブジェクトの初期化時に呼ばれるイベント
        void OnInitGraphicObject(AdvGraphicObject obj)
        {
            var renderObj = obj.RenderObject;
            //UGUIのグラフィックオブジェクトの場合のみ処理する
            if (renderObj.TryGetComponent<AdvGraphicObjectUguiBase>(out var uguiBase))
            {
                //選択肢クリック時のマウスカーソルを変更するコンポーネントを追加
                renderObj.gameObject.AddComponent<SampleSelectionClickMouseCursor>();
            }
        }

        //カーソルのテクスチャを取得
        public Texture2D GetCursorTexture(SampleSelectionClickMouseCursor target)
        {
            //オブジェクトの名前を取得
            var targetName = target.AdvGraphic.ParentObject.name;
            //選択肢の情報を取得
            var data = Engine.SelectionManager.SpriteSelections.Find(x=>x.SpriteName == targetName);
            if (data == null)
            {
                //選択肢が有効ではなくなってるので、nullを返す
                return null;
            }
            
            //選択済みかをシステムセーブデータからチェック
            return Engine.SystemSaveData.SelectionData.Check(data) ? cursorIconSelected : cursorIcon;
        }
        
        //対象のクリックイベントが追加された時（SelectionClickコマンドが有効になったとき）に呼ばれる
        public void OnAddClickEvent(SampleSelectionClickMouseCursor target)
        {
            foreach (var graphic in target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                graphic.raycastTarget = true;
            }
        }
        
        //対象のクリックイベントが追加された時（SelectionClickコマンドが無効になったとき）に呼ばれる
        public void OnRemoveClickEvent(SampleSelectionClickMouseCursor target)
        {
            foreach (var graphic in target.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
