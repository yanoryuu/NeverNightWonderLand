// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;
using UnityEngine.Profiling;
using System;

namespace Utage
{
	/// <summary>
	/// ノベル用に、禁則処理などを含めたテキスト表示
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/Internal/UguiNovelTextGenerator")]
	public class UguiNovelTextGenerator : MonoBehaviour
	{
		public UguiNovelText NovelText { get { return this.GetComponentCache(ref novelText); } }
		UguiNovelText novelText;

		//実際の頂点情報の計算などをする部分
		//巨大になるので、コンポーネントは設定データやイベントの呼び出し制御
		//実際の処理はInfoへと分けている
		UguiNovelTextGeneratorInfo Info
		{
			get
			{
				if (info == null)
				{
					info = new UguiNovelTextGeneratorInfo(this);
				}
				return info;
			}
		}
		UguiNovelTextGeneratorInfo info;

		//テキスト情報
		TextData TextData { get { return Info.TextData; } }

		//行のデータ
		internal List<UguiNovelTextLine> LineDataList { get { return Info.LineDataList; } }

		//当たり判定
		public List<UguiNovelTextHitArea> HitGroupLists { get { return Info.HitGroupLists; } }

		/// <summary>
		/// スペースの幅(px)
		/// </summary>
		public float Space
		{
			get { return space; }
			set { space = value; SetAllDirty(); }
		}
		[SerializeField]
		float space = -1;

		/// <summary>
		/// 文字間(px)
		/// </summary>
		public float LetterSpaceSize
		{
			get { return letterSpaceSize; }
			set { letterSpaceSize = value; SetAllDirty(); }
		}
		[SerializeField]
		float letterSpaceSize = 1;


		/// <summary>
		/// 禁則処理の仕方
		/// </summary>
		[System.Flags]
		public enum WordWrap
		{
			/// <summary>デフォルト（半角のみ）</summary>
			Default = 0x1,
			/// <summary>日本語の禁則処理</summary>
			JapaneseKinsoku = 0x2,
		};
		/// <summary>
		/// 禁則処理の仕方
		/// </summary>
		public WordWrap WordWrapType
		{
			get { return wordWrap; }
			set { wordWrap = value; SetAllDirty(); }
		}
		[SerializeField]
		[EnumFlagsAttribute]
		WordWrap wordWrap = WordWrap.Default | WordWrap.JapaneseKinsoku;

		/// <summary>表示する文字の長さ(-1なら全部表示)</summary>
		public int LengthOfView
		{
			get { return lengthOfView; }
			set
			{
				if (lengthOfView != value)
				{
					lengthOfView = value;
					NovelText.SetVerticesOnlyDirty();
				}
			}
		}
		[SerializeField]
		int lengthOfView = -1;

		/// <summary>現在の表示する文字の長さ</summary>
		internal int CurrentLengthOfView
		{
			get { return (LengthOfView < 0) ? TextData.Length : LengthOfView; }
		}

		/// <summary>
		/// //テキスト設定
		/// </summary>
		public UguiNovelTextSettings TextSettings
		{
			get { return textSettings; }
			set { textSettings = value; SetAllDirty(); }
		}
		[SerializeField]
		UguiNovelTextSettings textSettings;

		/// <summary>
		/// ルビのフォントサイズのスケール（ルビ対象の文字サイズに対しての倍率）
		/// </summary>
		public float RubySizeScale
		{
			get { return rubySizeScale; }
			set { rubySizeScale = value; SetAllDirty(); }
		}
		[SerializeField]
		float rubySizeScale = 0.5f;

		/// <summary>
		/// 上付き、下付き文字のサイズのスケール（上付き、下付き対象の文字サイズに対しての倍率）
		/// </summary>
		public float SupOrSubSizeScale
		{
			get { return supOrSubSizeScale; }
			set { supOrSubSizeScale = value; SetAllDirty(); }
		}
		[SerializeField]
		float supOrSubSizeScale = 0.5f;

		/// <summary>
		/// 絵文字のデータ
		/// </summary>
		public UguiNovelTextEmojiData EmojiData
		{
			get { return emojiData; }
			set { emojiData = value; SetAllDirty(); }
		}
		[SerializeField]
		UguiNovelTextEmojiData emojiData;

		public char DashChar { get { return (dashChar == 0) ? CharData.Dash : dashChar; } }
		[SerializeField]
		char dashChar = '—';

		public int BmpFontSize
		{
			get
			{
				if (NovelText.font != null && !NovelText.font.dynamic)
				{
					if (bmpFontSize <= 0)
					{
						Debug.LogError("bmpFontSize is zero", this);
						return 1;
					}
				}
				return bmpFontSize;
			}
		}

		[SerializeField]
		int bmpFontSize = 1;

		public bool IsUnicodeFont { get { return isUnicodeFont; } }
		[SerializeField]
		bool isUnicodeFont = false;


