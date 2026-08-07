#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UtageExtensions
{
	//SerializedPropertyの拡張メソッド
    public static class SerializedPropertyExtensions
    {
	    //Value（オブジェクト）とFiledInfoを取得
	    //値型の場合はboxingされたものが返る
        public static (object, FieldInfo) GetValueAndFieldInfo(this SerializedProperty property)
        {
	        //指定のオブジェクトのフィールド名から、フィールド情報を取得する
	        //継承元のprivateなフィールドも取得できるようにする（SerializeFiledの場合、必要になるので）
	        FieldInfo GetFiledDeep(object targetObj, string filedName)
	        {
		        Type type = targetObj.GetType();
		        do
		        {
			        FieldInfo fieldInfo = type.GetField(filedName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			        if (fieldInfo != null) return fieldInfo;
			        type = type.BaseType;
		        } while (type!=null);
		        return null;
	        }
	        

	        //ルートのオブジェクト
	        FieldInfo field = null;
	        object obj = property.serializedObject.targetObject;

	        //ルートのオブジェクトから、
	        //.で区切られたパスをたどってオブジェクトを取得していく
	        var propertyPathSplit = property.propertyPath.Split('.');
	        for(int i= 0; i < propertyPathSplit.Length; ++i)
	        {
		        var filedName = propertyPathSplit[i];
		        object parentObj = obj;
		        if (filedName == "Array")
		        {
			        //配列の要素の場合は、
			        //Array.data[インデックス]
			        //がパスとなっている
			        
			        int arrayIndex = int.Parse(propertyPathSplit[i + 1].Replace("data[", "").Replace("]", ""));
			        //配列の各要素にはField情報はない
			        field = null;
			        //配列の要素として内容を取得する
			        obj = ((System.Collections.IList)parentObj)[arrayIndex];
			        i += 1;
		        }
		        else
		        {
			        field = GetFiledDeep(parentObj,filedName);
			        if (field == null)
			        {
				        Debug.LogError($"Not found field {filedName} in {property.propertyPath}",
					        property.serializedObject.targetObject);
			        }
			        else
			        {
				        obj = field.GetValue(parentObj);
			        }
		        }
	        }
	        return (obj,field);
        }

        //Valueを取得
        //値型の場合はboxingされたものが返る
        public static object GetValue(this SerializedProperty property)
        {
	        return property.GetValueAndFieldInfo().Item1;
        }

        //FiledInfoを取得（Attributeを調べるためなどに利用）
        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
	        return property.GetValueAndFieldInfo().Item2;
        }

        //参照がMissingかどうか
        public static bool IsMissingReference(this SerializedProperty property)
        {
	        if (property.propertyType != SerializedPropertyType.ObjectReference) return false;
	        if (property.objectReferenceValue != null) return false;

	        var fileId = property.FindPropertyRelative("m_FileID");
	        if (fileId == null || fileId.intValue == 0) return false;
	        return true;
        }
        
        //Scriptかどうか
        public static bool IsScript(this SerializedProperty property)
		{
	        return property.propertyPath == "m_Script";
		}
    }
}
#endif
