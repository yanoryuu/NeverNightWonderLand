#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UtageExtensions
{
	//SerializedObjectの拡張メソッド
    public static class SerializedObjectExtensions
    {
	    //指定のserializedObjectの全てのプロパティを取得
	    public static IEnumerable<SerializedProperty> GetAllVisibleProperties(this SerializedObject serializedObject, bool includeScript = false)
	    {
		    SerializedProperty iterator = serializedObject.GetIterator();
		    while (iterator.NextVisible(true))
		    {
			    // スクリプトは無視
			    if (!includeScript && iterator.IsScript()) continue;
			    yield return iterator;
		    }
	    }

	    //指定のserializedObjectの全ての表示プロパティを取得
	    public static IEnumerable<SerializedProperty> GetAllProperties(this SerializedObject serializedObject, bool includeScript = false)
	    {
		    SerializedProperty iterator = serializedObject.GetIterator();
		    while(iterator.Next(true))
		    {
			    // スクリプトは無視
			    if (!includeScript && iterator.IsScript()) continue;
			    yield return iterator;
		    }
	    }
    }
}
#endif
