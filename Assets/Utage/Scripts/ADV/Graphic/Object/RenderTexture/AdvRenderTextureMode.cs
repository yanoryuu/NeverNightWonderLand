// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Utage
{
	/// <summary>
	/// テクスチャ書き込みのタイプ
	/// </summary>
	public enum AdvRenderTextureMode
	{
		None,			//
		Image,			//ImageやRawImageを描き込む（カスタムシェーダーを使う）
		DefaultShader,  //シェーダーそのまま使う
		Cusotm,         //
	}
}