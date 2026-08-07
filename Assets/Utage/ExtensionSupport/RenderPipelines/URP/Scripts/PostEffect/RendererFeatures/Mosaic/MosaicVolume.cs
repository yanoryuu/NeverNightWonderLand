#if UTAGE_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //Mosaic用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/Mosaic Volume")]
    public class MosaicVolume : VolumeComponent, IPostProcessComponent
    {
        public float Size => size.value;
        [SerializeField] FloatParameter size = new(1.0f);

        public Vector2 ReferenceResolution => referenceResolution.value;
        [SerializeField] NoInterpVector2Parameter referenceResolution = new( new Vector2(800,600));
        
        public bool IsActive()
        {
            return Size > 1.0f;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
#endif
