using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
    //バックログのTIPS処理の補助処理
    //TIPSのリンクがクリックされた時の処理を登録する
    public class AdvUguiBacklogTips
        : MonoBehaviour
    {
        [SerializeField] bool hideBacklogPanel = false;
        void Awake()
        {
            var linkTips = this.GetComponentInChildren<TextMeshProLinkTips>();
            if (linkTips == null) return;
            
            // TIPSのリンクがクリックされた時の処理を登録
            linkTips.OnClickTips.AddListener(OnClick);
        }

        void OnClick(TipsInfo tipsInfo)
        {
            var advEngine = this.GetComponentInParent<AdvEngine>();
            if (advEngine == null) return;

            var tipsUiController = advEngine.GetComponentInChildren<AdvUguiTipsUiController>(true);
            if (tipsUiController == null) return;

            var backLogManager = this.GetComponentInParent<AdvUguiBacklogManager>(true);
            if (backLogManager == null) return;

            if (hideBacklogPanel)
            {
                //バックログを非表示にする
                backLogManager.gameObject.SetActive(false);
            }
            else
            {
                //backLogManagerのUpdateを無効化して、右クリックによる戻る機能を無効化する 
                backLogManager.enabled = false;
            }

            tipsUiController.TipsDetail.onClose.AddListener(OnCloseTipsDetail);
            
            //TIP画面開く
            tipsUiController.TipsDetail.Open(tipsInfo, null);

            //TIPS画面を閉じたときの処理
            void OnCloseTipsDetail()
            {
                if (hideBacklogPanel)
                {
                    //バックログを非表示にしていた場合は戻す
                    backLogManager.gameObject.SetActive(true);
                }
                else
                {
                    backLogManager.enabled = true;
                }
                tipsUiController.TipsDetail.onClose.RemoveListener(OnCloseTipsDetail);
            }
        }
    }
}
