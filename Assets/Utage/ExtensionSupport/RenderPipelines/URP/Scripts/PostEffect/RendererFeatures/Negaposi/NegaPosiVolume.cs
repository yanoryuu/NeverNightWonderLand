#if UTAGE_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //Negaposi用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/NegaPosi Volume")]
    public class NegaPosiVolume : VolumeComponent, IPostProcessComponent
    {
        public bool Enable => enable.value;
        [SerializeField] BoolParameter enable = new (false);
        
        public bool IsActive()
        {
            return Enable;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
#endif
