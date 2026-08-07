// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utage
{
	//テクスチャをダイシング処理して軽量化して使えるようにする
	[System.Serializable]
	public class DicingTextureData
	{
		//このテクスチャデータの名前
		public string Name { get { return name; } }
		[SerializeField]
		string name = "";

		//アトラステクスチャの名前
		public string AtlasName { get { return atlasName; } }
		[SerializeField]
		string atlasName = "";

		//テクスチャのサイズ(幅)
		public int Width { get { return width; } }
		[SerializeField]
		int width = 0;

		//テクスチャのサイズ(高さ)
		public int Height { get { return height; } }
		[SerializeField]
		int height = 0;

		[SerializeField]
		List<int> cellIndexList = new List<int>();

		[SerializeField]
		int transparentIndex = 0;

#if UNITY_EDITOR
		public void InitOnImport(string name, string atlasName, int width, int height, List<int> cellIndexList, int transparentIndex)
		{
			this.name = name;
			this.atlasName = atlasName;
			this.width = width;
			this.height = height;
			this.cellIndexList = cellIndexList;
			this.transparentIndex = transparentIndex;
		}
#endif


		//四角形の頂点情報
		public class QuadVerts
		{
			public Vector4 v;
			public Rect uvRect;
			public bool isAllTransparent;
		};
		[NonSerialized]
		List<QuadVerts> verts = null;

		//頂点		
		internal List<QuadVerts> GetVerts(DicingTextures textures)
		{
			if (verts == null)
			{
				InitVerts(textures);
			}
			return verts;
		}

		//頂点		
		void InitVerts(DicingTextures atlas)
		{
			if (atlas == null) return;
			//頂点データ
			this.verts = new List<QuadVerts>();

			//アトラス内のセルのサイズ
			int atlasCellSize = atlas.CellSize;
			//パディング済みのセルサイズ（テクスチャの方のセルサイズ）
			int paddingCellSize = atlasCellSize - atlas.Padding * 2;

			//テクスチャ内のセルの数
			int cellCountX = Mathf.CeilToInt(1.0f * Width / paddingCellSize);
			int cellCountY = Mathf.CeilToInt(1.0f * Height / paddingCellSize);

			//アトラス画像について
			int atlasWidth = atlas.GetTexture(this.AtlasName).width;
			int atlasHeight = atlas.GetTexture(this.AtlasName).height;
			int atlasCellCountX = Mathf.CeilToInt(1.0f * atlasWidth / atlasCellSize);
//			int atlasCellCountY = Mathf.CeilToInt(1.0f * atlasHeight / atlasCellSize);

			//セルのインデックス
			int index = 0;
			for (int cellY = 0; cellY < cellCountY; ++cellY)
			{
				float y0 = cellY * paddingCellSize;
                //本来のテクスチャの大きさ以上にはしない
                float y1 = Mathf.Min( y0 + paddingCellSize, this.Height);
                for (int cellX = 0; cellX < cellCountX; ++cellX)
				{
                    DicingTextureData.QuadVerts quadVerts = new DicingTextureData.QuadVerts();
                    float x0 = cellX * paddingCellSize;
                    //本来のテクスチャの大きさ以上にはしない
                    float x1 = Mathf.Min(x0 + paddingCellSize, this.Width);
                    quadVerts.v = new Vector4(x0, y0, x1, y1);
					int cellIndex = cellIndexList[index];
					quadVerts.isAllTransparent = (cellIndex == transparentIndex);

					float ux = (cellIndex % atlasCellCountX) * atlasCellSize;
					float uy = (cellIndex / atlasCellCountX) * atlasCellSize;
					//パディングを考慮してUV値を設定しておく
					float uvX = 1.0f * (ux + atlas.Padding) / atlasWidth;
					float uvY = 1.0f * (uy + atlas.Padding) / atlasHeight;
					float uvW = 1.0f * (x1-x0) / atlasWidth;
					float uvH = 1.0f * (y1-y0) / atlasHeight;
					quadVerts.uvRect = new Rect(uvX, uvY, uvW, uvH);
					this.verts.Add(quadVerts);
					index++;
				}
			}
		}

		//描画頂点データに対してForeachして、funcitionをコールバックとして呼ぶ
		public void ForeachVertexList(Rect position, Rect uvRect, bool skipTransParentCell, DicingTextures textures, Action<Rect, Rect> function)
		{
			//縮尺
			Vector2 scale = new Vector2(position.width / this.Width, position.height / this.Height);
			ForeachVertexList(uvRect, skipTransParentCell, textures,
				(r1, r2) =>
				{
					r1.xMin *= scale.x;
					r1.xMax *= scale.x;
					r1.x += position.x;

					r1.yMin *= scale.y;
					r1.yMax *= scale.y;
					r1.y += position.y;
					function(r1, r2);
				});
		}


		//描画頂点データに対してForeachして、funcitionをコールバックとして呼ぶ
		//UV座標が1以上で、リピートする場合や、UVスクロールする場合を想定して
		//テクスチャ1枚ぶんで描ける矩形ごとに分割して描画
		//例えば、UV値が(-0.25,0,1,1)で横にUVスクロールしているなら、2分割になり、
		//例えば、UV値が(0,0,2,2)なら、4分割する
		public void ForeachVertexList(Rect uvRect, bool skipTransParentCell, DicingTextures textures, Action<Rect, Rect> function)
		{
			if (uvRect.width == 0 || uvRect.height == 0)
				return;
			if (uvRect.xMin < 0 )
			{
				uvRect.x += Mathf.CeilToInt(-uvRect.xMin);
			}
			if (uvRect.yMin < 0 )
			{
				uvRect.y += Mathf.CeilToInt(-uvRect.yMin);
			}

			bool flipX = false;
			if (uvRect.width < 0)
			{
				uvRect.width *= -1;
				flipX = true;
			}
			bool flipY = false;
			if (uvRect.height < 0)
			{
				uvRect.height *= -1;
				flipY = true;
			}

			float scaleX = 1.0f / uvRect.width;
			float fipOffsetX = 0;
			if (flipX)
			{
				scaleX *= -1;
				fipOffsetX = this.Width;
			}
			float scaleY = 1.0f / uvRect.height;
			float fipOffsetY = 0;
			if (flipY)
			{
				scaleY *= -1;
				fipOffsetY = this.Height;
			}

			float vBegin = uvRect.yMin % 1;
			float vEnd = uvRect.yMax % 1;
			if (vEnd == 0) vEnd = 1;

			float offsetY = 0;
			bool isFirstV = true;
			bool isEndV = false;
			Rect rect = new Rect();
			do
			{
				rect.yMin = isFirstV ? vBegin : 0;
				float rectY = (offsetY + 1.0f - rect.yMin);
				isEndV = (rectY >= uvRect.height) || Mathf.Approximately(rectY,uvRect.height);
				rect.yMax = isEndV ? vEnd : 1;

				float uBegin = uvRect.xMin%1;
				float uEnd = uvRect.xMax%1;
				if (uEnd == 0) uEnd = 1;

				float offsetX = 0;
				bool isFirstU = true;
				bool isEndU = false;
				do
				{
					rect.xMin = isFirstU ? uBegin : 0;
					float rectX = (offsetX + 1 - rect.xMin);
					isEndU = (rectX >= uvRect.width) || Mathf.Approximately(rectX, uvRect.width);
					rect.xMax = isEndU ? uEnd : 1;
					ForeachVertexListSub(rect, skipTransParentCell, textures,
						(r1, r2) =>
						{
							r1.xMin *= scaleX;
							r1.xMax *= scaleX;
							r1.x += (offsetX - rect.xMin) * scaleX * this.Width + fipOffsetX;
							r1.yMin *= scaleY;
							r1.yMax *= scaleY;
							r1.y += (offsetY - rect.yMin) * scaleY * this.Height + fipOffsetY;
							function(r1, r2);
						});
					offsetX += rect.width;
					isFirstU = false;
				} while (!isEndU);
				offsetY += rect.height;
				isFirstV = false;
			} while (!isEndV);
		}


		//描画頂点データに対してForeachして、funcitionをコールバックとして呼ぶ
		//UVが(0,0,1,1)の範囲であるのが必須
		//指定のUV座標に対してのもの
		void ForeachVertexListSub(Rect uvRect, bool skipTransParentCell, DicingTextures textures, Action<Rect, Rect> function)
		{
			Texture2D texture = textures.GetTexture(this.AtlasName);
			float textureW = texture.width;
			float textureH = texture.height;
			//頂点データを取得
			List<DicingTextureData.QuadVerts> verts = GetVerts(textures);

			//UV指定を考慮して、描画ピクセルの矩形を取得
			Rect pixcelRect = new Rect(uvRect.x * this.Width, uvRect.y * this.Height, uvRect.width * this.Width, uvRect.height * this.Height);
			for (int i = 0; i < verts.Count; ++i)
			{
				var vert = verts[i];
				//透明ならスキップ
				if (skipTransParentCell && vert.isAllTransparent) continue;

				//上下左右の座標
				float left = vert.v.x;
				float right = vert.v.z;
				float top = vert.v.y;
				float bottom = vert.v.w;

				Rect uv = vert.uvRect;
				if (left > pixcelRect.xMax || top > pixcelRect.yMax || right < pixcelRect.x || bottom < pixcelRect.y)
				{
					//全体が切り取り矩形の外
					continue;
				}
				else
				{
					//一部が切り取り矩形の外なら、頂点とUV値を調整して矩形内に収める
					if (left < pixcelRect.x)
					{
						uv.xMin += (pixcelRect.x - left) / textureW;
						left = pixcelRect.x;
					}
					if (right > pixcelRect.xMax)
					{
						uv.xMax += (pixcelRect.xMax - right) / textureW;
						right = pixcelRect.xMax;
					}

					if (top < pixcelRect.y)
					{
						uv.yMin += (pixcelRect.y - top) / textureH;
						top = pixcelRect.y;
					}
					if (bottom > pixcelRect.yMax)
					{
						uv.yMax += (pixcelRect.yMax - bottom) / textureH;
						bottom = pixcelRect.yMax;
					}
				}

				function(new Rect(left, top, right - left, bottom - top), uv);
			}
		}
	}
}
