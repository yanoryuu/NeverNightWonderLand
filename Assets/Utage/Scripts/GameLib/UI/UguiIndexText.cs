using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
    //インデックスのテキスト表示
    public class UguiIndexText : MonoBehaviour, 
        IUguiIndex
    {
        [HideIfTMP] protected Text text;
        [SerializeField, HideIfLegacyText] protected TextMeshProUGUI textMeshPro;
        
        //テキスト表示する場合の、インデックスに対するオフセット（主に0番を1と表示するためもの）
        [SerializeField] protected int indexOffsetToText = 1;
        //テキスト表示する場合の、フォーマット
        [SerializeField] protected string formatIndexText = "{0}";
        
        //現在のインデックス
        public int Index { get; private set; }
        
        public virtual void SetIndex(int index, int length)
        {
            Index = index;
            TextComponentWrapper.SetText(text, textMeshPro, string.Format(formatIndexText, index + indexOffsetToText));
        }
    }
}
