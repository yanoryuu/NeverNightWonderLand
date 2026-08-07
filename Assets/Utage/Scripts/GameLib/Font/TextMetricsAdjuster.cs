using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore;

namespace Utage
{
    //TextMeshProのフォントアセットのTextMetricsを調整するための補助ツールアセット
    [CreateAssetMenu(menuName = "Utage/Font/" + nameof(TextMetricsAdjuster))]
    public class TextMetricsAdjuster : ScriptableObject
    {
        //対象のフォントアセット
        TMP_FontAsset Font => font;
        [FormerlySerializedAs("baseFont")] [SerializeField] TMP_FontAsset font = null;
        
        //フォールバック対象のフォントアセット
        //ローカライズしている場合は、候補となる全てのフォントを入れる
        List<TMP_FontAsset> Fallbacks => fallbacks;
        [FormerlySerializedAs("fonts")] [SerializeField] List<TMP_FontAsset> fallbacks = new ();

        //無効になる文字の設定
        [Serializable]
        public class DisableCharacterSettings
        {
            public bool disableCombiningMark = true;
            public string disableCharacters = "〱〲";
        }
        public DisableCharacterSettings DisableCharacters => disableCharacterSettings;
        [SerializeField,UnfoldedSerializable] DisableCharacterSettings disableCharacterSettings = new();

        //AscentLineの最大値
        public float MaxAscent => maxAscent;
        [SerializeField] float maxAscent = 0;

        //DescentLineの最小値
        public float MinDescent => minDescent;
        [SerializeField] float minDescent = 0;

        [SerializeField, Button(nameof(Check), false)]
        string check;

        [SerializeField, Button(nameof(MakeTextMetrics), nameof(DisableMakeTextMetrics), false)]
        string makeTextMetrics;

        //Apply時に適用するTextMetricsの情報
        TextMetrics TextMetrics => textMetrics;
        [SerializeField] TextMetrics textMetrics = new();

        [SerializeField, Button(nameof(Apply), nameof(DisableApply), false)]
        string apply;

        //フォントの設定をチェック
        bool Check()
        {
            return CheckAndCreateAdjusterFonts().Item1;
        }
        
        IEnumerable<TMP_FontAsset> GetFonts()
        {
            yield return Font;
            foreach (var fallback in Fallbacks)
            {
                yield return fallback;
            }
        }
        
        (bool result, List<TextMetricsAdjusterFont> adjusterFonts, TextMetrics metrics) CheckAndCreateAdjusterFonts()
        {
            if( !CheckSub(true) ) return (false, null,null);
            
            bool result = true;

            List<TextMetricsAdjusterFont> adjusterFonts = GetFonts().Select(font => new TextMetricsAdjusterFont(font, this)).ToList();

            var metrics = new TextMetrics(Font.faceInfo);
            //対象フォントアセット内の全ての収録グリフの最大位置を取得
            float ascender = adjusterFonts.Select(x => x.AscenderGlyph.Ascent).Max();
            //対象フォントアセット内の全ての収録グリフの最小位置を取得
            float descender = adjusterFonts.Select(x => x.DecentGlyph.Descent).Min();

            metrics.AdjustLine(ascender, descender);
            if(this.MaxAscent != 0 && metrics.AscentLine > this.MaxAscent)
            {
                Debug.LogWarning($"AscentLine({metrics.AscentLine}) is overed {nameof(MaxAscent)}({this.MaxAscent}).",this);
            }
            if (this.MinDescent != 0 && metrics.DescentLine < this.MinDescent)
            {
                Debug.LogWarning($"DescentLine({metrics.DescentLine}) is overed {nameof(MinDescent)}({this.MinDescent}).", this);
            }

            foreach (var adjusterFont in adjusterFonts)
            {
                if (!adjusterFont.CheckLine())
                {
                    result = false;
                }
            }
            if (!result)
            {
                return (false, adjusterFonts, null);
            }

            return (true, adjusterFonts,metrics);
        }

        bool CheckSub(bool debugLog)
        {
            if (Font == null)
            {
                //フォントがない
                if(debugLog) Debug.LogError("Font is null");
                return false;
            }

            bool result = true;
            for (var i = 0; i < Fallbacks.Count; i++)
            {
                var fallback = Fallbacks[i];
                if (fallback == null)
                {
                    if (debugLog)
                    {
                        Debug.LogError($"Fallback[{i}] is null");
                    }
                    result = false;
                    continue;
                }
                if (fallback.faceInfo.pointSize != font.faceInfo.pointSize)
                {
                    if (debugLog)
                    {
                        Debug.LogError(
                            $"Font{fallback.name} PointSize({fallback.faceInfo.pointSize}) is not {Font.name} PointSize({font.faceInfo.pointSize})",
                            fallback);
                    }

                    return false;
                }
            }
            return result;
        }
        
        //TextMetricsを作成可能かチェック
        bool DisableMakeTextMetrics()
        {
            if (!CheckSub(false)) return true;
            return false;
        }

        //TextMetricsを作成
        void MakeTextMetrics()
        {
            (bool result, List<TextMetricsAdjusterFont> adjusterFonts, TextMetrics metrics) = CheckAndCreateAdjusterFonts();
            if( !result ) return;
            
            this.textMetrics = metrics;
        }

        //TextMetricsを各フォントに適用可能かチェック
        bool DisableApply()
        {
            if (!CheckSub(false)) return true;
            foreach (var tmpFontAsset in GetFonts())
            {
                if( !TextMetrics.EnableApply(tmpFontAsset,false) ) return true;
            }
            return false;
        }

        //TextMetricsを各フォントに適用
        void Apply()
        {
            foreach (var tmpFontAsset in GetFonts())
            {
                TextMetrics.ApplyToFontAsset(tmpFontAsset);
            }
        }
    }
}
