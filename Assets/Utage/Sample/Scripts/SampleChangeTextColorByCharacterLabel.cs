using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //キャラクターラベルによってメッセージウィンドウのテキスト色を変える
    public class SampleChangeTextColorByCharacterLabel : MonoBehaviour
    {
        //メッセージウィンドウ（おなじオブジェクトにアタッチすること）
        AdvUguiMessageWindowTMP MessageWindow => this.GetComponentCache(ref messageWindow);
        AdvUguiMessageWindowTMP messageWindow;

        //キャラクターラベルとテキスト色の対応
        [Serializable]
        class CharacterTextColor
        {
            public string characterLabel;
            public Color color = Color.white;
        }

        //キャラクターラベルとテキスト色の対応のリスト
        [SerializeField] List<CharacterTextColor> characterTextColors = new List<CharacterTextColor>();
        //キャラクターラベルがない場合のデフォルトの色
        public Color defaultColor = Color.white;

        void Awake()
        {
            MessageWindow.OnPostChangeText.AddListener(OnPostChangeText);
        }

        void OnDestroy()
        {
            MessageWindow.OnPostChangeText.RemoveListener(OnPostChangeText);
        }

        void OnPostChangeText(AdvMessageWindow message)
        {
            var text = MessageWindow.TextPro;
            //キャラクターラベルによって、テキストの色を変える
            var characterTextColor = characterTextColors.Find(x => x.characterLabel == message.CharacterLabel);
            if (characterTextColor != null)
            {
                text.Color = characterTextColor.color;
            }
            else
            {
                //キャラクターラベルがない場合は、デフォルトの色に戻す
                text.Color = defaultColor;
            }
        }
    }    
}

