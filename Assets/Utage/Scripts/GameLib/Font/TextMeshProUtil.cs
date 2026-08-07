using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Utage
{
    public static class TextMeshProUtil
    {
        //フォールバックを含めたフォントアセットを取得
        public static IEnumerable<TMP_FontAsset> GetFontAssetsWithFallback(TMP_FontAsset fontAsset)
        {
            void AddFontAsset(TMP_FontAsset font, HashSet<TMP_FontAsset> fontSet)
            {
                if(font == null) return;
                if (!fontSet.Add(font)) return;

                foreach (var fallbackFont in font.fallbackFontAssetTable)
                {
                    AddFontAsset(fallbackFont, fontSet);
                }
            }

            using (HashSetPool<TMP_FontAsset>.Get(out HashSet<TMP_FontAsset> fontSet))
            {
                AddFontAsset(fontAsset, fontSet);
                foreach (var font in fontSet)
                {
                    yield return font;
                }
            }
        }

        //フォールバックを含めたスプライトアセットを取得
        public static IEnumerable<TMP_SpriteAsset> GetSpriteAssetsWithFallback(TMP_SpriteAsset spriteAsset)
        {
            void AddSpriteAsset(TMP_SpriteAsset sprite, HashSet<TMP_SpriteAsset> spriteSet)
            {
                if (sprite == null) return;
                if (!spriteSet.Add(sprite)) return;

                foreach (var fallback in sprite.fallbackSpriteAssets)
                {
                    AddSpriteAsset(fallback, spriteSet);
                }
            }

            using (HashSetPool<TMP_SpriteAsset>.Get(out HashSet<TMP_SpriteAsset> spriteSet))
            {
                AddSpriteAsset(spriteAsset, spriteSet);
                foreach (var sprite in spriteSet)
                {
                    yield return sprite;
                }
            }
        }


        //マウスの位置にあるリンクを取得
        public static int FindIntersectingLinkAtMousePosition(TMP_Text text)
        {
            return FindIntersectingLink(text, InputUtil.GetMousePosition());
        }

        //指定の位置にあるリンクを取得
        public static int FindIntersectingLink(TMP_Text text, Vector3 position)
        {
#if UNITY_EDITOR
            //マウスが存在しない環境（バッチモードでの自動テスト等）では
            //マウス座標が無限大になり、Camera.ScreenPointToRayが
            //「Screen position out of view frustum」をフレームごとに出力してテストが失敗する。
            //エディタ限定のガードとし、実行時の挙動には影響させない。
            if (float.IsInfinity(position.x) || float.IsNaN(position.x)
                || float.IsInfinity(position.y) || float.IsNaN(position.y))
            {
                //TMPの規定値。リンクが見つからなかったことを表す
                return -1;
            }
#endif
            Camera camera = null;

            if (text.canvas != null && text.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = text.canvas.worldCamera;
            }
            return TMP_TextUtilities.FindIntersectingLink(text, position, camera);
        }
    }
}
