#if UNITY_EDITOR
using UnityEditor;
#endif
using UtageExtensions;

namespace Utage
{
    //関数呼び出しをする際の情報
    public class GuiDrawerFunction
    {
        //関数名
        public string Function { get; }
        
        //関数呼び出しの対象が、プロパティパスのトップではなく入れ子の内部になるか
        public bool Nested { get; }
        
        //関数呼び出しが無効
        public bool Disable => string.IsNullOrEmpty(Function);
        
        public GuiDrawerFunction(string function, bool nested)
        {
            Function = function;
            Nested = nested;
        }
#if UNITY_EDITOR
        public object GetTargetObject(SerializedProperty property)
        {
            if (!Nested)
            {
                return property.serializedObject.targetObject;
            }
            else
            {
                //入れ子の場合、親オブジェクトを返す
                string propertyPath = property.propertyPath;
                string parentPath = propertyPath.Substring(0, propertyPath.LastIndexOf('.'));
                SerializedProperty parentProperty = property.serializedObject.FindProperty(parentPath);
                return parentProperty.GetValue();
            }
        }
#endif
    }
}
