// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Utage
{
	/// <summary>
	/// ノベル用のアバターデータ（パーツの重ね合わせデータ）
	/// </summary>
	[CreateAssetMenu(menuName = "Utage/AvatarData")]
	public class AvatarData : ScriptableObject
	{
		//スプライト名をパターン名に変換
		static public string ToPatternName(Sprite sprite)
		{
			if (sprite == null) return "";
			return ToPatternName(sprite.name);
		}

		//スプライト名をパターン名に変換
		static public string ToPatternName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return "";
			}
			return name.Split('_')[0];
		}

		//タグをつけられたリソースカテゴリ（1フォルダ＝描画1階層ぶんの描画）
		[System.Serializable]
		public class Category
		{
			//カテゴリ名（元となるフォルダ名）
			public string Name { get { return name; } set { name = value; } }
			[SerializeField]
			string name;

			//描画順
			public int SortOrder { get { return sortOrder; } set { sortOrder = value; } }
			[SerializeField]
			int sortOrder;

			//タグ
			public string Tag { get { return tag; } set { tag = value; } }
			[SerializeField]
			string tag;

			//スプライトのリスト
			public List<Sprite> Sprites { get { return sprites; } set { sprites = value; } }
			[SerializeField]
			List<Sprite> sprites = new List<Sprite>();

			//すべてのパターン名を取得
			public HashSet<string> GetAllPatternNames()
			{
				HashSet<string> set = new HashSet<string>();
				this.Sprites.ForEach(x => set.Add(AvatarData.ToPatternName(x)));
				return set;
			}

			//パターン名からスプライトを取得
			public Sprite GetSprite(string pattern)
			{
				//パタ―ン名が一致する一つだけを取得
				Sprite sprite = Sprites.Find(x => AvatarData.ToPatternName(x) == pattern);
				if (sprite == null)
				{
					//パターン名で一致しない場合は、直接スプライト名を検索
					sprite = Sprites.Find(x => x != null && x.name == pattern);
				}
				if (sprite == null)
				{
					//それでも一致しない場合は、パターン名のほうが正規化されてない可能性があるので
					sprite = Sprites.Find(x => x != null && x.name == AvatarData.ToPatternName(pattern));
				}
				return sprite;
			}
		}

		//タグで分けられたカテゴリデータ
		//この中にスプライトなどが入っている
		[NotEditable]
		public List<Category> categories = new List<Category>();

#if UNITY_EDITOR
		//使用可能なタグ
		public List<string> TagList { get { return tagList; } }
		[SerializeField]
		List<string> tagList = new List<string>() { "body", "face", "eye", "lip", "hair", "accessory" };
#endif
		//アクセサリーなどのオプション表示のタグ
		public string OptionTag { get { return optionTag; } }

		[SerializeField]
		string optionTag = "accessory";

		//アバターのサイズ（表示ピクセル数）
		public Vector2 Size { get { return size; } internal set { size = value; } }
		[SerializeField, NotEditable]
		Vector2 size;

		public List<Sprite> MakeSortedSprites(AvatarPattern pattern)
		{
			List<Sprite> sprites = new List<Sprite>();
			foreach (var category in categories)
			{
				if (category.Tag != optionTag)
				{
					//アクセサリなどのオプションではないので
					//パターンデータに一致するスプライトを追加していく
					foreach (var data in pattern.DataList)
					{
						//カテゴリタグがあうものを取得
						if (category.Tag != data.Tag) continue;
						//パタ―ン名が一致する一つだけを取得
						sprites.Add(category.GetSprite(data.PatternName));
					}
				}
				else
				{
					//アクセサリなどのオプションの場合は、パターン名に一致するものはすべて取得
					foreach (var optionPattern in pattern.OptionPatternNameList)
					{
						sprites.AddRange(category.Sprites.FindAll(x => AvatarData.ToPatternName(x) == optionPattern));
					}
				}
			}
			return sprites;
		}

		public void CheckPatternError(AvatarPattern pattern)
		{
			foreach ( var patternData in pattern.DataList )
			{
				if (CheckPatternError(pattern,patternData))
				{
					Debug.LogErrorFormat("Tag:{0} Pattern:{1} is not found in AvatorData {2}", patternData.Tag, patternData.PatternName, this.name);
				}
			}
		}
		bool CheckPatternError(AvatarPattern pattern, AvatarPattern.PartternData patternData)
		{
			if (string.IsNullOrEmpty(patternData.PatternName)) return false;
			if (AvatarData.ToPatternName(patternData.PatternName)=="None") return false;

			foreach (var category in categories)
			{
				if (category.Tag != optionTag)
				{
					if (category.Tag != patternData.Tag) continue;
					if (category.GetSprite(patternData.PatternName) != null)
					{
						return false;
					}
				}
				else
				{
					foreach (var optionPattern in pattern.OptionPatternNameList)
					{
						if (category.Sprites.Exists(x => AvatarData.ToPatternName(x) == optionPattern))
						{
							return false;
						}
					}
				}
			}
			
			return true;
		}

		//アクセサリーなどのオプション表示のパターン名をすべて取得
		public List<string> GetAllOptionPatterns()
		{
			HashSet<string> set = new HashSet<string>();
			foreach (var category in categories)
			{
				if (category.Tag != this.OptionTag) continue;
				set.UnionWith(category.GetAllPatternNames());
			}
			return new List<string>(set);
		}

		//以下、エディタ上でのみ使用する設定
