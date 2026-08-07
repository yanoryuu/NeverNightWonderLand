// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UtageExtensions;

namespace Utage
{
	//SerializedObjectを扱いやすくしたクラス
	public class SerializedObjectHelper
	{
		public SerializedObjectHelper(SerializedObject serializedObject)
		{
			Init(serializedObject);
		}
		public SerializedObjectHelper(EditorWindow window)
		{
			Init(new SerializedObject(window));
			EditorWindow = window;
		}
		public SerializedObjectHelper(Editor editor)
		{
			Init(editor.serializedObject);
			Editor = editor;
		}

		//SerializedObject本体
		public SerializedObject SerializedObject{get; private set;}
		//スクリプトアセットを表示するか
		public bool IsDrawScript { get; set; }

		//スクリプトアセットを表示するか
		public EditorWindow EditorWindow { get; set; }
		//スクリプトアセットを表示するか
		public Editor Editor { get; set; }

		//プロパティ描画開始の時に呼ばれるコールバック
		public Action<SerializedProperty> OnBeginDrawProperty { get; set; }
		//プロパティ描画終了の時に呼ばれるコールバック
		public Action<SerializedProperty> OnEndDrawProperty { get; set; }

		//プロパティ描画のカスタムコールバック
		public Func<SerializedProperty, bool> DrawCustomProperty{ get; set; }

		//グループ情報
		class GroupInfo
		{
			public string GroupName { get; private set; }
			public string BeginPropertyName { get; private set; }
			public string EndPropertyName { get; private set; }
			public Action<SerializedObjectHelper> OnDrawCustom { get; private set; }

			public GroupInfo(string groupName, string beginPropertyName, string endPropertyName)
			{
				this.GroupName = groupName;
				this.BeginPropertyName = beginPropertyName;
				this.EndPropertyName = endPropertyName;
			}
			public GroupInfo(string groupName, string beginPropertyName, string endPropertyName, Action<SerializedObjectHelper> onDrawCustom)
			{
				this.GroupName = groupName;
				this.BeginPropertyName = beginPropertyName;
				this.EndPropertyName = endPropertyName;
				this.OnDrawCustom = onDrawCustom;
			}
		};
		List<GroupInfo> groupInfoList = new List<GroupInfo>();
		List<GroupInfo> customGroupInfoList = new List<GroupInfo>();

		void Init(SerializedObject serializedObject)
		{
			SerializedObject = serializedObject;
			this.IsDrawScript = true;
		}

		public void AddGroupInfo( string groupName, string beginPropertyName, string endPropertyName)
		{
			groupInfoList.Add( new GroupInfo(groupName, beginPropertyName, endPropertyName) );
		}
		public void AddCustomGroupInfo(string groupName, string beginPropertyName, string endPropertyName, Action<SerializedObjectHelper> onDrawCustom)
		{
			customGroupInfoList.Add(new GroupInfo(groupName, beginPropertyName, endPropertyName, onDrawCustom));
		}
		public void AddCustomGroupInfo(string beginPropertyName, string endPropertyName, Action<SerializedObjectHelper> onDrawCustom)
		{
			customGroupInfoList.Add(new GroupInfo("", beginPropertyName, endPropertyName, onDrawCustom));
		}


		//プロパティを全て描写するOnGUI
		public bool OnGUI()
		{
			SerializedObject.Update();
			DrawAllProperties();
			return SerializedObject.ApplyModifiedProperties();
		}

		//プロパティを全て描写
		public void DrawAllProperties()
		{
			SerializedProperty property;
			if (!DrawHeader(out property)) return;
			do
			{
				if (TryDrawCustomGroup(property))
				{
				}
				else
				{
					BeginGroup(property);
					if (OnBeginDrawProperty != null) OnBeginDrawProperty(property);

					if (DrawCustomProperty != null && DrawCustomProperty(property))
					{
						//カスタム描画
					}
					else
					{
						//通常描画
						if (property.name != "m_PersistentViewDataDictionary")
						{
							DrawProperty(property);
						}
					}

					if (OnEndDrawProperty != null) OnEndDrawProperty(property);
					EndGroup(property);
				}
			} while (property.NextVisible(!property.hasVisibleChildren));
		}

