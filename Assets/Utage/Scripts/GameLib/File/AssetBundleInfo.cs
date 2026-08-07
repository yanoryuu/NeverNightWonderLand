// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	public class AssetBundleInfo
	{
		public string Url { get; set; }		//途中で書き換えられるように、setプロパティをpublicに
		public Hash128 Hash { get; set; }
		public int Version { get; set; }
		public int Size { get; set; }       //DLサイズを設定可能に。Unity公式のマニフェストファイルではサイズが取得できないため、なんらかの形で独自設定が必要
		public bool CacheLoad { get; set; }		//キャッシュロード

		internal AssetBundleInfo(string url, Hash128 hash, int size = 0)
		{
			this.Url = url;
			this.Hash = hash;
			this.Version = int.MinValue;
			this.Size = size;
			this.CacheLoad = true;
		}
		internal AssetBundleInfo(string url, int version, int size = 0)
		{
			this.Url = url;
			this.Version = version;
			this.Size = size;
			this.CacheLoad = true;
		}
		internal AssetBundleInfo(string url)
		{
			this.Url = url;
			this.CacheLoad = false;
		}
	}
}