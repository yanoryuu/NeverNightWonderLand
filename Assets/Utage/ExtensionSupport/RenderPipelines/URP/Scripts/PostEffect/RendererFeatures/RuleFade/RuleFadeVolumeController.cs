#if UTAGE_URP
using System.IO;
using UnityEngine;
using UtageExtensions;

namespace Utage.RenderPipeline.Urp
{
    public class RuleFadeVolumeController : AdvVolumeComponentController<RuleFadeVolume>
    {

        public override string SaveName => "RuleFade";

        public override void OnClear()
        {
            this.SetActive(false);
        }

        public void SetRuleTexture(Texture2D texture)
        {
            VolumeComponent.RuleTexture = texture;
        }

        public void SetVague(float vague)
        {
            VolumeComponent.Vague = vague;
        }
        
        public void SetColor(Color color)
        {
            VolumeComponent.Color = color;
        }

        protected override int Version => 0;

        public override void OnWrite(BinaryWriter writer)
        {
            var textureName = "";
            if (VolumeComponent.RuleTexture != null)
            {
                textureName = VolumeComponent.RuleTexture.name;
            }
            writer.Write(textureName);
            writer.Write(VolumeComponent.Vague);
            writer.Write(VolumeComponent.Color);
        }

        public override void OnRead(BinaryReader reader, int version)
        {
            var textureName = reader.ReadString();
            var cameraPostEffectManager = this.GetComponentInParent<AdvCameraPostEffectManager>(true);
            if(cameraPostEffectManager == null)
            {
                Debug.LogError("Not found AdvCameraPostEffectManager");
                return;
            }
            
            var tex = string.IsNullOrEmpty(textureName)
                ? null
                : cameraPostEffectManager.AdvEngine.EffectManager.FindRuleTexture(textureName);  
            SetRuleTexture(tex);
            SetVague(reader.ReadSingle());
            SetColor(reader.ReadColor());
        }
    }
}
#endif