		void BeginGroup(SerializedProperty property)
		{
			GroupInfo info = groupInfoList.Find((item) => (item.BeginPropertyName == property.name));
			if(info!=null)
			{
				BeginGroup(info.GroupName);
			}
		}

		void EndGroup(SerializedProperty property)
		{
			GroupInfo info = groupInfoList.Find((item) => (item.EndPropertyName == property.name));
			if (info != null)
			{
				EndGroup(info.GroupName);
			}
		}

		//カスタム描画
		bool TryDrawCustomGroup(SerializedProperty property)
		{
			GroupInfo info = customGroupInfoList.Find((item) => (item.BeginPropertyName == property.name));
			if (info != null)
			{
				BeginGroup(info.GroupName);
				info.OnDrawCustom(this);
				while (property.name != info.EndPropertyName)
				{
					if( !property.NextVisible(!property.hasVisibleChildren) )
					{
						break;
					}
				}
				EndGroup(info.GroupName);
				return true;
			}
			else
			{
				return false;
			}
		}

		//ヘッダをスキップしたプロパティを取得する
		bool TryGetIteratorSkippedHeader(out SerializedProperty property)
		{
			property = SerializedObject.GetIterator();
			if (!property.NextVisible(true)) return false;
			if (property.displayName == "Script")
			{
				return property.NextVisible(true);
			}
			return true;
		}

		//ヘッダー部分の描画
		public bool DrawHeader()
		{
			SerializedProperty property;
			return DrawHeader(out property);
		}

		public bool DrawHeader(out SerializedProperty property)
		{
			property = SerializedObject.GetIterator();

			if (!property.NextVisible(true)) return false;

			//スクリプトを描画
			if (property.displayName == "Script")
			{
				if (IsDrawScript)
				{
					UnityEngine.Object obj = SerializedObject.targetObject;
					if (obj != null)
					{
						Type type = obj.GetType();
						MonoScript script = MonoScriptHelper.FindEditorScript(type);
						EditorGUILayout.ObjectField(property.displayName, script, type, true);
					}
					else
					{
						Debug.Log("Not found targetObject");
					}
				}
				return property.NextVisible(true);
			}
			return true;
		}

	
		//指定の名前のプロパティを描画
		public bool DrawProperty(string propertyName, string displayName = "")
		{
			SerializedProperty it = SerializedObject.FindProperty(propertyName);
			if (it!=null)
			{
				DrawProperty(it, displayName);
				return true;
			}
			else
			{
				Debug.Log("Not found " + propertyName);
				return false;
			}
		}

		//プロパティを描写
		public static void DrawProperty(SerializedProperty property, string displayName = "")
		{
			if (string.IsNullOrEmpty(displayName)) displayName = property.displayName;
			EditorGUILayout.PropertyField(property, new GUIContent(displayName), true);
		}

		//グループ開始
		public void BeginGroup(string groupName)
		{
			if (!string.IsNullOrEmpty(groupName))
			{
				UtageEditorToolKit.BeginGroup(groupName);
			}
		}

		//グループ終了
		public void EndGroup(string groupName)
		{
			if (!string.IsNullOrEmpty(groupName))
			{
				EndGroup();
			}
		}
		public void EndGroup()
		{
			UtageEditorToolKit.EndGroup();
		}

		//開始プロパティ名～終了プロパティ名の間のプロパティを全て描画
		public void DrawGroupProperties(string groupName, string beginPropertyName, string endPropertyName )
		{
			BeginGroup(groupName);
			SerializedProperty property = SerializedObject.FindProperty(beginPropertyName);
			do
			{
				//通常描画
				DrawProperty(property);
				if(property.name == endPropertyName) break;
			} while (property.NextVisible(!property.hasVisibleChildren));
			EndGroup(groupName);
		}


