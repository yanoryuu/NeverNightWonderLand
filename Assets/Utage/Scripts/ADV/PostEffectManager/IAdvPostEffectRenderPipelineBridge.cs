using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //ポストエフェクトをRenderPipelineによって処理を切り替えて実行するためのインターフェース
    public interface IAdvPostEffectRenderPipelineBridge
    {
        (IPostEffectStrength effect, float start, float end) DoCommandColorFade(Camera targetCamera, IAdvCommandFade command);
        (IPostEffectStrength effect, float start, float end) DoCommandRuleFade(Camera targetCamera, IAdvCommandFade command);
        
        //イメージエフェクト用
        (IPostEffect effect, Action onComplete) DoCommandImageEffect(Camera targetCamera, IAdvCommandImageEffect command,  Action onComplete);
        void DoCommandImageEffectAllOff(Camera targetCamera, IAdvCommandImageEffect command, Action onComplete);
        
        //ポストエフェクト用
        IPostEffect DoCommandPostEffect(Camera targetCamera, AdvCommandPostEffect command);
        //指定の名前のポストエフェクト（ボリュームオブジェクト）を探す
        IPostEffectVolumeObject FindVolume(Camera targetCamera, string volumeName);
        
        //指定のカメラに設定されている、エフェクト用のボリュームオブジェクトをすべて取得する
        //エフェクト用というのは、キャプチャとフェード用など専用のポストエフェクト以外のボリュームオブジェクト
        IEnumerable<IPostEffectVolumeObject> GetAllActiveEffectVolumes(Camera targetCamera);
    }
}
