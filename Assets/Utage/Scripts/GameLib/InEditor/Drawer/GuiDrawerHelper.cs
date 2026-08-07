// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UtageExtensions;
using Object = UnityEngine.Object;

namespace Utage
{

	public abstract class GuiDrawerHelper
	{
		//コンポーネントやScriptableObject（つまり、UnityEngine.Object）にあるメソッドを名前で呼び出す
		public object CallFunction(SerializedProperty property, GuiDrawerFunction function, object[] args = null)
		{
			return CallFunctionSub(property, function, args);
		}

		//コンポーネントやScriptableObject（つまり、UnityEngine.Object）にあるメソッドを名前で呼び出す
		public T CallFunction<T>(SerializedProperty property, GuiDrawerFunction function, object[] args = null)
		{
			return (T)CallFunctionSub(property, function, args);
		}

		//コンポーネントやScriptableObjectにあるメソッドを名前で呼び出す
		protected object CallFunctionSub(SerializedProperty property, GuiDrawerFunction function, object[] args)
		{
			object obj = function.GetTargetObject(property);
			var type = obj.GetType(); //その型を取得

			//メソッドを名前で検索
			var method = type.GetMethod(function.Function,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static); //メソッドを名前で検索
			if (method != null) return method.Invoke(obj, args); //メソッド呼び出し

			//プロパティを名前で検索
			var prop = type.GetProperty(function.Function,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.GetProperty);
			if (prop != null) return prop.GetValue(obj, args); //メソッド呼び出し

			Assert.IsTrue(true, function.Function + " is not found in " + type.ToString());
			return null;
		}


		//コンポーネントやScriptableObject（つまり、UnityEngine.Object）を取得
		public T TargetObject<T>(SerializedProperty property) where T : Object
		{
			return property.serializedObject.targetObject as T;
		}

		//子要素をすべて水平に描画（[System.Serializable]なものの描画に使う）		
		public void DrawHorizontalChildren(Rect position, SerializedProperty property, GUIContent label, float space = 8)
		{
			using var scope = new EditorGUI.PropertyScope(position, label, property);
			//インデント済みの全体矩形を取得
			Rect indentedRect = EditorGUI.IndentedRect(position);
			//インデント記録して、いったん0にする
			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			//子要素の数を取得
			int numChildren = CountChildren(property);
			//矩形を水平に均等分割
			List<Rect> rects = CalcHorizontalRects(indentedRect, numChildren, space);

			int i = 0;
			//各子要素を描画
			ForeachChildren(property, (child) =>
			{
				//子要素のラベル
				GUIContent childLabel = new GUIContent(child.displayName);
				//子要素のラベル部分の表示幅を調整
				EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(childLabel).x;
				//子要素を表示
				EditorGUI.PropertyField(rects[i], child, childLabel);
				++i;
			});

			//インデント戻す
			EditorGUI.indentLevel = indentLevel;
		}


		//子要素をインデントしてすべて描画		
		public void DrawChildrenWithIndent(Rect position, SerializedProperty property)
		{
			using (new EditorGUI.IndentLevelScope())
			{
				DrawChildren(position, property);
			}
		}

		//子要素をすべて描画
		protected void DrawChildren(Rect position, SerializedProperty property)
		{
			SerializedProperty endProperty = property.GetEndProperty();
			if(!property.NextVisible(true)) return;
			while (!SerializedProperty.EqualContents(property, endProperty))
			{
				DrawPropertyFieldAutoRect(property, ref position);
				//孫要素は描画済みなので展開しない(enterChildren=false)
				if (!property.NextVisible(false))
				{
					break;
				}
			}
		}

		//子要素をすべて描画した場合の高さを取得		
		public float GetPropertyHeightChildren(SerializedProperty property)
		{
			float height = 0;
			SerializedProperty endProperty = property.GetEndProperty();
			if (!property.NextVisible(true)) return height;
			while (!SerializedProperty.EqualContents(property, endProperty))
			{
				//孫要素を含めて描画した際の高さを取得
				height += EditorGUI.GetPropertyHeight(property);
				//孫要素は計算済みなので展開しない(enterChildren=false)
				if (!property.NextVisible(false))
				{
					break;
				}
			}
			return height;
		}

