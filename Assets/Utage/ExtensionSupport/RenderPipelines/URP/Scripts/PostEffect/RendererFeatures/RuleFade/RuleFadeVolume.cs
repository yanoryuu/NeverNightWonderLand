#if UTAGE_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //カラーフェード用のVolumeComponent
    [Serializable,VolumeComponentMenu("Utage/Rule Fade Volume")]
    public class RuleFadeVolume : VolumeComponent, IPostProcessComponent
    {
        public float Strength
        {
            get => strength.value;
            set => strength.value = value;
        }
        [SerializeField] ClampedFloatParameter strength = new (0, 0,1.0f);

        public Color Color
        {
            get => color.value;
            set => color.value = value;
        }

        [SerializeField] NoInterpColorParameter color = new (Color.black);

        public Texture RuleTexture
        {
            get => ruleTexture.value;
            set => ruleTexture.value = value;
        }
        [SerializeField] NoInterpTextureParameter ruleTexture = new (null);

        public float Vague
        {
            get => vague.value;
            set => vague.value = value;
        }
        [SerializeField] NoInterpClampedFloatParameter vague = new (0.2f, 0.0001f,1.0f);

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
