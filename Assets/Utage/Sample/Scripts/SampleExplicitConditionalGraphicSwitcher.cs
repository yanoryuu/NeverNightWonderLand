using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //明示的に呼び出されたときに、オブジェクトのConditionalをチェックして表示変更を行うコンポーネント
    //SampleAutoConditionalGraphicSwitcherの手動版
    //表示フラグ変更ボタンを押したときなどに呼び出す
    public class SampleExplicitConditionalGraphicSwitcher : MonoBehaviour
    {
        AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        public AdvEngine engine = null;

        //条件つきグラフィックの情報
        class ConditionalGraphic
        {
            public AdvGraphicObject graphicObject; //対象のオブジェクト
            public AdvGraphicInfo lastGraphicInfo; //条件変更前の描画情報
            public AdvGraphicInfo nextGraphicInfo; //条件変更後の描画情報
        }

        //明示的に呼び出して、オブジェクトのConditionalをチェックして表示変更を行う
        public void SwitchConditionalGraphicExplicit()
        {
            //表示条件が変更しているものを検索
            var targets = FindTargets();
            foreach (var target in targets)
            {
                SwitchGraphic(target);
            }
        }

        //表示条件が変更しているものを検索
        IEnumerable<ConditionalGraphic> FindTargets()
        {
            var graphicObjects = Engine.GraphicManager.AllGraphics();
            foreach (var graphicObject in graphicObjects)
            {
                var graphic = CreateConditionalGraphicIfNeed(graphicObject);
                if (graphic != null)
                {
                    yield return graphic;
                }
            }
        }

        //表示条件がかわっているなら、ConditionalGraphicを作成して返す
        ConditionalGraphic CreateConditionalGraphicIfNeed(AdvGraphicObject graphicObject)
        {
            var current = graphicObject.LastResource;
            if (current == null) return null;
            
            var graphicList = Engine.DataManager.FindGraphicInfoList(current); 
            //Mainのgeプロパティの内部でConditionalの条件チェックがされる
            var main = graphicList.Main; 
            
            //今の条件と、現在表示しているグラフィックが異なる
            if ( main!= current)
            {
                //情報を作成して返す
                return new ConditionalGraphic
                {
                    graphicObject = graphicObject,
                    lastGraphicInfo = current,
                    nextGraphicInfo = main,
                };
            }
            return null;
        }

        
        //描画を切り替える
        void SwitchGraphic(ConditionalGraphic graphic)
        {
            //複数同時にロードすることがあるので、ローダーコンポーネントを毎回Addする
            var loader = this.gameObject.AddComponent<AdvGraphicLoader>();

            //ロードしてから描画を変える
            loader.LoadGraphic(graphic.nextGraphicInfo, ()=>
            {
                OnLoaded(graphic);
                //ローダーは毎回破棄する
                Destroy(loader);
            });
        }

        void OnLoaded(ConditionalGraphic graphic)
        {
            if (CanDrawCurrentConditional(graphic))
            {
                //ロードしてる間にオブジェクトが消えたりして描画できなくなったので何もしない
                return;
            }
            //表示を変える
            var obj = graphic.graphicObject;
            var info = graphic.nextGraphicInfo;
            //オブジェト側のローダーに参照をもたせてから描画
            obj.Loader.LoadGraphic(info, () => obj.DrawSubExplicit(info, 0));
        }
        
        //今のフレームでの条件で、表示できるか？
        bool CanDrawCurrentConditional(ConditionalGraphic graphic)
        {
            //既に破棄されている
            if (graphic.graphicObject == null) return false;
            
            if (graphic.graphicObject.LastResource != graphic.lastGraphicInfo)
            {
                //表示オブジェクトが既に変わっている
                return false;
            }

            if (!Engine.GraphicManager.ContainsCurrentGraphic(graphic.graphicObject))
            {
                //表示オブジェクトがすでに管理外（フェードアウトなどで）
                return false;
            }

            return true;
        }
    }
}