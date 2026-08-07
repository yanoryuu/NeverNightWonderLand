// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// インポートした全シナリオデータ
	/// </summary>
	[CreateAssetMenu(menuName = "Utage/Scenario/Scenarios")]
	public class AdvImportScenarios : ScriptableObject
	{
		const int Version = 3;
		
		[SerializeField]
		int importVersion = 0;

		public bool CheckVersion()
		{
			return importVersion == Version;
		}

		//インポートされた章データ
		[SerializeField]
		List<AdvChapterData> chapters = new List<AdvChapterData>();
		public List<AdvChapterData> Chapters { get { return chapters; } }

#if UNITY_EDITOR
		//インポート時のクリア処理
		public void ClearOnImport()
		{
			importVersion = Version;
			this.Chapters.Clear();
		}
#endif

		//章データを追加
		public void AddChapter(AdvChapterData chapterData)
		{
			this.Chapters.Add(chapterData);
		}

		//章データを追加（既に同じ名前の章があったら追加しない）
		public bool TryAddChapter(AdvChapterData chapterData)
		{
			if (Chapters.Exists(x => x.name == chapterData.name))
			{
				return false;
			}
			else
			{
				this.Chapters.Add(chapterData);
				return true;
			}
		}
	}
}
