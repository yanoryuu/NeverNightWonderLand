using UnityEditor;
using UnityEngine;

#if UTAGE_URP_EDITOR

namespace Utage.RenderPipeline.Urp
{
    public static class UrpCameraSwitcherInEditor
    {
        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            //URPのカメラ設定を切り替える
            BrpToUrpCamaraSwitcherInEditor.OnReplaceInEditor -= OnReplaceInEditor;
            BrpToUrpCamaraSwitcherInEditor.OnReplaceInEditor += OnReplaceInEditor;
        }
        
        static void OnReplaceInEditor(BrpToUrpCamaraSwitcherInEditor switcher)
        {
            Switch(switcher);
            Debug.LogWarning("The camera settings for the Built-in Render Pipeline (BRP) have been automatically converted to be compatible with the Universal Render Pipeline (URP).",switcher);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitializeOnLoad()
        {
            BrpToUrpCamaraSwitcherInEditor.OnSwitch -= OnSwitch;
            BrpToUrpCamaraSwitcherInEditor.OnSwitch += OnSwitch;
            return;
        }

        static void OnSwitch(BrpToUrpCamaraSwitcherInEditor switcher)
        {
            Switch(switcher);
            Debug.LogWarning("The camera settings for the Built-in Render Pipeline (BRP) have been automatically converted to be compatible with the Universal Render Pipeline (URP).\nThis feature is for sample scenes and works only in the editor.\nIf you want to build the sample, please convert it manually.",switcher);
        }

        static void Switch(BrpToUrpCamaraSwitcherInEditor switcher)
        {
            if (switcher == null)
            {
                return;
            }
            var cameraManager = switcher.GetComponent<CameraManager>();
            if(cameraManager==null) return;
            
            var converter = new UrpSceneConverter();
            converter.SetUp(cameraManager);
        }
    }
}
#endif
