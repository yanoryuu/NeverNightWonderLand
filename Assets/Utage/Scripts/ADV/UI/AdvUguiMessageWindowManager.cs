// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// メッセージウィンドウの管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvUguiMessageWindowManager")]
	public class AdvUguiMessageWindowManager : MonoBehaviour, IAdvMessageWindowManager
	{
		//メッセージウィンドウのオブジェクトリスト
		//子オブジェクト以外の位置に置く場合にこれを使う
		[SerializeField]
		protected List<GameObject> messageWindowList;

		//メッセージウィンドウのオブジェクトリスト
		public virtual Dictionary<string, IAdvMessageWindow> AllWindows
		{
			get
			{
				if (allWindows == null)
				{
					InitWindows();
				}
				return allWindows;
			}
		}
		protected Dictionary<string, IAdvMessageWindow> allWindows = null;

		protected virtual void InitWindows()
		{
			allWindows = new Dictionary<string, IAdvMessageWindow>();
			foreach( var item in messageWindowList )
			{
				IAdvMessageWindow window = item.GetComponent<IAdvMessageWindow>();
				if (window == null)
				{
					Debug.LogErrorFormat("{0} is not MessageWindow");
					continue;
				}
				AddWindow(window);
			}
			foreach (var window in this.GetComponentsInChildren<IAdvMessageWindow>(true))
			{
				AddWindow(window);
			}
		}

		protected virtual void AddWindow(IAdvMessageWindow messageWindow)
		{
			string name = messageWindow.gameObject.name;

			//同じウィンドウが重複されて登録されていないかチェック
			if (allWindows.ContainsKey(name))
			{
				if (!allWindows.ContainsValue(messageWindow))
				{
					Debug.LogErrorFormat("{0} is already exists in windows");
				}
				return;
			}

			allWindows.Add(name, messageWindow);
		}

		internal virtual void Close()
		{
			this.gameObject.SetActive(false);
		}

		internal virtual void Open()
		{
			this.gameObject.SetActive(true);
		}
	}
}