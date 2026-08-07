using UnityEngine.EventSystems;

namespace Utage
{
    public interface ITextMeshProLinkEvent
    {
    }

    public interface ITextMeshProLinkEventChangeText : ITextMeshProLinkEvent
    {
        void OnChangeText();
    }

    public interface ITextMeshProLinkEventEnter : ITextMeshProLinkEvent
    {
        void OnEnterLink(int linkIndex);
    }

    public interface ITextMeshProLinkEventExit : ITextMeshProLinkEvent
    {
        void OnExitLink(int oldLinkIndex);
    }

    public interface ITextMeshProLinkEventClick : ITextMeshProLinkEvent
    {
        void OnClickLink(int linkIndex);
    }

    public interface ITextMeshProLinkEventClickNoLink : ITextMeshProLinkEvent
    {
        void OnClickNoLink(PointerEventData data);
    }

}
