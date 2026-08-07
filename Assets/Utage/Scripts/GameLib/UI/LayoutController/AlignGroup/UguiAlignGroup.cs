// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;

namespace Utage
{


	/// <summary>
	///  子オブジェクトを並べる
	/// </summary>
	[ExecuteInEditMode]
	public abstract class UguiAlignGroup : UguiLayoutControllerBase, ILayoutController
	{
		public bool isAutoResize = false;
		//子要素がゼロでも再配置をする
		public bool repositionZeroContent = true;
		public float space = 0;
		public void SetLayoutHorizontal()
		{
			tracker.Clear();
			Reposition();
		}

		public void SetLayoutVertical()
		{
			tracker.Clear();
			Reposition();
		}

		public List<GameObject> AddChildrenFromPrefab( int count, GameObject prefab, System.Action<GameObject,int> callcackCreateItem )
		{
			List<GameObject> goList = new List<GameObject> ();
			for( int i = 0; i < count; ++i )
			{
				GameObject go = CachedRectTransform.AddChildPrefab(prefab);
				goList.Add(go);
				if(callcackCreateItem!=null) callcackCreateItem(go,i);
			}
			return goList;
		}

		public void DestroyAllChildren()
		{
			CachedRectTransform.DestroyChildren ();
		}

		/// <summary>
		/// 
		/// </summary>
		public abstract void Reposition ();
	}
}
