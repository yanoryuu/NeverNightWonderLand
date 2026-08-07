using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UtageExtensions;

namespace Utage
{
    //TIPS系のUIを制御するコンポーネント
    public class AdvUguiTipsUiController : MonoBehaviour
    {
        public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] AdvEngine engine;

        public UtageUguiMainGame MainGame => this.GetComponentCacheFindIfMissing(ref mainGame);
        [SerializeField] UtageUguiMainGame mainGame;

        public UtageUguiTitle Title => this.GetComponentCacheFindIfMissing(ref title);
        [SerializeField] UtageUguiTitle title;

        public AdvUguiTipsDetail TipsDetail => this.GetComponentCacheFindIfMissing(ref tipsDetail);
        [SerializeField] AdvUguiTipsDetail tipsDetail;

        public AdvUguiTipsList TipsList => this.GetComponentCacheFindIfMissing(ref tipsList);
        [SerializeField] AdvUguiTipsList tipsList;

        
        //メインゲーム中のシナリオでTIPSがクリックされた時の処理
        public virtual void OnClickTipsInMainGame(TipsInfo tipsInfo)
        {
            MainGame.Close();
            TipsDetail.Open(tipsInfo, MainGame);
        }

        //メインゲーム中でTIPS一覧画面を開くボタンがクリックされた時の処理
        public virtual void OnClickTipsListInMainGame()
        {
            MainGame.Close();
            TipsList.Open(MainGame);
        }

        //タイトル画面でTIPS一覧画面を開くボタンがクリックされた時の処理
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
            TipsList.Open(Title);
        }
    }
}
