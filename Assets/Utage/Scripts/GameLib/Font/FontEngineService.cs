using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Utage
{
    //フォントエンジンの初期化とフォントのロードを行う
    public class FontEngineService : IDisposable
    {
        public bool Error { get; }
        public FontEngineService(Font font)
        {
            Error = !TryLoadFont(font);
        }
        
        bool TryLoadFont(Font font)
        {
            if (font == null)
            {
                Debug.LogError("Font is null");
                return false;
            }

            FontEngineError errorCode = FontEngine.InitializeFontEngine();
            if (errorCode != FontEngineError.Success)
            {
                Debug.LogError("FontEngine.InitializeFontEngine - Error [" + errorCode + "]");
                return false;
            }

            errorCode = FontEngine.LoadFontFace(font);

            if (errorCode != FontEngineError.Success)
            {
                Debug.LogError("FontEngine.LoadFontFace - Error [" + errorCode + "]", font);
                return false;
            }

            return true;
        }
        
        //指定のユニコードがフォント内にあるか
        public bool IsUnicodeInFont(uint unicode)
        {
            return TryGetGlyphIndex(unicode, out uint glyphIndex);
        }

        //指定のユニコードのグリフインデックスを取得
        public bool TryGetGlyphIndex(uint unicode, out uint glyphIndex)
        {
            return FontEngine.TryGetGlyphIndex(unicode, out glyphIndex);
        }

        //収録されているすべてのユニコードとグリフインデックスを取得
        public IEnumerable<(uint unicode, uint glyphIndex)> GetAllUnicodeAndGlyphIndex()
        {
            const uint minUnicode = 0;
            const uint maxUnicode = 0x10FFFF;

            for (uint unicode = minUnicode; unicode <= maxUnicode; unicode++)
            {
                if (TryGetGlyphIndex(unicode, out uint glyphIndex))
                {
                    yield return (unicode, glyphIndex);
                }
            }
        }

        public void Dispose()
        {
            if (!Error)
            {
                FontEngine.UnloadFontFace();
            }
        }
    }
}
