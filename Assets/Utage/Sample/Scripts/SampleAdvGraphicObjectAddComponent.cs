using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //オブジェクトの作成時に、AddComponentするサンプル
    public class SampleAdvGraphicObjectAddComponent : MonoBehaviour
    {
        private AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        public AdvEngine engine = null;
        public List<string> targetNames;

        void Awake()
        {
            engine.GraphicManager.OnInitGraphicObject.AddListener(OnInit);
        }
        
        //グラフィックオブジェクトの初期化時に呼ばれるイベント
        void OnInit(AdvGraphicObject obj)
        {
//          Debug.Log("OnInit: " + obj.gameObject.name);
            obj.TargetObject.gameObject.AddComponent<SampleSelectionClickColor>();
        }
    }
}