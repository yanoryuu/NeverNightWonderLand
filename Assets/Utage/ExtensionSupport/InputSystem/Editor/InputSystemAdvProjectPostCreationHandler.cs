#if UTAGE_INPUT_SYSTEM
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Utage.InputSystem
{
    //「UTAGE」新規プロジェクト作成時の追加処理
    //InputSystem対応処理を行う
    public class InputSystemAdvProjectPostCreationHandler : IAdvProjectPostCreationHandler
    {
        //エディタ起動時やスクリプトリロード時に、ハンドラーを登録
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            AdvProjectCreator.AddPostCreationHandler(new InputSystemAdvProjectPostCreationHandler());
        }

        //プロジェクト作成後の処理
        public void OnPostCreateProject(AdvProjectCreator project)
        {
            Debug.Log("OnPostCreateProject");
            //シーン新規作成時のみ
            switch (project)
            {
                case IAdvProjectCreatorAddScene:
                case IAdvProjectCreatorAssetOnly:
                    Debug.Log("OnPostCreateProject break");
                    return;
            }
            
            var legacy = SceneManagerEx.GetComponentInActiveScene<StandaloneInputModule>(true);
            if (legacy == null)
            {
                Debug.Log("OnPostCreateProject StandaloneInputModule not found");
                return;
            }
            
            InputSystemSwitcherInEditor.SwitchInputManagerToInputSystem(legacy);
        }
    }
}

#endif
