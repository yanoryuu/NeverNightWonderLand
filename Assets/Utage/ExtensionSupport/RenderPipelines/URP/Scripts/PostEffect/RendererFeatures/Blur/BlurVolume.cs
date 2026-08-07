#if UTAGE_URP
using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //Blur用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/Blur Volume")]
    public class BlurVolume : VolumeComponent, IPostProcessComponent
    {

        public float BlurSize => blurSize.value;
        [SerializeField] ClampedFloatParameter blurSize = new(0.0f, 0, 10.0f);

        public enum BlurType 
        {
            StandardGauss = 0,
            SgxGauss = 1,
        }
        public BlurType Type => blurType.value;
        [SerializeField] VolumeParameter<BlurType> blurType = new ();

        /*      //注　VolumeParameter<T>の補間は下記なので、enumの場合は実質補完されないはず
                enum
                 public virtual void Interp(T from, T to, float t)
                    // Default interpolation is naive
                    m_Value = t > 0f ? to : from;
                }
        */


        public int DownSample => downSample.value;
        [SerializeField] NoInterpClampedIntParameter downSample = new (1, 0, 2);

        public int BlurIterations => blurIterations.value;
        [SerializeField] NoInterpClampedIntParameter blurIterations = new (2, 1,4);


        public bool IsActive()
        {
            return BlurSize > 0;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
#endif
