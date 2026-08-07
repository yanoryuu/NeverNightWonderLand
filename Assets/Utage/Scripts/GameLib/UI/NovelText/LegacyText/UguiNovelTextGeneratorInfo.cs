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
	/// ノベル用の禁則処理などを含めたテキスト表示をする際の、情報の制御
	/// </summary>
	internal class UguiNovelTextGeneratorInfo
	{
		bool isDebugLog = false;

		UguiNovelTextGenerator Generator { get; set; }
		UguiNovelText NovelText { get { return Generator.NovelText; } }

		internal TextData TextData { get; set; }

		//文字のデータ
		List<UguiNovelTextCharacter> CharacterDataList { get; set; }

		//行のデータ
		internal List<UguiNovelTextLine> LineDataList { get; private set; }

		//最後の文字の座標（右下頂点座標）
		internal Vector3 EndPosition { get; private set; }

		//ルビやアンダーラインなどの付加的な描画情報
		internal UguiNovelTextGeneratorAdditional Additional { get; private set; }

		//絵文字などのグラフィックオブジェクト
		class GraphicObject
		{
			public UguiNovelTextCharacter character;
			public RectTransform graphic;

			public GraphicObject(UguiNovelTextCharacter character, RectTransform graphic)
			{
				this.character = character;
				this.graphic = graphic;
			}
		};

		List<GraphicObject> graphicObjectList = null;
		bool isInitGraphicObjectList = false;

		/// 表示の参照となる高さ
		internal float PreferredHeight { get; private set; }

		/// 表示の参照となる幅
		internal float PreferredWidth { get; private set; }

		// テキスト表示の最大幅（0以下は無限）
		public float MaxWidth { get; private set; }

		// テキスト表示の最大高さ（0以下は無限）
		public float MaxHeight { get; private set; }

		/// 実際に表示される高さ
		internal float Height { get; private set; }

		/// 実際に表示される幅
		internal float Width { get; private set; }

		//当たり判定
		internal List<UguiNovelTextHitArea> HitGroupLists { get; private set; }

		//フォントの情報を作成するビルダー
		UguiNovelTextFontInfoBuilder FontInfoBuilder { get; set; }

		internal UguiNovelTextGeneratorInfo(UguiNovelTextGenerator generator)
		{
			this.Generator = generator;
			this.CharacterDataList = new List<UguiNovelTextCharacter>();
			this.LineDataList = new List<UguiNovelTextLine>();
			this.HitGroupLists = new List<UguiNovelTextHitArea>();
			this.FontInfoBuilder = new UguiNovelTextFontInfoBuilder();
		}

		//各文字の情報を作成
		//フォントのテクスチャ情報から文字の大きさなどを取得し、各文字の基本情報を初期化する
		internal void BuildCharcteres()
		{
			//禁則処理等、すべての情報を再度作成
			Profiler.BeginSample("BuildCharcteres");

			//TextData作成
			this.TextData = new TextData(NovelText.text);
			if (isDebugLog) Debug.Log(this.TextData.ParsedText.OriginalText);

			//文字データを作成
			this.CharacterDataList = CreateCharacterDataList(this.TextData);
			//拡張的な情報を作成
			this.Additional = new UguiNovelTextGeneratorAdditional(this.CharacterDataList, this.Generator);
			//フォントの文字画像を準備・設定
			FontInfoBuilder.InitFontCharactes(NovelText.font, this.CharacterDataList, this.Additional);
			//拡張的な情報の初期化
			Additional.InitAfterCharacterInfo(this.Generator);

			//描画範囲のサイズに合わせて自動改行
			this.PreferredWidth = CalcPreferredWidth(this.CharacterDataList);

			ClearGraphicObjectList();
			Profiler.EndSample();
		}

		//テキストエリアの情報を作成（実際の表示位置や自動改行処理）
		internal void BuildTextArea(RectTransform rectTransform)
		{
			//禁則処理等、すべての情報を再度作成
			Profiler.BeginSample("BuildTextArea");

			//描画範囲のサイズに合わせて自動改行
			Rect rect = rectTransform.rect;
			float maxW = Mathf.Abs(rect.width);
			float maxH = Mathf.Abs(rect.height);

			//文字のX座標を計算（自動改行処理も行う）
			ApplyXPosition(this.CharacterDataList, maxW);
			//行ごとの文字データを作成
			this.LineDataList = CreateLineList(this.CharacterDataList, maxH);

			//今の描画範囲を更新
			this.MaxWidth = maxW;
			this.MaxHeight = maxH;

			//テキストのアンカーを適用する
			ApplyTextAnchor(this.LineDataList, NovelText.alignment, MaxWidth, MaxHeight);
			//Offsetを適用する
			ApplyOffset(this.LineDataList, MaxWidth, MaxHeight, rectTransform.pivot);
			//拡張的な情報の表示位置を初期化
			Additional.InitPosition(Generator);
			//当たり判定の情報を作成
			MakeHitGroups(this.CharacterDataList);

			MakeVerts(this.LineDataList);
			Profiler.EndSample();
		}

		//フォントのテクスチャだけ再作成（つまり、文字情報のうちUV座標だけ変更）
		internal void RebuildFontTexture(Font font)
		{
			if (this.TextData == null) return;

			//フォントの文字画像を準備・設定
			FontInfoBuilder.InitFontCharactes(NovelText.font, this.CharacterDataList, this.Additional);
			MakeVerts(this.LineDataList);
		}

		//頂点情報だけ再度作成
		internal void RemakeVerts()
		{
			if (this.TextData == null) return;

			MakeVerts(this.LineDataList);
		}

		//描画頂点情報を作成
		internal void CreateVertex(List<UIVertex> verts)
		{
			if (this.TextData == null) return;

			//描画用頂点データリストを作成・文字の表示長さを変更
			Profiler.BeginSample("CreateVertex");
			CreateVertexList(verts, this.LineDataList, Generator.CurrentLengthOfView);
			RefreshHitArea();
			Profiler.EndSample();
		}


		//文字データを作成
		List<UguiNovelTextCharacter> CreateCharacterDataList(TextData data)
		{
			List<UguiNovelTextCharacter> characterDataList = new List<UguiNovelTextCharacter>();
			if (data == null) return characterDataList;

			for (int i = 0; i < data.Length; i++)
			{
				UguiNovelTextCharacter character = new UguiNovelTextCharacter(data.CharList[i], Generator);
				characterDataList.Add(character);
			}
			return characterDataList;
		}

		//文字のX座標を計算（自動改行処理も行う）
		void ApplyXPosition(List<UguiNovelTextCharacter> characterDataList, float maxWidth)
		{
			ClacXPosition(characterDataList, true, true, maxWidth);
		}
		//自動改行なしでの幅を求める
		float CalcPreferredWidth(List<UguiNovelTextCharacter> characterDataList)
		{
			return ClacXPosition(characterDataList, false, false, float.MaxValue);
		}

		//文字のX座標を計算（自動改行処理も行う）
		float ClacXPosition(List<UguiNovelTextCharacter> characterDataList, bool autoLineBreak, bool applyX, float maxWidth)
		{
			float maxX = 0;
			float indentSize = 0;
			int index = 0;

			foreach (var item in characterDataList)
			{
				item.ResetXPosition();
			}

			//infoListがテキストの文字数ぶんになるまでループ
			while (index < characterDataList.Count)
			{
				//行の開始インデックス
				int beginIndex = index;
				float currentLetterSpace = 0;   //文字間のサイズ
				float x = 0;    //現在のX位置
								//一行ぶん（改行までの）の処理をループ内で処理
				while (index < characterDataList.Count)
				{
					UguiNovelTextCharacter currentData = characterDataList[index];
					bool isAutoLineBreak = false;   //自動改行をするか

					//行の先頭で、先頭の文字スペースが必要場合があるので加算する
					if (x == 0)
					{
						currentLetterSpace = Additional.GetTopLetterSpace(currentData, Generator);
						x += indentSize;
						if (index == 0 && IsAutoIndentation(currentData.Char))
						{
							indentSize = currentData.Width + Generator.LetterSpaceSize;
						}
					}

					//文字間の適用
					if (currentData.CustomInfo.IsRuby) currentLetterSpace += currentData.RubySpaceSize;
					x += currentLetterSpace;

					if (currentData.IsBlank)
					{
						//改行文字かスペース
					}
					else if (autoLineBreak)
					{
						//いったん改行データをクリア
						currentData.isAutoLineBreak = false;
						//横幅を越えるなら自動改行
						isAutoLineBreak = IsOverMaxWidth(x, Additional.GetMaxWidth(currentData), maxWidth);
						if (isAutoLineBreak)
						{
							//自動改行処理
							//改行すべき文字の位置まで戻る
							index = GetAutoLineBreakIndex(characterDataList, beginIndex, index);
							currentData = characterDataList[index];
							currentData.isAutoLineBreak = true;
						}
					}
					//1文字進める
					++index;

					bool br = (autoLineBreak && currentData.IsBrOrAutoBr) || currentData.IsBr;
					//改行処理
					if (br)
					{
						//改行なので行処理のループ終了
						break;
					}
					else
					{
						if (applyX)
						{
							currentData.InitPositionX(x);
						}
						//X位置を進める
						x += currentData.Width;
						if (currentData.RubySpaceSize != 0)
						{
							currentLetterSpace = currentData.RubySpaceSize;
						}
						else
						{
							currentLetterSpace = Generator.LetterSpaceSize;

							//文字間を無視する場合のチェック
							if (Generator.TextSettings)
							{
								if (index < characterDataList.Count)
								{
									if (Generator.TextSettings.IsIgonreLetterSpace(currentData, characterDataList[index]))
									{
										currentLetterSpace = 0;
									}
								}
							}
						}
					}
				}
				maxX = Mathf.Max(x, maxX);
			}
			return maxX;
		}


		//行データを作成する
		List<UguiNovelTextLine> CreateLineList(List<UguiNovelTextCharacter> characterDataList, float maxHeight)
		{
			//行データの作成＆Y位置の調整
			List<UguiNovelTextLine> lineList = new List<UguiNovelTextLine>();

			//行データを作成
			UguiNovelTextLine currentLine = new UguiNovelTextLine();
			foreach (UguiNovelTextCharacter character in characterDataList)
			{
				currentLine.AddCharaData(character);
				//改行処理
				if (character.IsBrOrAutoBr)
				{
					currentLine.EndCharaData(Generator);
					lineList.Add(currentLine);
					//次の行を追加
					currentLine = new UguiNovelTextLine();
				}
			}
			if (currentLine.Characters.Count > 0)
			{
				currentLine.EndCharaData(Generator);
				lineList.Add(currentLine);
			}

			if (lineList.Count <= 0) return lineList;

			float y = 0;
			//行間
			for (int i = 0; i < lineList.Count; ++i)
			{
				UguiNovelTextLine line = lineList[i];
				float y0 = y;
				y -= line.MaxFontSize;
				//縦幅のチェック
				line.IsOver = IsOverMaxHeight(-y, maxHeight);
				//表示する幅を取得
				if (!line.IsOver)
				{
					this.Height = -y;
				}
				this.PreferredHeight = -y;
				//Y座標を設定
				line.SetLineY(y, Generator);
				//行間を更新
				y = y0 - line.TotalHeight;
			}
			return lineList;
		}

		//テキストのアンカーを適用する
		void ApplyTextAnchor(List<UguiNovelTextLine> lineList, TextAnchor anchor, float maxWidth, float maxHeight)
		{
			Vector2 pivot = Text.GetTextAnchorPivot(anchor);
			foreach (UguiNovelTextLine line in lineList)
			{
				line.ApplyTextAnchorX(pivot.x, maxWidth);
			}

			if (pivot.y == 1.0f) return;

			float offsetY = (maxHeight - Height) * (pivot.y - 1.0f);
			foreach (UguiNovelTextLine line in lineList)
			{
				line.ApplyTextAnchorY(offsetY);
			}
		}

		//Offsetを適用する
		void ApplyOffset(List<UguiNovelTextLine> lineList, float maxWidth, float maxHeight, Vector2 pivot)
		{
			Vector2 offset = new Vector2(-pivot.x * maxWidth, (1.0f - pivot.y) * maxHeight);
			foreach (UguiNovelTextLine line in lineList)
			{
				line.ApplyOffset(offset);
			}
			if (isDebugLog) Debug.LogFormat("PosX={0}", this.LineDataList[0].Characters[0].PositionX);
		}

		//当たり判定の情報を作成
		void MakeHitGroups(List<UguiNovelTextCharacter> characterDataList)
		{
			this.HitGroupLists = new List<UguiNovelTextHitArea>();
			int index = 0;
			//一行ぶん（改行までの）の処理をループ内で処理
			while (index < characterDataList.Count)
			{
				UguiNovelTextCharacter currentData = characterDataList[index];
				if (currentData.charData.CustomInfo.IsHitEventTop)
				{
					CharData.HitEventType type = currentData.CustomInfo.HitEventType;
					string arg = currentData.CustomInfo.HitEventArg;
					List<UguiNovelTextCharacter> characterList = new List<UguiNovelTextCharacter>();
					characterList.Add(currentData);
					++index;
					while (index < characterDataList.Count)
					{
						UguiNovelTextCharacter c = characterDataList[index];
						if (!c.CustomInfo.IsHitEvent || c.CustomInfo.IsHitEventTop) break;
						characterList.Add(c);
						++index;
					}
					HitGroupLists.Add(new UguiNovelTextHitArea(NovelText, type, arg, characterList));
				}
				else
				{
					++index;
				}
			}
		}

		//当たり判定の情報を更新
		void RefreshHitArea()
		{
			foreach (var item in HitGroupLists)
			{
				item.RefreshHitAreaList();
			}
		}


		//各頂点データを構築
		void MakeVerts(List<UguiNovelTextLine> lineList)
		{
			Color color = NovelText.color;
			foreach (UguiNovelTextLine line in lineList)
			{
				foreach (UguiNovelTextCharacter character in line.Characters)
				{
					character.MakeVerts(color, Generator);
				}
			}
			Additional.MakeVerts(color, Generator);
		}

		//描画用頂点データリストを作成
		void CreateVertexList(List<UIVertex> verts, List<UguiNovelTextLine> lineList, int max)
		{
			if (lineList == null || (max <= 0 && lineList.Count <= 0))
			{
				return;
			}

			int count = 0;
			UguiNovelTextCharacter lastCaracter = null;
			foreach (UguiNovelTextLine line in lineList)
			{
				if (line.IsOver) break;

				for (int i = 0; i < line.Characters.Count; ++i)
				{
					UguiNovelTextCharacter character = line.Characters[i];
					character.IsVisible = (count < max);
					++count;
					if (character.IsBr) continue;
					if (character.IsVisible)
					{
						lastCaracter = character;
						this.EndPosition = new Vector3(lastCaracter.EndPositionX, line.Y0, 0);
						if (!character.IsNoFontData)
						{
							verts.AddRange(character.Verts);
						}
					}
				}
			}

			Additional.AddVerts(verts, this.EndPosition, Generator);
		}

		//最後の座標を計算
		internal void RefreshEndPosition()
		{
			int max = Generator.CurrentLengthOfView;
			if (LineDataList == null || (max <= 0 && LineDataList.Count <= 0))
			{
				return;
			}

			int count = 0;
			UguiNovelTextCharacter lastCaracter = null;
			foreach (UguiNovelTextLine line in LineDataList)
			{
				if (line.IsOver) break;

				for (int i = 0; i < line.Characters.Count; ++i)
				{
					UguiNovelTextCharacter character = line.Characters[i];
					character.IsVisible = (count < max);
					++count;
					if (character.IsBr) continue;
					if (character.IsVisible)
					{
						lastCaracter = character;
						this.EndPosition = new Vector3(lastCaracter.EndPositionX, line.Y0, 0);
					}
				}
			}
		}


		//絵文字などのグラフィックオブジェクトを全て削除
		void ClearGraphicObjectList()
		{
			if (graphicObjectList != null)
			{
				foreach (GraphicObject graphic in graphicObjectList)
				{
					if (Application.isPlaying)
					{
						GameObject.Destroy(graphic.graphic.gameObject);
					}
					else
					{
						GameObject.DestroyImmediate(graphic.graphic.gameObject);
					}
				}
				graphicObjectList.Clear();
				graphicObjectList = null;
				isInitGraphicObjectList = false;
			}
		}

		//絵文字など子オブジェクトとして表示するグラフィックを作成
		internal void UpdateGraphicObjectList(RectTransform parent)
		{
			//絵文字など子オブジェクトとして表示するグラフィックを作成
			if (!isInitGraphicObjectList)
			{
				ClearGraphicObjectList();
				graphicObjectList = new List<GraphicObject>();

				foreach (UguiNovelTextLine line in this.LineDataList)
				{
					foreach (UguiNovelTextCharacter character in line.Characters)
					{
						RectTransform graphicObjecct = character.AddGraphicObject(parent, Generator);
						if (graphicObjecct)
						{
							graphicObjectList.Add(new GraphicObject(character, graphicObjecct));
						}
					}
				}
				isInitGraphicObjectList = true;
			}

			foreach (GraphicObject graphicObject in graphicObjectList)
			{
				graphicObject.graphic.gameObject.SetActive(graphicObject.character.IsVisible);
			}
		}

		//以下、自動改行に必要な細かい処理

		//自動改行
		//禁則などで送り出しされる文字がある場合は、適切な改行の文字インデックスを返す
		int GetAutoLineBreakIndex(List<UguiNovelTextCharacter> characterList, int beginIndex, int index)
		{
			if (index <= beginIndex) return index;

			UguiNovelTextCharacter current = characterList[index];  //はみ出た文字
			UguiNovelTextCharacter prev = characterList[index - 1]; //一つ前の文字（改行文字候補）

			if (prev.IsBrOrAutoBr)
			{
				//前の文字が改行の場合、そのまま現在の文字を改行文字にする
				return index;
			}
			else if (CheckWordWrap(current, prev))
			{
				//禁則処理
				//改行可能な位置まで文字インデックスを戻す
				int i = ParseWordWrap(characterList, beginIndex, index - 1);
				if (i != beginIndex)
				{
					return i;
				}
				else
				{
					//前の文字を自動改行
					return --index;
				}
			}
			else
			{
				//前の文字を自動改行
				return --index;
			}
		}


		//WordWrap処理
		int ParseWordWrap(List<UguiNovelTextCharacter> infoList, int beginIndex, int index)
		{
			if (index <= beginIndex) return beginIndex;

			UguiNovelTextCharacter current = infoList[index];   //改行させる文字
			UguiNovelTextCharacter prev = infoList[index - 1];  //一つ前の文字

			if (CheckWordWrap(current, prev))
			{   //禁則に引っかかるので、一文字前をチェック
				return ParseWordWrap(infoList, beginIndex, index - 1);
			}
			else
			{
				return index - 1;
			}
		}

		//禁則のチェック
		bool CheckWordWrap(UguiNovelTextCharacter current, UguiNovelTextCharacter prev)
		{
			//ルビは開始の文字以外は改行できない
			if (current.IsDisableAutoLineBreak)
			{
				return true;
			}

			if (Generator.TextSettings != null)
			{
				return Generator.TextSettings.CheckWordWrap(Generator, current, prev);
			}
			else
			{
				return false;
			}
		}

		//最大横幅のチェック
		bool IsOverMaxWidth(float x, float width, float maxWidth)
		{
			return (x > 0) && (x + width) > maxWidth;
		}

		//最大縦幅のチェック
		bool IsOverMaxHeight(float height, float maxHeight)
		{
			return height > maxHeight;
		}

		bool IsAutoIndentation(char character)
		{
			if (Generator.TextSettings != null)
			{
				return Generator.TextSettings.IsAutoIndentation(character);
			}
			else
			{
				return false;
			}
		}
	}
}


