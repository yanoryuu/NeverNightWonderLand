// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace Utage
{
	[System.Serializable]
	public class AssetBuildTimeStampInfo
	{
		public string path;
		public string guid;
		public long timeStamp;
		public bool Compare(AssetBuildTimeStampInfo target)
		{
			return this.path == target.path && this.guid == target.guid && this.timeStamp == target.timeStamp;
		}
	}
	[System.Serializable]
	public class AssetBuildTimeStamp
	{
		public List<AssetBuildTimeStampInfo> InfoList { get { return infoList; } }
		[SerializeField]
		List<AssetBuildTimeStampInfo> infoList = new List<AssetBuildTimeStampInfo>();

		public void MakeList(List<UnityEngine.Object> assets)
		{
			infoList = new List<AssetBuildTimeStampInfo>();
			foreach ( var asset in assets)
			{
				AssetBuildTimeStampInfo info = new AssetBuildTimeStampInfo();
				info.path = AssetDatabase.GetAssetPath(asset);
				info.guid = AssetDatabase.AssetPathToGUID(info.path);
				string fullpath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + info.path;
				info.timeStamp = System.IO.File.GetLastWriteTime(fullpath).Ticks;
				infoList.Add(info);
			}

			infoList.Sort((a, b) => (a.guid.CompareTo(b.guid)));
		}
		public bool Compare(AssetBuildTimeStamp target)
		{
			if(target.infoList.Count != infoList.Count)
			{
				return false;
			}

			for( int i = 0; i < this.infoList.Count; i++ )
			{
				if (!this.infoList[i].Compare( target.infoList[i]) )
				{
					return false;
				}
			}
			return true;
		}
	}
}
#endif
