// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace Utage
{
	//差分を含めたテクスチャをパッキングして合成する
	public static class DicingTexturePacker
	{
		//パッキングする
		public static void Pack(DicingTextures target, bool rebuild)
		{
			if (target == null)
			{
				Debug.LogError("TARGET IS NULL ");
				return;
			}
			if (target.InputDir == null) return;
			if (target.OutputDir == null) return;

			Profiler.BeginSample("Pack");
			DisplayProgressBar(target, 0, "Start Packing");
			string assetName = target.name;
			try
			{
				PackSub(target, rebuild);
			}
			catch (System.Exception e)
			{
				Debug.LogError(assetName + e.Message, target);
				Debug.LogError(e.StackTrace);
			}
			EditorUtility.UnloadUnusedAssetsImmediate();
			System.GC.Collect();
			EditorUtility.ClearProgressBar();
			AssetDatabase.Refresh();
			Profiler.EndSample();
		}

		//パッキングする
		static void PackSub(DicingTextures target, bool rebuild)
		{
			AssetBuildTimeStamp timeStamp = null;
			string targetName = target.name;
			if (!RebuildCheckTimeStamp(target, rebuild, out timeStamp))
			{
				//メモリリーク対策(Unity2017.4では動作を確認。5.5ではリークが残る)
				timeStamp = null;
				System.GC.Collect();
				EditorUtility.UnloadUnusedAssetsImmediate(true);
				Debug.Log(string.Format(targetName + " is not changed"));
				return;
			}
			EditorUtility.SetDirty(target);
			//使用テクスチャをロード
			Profiler.BeginSample("LoadTextures");
			List<TextureInfo> textures = LoadTextures(target);
			Profiler.EndSample();
			//アトラス情報を作成
			Profiler.BeginSample("MakeAtlas");
			List<Atlas> atlasList = MakeAtlas(target, textures, target.InputDir);
			Profiler.EndSample();
			//アトラス画像とデータを出力して、ターゲットをリビルド
			Profiler.BeginSample("RebuildTarget");
			RebuildTarget(timeStamp, target, atlasList);
			Profiler.EndSample();

			//どれくらいサイズが減ったか、計測して出力
			OutputPerformace(targetName, textures, atlasList);
		}

		static void OutputPerformace(string targetName, List<TextureInfo> textures, List<Atlas> atlasList)
		{
			const int M = 1024*1024;
			//どれくらいサイズが減ったか、ピクセル数で計測して出力
			float totalPixelsCount = 0;
			textures.ForEach(x => totalPixelsCount += x.Texture.width * x.Texture.height);
			totalPixelsCount /= M;

			float totalAtlasSize = 0;
			atlasList.ForEach(x => totalAtlasSize += x.Width * x.Height);
			totalAtlasSize /= M;

			//消費メモリサイズを計測
			float totalMemSize = 0;
			textures.ForEach(
				x =>
				{
					int mem = Mathf.NextPowerOfTwo(x.Texture.width) * Mathf.NextPowerOfTwo(x.Texture.height);
					mem *= x.IsNoneAlpha ? 3 : 4;
					totalMemSize += mem;
				});
			totalMemSize /= M;

			float totalAtlasMemSize = 0;
			atlasList.ForEach(
				x =>
				{
					int mem = Mathf.NextPowerOfTwo(x.Width) * Mathf.NextPowerOfTwo(x.Height);
					mem *= x.IsNoneAlpha ? 3 : 4;
					totalAtlasMemSize += mem;
				});
			totalAtlasMemSize /= M;

			string msg0 = string.Format(" FileCount {0} -> {1}", textures.Count, atlasList.Count);
			string msg1 = string.Format(" TextureSize {1:.00} MB -> {2:.00} MB : {0:.00}% ", 100.0f * totalAtlasMemSize / totalMemSize, totalMemSize, totalAtlasMemSize);
			string msg2 = string.Format(" Pixcels {1:.00} M px -> {2:.00} M px : {0:.00}%", 100.0f * totalAtlasSize / totalPixelsCount, totalPixelsCount, totalAtlasSize);
			Debug.Log(targetName + msg0 + " " + msg1 + "\n" + msg2);
		}

		//進行状況のプログレスバーを表示
		static void DisplayProgressBar(DicingTextures target, float progress, string info= "")
		{
			EditorUtility.DisplayProgressBar(target.name + " Paking Dicing Textures", info, progress );
		}

		//再ビルドが必要かタイムスタンプをチェック
		static bool RebuildCheckTimeStamp(DicingTextures target, bool rebuild, out AssetBuildTimeStamp timeStamp)
		{
			MainAssetInfo dir = new MainAssetInfo(target.InputDir);
			timeStamp = dir.MakeBuildTimeStampAllChildren<Texture2D>();
			dir = null;
			if (rebuild)
			{
				return true;
			}
			else
			{
				//タイムスタンプが一致するなら、再ビルド必要ない
				return !target.BuildTimeStamp.Compare(timeStamp);
			}
		}

		//まとめる対象のテクスチャをロード
		static List<TextureInfo> LoadTextures(DicingTextures target)
		{
			List<TextureInfo> textures = new List<TextureInfo>();
			MainAssetInfo dir = new MainAssetInfo(target.InputDir);

			int count = 0;
			List<MainAssetInfo> assets = dir.GetAllChildren();
			foreach (var asset in assets)
			{
				if (asset.Asset is Texture2D)
				{
					textures.Add(new TextureInfo(asset.Asset as Texture2D, target));
				}
				++count;
				DisplayProgressBar(target, 0.3f *count/ assets.Count, "Load Textures");
			}
			return textures;
		}

		//テクスチャからアトラスを作成
		static List<Atlas> MakeAtlas(DicingTextures target, List<TextureInfo> textures, Object inptDir)
        {
			int count = 0;
			List<Atlas> atlasList = new List<Atlas>();
            foreach (var textureInfo in textures)
            {
				//既存のアトラス画像に追加してみる
                bool success = false;
                foreach (var atlas in atlasList)
                {
                    if (atlas.TryAddTexture(textureInfo))
                    {
                        success = true;
                        break;
                    }
                }
				//追加できなかったので新しくアトラス画像を作る
				if (!success)
                {
                    Atlas atlas = new Atlas( target.name + "_" + target.AtlasName + atlasList.Count, target);
					if (!atlas.TryAddTexture(textureInfo))
                    {
                        Debug.LogError("Texture Pack Error", textureInfo.Texture);
                    }
                    else
                    {
                        atlasList.Add(atlas);
                    }
                }
				++count;
				DisplayProgressBar(target, 0.3f + 0.4f * count / textures.Count, "Packing Textures");
			}
			return atlasList;
        }

		//アトラス画像とデータを出力して、インポートするターゲットをリビルド
		static void RebuildTarget(AssetBuildTimeStamp buildTimeStamp, DicingTextures target, List<Atlas> atlasList)
        {
			List<string> outputTexturePathList = new List<string>();
			List<DicingTextureData> textureDataList = new List<DicingTextureData>();

			//アトラス画像をファイルとして出力
			string dir = AssetDatabase.GetAssetPath(target.OutputDir);
			int count = 0;
			foreach (var atlas in atlasList)
			{
				++count;
				Texture2D texture = atlas.MakeAtlasTexture();
				string path = FilePathUtil.Combine(dir, atlas.Name + ".png");
				outputTexturePathList.Add(path);

				atlas.Write(path, texture);
				Object.DestroyImmediate(texture);
				DisplayProgressBar(target, 0.7f + 0.2f * count / atlasList.Count, "Make AtlasTexture");

				//元テクスチャを再現するための、頂点データやアトラス画像に対するUV値を作成
				textureDataList.AddRange(atlas.MakeImportData());

				DisplayProgressBar(target, 0.7f + 0.29f * count / atlasList.Count, "Make AtlasTexture");
			}
			textureDataList.Sort((a, b) => string.Compare(a.Name, b.Name));

			//新しいテクスチャをロード
			List<Texture2D> newTextureList = new List<Texture2D>();
			foreach (var path in outputTexturePathList)
			{
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (texture == null)
				{
					AssetDatabase.ImportAsset(path);
					texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				}
				newTextureList.Add(texture);
			}

			//登録済みのテクスチャのうち、使わなくなったものを削除
			List<Texture2D> removeTextureList = new List<Texture2D>();
			foreach (var texture in target.AtlasTextures)
			{
				if (!newTextureList.Contains(texture))
				{
					removeTextureList.Add(texture);
				}
			}

			//テクスチャを設定
			target.Build(buildTimeStamp, newTextureList, textureDataList);


			//インポート設定を上書き
			foreach (var path in outputTexturePathList) {
				AssetDatabase.ImportAsset(path);
				OverrideAtlasTextureImportSetting(path, target);
			}

			//使わなくなったものを削除
			foreach (var texture in removeTextureList)
			{
				Debug.Log("Remove " + texture.name);
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(texture));
			}
		}

		//アトラス画像のテクスチャインポート設定を上書き
		static void OverrideAtlasTextureImportSetting(string path, DicingTextures target)
		{
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer != null && target.OverrideTextureImporter)
			{
				bool hasChanged = TryOverrideTextureImportSetting(importer, target.MaxTxetureSize);
				if (hasChanged)
				{
					importer.SaveAndReimport();
					return;
				}
			}

			AssetDatabase.ImportAsset(path);
		}

		//元画像のテクスチャインポート設定を上書き
		static void OverrideTextureImportSetting(string path, DicingTextures target)
		{
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;

			bool hasChanged = TryOverrideTextureImportSetting(importer, target.MaxTxetureSize);

			if (importer.isReadable != true)
			{
				importer.isReadable = true;
				hasChanged = true;
			}

			if (hasChanged)
			{
				importer.SaveAndReimport();
			}
		}

		//元画像のテクスチャインポート設定を上書き
		static bool TryOverrideTextureImportSetting(TextureImporter importer, int maxTextureSize)
		{
			bool hasChanged = false;

#if UNITY_5_5_OR_NEWER
			if (importer.textureType != TextureImporterType.Default)
			{
				importer.textureType = TextureImporterType.Default;
				hasChanged = true;
			}
#else
			if (importer.textureType != TextureImporterType.Advanced)
			{
				importer.textureType = TextureImporterType.Advanced;
				hasChanged = true;
			}
#endif
			//MipMapはオフに
			if (importer.mipmapEnabled != false)
			{
				importer.mipmapEnabled = false;
				hasChanged = true;
			}
			//True Color
#if UNITY_5_5_OR_NEWER
			if (importer.textureCompression != TextureImporterCompression.Uncompressed)
			{
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				hasChanged = true;
			}
#else
			if (importer.textureFormat != TextureImporterFormat.AutomaticTruecolor)
			{
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
				hasChanged = true;
			}
#endif
			//テクスチャサイズの設定
//			if (importer.maxTextureSize != maxTextureSize)
			if (importer.maxTextureSize < maxTextureSize)	//アトラスの解像度未満にはならないようにしておく
			{
				importer.maxTextureSize = maxTextureSize;
				hasChanged = true;
			}
			//アルファの透明設定
			if (importer.alphaIsTransparency != true)
			{
				importer.alphaIsTransparency = true;
				hasChanged = true;
			}
			//Clamp設定
			if (importer.wrapMode != TextureWrapMode.Clamp)
			{
				importer.wrapMode = TextureWrapMode.Clamp;
				hasChanged = true;
			}
			//Non Power of 2
			if (importer.npotScale != TextureImporterNPOTScale.None)
			{
				importer.npotScale = TextureImporterNPOTScale.None;
				hasChanged = true;
			}

			return hasChanged;
		}

		//ダイシング処理で作るセル
		class ColorCell
        {
			//色
			public Color32[] Collors;

			//全て透明の場合
			public bool IsAllTransparnet { get; set; }

			//α値がない場合
			public bool IsNoneAlpha { get; set; }

			internal bool Compare(ColorCell cell)
            {
				if (IsAllTransparnet || cell.IsAllTransparnet)
				{
					return IsAllTransparnet && cell.IsAllTransparnet;
				}
				if (Collors.Length != cell.Collors.Length) return false;

				for (int i = 0; i < Collors.Length; ++i)
                {
                    if (Collors[i].r != cell.Collors[i].r) return false;
                    if (Collors[i].g != cell.Collors[i].g) return false;
                    if (Collors[i].b != cell.Collors[i].b) return false;
					if (Collors[i].a != cell.Collors[i].a) return false;
                }
                return true;
            }
			internal bool Compare(ColorCell cell, int threshold)
			{
				if (IsAllTransparnet || cell.IsAllTransparnet)
				{
					return IsAllTransparnet && cell.IsAllTransparnet;
				}
				if (Collors.Length != cell.Collors.Length)
				{
					return false;
				}

				for (int i = 0; i < Collors.Length; ++i)
				{
					int diff = 0;
					diff += Mathf.Abs(Collors[i].r - cell.Collors[i].r);
					diff += Mathf.Abs(Collors[i].g - cell.Collors[i].g);
					diff += Mathf.Abs(Collors[i].b - cell.Collors[i].b);
					diff += Mathf.Abs(Collors[i].a - cell.Collors[i].a);
					if (diff > threshold) return false;
				}
				return true;
			}
		}

		//パックする前のテクスチャ情報
		class TextureInfo
		{
			//テクスチャの幅
			public int Width { get { return texture.width; } }
			//テクスチャの高さ
			public int Height { get { return texture.height; } }

			//テクスチャの名前(元フォルダからの相対)
			public string Name { get { return name; } }
			string name;

			//アトラス化した先のセルのインデックス
			public List<int> cellIndexLists = new List<int>();

			//テクスチャデータ
			public Texture2D Texture { get { return texture; } }

			public bool IsNoneAlpha { get; internal set; }

			Texture2D texture;

			//セルのデータ
			ColorCell[,] cells;

			internal ColorCell GetCell( int x, int y )
			{
				return cells[x,y];
			}


			internal TextureInfo(Texture2D texture, DicingTextures target)
			{
				if (target.OverrideTextureImporter)
				{
					string path = AssetDatabase.GetAssetPath(texture);
					OverrideTextureImportSetting(path, target);
				}
				this.texture = texture;
				this.name = FilePathUtil.RemoveDirectory(AssetDatabase.GetAssetPath(texture), AssetDatabase.GetAssetPath(target.InputDir));
				this.name = FilePathUtil.GetPathWithoutExtension(this.name);
				MakeCells(target);
			}

			void MakeCells(DicingTextures target)
			{
				this.IsNoneAlpha = true;

				int atlasCellSize = target.CellSize;
				int textureCellSize = target.CellSize - target.Padding * 2;
				int padding = target.Padding;
				//テクスチャの全カラー情報
				Color32[] textureColors = texture.GetPixels32();

				int cellCountX = Mathf.CeilToInt(1.0f * Width / textureCellSize);
				int cellCountY = Mathf.CeilToInt(1.0f * Height / textureCellSize);

				this.cells = new ColorCell[cellCountX,cellCountY];
				for (int cellX = 0; cellX < cellCountX; ++cellX)
				{
					int x = cellX * textureCellSize;
					for (int cellY = 0; cellY < cellCountY; ++cellY)
					{
						int y = cellY * textureCellSize;
						Profiler.BeginSample("GetPixels32");
						ColorCell cell = MakeCell(textureColors, x - padding, y - padding, atlasCellSize, atlasCellSize);
						this.cells[cellX, cellY] = cell;
						Profiler.EndSample();
						Profiler.BeginSample("new ColorCell");
						Profiler.EndSample();
						if (!cell.IsNoneAlpha)
						{
							this.IsNoneAlpha = false;
						}
					}
				}
			}

			//指定の矩形のカラー配列を取得
			ColorCell MakeCell(Color32[] textureColors, int x0, int y0, int cellSizeW, int cellSizeH)
			{
				bool isAllTransParent = true;
				bool isNoneAlpha = true;
				ColorCell cell = new ColorCell();
				int cellCount = cellSizeW * cellSizeH;
				Color32[] colors = null;
				for (int y1 = 0; y1 < cellSizeH; ++y1)
				{
					int y = y0 + y1;
					for (int x1 = 0; x1 < cellSizeW; ++x1)
					{
						int x = x0 + x1;
						if (x < 0 || y < 0 || x >= Width || y >= Height)
						{
						}
						else
						{
							bool alphaZero = (textureColors[x + y * Width].a == 0);
							isAllTransParent &= alphaZero;
							isNoneAlpha &= !alphaZero;
							if (!isAllTransParent && !isNoneAlpha)
							{
								break;
							}
						}
					}
					if (!isAllTransParent && !isNoneAlpha)
					{
						break;
					}
				}
				if (!isAllTransParent)
				{
					colors = new Color32[cellCount];
					int index = 0;
					for (int y1 = 0; y1 < cellSizeH; ++y1)
					{
						int y = Mathf.Clamp(y0 + y1, 0, Height - 1);
						for (int x1 = 0; x1 < cellSizeW; ++x1)
						{
							int x = Mathf.Clamp(x0 + x1, 0, Width - 1);
							colors[index] = textureColors[x + y * Width];
							++index;
						}
					}
				}
				cell.IsNoneAlpha = isNoneAlpha;
				cell.IsAllTransparnet = isAllTransParent;
				cell.Collors = colors;
				return cell;
			}
		}

		//アトラス用データ
		class Atlas
        {
            //セルのデータ
            internal List<ColorCell> cells = new List<ColorCell>();

			//セルの大きさ(px)
			internal DicingTextures TargetSetting { get; private set; }

			//セルの大きさ(px)
			internal int CellSize { get { return TargetSetting.CellSize; } }

			//セルと境界線ピクセル数
			internal int Padding { get { return TargetSetting.Padding; } }

			//アトラスに合成した元画像のデータ
			internal List<TextureInfo> textures = new List<TextureInfo>();

            //セルの数の上限（総数）
            internal int MaxCellCount { get; private set; }

            //生成されるアトラスのテクスチャサイズ（幅）(px)
            internal int Width { get; private set; }

			//生成されるアトラスのテクスチャサイズ（高さ）(px)
			internal int Height { get; private set; }

			//名前
			public string Name { get; private set;  }
			public bool IsNoneAlpha { get; internal set; }

			//コンストラクタ
			internal Atlas(string name, DicingTextures targetSetting)
            {
                Name = name;
				TargetSetting = targetSetting;
				int max  = Mathf.CeilToInt(1.0f* targetSetting.MaxTxetureSize / CellSize);
				MaxCellCount = max * max;
			}

            //指定数のセルを追加できるかチェック
            internal bool CheckNewCellCount(int count)
            {
                return (cells.Count + count <= MaxCellCount);
            }

            //新たなテクスチャを追加してみる
            internal bool TryAddTexture(TextureInfo textureInfo)
            {
                List<ColorCell> newCells;
				List<int> indexList;
				if (TryAddTexture(textureInfo, out newCells, out indexList))
                {
					cells.AddRange(newCells);
					textureInfo.cellIndexLists = indexList;
					textures.Add(textureInfo);
                    return true;
                }
                else
                {
                    return false;
                }
            }

			//アトラス画像に新たなテクスチャを追加してみる
			internal bool TryAddTexture(TextureInfo texture, out List<ColorCell> newCells, out List<int> indexList)
			{
				indexList = new List<int>();
				newCells = new List<ColorCell>();

				int textureCellSize = CellSize - Padding * 2;
				int cellCountX = Mathf.CeilToInt(1.0f * texture.Width / textureCellSize);
				int cellCountY = Mathf.CeilToInt(1.0f * texture.Height / textureCellSize);

				for (int cellY = 0; cellY < cellCountY; ++cellY)
				{
					for (int cellX = 0; cellX < cellCountX; ++cellX)
					{
						ColorCell cell = texture.GetCell(cellX, cellY);
						//アトラス内のセルリストと比較
						int cellIndex = cells.FindIndex(item => (item.Compare(cell)));
						if (cellIndex < 0)
						{
							//新しいセルリストと比較
							cellIndex = newCells.FindIndex(item => (item.Compare(cell)));
							if (cellIndex < 0)
							{
								//新しいセルリストにもないので新規セルを作成
								if (!CheckNewCellCount(newCells.Count + 1))
								{
									//アトラス画像に入りきらない
									return false;
								}
								cellIndex = newCells.Count;
								newCells.Add(cell);
							}
							cellIndex += cells.Count;
						}
						indexList.Add(cellIndex);
					}
				}
				return true;
			}


			//アトラスの画像データを出力
			internal void Write(string path, Texture2D texture)
            {
				byte[] bytes = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, bytes);
			}

            //アトラスの画像を作成
            internal Texture2D MakeAtlasTexture()
            {
				InitAtlasSize();
				Texture2D texture = new Texture2D(Width, Height);

				this.IsNoneAlpha = true;
				foreach ( var cell in cells )
				{
					if (!cell.IsNoneAlpha)
					{
						IsNoneAlpha = false;
						break;
					}
				}

				//デフォルト（書き込みをしない）色
				Color32[] defaultColorArray = new Color32[CellSize * CellSize];
				byte alpha = IsNoneAlpha ? byte.MaxValue : byte.MinValue;
				Color32 defaultColor = new Color32(0, 0, 0, alpha);
				for (int i = 0; i < defaultColorArray.Length; ++i)
				{
					defaultColorArray[i] = defaultColor;
				}

				//
				int cellIndex = 0;
                for (int y = 0; y < Height; y+=CellSize)
                {
                    for (int x = 0; x < Width; x+=CellSize)
                    {
						try
						{
							if (cellIndex >= cells.Count)
							{
								//セルの登録がない場合完全に透明
								texture.SetPixels32(x, y, CellSize, CellSize, defaultColorArray);
							}
							else
							{
								ColorCell cell = cells[cellIndex];
								if (cell.IsAllTransparnet)
								{
									//セルの登録がない場合完全に透明
									texture.SetPixels32(x, y, CellSize, CellSize, defaultColorArray);
								}
								else
								{
									texture.SetPixels32(x, y, CellSize, CellSize, cell.Collors);
								}
								++cellIndex;
							}
						}
						catch (System.Exception e)
						{
							Debug.LogError(e.Message);
						}
					}
				}
				texture.Apply();
				if (cells.Count != cellIndex)
				{
					Debug.Log("");
				}
                return texture;
            }

			void InitAtlasSize()
			{
				int totalCellSize = this.cells.Count * CellSize * CellSize;
				for (int size = 1; size <= this.TargetSetting.MaxTxetureSize; size *= 2 )
				{
					if (size*size >= totalCellSize)
					{
						Width = size;
						int countX = Mathf.CeilToInt(1.0f * Width / CellSize);
						int countY = Mathf.CeilToInt(1.0f * this.cells.Count / countX);
						Height = countY * CellSize;
						return;
					}
				}
				Debug.LogError("Cant Init Atlas Size");
			}

			//インポートデータを作成
			internal List<DicingTextureData> MakeImportData()
			{
				List<DicingTextureData> list = new List<DicingTextureData>();
				foreach (TextureInfo texture in textures)
				{
					DicingTextureData data = new DicingTextureData();
					data.InitOnImport(texture.Name, this.Name, texture.Width, texture.Height, 
						texture.cellIndexLists,
						this.cells.FindIndex( x=> x.IsAllTransparnet));
					list.Add(data);
				}
				return list;
			}
		}
	}
}
