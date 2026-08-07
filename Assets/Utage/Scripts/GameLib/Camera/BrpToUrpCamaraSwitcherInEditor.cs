//waring:CS0414を無視
#pragma warning disable 0414

using System;
using UnityEngine;

namespace Utage
{
    //プロジェクトでURPが利用可能であれば、BRP用のカメラをURP用に切り替える
    //アセンブリ管理のために、コールバックを使う
    //主にサンプルで使用するためのものなので、エディタ時のみ動く（グローバルなイベントを使ってるので、ランタイムに余計な負荷をかけないため）
    public class BrpToUrpCamaraSwitcherInEditor　:MonoBehaviour
    {
        //URPに置き換えたかどうか
        [SerializeField] bool replaced = false; 
#if UNITY_EDITOR        
        [field:StaticField] public static event Action<BrpToUrpCamaraSwitcherInEditor> OnReplaceInEditor;
        [field:StaticField] public static event Action<BrpToUrpCamaraSwitcherInEditor> OnSwitch;

        void Awake()
        {
            if (replaced) return;
            OnSwitch?.Invoke(this);
        }
        
        
        [SerializeField,Hide(nameof(HideReplaceButton)), Button(nameof(Replace))] string replaceToUrp = "ReplaceToURP";

        private bool HideReplaceButton => replaced;

        void Replace()
        {
            if (replaced) return;
            OnReplaceInEditor?.Invoke(this);
            replaced = true;
        }
#endif
    }
}
