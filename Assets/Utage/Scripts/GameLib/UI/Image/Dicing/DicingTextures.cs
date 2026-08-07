// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	//テクスチャをダイシング処理して軽量化して使えるようにする
	[CreateAssetMenu(menuName = "Utage/DicingTextures")]
	public class DicingTextures : ScriptableObject
	{
		//****ゲーム内でも使う設定****
		//セルのサイズ
		public int CellSize
		{
			get { return cellSize; }
			set { cellSize = value; }
		}
		[HelpBoxAttribute("ImportSetting", HelpBoxAttribute.Type.Info)]
		[SerializeField, IntPopup(32, 64, 128, 256)]
		int cellSize = 64;

		//****インポート設定****


		//押し出し処理をしているピクセル数
		public int Padding
		{
			get { return padding; }
			set { padding = value; }
		}
		[SerializeField, Min(1)]
		int padding = 3;

		[SerializeField, NotEditable]
		List<Texture2D> atlasTextures = new List<Texture2D>();
		//使用するテクスチャ
		public List<Texture2D> AtlasTextures
		{
			get { return atlasTextures; }
		}


		public DicingTextureData GetTextureData(string pattern)
		{
			foreach (var item in textureDataList)
			{
				if (item.Name == pattern)
				{
					return item;
				}

			}
			return null;
		}

		public bool Exists(string pattern)
		{
			return textureDataList.Exists(x => x.Name == pattern);
		}

		//テクスチャデータのリスト
		public List<DicingTextureData> TextureDataList { get { return textureDataList; } }
		[SerializeField, NotEditable]
		List<DicingTextureData> textureDataList = new List<DicingTextureData>();

		//テクスチャデータのリストを先頭フォルダ指定で取得する
		public List<DicingTextureData> GetTextureDataList(string topDirectory)
		{
			if (string.IsNullOrEmpty(topDirectory)) return TextureDataList;

			if (!topDirectory.EndsWith("/"))
			{
				topDirectory += "/";
			}
			List<DicingTextureData> list = new List<DicingTextureData>();
			foreach (var item in TextureDataList)
			{
				if (item.Name.StartsWith(topDirectory))
				{
					list.Add(item);
				}
			}
			return list;
		}

		public Texture2D GetTexture(string name)
		{
			return atlasTextures.Find(x => (x != null) && (x.name == name));
		}

		public List<string> GetPattenNameList()
		{
			List<string> list = new List<string>();
			foreach (var item in textureDataList)
			{
				list.Add(item.Name);
			}
			return list;
		}

		//頂点		
		public List<DicingTextureData.QuadVerts> GetVerts(DicingTextureData data)
		{
			return data.GetVerts(this);
		}


#if UNITY_EDITOR

		//生成するテクスチャの最大サイズ
		public int MaxTxetureSize
		{
			get { return maxTxetureSize; }
			set { maxTxetureSize = value; }
		}
		[SerializeField, IntPopup(1024, 2048, 4096)]
		int maxTxetureSize = 4096;

		//生成するテクスチャのアトラス名
		public string AtlasName
		{
			get { return atlasName; }
		}
		[SerializeField]
		string atlasName = "Atlas";

		//入力元となるフォルダ
		public Object InputDir
		{
			get { return inputDir; }
			set { inputDir = value; }
		}
		[SerializeField, Folder]
		Object inputDir;

		//出力先となるフォルダ
		public Object OutputDir
		{
			get { return outputDir; }
			set { outputDir = value; }
		}
		[SerializeField, Folder]
		Object outputDir;

		public bool OverrideTextureImporter
		{
			get { return overrideTextureImporter; }
		}
		[SerializeField]
		bool overrideTextureImporter = true;

		public AssetBuildTimeStamp BuildTimeStamp
		{
			get { return buildTimeStamp; }
		}
		[SerializeField]
		AssetBuildTimeStamp buildTimeStamp = new AssetBuildTimeStamp();


		//リビルド
		public void Build(AssetBuildTimeStamp buildTimeStamp, List<Texture2D> outputTextures, List<DicingTextureData> textureDataList)
		{
			this.buildTimeStamp = buildTimeStamp;
			this.textureDataList = textureDataList;
			this.atlasTextures = outputTextures;
			if (buildTimeStamp == null)
			{
				Debug.LogError("buildTimeStamp is null");
			}
			else if (buildTimeStamp.InfoList.Count == 0)
			{
				Debug.LogError("buildTimeStamp is zero");
			}
			EditorUtility.SetDirty(this);
		}

		public virtual void OnPreviewGUI(DicingTextureData data, Rect r)
		{
			Texture2D texture = this.GetTexture(data.AtlasName);
			if (texture == null) return;
			float scale = Mathf.Min(r.width / data.Width, r.height / data.Height);
			float x0 = r.x + r.width / 2 - scale * data.Width / 2;
			float y0 = r.y + Mathf.Max(r.height/2+ scale * data.Height/2, scale * data.Height);

			data.ForeachVertexList(new Rect(0, 0, 1, 1), true, this,
				(r1, uv) =>
				{
					r1.xMin *= scale;
					r1.xMax *= scale;
					r1.x += x0;

					//RectはGUI系だとYが上下逆なのでその処理を
					r1.yMin *= -scale;
					r1.yMax *= -scale;
					r1.y += y0;

					float tmp = uv.yMin;
					uv.yMin = uv.yMax;
					uv.yMax = tmp;

					GUI.DrawTextureWithTexCoords(r1, texture, uv);
				});
		}
#endif
	}
}