#if UNITY_EDITOR

		[Space(8)]
		[HelpBox("リソースデータ一覧。描画順はドラッグ＆ドロップで変更できます")]
		[SerializeField]
		AvatorFolderDataList dataList = new AvatorFolderDataList();

		//リソースをリインポートするボタン
		[Button("ReimportResources", "ReimportResources")]
		public string reimportResourcesButton = "";
		void ReimportResources()
		{
			hasReimport = true;
			EditorUtility.SetDirty(this);
		}

		//リソースを読み込んで、データを作成・更新するボタン
		[Button("OnApply", "Apply")]
		public string applyButton = "";

		//リソースを読み込んで、データを作成・更新
		void OnApply()
		{
			hasApply = true;
		}
		//プレビュー用のパターンデータ
		[HelpBox("プレビュー設定")]
		[SerializeField, NovelAvatarPattern("GetThis")]
		AvatarPattern previewPattern = new AvatarPattern();
		AvatarData GetThis() { return this; }

		[UpdateFunction("UpdateFunction")]
		public int updateFunction = 0;

		//サイズを設定しなおし
		void RebuildSize()
		{
			bool isFirst = true;
			Vector2 size = Vector2.zero;
			foreach (var category in categories)
			{
				foreach (var part in category.Sprites)
				{
					if (isFirst)
					{
						size = part.rect.size;
					}
					else if (size != part.rect.size)
					{
						//サイズがすべて同一である必要がある
						Debug.LogError("All sprite must be the same size.", part);
					}
				}
			}
			this.Size = size;
		}

		bool hasApply = false;
		bool hasReimport = false;
		void UpdateFunction()
		{
			if (hasApply)
			{
				Debug.Log("Rebuilding...", this);
				categories.Clear();
				int count = dataList.List.Count;
				for (int i = 0; i < count; ++i)
				{
					AvatorFolderData data = dataList.List[i];
					Category category = new Category();
					category.SortOrder = i;
					category.Name = data.folder.name;
					category.Tag = data.tag;
					category.Sprites = data.GetAllAssets<Sprite>();
					categories.Add(category);
				}
				categories.Sort((a, b) => a.SortOrder - b.SortOrder);
				RebuildSize();
				//プレビュー用のパターンデータも作り直し
				previewPattern.RebuildOnApply(this);
				hasApply = false;
				hasReimport = false;
				GUIUtility.ExitGUI();
			}
			else if (hasReimport)
			{
				Debug.Log("ReimportResources...", this);
				dataList.ReimportResources("Tag" + this.name);
				hasApply = false;
				hasReimport = false;
				GUIUtility.ExitGUI();
			}
		}

		//指定のタグ内にあるパターン名のリストを取得
		internal List<string> GetPatternNameListInTag(string tag)
		{
			List<string> patterns = new List<string>();
			categories.ForEach(x =>
			{
				if (x.Tag == tag)
				{
					patterns.AddRange(x.GetAllPatternNames());
				}
			});
			return patterns;
		}

		//プレビュー
		public void OnPreviewGUI(Rect r, GUIStyle background, AvatarPattern patternList)
		{
			List<Sprite> parts = MakeSortedSprites(patternList);
			foreach (Sprite part in parts)
			{
				if (part == null) continue;
				GUI.DrawTexture(r, AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(part)), ScaleMode.ScaleToFit, true);
			}
		}


		//インスペクター表示
		[CustomEditor(typeof(AvatarData))]
		public class NovelAvatarDataInspector : Editor
		{
			AvatarData Obj { get { return this.target as AvatarData; } }

			//プレビュー表示する場合true
			public override bool HasPreviewGUI()
			{
				return true;
			}

			public override void OnPreviewGUI(Rect r, GUIStyle background)
			{
				AvatarData obj = this.target as AvatarData;
				obj.OnPreviewGUI(r, background, obj.previewPattern);
			}
		}
#endif
	}
}
