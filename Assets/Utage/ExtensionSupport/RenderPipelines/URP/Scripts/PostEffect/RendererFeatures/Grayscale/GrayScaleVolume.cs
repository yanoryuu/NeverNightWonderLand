#if UTAGE_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //カラーフェード用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/GrayScale Volume")]
    public class GrayScaleVolume : VolumeComponent, IPostProcessComponent
    {
        public float Strength => strength.value;
        [SerializeField] ClampedFloatParameter strength = new (0, 0,1.0f);
        
        public bool IsActive()
        {
            return Strength > 0;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
#endif
