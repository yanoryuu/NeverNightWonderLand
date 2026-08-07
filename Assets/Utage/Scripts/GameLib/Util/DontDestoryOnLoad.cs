// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;

namespace Utage
{

	/// <summary>
	/// シーン切り替えでオブジェクトを破棄しないようにするかのON,OFFをする
	/// </summary>
	[AddComponentMenu("Utage/Lib/Other/DontDestoryOnLoad")]
	public class DontDestoryOnLoad : MonoBehaviour
	{
		/// <summary>
		/// シーン切り替え時に破棄するか
		/// </summary>
		[SerializeField]
		bool dontDestoryOnLoad = false;

		void Awake()
		{
			if (dontDestoryOnLoad)
			{
				DontDestroyOnLoad(this.gameObject);
			}
		}
	}
}
