using System;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    public class SampleCustomUiState : MonoBehaviour
    {
        AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine); 
        [SerializeField] AdvEngine engine;
        
        //表示のオンオフをしたい宴のレイヤー名のリスト
        [SerializeField] List<string> layerNames = new ();
        
        AdvUguiManager UiManager => Engine.UiManager as AdvUguiManager;

        void Awake()
        {
            UiManager.OnChangeStatus.AddListener(OnChangeUiStatus);
        }

        //UIの状態が変わったときに呼ばれる
        void OnChangeUiStatus()
        {
            switch (UiManager.Status)
            {
                case AdvUiManager.UiStatus.Backlog:
                    ChangeLayerVisible(false);
                    break;
                case AdvUiManager.UiStatus.HideMessageWindow:
                    ChangeLayerVisible(false);
                    break;
                case AdvUiManager.UiStatus.Default:
                    ChangeLayerVisible(true);
                    break;
            }
        }
        
        //レイヤーの表示、非表示を切り替える
        void ChangeLayerVisible(bool visible)
        {
            foreach (var layerName in layerNames)
            {
                AdvGraphicLayer layer = Engine.GraphicManager.FindLayer(layerName);
                if (layer == null)
                {
//                Debug.LogError("レイヤーが見つかりません");
                    continue;
                }
                layer.Canvas.enabled = visible;
                
            }
        }
    }
}