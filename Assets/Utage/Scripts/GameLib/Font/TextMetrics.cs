using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Utage
{
    //TextMetricsの情報
    [System.Serializable]
    public class TextMetrics
    {
#if UNITY_6000_0_OR_NEWER
        //Unity6000.3以降で型がintからfloatに変わったので、その対応
        [SerializeField] float pointSize;
#else
        [SerializeField] int pointSize;
#endif
        [SerializeField] float lineHeight;
        [SerializeField] float ascentLine;
        [SerializeField] float capLine;
        [SerializeField] float meanLine;
        [SerializeField] float baseline;
        [SerializeField] float descentLine;
        [SerializeField] float superscriptOffset;
        [SerializeField] float superscriptSize;
        [SerializeField] float subscriptOffset;
        [SerializeField] float subscriptSize;
        [SerializeField] float underlineOffset;
        [SerializeField] float underlineThickness;
        [SerializeField] float strikethroughOffset;
        [SerializeField] float strikethroughThickness;
        [SerializeField] float tabWidth;

        public float AscentLine => ascentLine;
        public float DescentLine => descentLine;

        public TextMetrics()
        {
        }

        public TextMetrics(FaceInfo faceInfo)
        {
            pointSize = faceInfo.pointSize;

            lineHeight = faceInfo.lineHeight;
            ascentLine = faceInfo.ascentLine;
            capLine = faceInfo.capLine;
            meanLine = faceInfo.meanLine;
            baseline = faceInfo.baseline;
            descentLine = faceInfo.descentLine;
            superscriptOffset = faceInfo.superscriptOffset;
            superscriptSize = faceInfo.superscriptSize;
            subscriptOffset = faceInfo.subscriptOffset;
            subscriptSize = faceInfo.subscriptSize;
            underlineOffset = faceInfo.underlineOffset;
            underlineThickness = faceInfo.underlineThickness;
            strikethroughOffset = faceInfo.strikethroughOffset;
            strikethroughThickness = faceInfo.strikethroughThickness;
            tabWidth = faceInfo.tabWidth;
        }
        
        //指定のascent, descentで各ラインを調整する
        public void AdjustLine(float ascent, float descent)
        {
            //ほとんどのフォントは、中央揃え配置で使う中心（AscenderLine+DecentLine）/2と、Capline配置で使う中心（CapLine /2）が一致するように
            //AscentLineが設定されているようなので、そのようにAscentLineを設定する。
            
            //新しいdescentLineを計算。指定のdescentと、ascentとcapLineから逆算した値のうち、低いほうを採用
            this.descentLine = Mathf.Min(descent,this.capLine - ascent);
            //新しいdescentLineからascentLineを逆算
            this.ascentLine = this.capLine - descentLine;
            Debug.Log($"ascentLine{ascentLine} capLine{capLine} descentLine{descentLine}\n" +
                      $"center{(ascentLine + descentLine) / 2} capCenter{capLine / 2}\n"+
                      $"source ascent {ascent} source descent{descent}\n");

            //Superscript OffsetはAscenderLineに合わせる
            superscriptOffset = this.ascentLine;
            //Subscript OffsetはDecentLineに合わせる
            subscriptOffset = this.descentLine;
            //LineHeightはAscenderLineのDecentLine合計サイズにする
            lineHeight = this.ascentLine - this.descentLine;
        }

        //指定のフォントアセットに、テキストメトリクスを設定可能かチェック
        public bool EnableApply(TMP_FontAsset fontAsset, bool debuglog)
        {
            var originalFaceInfo = fontAsset.faceInfo;
            if (IsEqualPointSize(fontAsset))
            {
                if (debuglog)
                {
                    Debug.LogError(
                        $"Font{fontAsset.name} PointSize({originalFaceInfo.pointSize}) is not TextMetrics PointSize{this.pointSize}",
                        fontAsset);                    
                }
                return false;
            }
            return true;
        }
        
        //指定のフォントアセットに、テキストメトリクスを設定する
        public void ApplyToFontAsset(TMP_FontAsset fontAsset)
        {
            var originalFaceInfo = fontAsset.faceInfo;
            if(!IsEqualPointSize(fontAsset))
            {
                Debug.LogError($"Font{fontAsset.name} PointSize({originalFaceInfo.pointSize}) is not TextMetrics PointSize{this.pointSize}",fontAsset);
                return;
            }
            var newFaceInfo = CreateFaceInfo(fontAsset.faceInfo);
            if (!fontAsset.faceInfo.Equals(newFaceInfo))
            {
                Debug.Log($"{fontAsset.name} TextMetrics Changed",fontAsset);
                fontAsset.faceInfo = newFaceInfo;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(fontAsset);
#endif
            }
        }
        
        //PointSizeの比較
        bool IsEqualPointSize(TMP_FontAsset fontAsset)
        {
            var originalPointSize = fontAsset.faceInfo.pointSize;
#if UNITY_6000_0_OR_NEWER
            //Unity6000.3以降で型がintからfloatに変わったので、その対応
            return Mathf.Approximately(originalPointSize ,pointSize);
#else
            return originalPointSize == pointSize;
#endif
        }

        FaceInfo CreateFaceInfo(FaceInfo srcFaceInfo)
        {
            //構造体なのでコピー(FamilyNameだったりの、TextMetrics以外の情報をコピーする)
            FaceInfo faceInfo = srcFaceInfo;
            faceInfo.lineHeight = lineHeight;
            faceInfo.ascentLine = ascentLine;
            faceInfo.capLine = capLine;
            faceInfo.meanLine = meanLine;
            faceInfo.baseline = baseline;
            faceInfo.descentLine = descentLine;
            faceInfo.superscriptOffset = superscriptOffset;
            faceInfo.superscriptSize = superscriptSize;
            faceInfo.subscriptOffset = subscriptOffset;
            faceInfo.subscriptSize = subscriptSize;
            faceInfo.underlineOffset = underlineOffset;
            faceInfo.underlineThickness = underlineThickness;
            faceInfo.strikethroughOffset = strikethroughOffset;
            faceInfo.strikethroughThickness = strikethroughThickness;
            faceInfo.tabWidth = tabWidth;

            return faceInfo;
        }
    }
}
