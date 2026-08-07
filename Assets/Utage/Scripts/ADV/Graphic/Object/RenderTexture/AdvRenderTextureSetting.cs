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
	public class AdvRenderTextureSetting
	{
		// テクスチャ書き込みのタイプ
		public AdvRenderTextureMode RenderTextureType { get; protected set; }
		// テクスチャ書き込みの際のテクスチャサイズ
		public Vector2 RenderTextureSize { get; protected set; }
		// テクスチャ書き込みの際のテクスチャの倍率
		public float RenderTextureScale { get; protected set; }
		// テクスチャ書き込みの際のオブジェクトのオフセット
		public Vector3 RenderTextureOffset { get; protected set; }

		// テクスチャ書き込みが有効か
		public bool EnableRenderTexture { get { return RenderTextureType != AdvRenderTextureMode.None; } }

		//テクスチャ書き込みが有効ならマテリアルを作成して返す
		public Material GetRenderMaterialIfEnable( Material material )
		{
			if (EnableRenderTexture && ( material ==null || material.shader != ShaderManager.RenderTexture))
			{
				return new Material(ShaderManager.RenderTexture);
			}
			return material;
		} 


		public void Parse(StringGridRow row)
		{
			this.RenderTextureType = AdvParser.ParseCellOptional<AdvRenderTextureMode>(row, AdvColumnName.RenderTexture, AdvRenderTextureMode.None);

			if(RenderTextureType != AdvRenderTextureMode.None)
			{
				try
				{
					float[] rect = row.ParseCellArray<float>(AdvColumnName.RenderRect.QuickToString());
					if (rect.Length != 4)
					{
						Debug.LogError(row.ToErrorString("IconRect. Array size is not 4"));
					}
					else
					{
						this.RenderTextureOffset = new Vector3(-rect[0], -rect[1], 1000);
						this.RenderTextureSize = new Vector2(rect[2], rect[3]);
					}
					this.RenderTextureScale = row.ParseCellOptional(AdvColumnName.RenderTextureScale.QuickToString(),1.0f);
					this.RenderTextureSize *= RenderTextureScale;
				}
				catch(Exception )
				{
//					Debug.LogError(row.ToErrorString("IconRect. Array size is not 4"));
				}
			}
		}
	}
}
