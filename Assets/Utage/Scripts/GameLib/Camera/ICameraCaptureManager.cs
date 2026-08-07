using System;
using UnityEngine;

namespace Utage
{
    public interface ICameraCaptureManager
    {
        public void Capture(Camera targetCamera, RenderTexture captureTextureToWrite, Action onComplete);
    }
}