		public bool DrawPropertyArrayElement(string propertyName, string displayName, int index)
		{
			string propertyPath = string.Format("{0}.Array.data[{1}]", propertyName, index);
			SerializedProperty property = SerializedObject.FindProperty(propertyPath);
			if (property != null)
			{
				SerializedProperty end = property.Copy();
				end.NextVisible(false);
				SerializedProperty child = property.Copy();
				child.NextVisible(true);
				do
				{
					EditorGUILayout.PropertyField(child, new GUIContent(child.displayName), true);
				} while (child.NextVisible(false) && !SerializedProperty.EqualContents(child,end));
				return true;
			}
			else
			{
				Debug.Log("Not found " + propertyPath);
				return false;
			}
		}

		//プロパティを全て描写
		public static void DebugDrawAllPropertiePath(SerializedObject serializedObject)
		{
			SerializedProperty it = serializedObject.GetIterator();
			do{
				Debug.Log(it.propertyPath);
			}while(it.NextVisible(true));
		}


		//ヘッダ部分を除く、表示可能なプロパティのカウントを取得
		int GetCountVisibleProperties()
		{
			int count = 0;
			SerializedProperty property;
			if (TryGetIteratorSkippedHeader(out property))
			{
				while (true)
				{
					++count;
					if (!property.NextVisible(!property.hasVisibleChildren))
					{
						break;
					}
				}
			}
			return count;
		}

