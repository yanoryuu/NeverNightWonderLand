#if UNITY_EDITOR
using System;

namespace Utage
{
    //EditorGuiLayoutでグループ表示をするためのスコープ
    public class EditorGuiLayoutGroupScope : IDisposable
    {
        public EditorGuiLayoutGroupScope(string label)
        {
            UtageEditorToolKit.BeginGroup(label);
        }

        public void Dispose()
        {
            UtageEditorToolKit.EndGroup();
        }
    }
}

#endif
