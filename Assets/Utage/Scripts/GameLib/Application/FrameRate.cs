using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    public class FrameRate : MonoBehaviour
    {
        public int DefaultFrameRate
        {
            get => defaultFrameRate;
            set => defaultFrameRate = value;
        }
        [SerializeField] int defaultFrameRate = 60;

        //起動時にフレームレートが指定されていたら、そのフレームレートに合わせる
        protected virtual void Awake()
        {
            if (defaultFrameRate > 0)
            {
                ChangeFramerate(defaultFrameRate);
            }
        }

        public void ChangeFramerate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
        }
    }
}
