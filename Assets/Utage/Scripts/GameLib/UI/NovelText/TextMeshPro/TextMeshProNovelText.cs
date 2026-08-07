
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;

namespace Utage
{
	//NovelTextのTextMeshPro版
	//ルビとか宴のタグも解析して表示する
	[AddComponentMenu("Utage/TextMeshPro/TextMeshProNovelText")]
	public class TextMeshProNovelText : MonoBehaviour
		, INovelText
	{
		//ルビのプレハブ
		public TextMeshProRuby RubyPrefab
		{
			get => rubyPrefab;
			set => rubyPrefab = value;
		}
		[SerializeField] TextMeshProRuby rubyPrefab = null;

		//ルビが本文よりも大きかった場合に、本文のほうに余白を開けるようにテキストを作り直す
		protected bool RemakeLargeRuby => remakeLargeRuby;
		[SerializeField] bool remakeLargeRuby = true;

		//宴のテキスト解析と実際に表示するテキストに違いがないかなどのチェックを行う
		//タグの構文エラーチェックなどの代用となる
		protected bool DebugLogError => debugLogError;
		[SerializeField] bool debugLogError = true;

		//文字溢れをランタイムでチェックする
		protected bool CheckOverFlow => checkOverFlow;
		[SerializeField] bool checkOverFlow = false;

		public TMP_Text TextMeshPro => this.GetComponentCache(ref textMeshPro);
		TMP_Text textMeshPro;

		public virtual int MaxVisibleCharacters
		{
			get
			{
				return TextMeshPro.maxVisibleCharacters;
			}
			set
			{
				if (TextMeshPro.maxVisibleCharacters != value)
				{
					TextMeshPro.maxVisibleCharacters = value;
					UpdateVisibleIndex();
				}
			}
		}

		public Color Color
		{
			get
			{
				return TextMeshPro.color;
			}
			set
			{
				TextMeshPro.color = value;
				if (RubyObjectList.Count > 0)
				{
					var textInfo = TextMeshPro.ForceGetTextInfo();
					foreach (var ruby in RubyObjectList)
					{
						ruby.UpdateColor(textInfo);
					}
				}
			}
		}

		public Vector3 CurrentEndPosition
		{
			get
			{
				if (HasChanged)
				{
					ForceUpdate();
				}
				int index = MaxVisibleCharacters - 1;
				TMP_TextInfo info = TextMeshPro.textInfo;
				int len = info.characterInfo.Length;
				if (index < 0 || len <= index)
				{
					if (index == -1 && len > 0)
					{
						index = 0;
					}
					else
					{
						Debug.LogFormat("index={0} len={1} ", index, len);
						return TextMeshPro.rectTransform.anchoredPosition3D;
					}
				}
				var c = info.characterInfo[index];
				var line = info.lineInfo[c.lineNumber];
				Vector3 pos = c.bottomRight;
				pos.y = line.baseline;
				return pos;
			}
		}

		protected TextData TextData { get; set; }
		//ルビの情報
		protected List<TextMeshProRubyInfo> RubyInfoList { get; } = new List<TextMeshProRubyInfo>();

		protected List<TextMeshProRuby> RubyObjectList { get; } = new List<TextMeshProRuby>();

		protected bool HasChanged { get; set; }
		
		//既にUpdateのタイミングがすぎているか
		//これがtrueの場合は、テキスト変更が呼び出されたらすぐにForceUpdateで強制的に更新
		//TextMeshProは、ScriptOrderが-100などに設定されているため、通常のスクリプトのLateUpdateのタイミングで更新しても間に合わない
		protected bool Updated { get; set; }


		public void SetNovelTextData(TextData textData, int length)
		{
			this.TextData = textData;
			MaxVisibleCharacters = length;
			HasChanged = true;
			if (Updated)
			{
				ForceUpdate();
			}
		}

		public void SetText(string text)
		{
			this.TextData = new TextData(text);
			HasChanged = true;
			if (Updated)
			{
				ForceUpdate();
			}
		}

		public virtual string GetText()
		{
			return TextMeshPro.text;
		}
		public virtual void SetTextDirect(string text)
		{
			TextMeshPro.text = text;
		}


		public void Clear()
		{
			//LateUpdateなどで遅れる場合があるので、直接クリアもする
			SetTextDirect("");
			SetText("");
		}

		private void Update()
		{
			UpdateIfChanged();
			Updated = true;
			
		}

		private void LateUpdate()
		{
			UpdateIfChanged();
			Updated = false;
		}

		public void UpdateIfChanged()
		{
			if (HasChanged)
			{
				ForceUpdate();
			}
		}

		public void ForceUpdate(bool ignoreActiveState = false)
		{
			if (this.TextData == null)
			{
				return;
			}
			ForceUpdateSub(ignoreActiveState,false);

			//大きなルビがあった場合に作り直す
			if (CheckRemakeRuby())
			{
				ForceUpdateSub(ignoreActiveState,true);
			}

			HasChanged = false;

#if UNITY_EDITOR
			if (DebugLogError && Application.isPlaying)
			{
				if(!ignoreActiveState && !this.TextMeshPro.IsActive())
				{
					return;
				}
				if (!CheckTextParse())
				{
					ForceUpdateSub(ignoreActiveState, false);
				}
				if ( CheckOverFlow && TextMeshPro.isTextOverflowing && TextData.Length > 0)
				{
					Debug.LogErrorFormat(this,"Overflowing text range.\n{0}", TextMeshPro.text);
				}
			}
#endif
		}
		
		//リッチテキストテキスト解析の結果の文字数と、TextMeshProの表示文字数が一致しているかチェックする
		bool CheckTextParse()
		{
			if (TextMeshPro == null) return true;
			if (TextMeshPro.textInfo == null) return true;
			int len = TextData.Length;
			int count = TextMeshPro.textInfo.characterCount;
			if (len != count)
			{
				string tmpTxt = "";
				for (var i = 0; i < count; i++)
				{
					tmpTxt += TextMeshPro.textInfo.characterInfo[i].character;
				}

				try
				{
					string text = TextMeshPro.text;
					string errorString =
						$"Please Check for errors in rich text tags. The number of characters {len} in the text analysis result and the number of characters {count} displayed in TextMeshPro are different." +
						$" \n{text}" +
						$"\n---" +
						$"\n{tmpTxt}";
					Debug.LogError(errorString,this);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
				return false;
			}
			return true;
		}

		bool CheckRemakeRuby()
		{
			if (!RemakeLargeRuby) return false;
			foreach ( var item in RubyInfoList )
			{
				if (item.RemakeCspace>0) return true;
			}
			return false;
		}

		protected virtual void ForceUpdateSub(bool ignoreActiveState, bool remakingLargeRuby)
		{
			Profiler.BeginSample("MakeTextMeshProString");
			string textMeshProString = MakeTextMeshProString(remakingLargeRuby);
			Profiler.EndSample();
			ForceUpdateText(textMeshProString, ignoreActiveState);
			MakeRubyTextObjects();
		}

		protected virtual void ForceUpdateSubInEditor()
		{
			Profiler.BeginSample("ForceUpdateSubInEditor");
			string textMeshProString = MakeTextMeshProString(false);
			Profiler.EndSample();
			ForceUpdateText(textMeshProString,true);
		}

		protected virtual void ForceUpdateText(string textMeshProString,bool ignoreActiveState)
		{
			TextMeshPro.text = textMeshProString;
			TextMeshPro.ForceMeshUpdate(ignoreActiveState);
		}

		// TextMeshPro形式のタグつき文字列を作成
		string MakeTextMeshProString(bool remakingLargeRuby)
		{
			StringBuilder builder = new StringBuilder();
			if (remakingLargeRuby)
			{
				List<TextMeshProRubyInfo> oldRubyList = new List<TextMeshProRubyInfo>();
				oldRubyList.AddRange(RubyInfoList);
				MakeTextMeshProString(builder, remakingLargeRuby, oldRubyList);
			}
			else
			{
				MakeTextMeshProString(builder, remakingLargeRuby, null);
			}
			return builder.ToString();
		}

		// TextMeshPro形式のタグつき文字列を作成
		void MakeTextMeshProString(StringBuilder builder, bool remakingLargeRuby, List<TextMeshProRubyInfo> oldRubyList)
		{
			RubyInfoList.Clear();
			TextMeshProRubyInfo rubyInfo = null;
			int index = 0;
			int countCharacter = 0;
			foreach (IParsedTextData data in TextData.ParsedText.ParsedDataList)
			{
				if (data is CharData)
				{
					CharData c = data as CharData;
					if (TextData.CharList[index] != c)
					{
						Debug.LogError("テキストの解析が失敗しています");
						continue;
					}
					CharData.CustomCharaInfo info = c.CustomInfo;
					bool ignoreChar = (info.IsEmoji) || info.IsDash;
					if (!ignoreChar)
					{
						c.AppendToStringBuilder(builder);
						++countCharacter;
					}
					index++;
				}
				else if (data is TagData tagData)
				{
					if (!tagData.IgnoreTagString)
					{
						builder.Append(tagData.TagString);
					}
					switch (tagData.TagName)
					{
						case "ruby":
							rubyInfo = new TextMeshProRubyInfo(tagData.TagArg, countCharacter, false);
							builder.Append("<nobr>");
							if (remakingLargeRuby)
							{
								int rubyInex = RubyInfoList.Count;
								if (rubyInex >= oldRubyList.Count)
								{
									Debug.LogErrorFormat("Ramake Ruby error index={0} oldRubyList.Count={1}", rubyInex, oldRubyList.Count);
									rubyInex = oldRubyList.Count - 1;
								}
								float width = oldRubyList[rubyInex].RemakeCspace;
								builder.Append("<space=").Append(width).Append(">");
								builder.Append("<cspace=").Append(width).Append(">");
							}
							break;
						case "/ruby":
							if (remakingLargeRuby)
							{
								int rubyInex = RubyInfoList.Count;
								if (rubyInex >= oldRubyList.Count)
								{
									Debug.LogErrorFormat("Ramke Ruby error index={0} oldRubyList.Count={1}", rubyInex, oldRubyList.Count);
									rubyInex = oldRubyList.Count - 1;
								}
								float width = oldRubyList[rubyInex].RemakeCspace;
								builder.Append("</cspace>");
								builder.Append("<space=").Append(width).Append(">");
							}
							rubyInfo.SetEndIndex(countCharacter-1);
							RubyInfoList.Add(rubyInfo);
							//通常のルビは改行不可を解除
							builder.Append("</nobr>");
							break;
						case "em":
							rubyInfo = new TextMeshProRubyInfo(tagData.TagArg, countCharacter, true);
							break;
						case "/em":
							//傍点の場合、文字づつ作成
							int begin = rubyInfo.BeginIndex;
							int end = countCharacter - 1;
							string str = rubyInfo.Ruby;
							for (int i = begin; i <= end; i++)
							{
								RubyInfoList.Add(new TextMeshProRubyInfo(str, i, i));
							}
							break;
						default:
							break;
					}

					countCharacter += tagData.CountCharacter;
				}
			}
		}


		//ルビのテキストオブジェクトを作成
		void MakeRubyTextObjects()
		{
			//現在のルビのオブジェクトを全て消去
			foreach (var obj in RubyObjectList)
			{
				if (Application.isPlaying)
				{
					GameObject.Destroy(obj.gameObject);
				}
				else
				{
					GameObject.DestroyImmediate(obj.gameObject);
				}
			}
			RubyObjectList.Clear();
			if (RubyInfoList.Count > 0)
			{
				TMP_TextInfo textInfo = TextMeshPro.ForceGetTextInfo();
				foreach (var ruby in RubyInfoList)
				{
					AddRubyTextObject(textInfo, ruby);
				}
				UpdateVisibleIndex();
			}
			HasChanged = false;
		}


		//表示文字数に応じて、ルビの表示のオン、オフを更新
		protected void UpdateVisibleIndex()
		{
			if (this.RubyObjectList.Count <= 0) return;

			int index = MaxVisibleCharacters - 1;
			foreach (var obj in RubyObjectList)
			{
				obj.SetVisibleIndex(index);
			}
		}

		//ルビを作成して配置する
		void AddRubyTextObject(TMP_TextInfo textInfo, TextMeshProRubyInfo rubyInfo)
		{
			if (string.IsNullOrEmpty(rubyInfo.Ruby)) return;
			if (rubyPrefab == null)
			{
//				Debug.LogError("ルビ用のプレハブがありません");
				return;
			}

			int len = textInfo.characterInfo.Length;
			if (rubyInfo.BeginIndex >= len || rubyInfo.EndIndex >= len)
			{
				Debug.LogError("RubyIndex Error");
				return;
			}

			//プレハブからルビのオブジェクトを作成する
			GameObject go = this.transform.AddChildPrefab(rubyPrefab.gameObject);
			go.hideFlags = HideFlags.DontSave;
			TextMeshProRuby ruby = go.GetComponent<TextMeshProRuby>();
			ruby.Init(textInfo, rubyInfo, GetPivotOffset());
			if (ruby.RubyWidth > ruby.TextWidth)
			{
				int count = (rubyInfo.EndIndex - rubyInfo.BeginIndex +1) +1;
				float w = (ruby.RubyWidth - ruby.TextWidth)/ count + TextMeshPro.characterSpacing;
				rubyInfo.RemakeCspace = w;
			}
			RubyObjectList.Add(ruby);
		}

		Vector2 GetPivotOffset()
		{
			if (TextMeshPro.transform is RectTransform)
			{
				RectTransform rect = TextMeshPro.transform as RectTransform;
				Rect r = rect.rect;
				Vector2 pivot = rect.pivot;
				float x_offset = (pivot.x - 0.5f) * r.width;
				float y_offset = (pivot.y - 0.5f) * r.height;
				return new Vector2(x_offset, y_offset);
			}
			else
			{
				return Vector2.zero;
			}
		}

		//指定テキストに対する表示文字数チェック
		public bool TryCheckCharacterCount(string text, out int count, out string errorString)
		{
			SetText(text);
			count = TextData.CharList.Count;
			errorString = "";
			
			//強制アップデートして文字あふれチェック
			ForceUpdateSubInEditor();
			CheckTextParse();
			if (!TextMeshPro.isTextOverflowing)
			{
				return true;
			}

			//文字溢れがある場合
			int overflowIndex = TextMeshPro.firstOverflowCharacterIndex;
			if (TextMeshPro.textInfo == null)
			{
				errorString =
					$"TextMeshPro.textInfo is null. overflowIndex = {overflowIndex} count = {count}\n{TextMeshPro.text}";
				return false;
			}

			var characterInfo = TextMeshPro.textInfo.characterInfo;
			if (overflowIndex < 0 || characterInfo.Length <= overflowIndex)
			{
				//文字あふれがあるが、インデックスが指定の文字インデクスを超えている
				errorString =
					$"overflowIndex is index over ={overflowIndex}/{characterInfo.Length}  count ={count}\n{TextMeshPro.text}";
				return false;
			}

			errorString = $"overflowIndex={overflowIndex} count ={count}\n";
			// 指定のインデックスで文字列を二つに分割
			string firstPart = "";
			for (var i = 0; i < overflowIndex; i++)
			{
				firstPart += characterInfo[i].character;
			}

			string secondPart = "";
			int max = Mathf.Min(characterInfo.Length, TextMeshPro.textInfo.characterCount);
			for (var i = overflowIndex; i < max; i++)
			{
				secondPart += characterInfo[i].character;
			}

			errorString += StringUtil.EscapeNewlines(firstPart + ColorUtil.ToColorTagErrorMsg("/-- over flow --/") + secondPart);
			return false;
		}
	}
}
