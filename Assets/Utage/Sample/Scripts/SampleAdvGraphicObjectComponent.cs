// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;

namespace Utage
{
	//AdvGraphicObjectプレハブに追加するコンポーネントのサンプル
	public class SampleAdvGraphicObjectComponent : MonoBehaviour
	{

		//クリック処理のサンプル
		public void OnClick()
		{
			//ログを出力（不要なら消すこと）
			Debug.Log($"OnClick",this);
		}
	}
		
}
