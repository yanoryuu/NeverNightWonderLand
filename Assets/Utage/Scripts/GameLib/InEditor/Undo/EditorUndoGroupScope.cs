#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Utage
{
    //エディターでのUndo処理をグループ化するためのスコープ
    public class EditorUndoGroupScope : IDisposable
    {
        int GroupId { get; }
        public EditorUndoGroupScope(string groupName)
        {
            // アンドゥ操作をまとめるためのグループIDを取得
            GroupId = Undo.GetCurrentGroup();
            // アンドゥ操作の開始
            Undo.SetCurrentGroupName(groupName);
        }

        public void Dispose()
        {
            // アンドゥ操作の終了
            Undo.CollapseUndoOperations(GroupId);
        }
    }
}

#endif
