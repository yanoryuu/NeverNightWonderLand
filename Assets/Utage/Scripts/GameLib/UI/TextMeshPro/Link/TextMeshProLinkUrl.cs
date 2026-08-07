using System;
using TMPro;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    public class TextMeshProLinkUrl
        : MonoBehaviour
            , ITextMeshProLinkEventChangeText
            , ITextMeshProLinkEventEnter
            , ITextMeshProLinkEventExit
            , ITextMeshProLinkEventClick
    {
        TextMeshProLinkEventHandler LinkEventHandler => this.GetComponentCacheCreateIfMissing(ref linkEventHandler);
        TextMeshProLinkEventHandler linkEventHandler;

        [SerializeField] bool enableNoneIdType = true;
        [SerializeField] Color linkTextColor = new Color(64.0f / 255, 160.0f / 255, 1.0f);
        [SerializeField] Color underLineColor = new Color(64.0f / 255, 160.0f / 255, 1.0f, 0f);
        [SerializeField] Color linkTextColorFocused = new Color(128.0f / 255, 200.0f / 255, 1.0f);
        [SerializeField] Color underLineColorFocused = new Color(128.0f / 255, 200.0f / 255, 1.0f);
        
        const string IdType = "url";

        public void OnChangeText()
        {
            foreach (var linkIndex in LinkEventHandler.GetAllLinkIndexParsedByType(IdType, enableNoneIdType))
            {
                LinkEventHandler.SetLinkColor(linkIndex, linkTextColor, underLineColor);
            }
        }

        public void OnClickLink(int linkIndex)
        {
            if (!LinkEventHandler.TryGetLinkIdParsedByType(linkIndex, IdType, out string idParsed, enableNoneIdType)) return;

            string url = idParsed;
            Application.OpenURL(url);
        }

        public void OnEnterLink(int linkIndex)
        {
            if(!LinkEventHandler.CheckLinkIdType(linkIndex,IdType, enableNoneIdType)) return;
            
            LinkEventHandler.SetLinkColor(linkIndex, linkTextColorFocused, underLineColorFocused);
        }

        public void OnExitLink(int linkIndex)
        {
            if (!LinkEventHandler.CheckLinkIdType(linkIndex, IdType, enableNoneIdType)) return;

            LinkEventHandler.SetLinkColor(linkIndex, linkTextColor, underLineColor);
        }
    }
}
