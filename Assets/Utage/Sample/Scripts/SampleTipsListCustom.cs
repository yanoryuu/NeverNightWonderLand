using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //TIPSリストの初期化処理をカスタムするサンプル
    public class SampleTipsListCustom : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI text;
        
        AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] AdvEngine engine;
		
        //TIPS管理
        TipsManager TipsManager => Engine.GetComponentCacheInChildren(ref tipsManager);
        [SerializeField] TipsManager tipsManager;

        AdvUguiTipsList TipsList => this.GetComponentCache(ref tipsList);
        [SerializeField] AdvUguiTipsList tipsList;
            
        void Awake()
        {
            TipsList.OnInit.AddListener(OnInit);
        }

        //TIPSリストのボタンの追加初期化処理
        //AdvUguiTipsListButtonのOnInitイベントに登録
        void OnInit()
        {
            //TIPの解放率と、解放数と総数をテキストに表示する
            int total = TipsManager.TipsMap.Count;
            int countOpend = 0;
            foreach (var keyValuePair in TipsManager.TipsMap)
            {
                var tipsInfo = keyValuePair.Value;
                if(tipsInfo.IsOpened)
                {
                    countOpend++;
                }
            }
            
            int percent = (int)((float)countOpend / total * 100);
            text.text = $"TIPS {countOpend}/{total} : {percent}%"; 
        }
    }

}