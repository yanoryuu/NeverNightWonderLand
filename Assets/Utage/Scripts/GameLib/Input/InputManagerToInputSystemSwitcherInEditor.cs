using System;
using UnityEngine;

namespace Utage
{
    //プロジェクトでInputSystemが利用可能であれば、InputManagerを自動的にInputSystemに切り替えるためのコンポーネント
    //アセンブリ管理のために、コールバックを使う
    //主にサンプルで使用するためのものなので、エディタ時のみ動く（グローバルなイベントを使ってるので、ランタイムに余計な負荷をかけないため）
    public class InputManagerToInputSystemSwitcherInEditor　:MonoBehaviour
    {
#if UNITY_EDITOR        
        [field:StaticField] public static event Action<InputManagerToInputSystemSwitcherInEditor> OnSwitch;

        void Awake()
        {
            OnSwitch?.Invoke(this);
        }
#endif
    }
}
