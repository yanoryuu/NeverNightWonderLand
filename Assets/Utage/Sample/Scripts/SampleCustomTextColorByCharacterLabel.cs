// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //キャラクターごとにテキストの色を変えるサンプル
    public class SampleCustomTextColorByCharacterLabel : MonoBehaviour
    {
        AdvUguiMessageWindowTMP MessageWindow => this.GetComponentCache(ref messageWindow);
        AdvUguiMessageWindowTMP messageWindow;
       
        [Serializable]
        class CharacterTextColor
        {
            //キャラクターごとのテキスト色の設定
            public string characterLabel;
            public Color color = Color.white;
            public Color outlineColor = Color.white;
            public Color nameColor = Color.white;
            public Color nameOutlineColor = Color.white;
        }
        [SerializeField] List<CharacterTextColor> characterTextColors = new List<CharacterTextColor>();
        
        //デフォルトのテキスト色の設定
        public Color defaultColor = Color.white;
        public Color defaultOutlineColor = Color.white;
        public Color defaultNameColor = Color.white;
        public Color defaultNameOutlineColor = Color.white;

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
            var nameText = MessageWindow.NameTextPro;
            //キャラクターラベルによって、テキストの色を変える
            var characterTextColor = characterTextColors.Find(x => x.characterLabel == message.CharacterLabel);
            if (characterTextColor != null)
            {
                text.Color = characterTextColor.color;
                text.TextMeshPro.outlineColor = characterTextColor.outlineColor;
                
                nameText.Color = characterTextColor.nameColor;
                nameText.TextMeshPro.outlineColor = characterTextColor.nameOutlineColor;
            }
            else
            {
                //キャラクターラベルがない場合は、デフォルトの色に戻す
                text.Color = defaultColor;
                text.TextMeshPro.outlineColor = defaultOutlineColor;
                
                nameText.Color = defaultNameColor;
                nameText.TextMeshPro.outlineColor = defaultNameOutlineColor;
            }
        }
    }
}
