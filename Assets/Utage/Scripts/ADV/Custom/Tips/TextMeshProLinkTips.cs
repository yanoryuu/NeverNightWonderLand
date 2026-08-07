using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
    //TextMeshProのLinkタグを使ったTips処理
    public class TextMeshProLinkTips
        : MonoBehaviour
            , ITextMeshProLinkEventChangeText
            , ITextMeshProLinkEventEnter
            , ITextMeshProLinkEventExit
            , ITextMeshProLinkEventClick
    {
        TextMeshProLinkEventHandler LinkEventHandler => this.GetComponentCacheCreateIfMissing(ref linkEventHandler);
        TextMeshProLinkEventHandler linkEventHandler;

        public AdvEngine Engine
        {
            protected get => this.GetComponentCacheInParent(ref engine);
            set => engine = value;
        }
        [SerializeField] AdvEngine engine;

        public TipsManager TipsManager
        {
            protected get => Engine.GetComponentCacheInChildren(ref tipsManager);
            set => tipsManager = value;
        }
        [SerializeField] TipsManager tipsManager;

        [SerializeField] Color textColor = new Color(238.0f / 255, 80.0f/255 , 180.0f/255);
        [SerializeField] Color underLineColor = new Color(238.0f / 255, 80.0f / 255, 180.0f / 255,0.0f);
        [SerializeField] Color textColorFocused = new Color(238.0f / 255, 138.0f / 255, 230.0f / 255);
        [SerializeField] Color underLineColorFocused = new Color(238.0f / 255, 138.0f / 255, 230.0f / 255);

        [SerializeField] Color textColorRead = new Color(190.0f / 255, 72.0f / 255, 150.0f / 255);
        [SerializeField] Color underLineColorRead = new Color(190.0f / 255, 72.0f / 255, 150.0f / 255, 0.0f);
        [SerializeField] Color textColorFocusedRead = new Color(200.0f / 255, 110.0f / 255, 170.0f / 255);
        [SerializeField] Color underLineColorFocusedRead = new Color(200.0f / 255, 110.0f / 255, 170.0f/255);

        //TIPSをクリックしたときのイベント
        public UnityEvent<TipsInfo> OnClickTips => onClickLink;
        [SerializeField] UnityEvent<TipsInfo> onClickLink = new();

        const string IdType = "tips";

        public virtual void OnChangeText()
        {
            RefreshTipsColors();
        }

        public virtual void RefreshTipsColors()
        {
            foreach (var linkIndex in LinkEventHandler.GetAllLinkIndexParsedByType(IdType))
            {
                if (!LinkEventHandler.TryGetLinkIdParsedByType(linkIndex, IdType, out string idParsed)) return;
                {
                    var tipsInfo = TipsManager.OpenTips(idParsed);
                    if (tipsInfo == null)
                    {
                        Debug.LogWarning($"TipsInfo not found. idParsed={idParsed}");
                    }
                    LinkEventHandler.SetLinkColor(linkIndex, GetTextColor(tipsInfo, false),
                        GetUnderLineColor(tipsInfo, false));
                }
            }
        }

        public virtual void OnClickLink(int linkIndex)
        {
            if (!LinkEventHandler.TryGetLinkIdParsedByType(linkIndex, IdType, out string idParsed)) return;

            TipsInfo info = TipsManager.GetTipsInfo(idParsed);
            if (info == null) return;
            OnClickTips.Invoke(info);
        }

        public virtual void OnEnterLink(int linkIndex)
        {
            if (!LinkEventHandler.TryGetLinkIdParsedByType(linkIndex, IdType, out string idParsed)) return;

            var tipsInfo = TipsManager.GetTipsInfo(idParsed);
            LinkEventHandler.SetLinkColor(linkIndex, GetTextColor(tipsInfo, true), GetUnderLineColor(tipsInfo, true));
        }

        public virtual void OnExitLink(int linkIndex)
        {
            if (!LinkEventHandler.TryGetLinkIdParsedByType(linkIndex, IdType, out string idParsed)) return;

            var tipsInfo = TipsManager.GetTipsInfo(idParsed);
            LinkEventHandler.SetLinkColor(linkIndex, GetTextColor(tipsInfo, false), GetUnderLineColor(tipsInfo, false));
        }

        protected virtual Color GetTextColor(TipsInfo tipsInfo, bool focused)
        {
            bool hasRead = tipsInfo?.HasRead ?? false;
            if (hasRead)
            {
                return focused ? textColorFocusedRead : textColorRead;
            }
            else
            {
                return focused ? textColorFocused : textColor;
            }
        }

        protected virtual Color GetUnderLineColor(TipsInfo tipsInfo, bool focused)
        {
            bool hasRead = tipsInfo?.HasRead ?? false;
            if (hasRead)
            {
                return focused ? underLineColorFocusedRead : underLineColorRead;
            }
            else
            {
                return focused ? underLineColorFocused : underLineColor;
            }
        }
    }
}
