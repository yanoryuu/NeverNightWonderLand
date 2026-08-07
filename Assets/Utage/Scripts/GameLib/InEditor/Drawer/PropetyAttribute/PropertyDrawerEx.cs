// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Utage
{
	/// <summary>
	/// プロパティ拡張を使いやすくるするための基底クラス
	/// </summary>
	public abstract class PropertyDrawerEx : PropertyDrawer
	{
		protected abstract GuiDrawerHelper Helper { get; }
	}

	/// <summary>
	/// プロパティ拡張を使いやすくるするための基底クラス
	/// Genericでアトリビュートの型を指定
	/// </summary>
	public class PropertyDrawerEx<T> : PropertyDrawerEx where T : PropertyAttribute
	{
		public T Attribute { get { return (this.attribute as T); } }
		protected override GuiDrawerHelper Helper { get; }

		public PropertyDrawerEx()
		{
			Helper = new GuiDrawerHelper<PropertyDrawerEx<T>>(this);
		}
	}
}
#endif
