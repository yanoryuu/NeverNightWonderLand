using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;
using System;
using TMPro;

namespace Utage
{
    //ガイドメッセージ表示（画面手前に一定時間だけ表示されるメッセージ）
    public class SystemUiGuideMessage : MonoBehaviour
    {
        [SerializeField, HideIfTMP] protected Text text = null;
        [SerializeField, HideIfLegacyText] protected TextMeshProUGUI textTmp = null;
        [SerializeField] protected CanvasGroup canvasGroup = null; //対象のキャンバスグループ

        [SerializeField] protected float duration = 1.5f;   //表示時間(フェード時間含む)
        [SerializeField] protected float fadInTime = 0.1f;  //フェードイン時間
        [SerializeField] protected float fadOutTime = 0.1f; //フェードアウト時間


        public bool IsPlaying { get; protected set; }
        public float ElapsedTime { get; protected set; }

        public virtual void Open(string message)
        {
            this.gameObject.SetActive(true);
            TextComponentWrapper.SetText(text, textTmp, message);
            IsPlaying = true;
            ElapsedTime = 0;
            if (canvasGroup!=null)
            {
                canvasGroup.alpha = 0;
            }
        }

        protected virtual void Update()
        {
            if(!IsPlaying) return;

            if (ElapsedTime > duration)
            {
                Close();
                return;
            }

            if (canvasGroup != null)
            {
                if (ElapsedTime < fadInTime)
                {
                    canvasGroup.alpha = ElapsedTime / fadInTime;
                }
                else if (ElapsedTime < duration - fadOutTime)
                {
                    canvasGroup.alpha = 1.0f;
                }
                else
                {
                    canvasGroup.alpha = (duration - ElapsedTime) / fadOutTime;
                }
            }

            ElapsedTime += Time.deltaTime;
//          Debug.Log($"ElapsedTime={ElapsedTime} alpha={target.alpha}");
        }

        public virtual void Close()
        {
            IsPlaying = false;
            this.gameObject.SetActive(false);
        }
    }
}
