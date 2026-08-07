#if UTAGE_URP
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UtageExtensions;

namespace Utage.RenderPipeline.Urp
{
    //カラーフェード用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/Color Fade Volume")]
    public class ColorFadeVolume : VolumeComponent
        , IPostProcessComponent
    {
        public float Strength
        {
            get => strength.value;
        }
        [SerializeField] ClampedFloatParameter strength = new (0, 0,1.0f);

        public Color Color
        {
            get => color.value;
            set => color.value = value;
        }
        [SerializeField] NoInterpColorParameter color = new (Color.black);
        
        public bool IsActive()
        {
            return Strength > 0;
        }

        public bool IsTileCompatible()
        {
            return false;
        }

        public string SaveName => "ColorFadeVolume";

        const int Version = 0;

        //セーブデータ用のバイナリ書き込み
        public void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Color);
        }

        //セーブデータ用のバイナリ読み込み
        public void Read(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            if (version < 0 || version > Version)
            {
                Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
                return;
            }
            
            Color = reader.ReadColor();
        }
    }
}
#endif
