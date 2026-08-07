using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UtageExtensions;

namespace Utage
{
    //TIPS系のUIを制御するコンポーネント
    public class SampleUiTipsMasterDetailUiController : MonoBehaviour
    {
        public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] AdvEngine engine;

        public UtageUguiMainGame MainGame => this.GetComponentCacheFindIfMissing(ref mainGame);
        [SerializeField] UtageUguiMainGame mainGame;

        public UtageUguiTitle Title => this.GetComponentCacheFindIfMissing(ref title);
        [SerializeField] UtageUguiTitle title;

        public SampleUiTipsMasterDetail TipsMasterDetail => this.GetComponentCacheFindIfMissing(ref tipsMasterDetail);
        [SerializeField] SampleUiTipsMasterDetail tipsMasterDetail;

        
        //メインゲーム中のシナリオでTIPSがクリックされた時の処理
        public virtual void OnClickTipsInMainGame(TipsInfo tipsInfo)
        {
            MainGame.Close();
            TipsMasterDetail.Open(tipsInfo, MainGame);
        }

        //メインゲーム中でTIPS画面を開くボタンがクリックされた時の処理
        public virtual void OnClickTipsListInMainGame()
        {
            MainGame.Close();
            TipsMasterDetail.Open(MainGame);
        }

        //タイトル画面でTIPS画面を開くボタンがクリックされた時の処理
        public virtual void OnClickTipsListFromTitle()
        {
            OnClickTipsListFromTitle(true);
        }
        public virtual void OnClickTipsListFromTitle(bool closeTitle)
        {
            if (closeTitle)
            {
                Title.Close();
            }
            TipsMasterDetail.Open(Title);
        }
    }
}
