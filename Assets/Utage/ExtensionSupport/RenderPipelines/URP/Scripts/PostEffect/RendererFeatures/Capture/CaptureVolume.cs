#if UTAGE_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //画面キャプチャ用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/Capture Volume")]
    public class CaptureVolume : VolumeComponent, IPostProcessComponent
    {
        public bool Enable
        {
            get => enable.value;
            set => enable.value = value;
        }

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
