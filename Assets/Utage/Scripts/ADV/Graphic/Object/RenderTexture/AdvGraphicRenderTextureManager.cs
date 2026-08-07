// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// テクスチャ書き込みクラス
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvGraphicRenderTextureManager")]
	public class AdvGraphicRenderTextureManager : MonoBehaviour
	{
		public float offset = 10000;

		//Trueにするとテクスチャ書き込みを使う場合に、もとのレイヤー設定をテクスチャ書き込み用の描画レイヤーに反映させる。
		//宴4からデフォルトでtrueに。それ以前では互換性維持のためにデフォルトをfalse
		public bool EnableChangeLayer {get{return enableChangeLayer;}}
		[SerializeField] bool enableChangeLayer = true;

		List<AdvRenderTextureSpace> SpaceList { get; } = new ();

		//テクスチャ書き込み用の空間（カメラ・キャンバス・オブジェクト）を追加
		internal AdvRenderTextureSpace CreateSpace()
		{
			AdvRenderTextureSpace space = this.transform.AddChildGameObjectComponent<AdvRenderTextureSpace>("RenderTextureSpace");
			int index = 0;
			for ( ; index < SpaceList.Count; ++index)
			{
				if (SpaceList[index] == null)
				{
					SpaceList[index] = space;
					break;
				}
			}
			if (index>= SpaceList.Count)
			{
				SpaceList.Add(space);
			}
			//描画領域が重複しないように、有り得ないほど遠くに置く
			space.transform.localPosition = new Vector3(0, (index + 1) * offset, 0);
			return space;
		}
	}
}
