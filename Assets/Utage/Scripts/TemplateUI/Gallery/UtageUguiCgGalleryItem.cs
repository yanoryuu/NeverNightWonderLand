// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utage;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

namespace Utage
{

    /// <summary>
    /// CGギャラリーの各ボタンのUIのサンプル
    /// </summary>
    [AddComponentMenu("Utage/TemplateUI/UtageUguiCgGalleryItem")]
    public class UtageUguiCgGalleryItem : MonoBehaviour
    {
        public AdvUguiLoadGraphicFile texture;
        [HideIfTMP] public Text count;
        [HideIfLegacyText] public TextMeshProUGUI countTmp;
        [SerializeField] bool keepTextureActive;    //テクスチャのアクティブのオンオフを切り替えるか

        [SerializeField] string formatCount = "{0,2}/{1,2}";
        
        //初期化時に呼ばれるイベント
        public UnityEvent OnInit => onInit;
        [SerializeField] UnityEvent onInit = new();
        
        public AdvCgGalleryData Data
        {
            get { return data; }
        }

        AdvCgGalleryData data;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data">セーブデータ</param>
        /// <param name="index">インデックス</param>
        public virtual void Init(AdvCgGalleryData data, Action<UtageUguiCgGalleryItem> ButtonClickedEvent)
        {
            Init(data);
            
            //コールバックを登録する（宴3までの古いやり方）
            UnityEngine.UI.Button button = this.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(() => ButtonClickedEvent(this));
            button.interactable = data.IsOpened;
        }

        //初期化
        //クリックイベントを登録しない場合
        public virtual void Init(AdvCgGalleryData data)
        {
            this.data = data;
            bool isOpen = data.IsOpened;
            if (isOpen)
            {
                if(!keepTextureActive) texture.gameObject.SetActive(true);
                texture.LoadTextureFile(data.ThumbnailPath);
                SetCountText(string.Format(formatCount, data.NumOpen, data.NumTotal));
            }
            else
            {
                if (!keepTextureActive) texture.gameObject.SetActive(false);
                SetCountText("");
            }
            OnInit.Invoke();
        }

        //クリックイベントを登録しない場合はこちら経由で
        //プレハブ上で、Buttonコンポーネントのインスペクターから登録しておく想定
        public virtual void OnClicked()
        {
            this.GetComponentInParent<UtageUguiCgGallery>().OnClickedButton(this);
        }

        
        public virtual void SetCountText(string text)
        {
            TextComponentWrapper.SetText(count, countTmp, text);
        }

    }
}
