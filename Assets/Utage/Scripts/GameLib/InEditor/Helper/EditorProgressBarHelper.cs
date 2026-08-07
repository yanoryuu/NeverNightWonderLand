#if UNITY_EDITOR

using System;
using UnityEditor;

namespace Utage
{
    //Unityエディタ上にプログレスバーを表示させるためのヘルパー
    public class EditorProgressBarHelper : IDisposable
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsPlaying { get; private set; }

        public int MainStepMax { get; private set; }
        public int MainStepCount { get; private set; }


        public int SubStepMax { get; private set; }
        public int SubStepCount { get; private set; }

        public float Progress { get; private set; }

        public EditorProgressBarHelper(string title)
        {
            Title = title;
        }
        
        //メインステップの最大値を初期化
        public void InitMainStep(int mainStep)
        {
            MainStepMax = mainStep;
            MainStepCount = 0;
        }

        //サブステップをリセット
        public void ResetSubStep(int subStep, string message)
        {
            Message = message; 
            SubStepMax = subStep;
            SubStepCount = 0;
            DisplayProgressBar();
        }

        //サブステップを加算
        public void AddSubStep()
        {
            SubStepCount++;
            if (SubStepCount >= SubStepMax)
            {
                SubStepCount = 0;
                MainStepCount++;
            }
            DisplayProgressBar();
        }

        // プログレスバーの表示
        public void DisplayProgressBar()
        {
            IsPlaying = true;
            UpdateProgress();
            EditorUtility.DisplayProgressBar(Title,$"{Message} {Progress:F2}%", Progress);
        }

        void UpdateProgress()
        {
            float subProgress = SubStepMax <= 0 ? 0 : (float)SubStepCount / SubStepMax;
            Progress = MainStepMax <= 0 ? 0 : ((float) MainStepCount  + subProgress)  / MainStepMax;
        }

        // プログレスバーの表示を終了
        public void End()
        {
            if(!IsPlaying) return;
            IsPlaying = false;
            EditorUtility.ClearProgressBar();
        }
        
        public void Dispose()
        {
            End();
        }
    }
}

#endif
