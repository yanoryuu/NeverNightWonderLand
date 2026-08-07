#if UTAGE_URP
using System.IO;
using UnityEngine;
using UtageExtensions;

namespace Utage.RenderPipeline.Urp
{
    public class ColorFadeVolumeController : AdvVolumeComponentController<ColorFadeVolume>
    {
        public override void OnClear()
        {
            this.SetActive(false);
        }

        public override string SaveName => "ColorFade";

        public void SetColor(Color color)
        {
            VolumeComponent.Color = color;
        }

        protected override int Version => 0;

        public override void OnWrite(BinaryWriter writer)
        {
            writer.Write(VolumeComponent.Color);
        }

        public override void OnRead(BinaryReader reader, int version)
        {
            SetColor(reader.ReadColor());
        }
    }
}
#endif
