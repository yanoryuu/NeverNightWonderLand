#if UTAGE_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //FishEye用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/Fish Eye Volume")]
    public class FishEyeVolume : VolumeComponent, IPostProcessComponent
    {
        public float Strength => strength.value;
        [SerializeField] ClampedFloatParameter strength = new (0, 0, 1.0f);

        public float IntensityX => intensityX.value;
        [SerializeField] NoInterpClampedFloatParameter intensityX = new (0.5f, 0, 1.0f);

        public float IntensityY => intensityY.value;
        [SerializeField] NoInterpClampedFloatParameter intensityY = new (0.5f, 0, 1.0f);

        public float BaseSize => baseSize.value;
        [SerializeField] NoInterpFloatParameter baseSize = new(80.0f / 512.0f);


        public bool IsActive()
        {
            return Strength * BaseSize > 0;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
#endif
