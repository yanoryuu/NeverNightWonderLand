using UnityEngine;

namespace Utage
{
    //ポストエフェクトのコンポーネントに共通のインターフェース
    public interface IPostEffect
    {
        GameObject gameObject { get; }
        bool enabled { get; set; }
    }
    
    //ポストエフェクトのコンポーネントのうち、強度を持つものに共通のインターフェース
    public interface IPostEffectStrength : IPostEffect
    {
        float Strength { get; set; }
    }

    //ポストエフェクトのコンポーネントのうち、ポストエフェクト用のVolumeに共通のインターフェース
    public interface IPostEffectVolumeObject : IPostEffectStrength
    {
        void OnClear();
    }
}
