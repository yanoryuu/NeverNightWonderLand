#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Utage
{
    public static class UndoEx
    {
        public static void RemoveComponent<T>(GameObject go)
            where T : Component 
        {
            var component = go.GetComponent<T>();
            if(component==null) return;
            Undo.DestroyObjectImmediate(component);
        }
    }
}
#endif