		//二つのプロパティの間の高さを取得		
		public float GetPropertyHeightBetween(SerializedProperty beginProperty, SerializedProperty endProperty)
		{
			float height = 0;
			SerializedProperty property = beginProperty.Copy();
			do
			{
				//高さを加算
				height += EditorGUI.GetPropertyHeight(property);
			} while (!SerializedProperty.EqualContents(property, endProperty) && property.NextVisible(false));
			return height;
		}

		//矩形を再計算して、フィールドを描画
		public void DrawPropertyFieldAutoRect(SerializedProperty property, ref Rect position)
		{
			position.height = EditorGUI.GetPropertyHeight(property);
			EditorGUI.PropertyField(position, property, new GUIContent(property.displayName), true);
			position.y += position.height;
		}

		//矩形を再計算して、ラベルを描画
		public void DrawLabelFieldAutoRect(string label, ref Rect position)
		{
			DrawLabelFieldAutoRect(label, ref position, EditorStyles.label);
		}

		public void DrawLabelFieldAutoRect(string label, ref Rect position, GUIStyle style)
		{
			float height = EditorGUIUtility.singleLineHeight;
			position.height = height;
			EditorGUI.LabelField(position, label, style);
			position.y += height;
		}

		//フィールドを取得
		public T GetField<T>(SerializedProperty property)
		{
			return (T)GetFieldSub(property);
		}

		protected object GetFieldSub(SerializedProperty property)
		{
			var obj = property.serializedObject.targetObject; //コンポーネントやScriptableObject
			var type = obj.GetType(); //その型を取得

			//フィールドを名前で検索
			var field = type.GetField(property.name,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField);
			if (field != null) return field.GetValue(obj); //メソッド呼び出し

			Assert.IsTrue(true, property.name + " is not found in " + type.ToString());
			return null;
		}

		public void SetDirty(SerializedProperty property)
		{
			EditorUtility.SetDirty(property.serializedObject.targetObject);
		}


		//ヘッダ部分を除く、表示可能なプロパティのカウントを取得
		protected int CountChildren(SerializedProperty property)
		{
			int count = 0;
			ForeachChildren(property, (x) => ++count);
			return count;
		}

		//表示可能な子要素に対してのForeach
		protected void ForeachChildren(SerializedProperty property, System.Action<SerializedProperty> childAction)
		{
			if (!property.hasVisibleChildren) return;

			var child = property.Copy();
			var end = property.Copy().GetEndProperty();
			if (child.Next(true))
			{
				while (!SerializedProperty.EqualContents(child, end))
				{
					childAction(child);
					if (!child.Next(false))
						break;
				}
			}
		}


		//指定個数ぶん横に分割した矩形のリストを取得
		//spaceは分割した矩形同士のスペース
		public List<Rect> CalcHorizontalRects(Rect rect, int num, float space = 0)
		{
			if (num <= 0) return new List<Rect> { rect };

			List<Rect> rects = new List<Rect>();

			float w = (rect.width - space * (num - 1)) / num;
			float x = rect.x;
			for (int i = 0; i < num; ++i)
			{
				Rect r = rect;
				r.x = x;
				r.width = w;
				rects.Add(r);
				x += w + space;
			}

			return rects;
		}
		
		
		//表示ラベルを文字数に合わせて広げて表示するPropertyField
		public bool DrawPropertyFieldExpandLabel(Rect position, SerializedProperty property)
		{
			return DrawPropertyFieldExpandLabel(position, property, property.displayName);
		}

		//表示ラベルを文字数に合わせて広げて表示するPropertyField
		public bool DrawPropertyFieldExpandLabel(Rect position, SerializedProperty property, string displayName)
		{
			//子要素のラベル
			GUIContent childLabel = new GUIContent(displayName);
			//子要素のラベル部分の表示幅を調整
			float labelWidth = GUI.skin.label.CalcSize(childLabel).x;
			EditorGUIUtility.labelWidth = labelWidth;
			//子要素を表示
			return EditorGUI.PropertyField(position, property, childLabel);
		}

