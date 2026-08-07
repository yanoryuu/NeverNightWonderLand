using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
    public class SampleBacklogCharacterIcon : MonoBehaviour
    {
        //キャラクターアイコン
        public Image characterIcon;

        //アイコン情報
        [System.Serializable]
        public class IconInfo
        {
            public string characterLabel;
            public Sprite sprite;
        }
        public List<IconInfo> iconInfos = new();

        private AdvUguiBacklogTMP Backlog => this.GetComponentCache(ref backlog);
        AdvUguiBacklogTMP backlog;
        void Awake()
        {
            Backlog.OnInit.AddListener(OnInit);
        }

        public void OnInit()
        {
            IconInfo iconInfo = null;
            //バックログの中のキャラクターラベルを取得
            var characterLabel = Backlog.Data.GetCharacterLabel();

            //キャラクターラベルに対応するアイコン情報を取得
            if (!string.IsNullOrEmpty(characterLabel))
            {
                foreach (var info in iconInfos)
                {
                    if(info.characterLabel == characterLabel)
                    {
                        iconInfo = info;
                        break;
                    }
                }
            }

            if (iconInfo == null)
            {
                //アイコン情報が見つからない場合は非表示
                characterIcon.gameObject.SetActive(false);
            }
            else
            {
                //キャラクターラベルに対応するアイコンを表示
                characterIcon.gameObject.SetActive(true);
                characterIcon.sprite = iconInfo.sprite;
            }
        }
    }
}
