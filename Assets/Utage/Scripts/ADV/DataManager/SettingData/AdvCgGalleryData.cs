// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// CG回想用のデータ
	/// </summary>
	public partial class AdvCgGalleryData
	{
		/// <summary>
		/// ファイル名
		/// </summary>
		List<AdvTextureSettingData> list;

		/// <summary>
		/// サムネイル表示用のテクスチャのパス
		/// </summary>
		public string ThumbnailPath { get { return this.thumbnailPath; } }
		string thumbnailPath;

		/// <summary>登録されている数</summary>
		public int NumTotal	{get{return list.Count;}}

		/// <summary>回想がオープンされている数</summary>
		public int NumOpen
		{ 
			get
			{
				int num = 0;
				if( saveData == null ) return 0;

				foreach (var item in list)
				{
					if (saveData.CheckCgLabel(item.Key))
					{
						++num;
					}
				}
				return num;
			}
		}
		
		//解放されているか？（解放数が1以上）
		public bool IsOpened => NumOpen > 0;

		/// <summary>セーブデータ</summary>
		AdvGallerySaveData saveData;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="title">表示タイトル</param>
		public AdvCgGalleryData(string thumbnailPath, AdvGallerySaveData saveData)
		{
			this.thumbnailPath = thumbnailPath;
			this.list = new List<AdvTextureSettingData>();
			this.saveData = saveData;
		}

		/// <summary>
		/// テクスチャデータを追加
		/// </summary>
		/// <param name="data">テクスチャデータ</param>
		public void AddTextureData(AdvTextureSettingData data)
		{
			list.Add(data);
		}

		/// <summary>
		/// 閲覧可能な、指定インデックスのデータを取得
		/// </summary>
		/// <param name="index">インデックス</param>
		public AdvTextureSettingData GetDataOpened(int index)
		{
			int num = 0;
			foreach (var item in list)
			{
				if (saveData.CheckCgLabel(item.Key))
				{
					if( index == num )
					{
						return item;
					}
					++num;
				}
			}
			return null;
		}
	}
}