		//全ての可視プロパティをバイナリ書き込み
		public void WriteAllVisibleProperties(BinaryWriter writer)
		{
			try
			{
				writer.Write(GetCountVisibleProperties());
				SerializedProperty property;
				if (!TryGetIteratorSkippedHeader(out property)) return;

				while (true)
				{
					WriteProperty(property, writer);
					if (!property.NextVisible(!property.hasVisibleChildren))
					{
						break;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}
		void WriteProperty(SerializedProperty propetry, BinaryWriter writer)
		{
			try
			{
				string propetryValue = BinaryUtil.BinaryWriteToString(wirter => WritePropertyValue(propetry, wirter));
				writer.Write(propetry.propertyPath);
				writer.Write(propetry.propertyType.ToString());
				writer.Write(propetryValue);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		//全ての可視プロパティをバイナリ書き込み
		public void ReadAllVisibleProperties(BinaryReader reader)
		{
			try
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; ++i)
				{
					ReadProperty(reader);
				}
			}
			catch(Exception e)
			{
				Debug.LogError(e.Message);
			}
			SerializedObject.ApplyModifiedProperties();
		}

		void ReadProperty(BinaryReader reader)
		{
			string propertyPath = reader.ReadString();
			string propertyType = reader.ReadString();
			string value = reader.ReadString();
			ReadProperty(propertyPath, propertyType, value);
		}
		void ReadProperty(string propertyPath, string propertyType, string value)
		{
			SerializedProperty property = SerializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogWarning("LoadError " + propertyPath);
				return;
			}
			if (property.propertyType.ToString() != propertyType)
			{
				Debug.LogWarning("Load TypeError" + propertyPath + ":" + propertyType);
				return;
			}
			try
			{
				BinaryUtil.BinaryReadFromString(value, reader => ReadPropertyValue(property, reader));
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		//プロパティの値をバイナリ書き込み
		void WritePropertyValue(SerializedProperty property, BinaryWriter writer)
		{
			property = property.Copy();

			switch(property.propertyType)
			{
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Character:
				case SerializedPropertyType.LayerMask:
				case SerializedPropertyType.Enum:
					writer.Write(property.intValue);
					break;
				case SerializedPropertyType.Boolean:
					writer.Write(property.boolValue);
					break;
				case SerializedPropertyType.Float:
					writer.Write(property.floatValue);
					break;
				case SerializedPropertyType.String:
					writer.Write(property.stringValue);
					break;
				case SerializedPropertyType.ArraySize:
					writer.Write(property.arraySize);
					break;

				case SerializedPropertyType.Color:
					writer.Write(property.colorValue);
					break;
				case SerializedPropertyType.Vector2:
					writer.Write(property.vector2Value);
					break;
				case SerializedPropertyType.Vector3:
					writer.Write(property.vector3Value);
					break;
				case SerializedPropertyType.Vector4:
					writer.Write(property.vector4Value);
					break;
				case SerializedPropertyType.Rect:
					writer.Write(property.rectValue);
					break;
				case SerializedPropertyType.Bounds:
					writer.Write(property.boundsValue);
					break;
				case SerializedPropertyType.Quaternion:
					writer.Write(property.quaternionValue);
					break;

				case SerializedPropertyType.ObjectReference:
					writer.Write(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(property.objectReferenceValue)) );
					break;
				case SerializedPropertyType.Generic:
					WriteGenericPropertyValue(property, writer);
					break;
				case SerializedPropertyType.AnimationCurve:
				case SerializedPropertyType.Gradient:
				default:
					Debug.LogError("Write Not Support Property :" + property.name + ":" + property.propertyType.ToString());
					break;
			}
		}

		//プロパティの値をバイナリ読み込み
		void ReadPropertyValue(SerializedProperty property, BinaryReader reader)
		{
			property = property.Copy();
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Character:
				case SerializedPropertyType.LayerMask:
				case SerializedPropertyType.Enum:
					property.intValue = reader.ReadInt32();
					break;
				case SerializedPropertyType.Boolean:
					property.boolValue = reader.ReadBoolean();
					break;
				case SerializedPropertyType.Float:
					property.floatValue = reader.ReadSingle();
					break;
				case SerializedPropertyType.String:
					property.stringValue = reader.ReadString();
					break;
				case SerializedPropertyType.ArraySize:
					property.arraySize = reader.ReadInt32();
					break;

				case SerializedPropertyType.Color:
					property.colorValue = reader.ReadColor();
					break;
				case SerializedPropertyType.Vector2:
					property.vector2Value = reader.ReadVector2();
					break;
				case SerializedPropertyType.Vector3:
					property.vector3Value = reader.ReadVector3();
					break;
				case SerializedPropertyType.Vector4:
					property.vector4Value = reader.ReadVector4();
					break;
				case SerializedPropertyType.Rect:
					property.rectValue = reader.ReadRect();
					break;
				case SerializedPropertyType.Bounds:
					property.boundsValue = reader.ReadBounds();
					break;
				case SerializedPropertyType.Quaternion:
					property.quaternionValue = reader.ReadQuaternion();
					break;
				case SerializedPropertyType.ObjectReference:
					property.objectReferenceValue = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath(reader.ReadString()), typeof(UnityEngine.Object) );
					break;
				case SerializedPropertyType.Generic:
					ReadGenericPropertyValue(property, reader);
					break;
				case SerializedPropertyType.AnimationCurve:
				case SerializedPropertyType.Gradient:
				default:
					Debug.LogError("Read Not Support Property :" + property.name + ":" + property.propertyType.ToString());
					break;
			}
		}



		//Genericプロパティ(Serializableなクラスや配列、リスト)の値をバイナリ書き込み
		void WriteGenericPropertyValue(SerializedProperty property, BinaryWriter writer)
		{
			if (property.isArray)
			{
				//配列の場合
				writer.Write(property.arraySize);
				for (int i = 0; i < property.arraySize; i++)
				{
					WriteProperty(property.GetArrayElementAtIndex(i),writer);
				}
			}
			else
			{
				int countChild = property.Copy().CountInProperty();
				writer.Write(countChild);
				
				// Serializableなクラス
				var child = property.Copy();
				var end = property.Copy().GetEndProperty();
				if (child.Next(true))
				{
					while (!SerializedProperty.EqualContents(child, end))
					{
						WriteProperty(child,writer);
						if (!child.Next(false))
							break;
					}
				}
			}
		}

		//Genericプロパティの値をバイナリ読み込み
		void ReadGenericPropertyValue(SerializedProperty property, BinaryReader reader)
		{
			if (property.isArray)
			{
				//配列の場合
				property.arraySize = reader.ReadInt32();
				for (int i = 0; i < property.arraySize; i++)
				{
					ReadProperty(reader);
				}
			}
			else
			{
				/*int countChild = */reader.ReadInt32();
				// Serializableなクラス
				var child = property.Copy();
				var end = property.Copy().GetEndProperty();
				if (child.Next(true))
				{
					while (!SerializedProperty.EqualContents(child, end))
					{
						ReadProperty(reader);
						if (!child.Next(false))
							break;
					}
				}
			}
		}
	}
}
#endif
