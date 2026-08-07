// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// パーティクルを拡大縮小する。
	/// 拡大縮小に必要な設定に自動書き換えする
	/// </summary>
	[AddComponentMenu("Utage/Lib/Effect/ParticleAutomaticDestroyer")]
	public class ParticleAutomaticDestroyer : MonoBehaviour
	{
		bool isPlalyed = false;

		void Update()
		{
			if (CheckPlaying())
			{
				isPlalyed = true;
			}
			else if(isPlalyed)
			{
				Destroy(this.gameObject);
			}
		}

		bool CheckPlaying()
		{
			foreach (ParticleSystem particle in this.GetComponentsInChildren<ParticleSystem>(true))
			{
				if (particle.isPlaying) return true;
			}
			return false;
		}
	}
}
