using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
    //Unityのプロジェクト設定のボリューム（AudioListener.volume）を使ってミュートを制御するサンプル
    public class SampleMuteAudioListener : MonoBehaviour
    {
        //ミュート切り替えのToggleボタン
        [SerializeField] Toggle toggle;
        
        // ミュート状態かどうか
        public bool IsMute => AudioListener.volume <= 0f;
        
        void Awake()
        {
            toggle.onValueChanged.AddListener(SetMute);
        }
        
        void OnEnable()
        {
            toggle.isOn = IsMute;
        }

        // ミュートのオンオフ設定
        public void SetMute(bool mute)
        {
            AudioListener.volume = mute ? 0.0f : 1.0f;
        }
        
        // ミュートのオンオフ切り替え
        public void ToggleMute()
        {
            SetMute(!IsMute);
        }
    }
}