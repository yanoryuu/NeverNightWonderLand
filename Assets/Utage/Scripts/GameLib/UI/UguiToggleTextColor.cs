using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    //Toggleのオンオフで、テキストの色を変える
    public class UguiToggleTextColor : MonoBehaviour
    {
        protected Toggle Toggle => this.GetComponentCache(ref toggle);
        [SerializeField] protected Toggle toggle;
        [SerializeField] protected TextMeshProUGUI text;

        [SerializeField] protected Color onColor = Color.white;
        [SerializeField] protected Color offColor = Color.white;
        
        void Awake()
        {
            Toggle.onValueChanged.AddListener(OnToggleValueChanged);
            OnToggleValueChanged(Toggle.isOn);
        }

        void OnToggleValueChanged(bool isOn)
        {
            text.color = isOn ? onColor : offColor;
        }
    }
}
