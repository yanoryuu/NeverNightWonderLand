// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using System.Collections;

namespace Utage
{
	/// <summary>
	/// 自動でゲームスタートする
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/AutoStartGame")]
	public class AutoStartGame : MonoBehaviour
	{
		public UtageUguiTitle title;
		public float timeLimit = 0;
		float time = 0;

		void OnEnable()
		{
			time = 0;
		}

		void Update()
		{
			if (time > timeLimit)
			{
				title.OnTapStart();
			}

			time += Time.deltaTime;
		}
	}
}
