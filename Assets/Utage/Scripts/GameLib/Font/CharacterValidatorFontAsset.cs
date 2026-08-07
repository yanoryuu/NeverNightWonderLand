using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Utage
{
    //文字化けを防ぐために、想定外の文字が含まれていないかチェックするクラス
    public class CharacterValidatorFontAsset : CharacterValidator
    {
        public TMP_FontAsset FontAsset { get; }
        public override Object TargetAsset => FontAsset;

        public CharacterValidatorFontAsset(TMP_FontAsset fontAsset)
        {
            FontAsset = fontAsset;
            AddCharacterInFontAsset(FontAsset);

            uint[] whiteSpaceUnicodeArray =
            {
                0x03, // End of Text \u0003 
                0x09, // Tab   \u0009
                0x0A, // Line Feed (LF) \u000A
                0x0B, // Vertical Tab (VT) \u000B
                0x0D, // Carriage Return (CR) \u000D
                0x061C, // Arabic Letter Mark \u061C
                0x200B, // Zero Width Space <ZWSP> \u2000B
                0x200E, // Left-To-Right Mark \u200E
                0x200F, // Right-To-Left Mark \u200F
                0x2028, // Line Separator \u2028
                0x2029, // Paragraph Separator \u2029
                0x2060, // Word Joiner <WJ> / Zero Width Non-Breaking Space \u2060
            };

            foreach (var unicode in whiteSpaceUnicodeArray)
            {
                this.AddEnableCharacter(unicode);
            }
        }

        void AddCharacterInFontAsset(TMP_FontAsset asset)
        {
            foreach (var character in asset.characterTable)
            {
                this.AddEnableCharacter(character.unicode);
            }
        }

        public void AddFallbackFont(TMP_FontAsset fallbackFont)
        {
            foreach (var asset in TextMeshProUtil.GetFontAssetsWithFallback(fallbackFont))
            {
                AddCharacterInFontAsset(asset);
            }
        }

        public void AddSpriteAsset(TMP_SpriteAsset spriteAsset)
        {
            foreach (var sprite in TextMeshProUtil.GetSpriteAssetsWithFallback(spriteAsset))
            {
                foreach (var character in sprite.spriteCharacterTable)
                {
                    var unicode = character.unicode;
                    if (unicode != 0xfffe)
                    {
                        this.AddEnableCharacter(unicode);
                    }
                }
            }
        }
    }
}