		//表示ラベルを文字数に合わせて広げて表示するPropertyField
		//変更があったときに呼ばれるコールバックつき
		public bool DrawPropertyFieldExpandLabel(Rect position, SerializedProperty property, System.Action onChanged )
		{
			return DrawPropertyFieldExpandLabel(position, property, property.displayName, onChanged);
		}

		//表示ラベルを文字数に合わせて広げて表示するPropertyField
		//変更があったときに呼ばれるコールバックつき
		public bool DrawPropertyFieldExpandLabel(Rect position, SerializedProperty property, string displayName, System.Action onChanged)
		{
			EditorGUI.BeginChangeCheck();
			bool ret = DrawPropertyFieldExpandLabel(position, property, displayName);
			if (EditorGUI.EndChangeCheck())
			{
				onChanged();
			}
			return ret;
		}

		//文字列の配列のプロパティをマスクフィールド（つまりチェックボックスグループのかわり）で表示する
		public void DrawStringArrayPropertyMaskFiled(Rect position, SerializedProperty property, List<string> options)
		{
			int currentMask = 0;
			for ( int i = 0; i < property.arraySize; ++i )
			{
				SerializedProperty child = property.GetArrayElementAtIndex(i);
				int index = options.FindIndex(x => x == child.stringValue);
				if (index >= 0)
				{
					currentMask |= (0x1 << index);
				}
			}
			int mask = EditorGUI.MaskField(position, property.displayName, currentMask, options.ToArray());
			if (mask != currentMask)
			{
				List<string> list = new List<string>();
				for (int i = 0; i < options.Count; ++i)
				{
					int bin = (0x1 << i);
					if ((mask & bin) != bin) continue;
					list.Add(options[i]);
				}
				SetStringList(property,list);
			}
		}

		//文字列の配列を上書き設定
		public void SetStringList(SerializedProperty property, List<string> list)
		{
			property.arraySize = list.Count;
			for (int i = 0; i < property.arraySize; ++i)
			{
				SerializedProperty child = property.GetArrayElementAtIndex(i);
				child.stringValue = list[i];
			}
		}

		//文字列の配列を取得
		public List<string> GetStringList(SerializedProperty property)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < property.arraySize; ++i)
			{
				SerializedProperty child = property.GetArrayElementAtIndex(i);
				list.Add( child.stringValue );
			}
			return list;
		}

		//インデント済みの表示幅を取得
		public float GetIndentedViewWidth()
		{
			return GetIndentedViewWidth(EditorGUI.indentLevel);
		}
		public float GetIndentedViewWidth(int indentLevel)
		{
			//インデントの幅を計算
			int offset = GetIndentedOffsetWidth(indentLevel);
			return EditorGUIUtility.currentViewWidth - offset;
		}
		
		//インデントによるオフセット幅（ずれる幅）を取得
		public int GetIndentedOffsetWidth()
		{
			return GetIndentedOffsetWidth(EditorGUI.indentLevel);
		}
		public int GetIndentedOffsetWidth(int indentLevel)
		{
			//インデントの幅を計算
			return indentLevel * 15;
		}
		
		//指定のAttributeを持つ次のプロパティを検索
		public SerializedProperty FindNextPropertyHasAttribute<T>(SerializedProperty property)
		{
			SerializedProperty next = property.Copy();
			while (next.NextVisible(false))
			{
				FieldInfo fieldInfo = next.GetFieldInfo();
				if (fieldInfo != null && fieldInfo.IsDefined(typeof(T)))
				{
					return next;
				}
			}
			return null;
		}
	}

	public class GuiDrawerHelper<T> : GuiDrawerHelper
		where T : GUIDrawer
	{
		protected T Drawer { get; }

		public GuiDrawerHelper(T drawer)
		{
			Drawer = drawer;
		}
	}
}
#endif
