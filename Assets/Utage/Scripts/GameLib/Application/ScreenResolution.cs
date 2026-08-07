using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    //画面解像度の制御コンポーネント
    public class ScreenResolution : MonoBehaviour
    {
        //デフォルトのウィンドウサイズ
        public int DefaultWindowWidth
        {
            get => defaultWindowWidth;
            set => defaultWindowWidth = value;
        }
        [SerializeField] int defaultWindowWidth = 0;

        public int DefaultWindowHeight
        {
            get => defaultWindowHeight;
            set => defaultWindowHeight = value;
        }
        [SerializeField] int defaultWindowHeight = 0;

        /// <summary>
        /// フルスクリーンか
        /// </summary>
        public bool IsFullScreen
        {
            get { return Screen.fullScreen; }
            set
            {
                if (UtageToolKit.IsPlatformStandAloneOrEditor())
                {
                    //PC版のみ、フルスクリーン切り替え
                    ChangeFullScreenAndResolution(value);
                }
            }
        }

        /// <summary>
        /// フルスクリーン切り替え
        /// </summary>
        public void ToggleFullScreen()
        {
            IsFullScreen = !IsFullScreen;
        }

        //フルスクリーンのオンオフと、スクリーン解像度を変更
        public virtual void ChangeFullScreenAndResolution(bool fullScreen)
        {
            if (fullScreen)
            {
                //全画面にする場合は、モニターの最大解像度に合わせる
                //単にScreen.fullScreenのみをtrueにした場合、低解像度のまま表示のみ拡大されるため、見栄えが良くないため
                //Alt+Enterでもこちらと同じ動作になるはず
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
            }
            else
            {
                if (defaultWindowWidth > 0 && defaultWindowHeight > 0)
                {
                    //デフォルトのウィンドウサイズに戻す
                    //フルスクリーン切り替え前の元のウィンドウサイズは取得できないため、明示的にデフォルトのウィンドウサイズを設定する
                    Screen.SetResolution(defaultWindowWidth, defaultWindowHeight, false);
                }
                else
                {
                    Screen.fullScreen = false;
                }
            }
        }
    }
}
