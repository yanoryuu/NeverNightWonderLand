// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// Sample LoadErrorのコールバック関数を書き換え
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SamplePageEvent")]
	public class SamplePageEvent : MonoBehaviour
	{
		void Awake()
		{
		}

		public void OnBeginText(AdvPage page)
		{
			Debug.Log("OnBeginText");
		}

		public void OnEndText(AdvPage page)
		{
			Debug.Log("OnEndText");
		}

		public void OnChangeText(AdvPage page)
		{
			Debug.Log("OnChangeText");
		}
	}
}
