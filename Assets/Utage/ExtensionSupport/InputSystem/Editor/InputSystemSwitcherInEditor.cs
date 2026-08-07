#if UTAGE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Utage.InputSystem
{
    static class InputSystemSwitcherInEditor
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitializeOnLoad()
        {
            InputManagerToInputSystemSwitcherInEditor.OnSwitch -= OnSwitch;
            InputManagerToInputSystemSwitcherInEditor.OnSwitch += OnSwitch;
            return;
                
            static void OnSwitch(InputManagerToInputSystemSwitcherInEditor switcher)
            {
                if (switcher == null)
                {
                    return;
                }

                var standaloneInputModule = switcher.GetComponent<StandaloneInputModule>();
                if(standaloneInputModule==null) return;
                
                var gameObject = standaloneInputModule.gameObject;
                SwitchInputManagerToInputSystem(standaloneInputModule);
                Debug.LogWarning("Switched from InputManager to InputSystem automatically.\nThis feature is for sample scenes and works only in the editor.\nIf you want to build the sample, please convert it to InputSystem manually.",gameObject);
            };
        }
        
        //InputManagerからInputSystemに切り替える
        public static void SwitchInputManagerToInputSystem(StandaloneInputModule legacy)
        {
            if(legacy ==null) return;
            
            GameObject go = legacy.gameObject;
            // 新しいInputSystemUIInputModuleを追加（なければ）
            var inputModule = go.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = go.AddComponent<InputSystemUIInputModule>();
            }
            
            // StandaloneInputModuleを削除
            Object.DestroyImmediate(legacy);
        }
    }
}
#endif
