#if UTAGE_RENDER_PIPELINE
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Utage.RenderPipeline
{
    //VolumeComponentの操作処理コンポーネントの基底クラス
    //キーフレーム操作のためのMonoBehaviour化と、セーブデータの読み書きを行う
    public abstract class AdvVolumeComponentController : MonoBehaviour
    {
        public abstract void SetActive(bool isActive);
        public abstract void OnClear();
        public abstract string SaveName { get; }
        public abstract void Write(BinaryWriter writer);
        public abstract void Read(BinaryReader reader);
    }

    public abstract class AdvVolumeComponentController<T> : AdvVolumeComponentController
        where T : VolumeComponent
    {
        protected AdvPostEffectVolume AdvPostEffectVolume { get; private set; }
        public T VolumeComponent { get; private set; }

        protected virtual void Awake()
        {
            AdvPostEffectVolume = GetComponent<AdvPostEffectVolume>();
            VolumeComponent = AdvPostEffectVolume.TryGetVolumeComponent<T>(out var component) ? component : null;
            if (VolumeComponent == null)
            {
                Debug.LogError("Not found " + typeof(T).Name);
            }
        }

        public override void SetActive(bool isActive)
        {
            VolumeComponent.active = isActive;
        }

        protected abstract int Version { get; }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Version);
            OnWrite(writer);
        }

        public abstract void OnWrite(BinaryWriter writer);

        public override void Read(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            if (version < 0 || version > Version)
            {
                Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
                return;
            }
            OnRead(reader, version);
        }
        public abstract void OnRead(BinaryReader reader, int version);
        

    }
}
#endif
