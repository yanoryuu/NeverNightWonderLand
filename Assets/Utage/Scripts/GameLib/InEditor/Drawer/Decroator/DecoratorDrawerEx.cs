// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{

#if UNITY_EDITOR
	/// <summary>
	/// デコレーター表示を使いやすくるするための基底クラス
	/// </summary>
	public abstract class DecoratorDrawerEx : DecoratorDrawer
	{
		protected abstract GuiDrawerHelper Helper { get; }
	}

	/// <summary>
	/// デコレーター表示を使いやすくるするための基底クラス
	/// Genericでアトリビュートの型を指定
	/// </summary>
	public class DecoratorDrawerEx<T> : DecoratorDrawerEx where T : PropertyAttribute
	{
		public T Attribute { get { return (this.attribute as T); } }
		protected override GuiDrawerHelper Helper { get; }

		public DecoratorDrawerEx()
		{
			Helper = new GuiDrawerHelper<DecoratorDrawerEx<T>>(this);
		}
	}
#endif
}