		RectTransform cachedRectTransform;
		public RectTransform CachedRectTransform { get { if (this.cachedRectTransform == null) cachedRectTransform = GetComponent<RectTransform>(); return cachedRectTransform; } }


		//内容が変化しているか
		enum ChagneType
		{
			None,
			VertexOnly,
			All,
		};
		ChagneType CurrentChangeType { get; set; }

		// 表示される高さ
		public float PreferredHeight
		{
			get
			{
				Refresh();
				return Info.PreferredHeight;
			}
		}

		//推奨される幅(幅が決まっていなくて自動改行がない前提)
		public float PreferredWidth
		{
			get
			{
				Refresh();
				return Info.PreferredWidth;
			}
		}

		public Vector3 EndPosition { get { return Info.EndPosition; } }

		internal void RefreshEndPosition()
		{
			Refresh();
			Info.RefreshEndPosition();
		}

		bool isDebugLog = false;

#if UNITY_EDITOR
		protected void OnValidate()
		{
			SetAllDirty();
		}
#endif
		void SetAllDirty()
		{
			NovelText.SetAllDirty();
		}

		void OnEnable()
		{
			//これやらないとLateUpdateが間に合わないときがある
			RefreshAll();
		}

		//頂点情報を作成
		void LateUpdate()
		{
			Refresh();
			Info.UpdateGraphicObjectList(this.CachedRectTransform);
		}

		void RefreshAll()
		{
			ChangeAll();
			Refresh();
		}

		void Refresh()
		{
			switch (CurrentChangeType)
			{
				case ChagneType.All:
					if (isDebugLog) Debug.Log("RefreshAll " + this.NovelText.text);
					Info.BuildCharcteres();
					Info.BuildTextArea(this.CachedRectTransform);
					break;
				case ChagneType.VertexOnly:
					break;
				case ChagneType.None:
					break;
			}
			CurrentChangeType = ChagneType.None;
		}

		//変更
		internal void ChangeAll()
		{
			CurrentChangeType = ChagneType.All;
			if (isDebugLog) Debug.Log("CurrentChangeType = ChagneType.All" + this.NovelText.text);
		}

		//頂点のみ変更
		internal void ChangeVertsOnly()
		{
			if (CurrentChangeType == ChagneType.All) return;

			CurrentChangeType = ChagneType.VertexOnly;
			if (isDebugLog) Debug.Log("CurrentChangeType = ChagneType.VertexOnly" + this.NovelText.text);
		}

		//頂上情報を再作成
		internal void RemakeVerts()
		{
			if (CurrentChangeType == ChagneType.All) return;
			this.Info.RemakeVerts();
		}

		//頂点変更時に呼ばれる（LateUpdateに間に合わないケースはここで処理される）
		internal void OnDirtyVerts()
		{
			if (isDebugLog) Debug.Log("OnDirtyVerts"  +  CurrentChangeType.ToString() + this.NovelText.text);
			Refresh();
		}

		internal void OnTextureRebuild(Font font)
		{
			if (font == this.NovelText.font)
			{
				if (isDebugLog) Debug.Log("OnTextureRebuild " + this.NovelText.text);
//				Info.BuildCharcteres();
//				Info.BuildTextArea(this.CachedRectTransform);
				Info.RebuildFontTexture(font);
			}
		}

		//頂点情報を作成
		public void CreateVertex(List<UIVertex> verts)
		{
			if (CurrentChangeType != ChagneType.None)
			{
				if (Application.isPlaying)
				{
					Debug.LogError("NotTextUpdated OnCreateVertex " + this.NovelText.text);
				}
				return;
			}
			if (isDebugLog) Debug.Log("CreateVertex" + this.NovelText.text);
			this.Info.CreateVertex(verts);
		}

		//文字が範囲外かどうかのチェック
		public bool EditorCheckRect(string text, out int len, out string errorString)
		{
			this.NovelText.text = text;
			ChangeAll();
			Refresh();
			errorString = "";
			bool isOver = false;
			foreach (var item in Info.LineDataList)
			{
				if (item.IsOver)
				{
					isOver = true;
					break;
				}
			}

			if (isOver)
			{
				System.Text.StringBuilder normalText = new System.Text.StringBuilder();
				System.Text.StringBuilder overedText = new System.Text.StringBuilder();
				System.Text.StringBuilder builder = normalText;
				int overLineCount = 0;
				foreach (var line in Info.LineDataList)
				{
					if (line.IsOver)
					{
						builder = overedText;
						++overLineCount;
						if (overLineCount > 10)
						{
							builder.AppendLine("...");
							break;
						}
					}
					foreach (var c in line.Characters)
					{
						builder.Append(c.Char);
						if (c.isAutoLineBreak)
						{
							builder.AppendLine();
						}
					}
				}
				errorString += normalText.ToString() + TextParser.AddTag(overedText.ToString(), "color", "red");
			}

			len = Info.TextData.Length;
			return !isOver;
		}
	}
}
