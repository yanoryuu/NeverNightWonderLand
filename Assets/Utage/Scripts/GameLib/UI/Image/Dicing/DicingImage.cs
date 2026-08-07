// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif


namespace Utage
{

	/// <summary>
	/// ダイシング（賽の目状に分割・再結合）処理したイメージ表示
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/DicingImage")]
	[ExecuteInEditMode,RequireComponent(typeof(RectTransform),typeof(CanvasRenderer))]
	public class DicingImage : MaskableGraphic, ICanvasRaycastFilter
	{
		//ダイシングデータ
		public DicingTextures DicingData
		{
			get { return dicingData; }
			set
			{
				dicingData = value;
				pattern = "";
				OnChangePattern();
				this.SetAllDirty();
			}
		}
		[SerializeField]
		DicingTextures dicingData;

		//現在のパターン名
		private string Pattern
		{
			get { return pattern; }
			set
			{
				if (!DicingData.Exists(value))
				{
					Debug.LogError(value + " is not find in " + DicingData.name);
					return;
				}
				pattern = value;
				OnChangePattern();
				this.SetAllDirty();
			}
		}

		[SerializeField,StringPopupFunction("GetPattenNameList")]
		string pattern;

		//現在のパターン名
		public string MainPattern { get; private set; }

		Dictionary<string, string> patternOption = new Dictionary<string, string>();

		//現在のパターン名
		public void ChangePattern(string pattern)
		{
			this.MainPattern = pattern;
			this.patternOption.Clear();
			this.Pattern = pattern;
		}

		//現在のパターンをオプション付きで変更(主に目パチや口パクに使う)
		public bool TryChangePatternWithOption(string mainPattern, string optionTag, string option)
		{
			this.MainPattern = mainPattern;
			this.patternOption[optionTag] = option;
			string pattern = MakePatternWithOption();
			if (DicingData.Exists(pattern))
			{
				this.Pattern = pattern;
				return true;
			}
			else if (DicingData.Exists(option))
			{
				this.Pattern = option;
				return true;
			}
			else
			{
				this.Pattern = MainPattern;
				return false;
			}
		}

		//オプション付きのパターン名を取得
		public string MakePatternWithOption()
		{
			string pattern = this.MainPattern;
			SortedDictionary<string, string> sorted = new SortedDictionary<string, string>(this.patternOption);
			foreach (KeyValuePair<string, string> keyValue in sorted)
			{
				pattern += keyValue.Value;
			}
			return pattern;
		}

		//パターンの名前リストを取得
		List<string> GetPattenNameList()
		{
			if(dicingData==null) return null;

			return dicingData.GetPattenNameList();
		}

		//現在のパターンデータ
		public DicingTextureData PatternData { get { return patternData; } }
		DicingTextureData patternData = null;

		
		//透明なセルの描画をスキップするか（ポリゴンが四角形にならない）
		public bool SkipTransParentCell { get { return skipTransParentCell; } set { skipTransParentCell = value; } }
		[SerializeField]
		bool skipTransParentCell = true;

		public Rect UvRect
		{
			get
			{
				return uvRect;
			}
			set
			{
				uvRect = value;
				this.SetAllDirty();
			}
		}
		[SerializeField]
		Rect uvRect = new Rect(0,0,1,1);

		//テクスチャ―（これはアトラス画像になる）
		public override Texture mainTexture
		{
			get
			{
				if (m_Texture == null)
				{
					if (material != null && material.mainTexture != null)
					{
						return material.mainTexture;
					}
					return s_WhiteTexture;
				}

				return m_Texture;
			}
		}
		Texture m_Texture;

		//パターンチェンジ（通常で言うテクスチャ差し替え）
		void OnChangePattern()
		{
			if (DicingData == null || string.IsNullOrEmpty(pattern))
			{
				m_Texture = s_WhiteTexture;
				return;
			}

			this.patternData = DicingData.GetTextureData(Pattern);
			if (patternData == null)
			{
				Debug.LogError(Pattern + " is not find in " + DicingData.name);
				return;
			}

			this.m_Texture = DicingData.GetTexture(patternData.AtlasName);
		}

		//本来のサイズに合わせる
		public override void SetNativeSize()
		{
			if (PatternData == null) return;

			rectTransform.anchorMax = rectTransform.anchorMin;
			rectTransform.sizeDelta = GetNaitiveSize();
		}

		internal List<DicingTextureData.QuadVerts> GetVerts(DicingTextureData patternData)
		{
			return DicingData.GetVerts(patternData);
		}


		Vector2 GetNaitiveSize()
		{
			return new Vector2(uvRect.width* PatternData.Width, uvRect.height* PatternData.Height);
		}

		//描画ポリゴンつくる
		protected override void OnPopulateMesh(VertexHelper vh)
		{
			OnChangePattern();
			if (PatternData == null) return;
			var color32 = color;
			vh.Clear();

			int index = 0;
			this.ForeachVertexList(
				(r, uv) =>
				{
					vh.AddVert(new Vector3(r.xMin, r.yMin), color32, new Vector2(uv.xMin, uv.yMin));
					vh.AddVert(new Vector3(r.xMin, r.yMax), color32, new Vector2(uv.xMin, uv.yMax));
					vh.AddVert(new Vector3(r.xMax, r.yMax), color32, new Vector2(uv.xMax, uv.yMax));
					vh.AddVert(new Vector3(r.xMax, r.yMin), color32, new Vector2(uv.xMax, uv.yMin));

					vh.AddTriangle(index + 0, index + 1, index + 2);
					vh.AddTriangle(index + 2, index + 3, index + 0);
					index += 4;
				});
		}



		//描画頂点データに対してForeachする（途中でberakした場合はfalseが帰る）
		protected void ForeachVertexList(Action<Rect, Rect> function)
		{
			//描画領域
			Rect r = GetPixelAdjustedRect();
			PatternData.ForeachVertexList(r, this.uvRect, this.skipTransParentCell, this.DicingData, function);
		}

		//ヒットテスト
		public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
		{
			Vector2 localPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectTransform, sp, eventCamera, out localPosition);
			return HitTest(localPosition);
		}


		public bool HitTest(Vector2 localPosition)
		{
			if (!GetPixelAdjustedRect().Contains(localPosition)) return false;
			if (PatternData == null) return false;

			bool isHit = false;
			this.ForeachVertexList(
				(r, uv) =>
				{
					isHit |= r.Contains(localPosition);
				});
			return isHit;
		}
#if UNITY_EDITOR
		
		//インスペクターの表示拡張
		[CustomEditor(typeof(DicingImage))]
		public class UguiNovelImageDicingInspector : Editor
		{
			protected SerializedProperty m_Script;
			protected SerializedProperty m_Color;
			protected SerializedProperty m_Material;
			protected SerializedProperty m_RaycastTarget;

			private GUIContent m_CorrectButtonContent;

			protected SerializedProperty dicingData;
			protected SerializedProperty pattern;
			protected SerializedProperty skipTransParentCell;
			protected SerializedProperty uvRect;


			protected void OnEnable()
			{
				m_Script = serializedObject.FindProperty("m_Script");
				m_Color = serializedObject.FindProperty("m_Color");
				m_Material = serializedObject.FindProperty("m_Material");
				m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");

				m_CorrectButtonContent = new GUIContent("Set Native Size", "Sets the size to match the content.");

				dicingData = serializedObject.FindProperty("dicingData");
				pattern = serializedObject.FindProperty("pattern");
				skipTransParentCell = serializedObject.FindProperty("skipTransParentCell");
				uvRect = serializedObject.FindProperty("uvRect");
			}

			//インスペクター描画
			public override void OnInspectorGUI()
			{
				serializedObject.Update();

				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.PropertyField(m_Script);
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.PropertyField(m_Color);
				EditorGUILayout.PropertyField(m_Material);
				EditorGUILayout.PropertyField(m_RaycastTarget);

				EditorGUILayout.PropertyField(dicingData);
				EditorGUILayout.PropertyField(pattern);
				EditorGUILayout.PropertyField(skipTransParentCell);
				EditorGUILayout.PropertyField(uvRect, new GUIContent("UV Rectangle") );

				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.Space(EditorGUIUtility.labelWidth);
					if (GUILayout.Button(m_CorrectButtonContent, EditorStyles.miniButton))
					{
						foreach (Graphic graphic in targets.Select(obj => obj as Graphic))
						{
							Undo.RecordObject(graphic.rectTransform, "Set Native Size");
							graphic.SetNativeSize();
							EditorUtility.SetDirty(graphic);
						}
					}
				}
				EditorGUILayout.EndHorizontal();

				serializedObject.ApplyModifiedProperties();
			}


			//プレビュー表示する場合true
			public override bool HasPreviewGUI()
			{
				return true;
			}

			public override void OnPreviewGUI(Rect r, GUIStyle background)
			{
				DicingImage obj = this.target as DicingImage;
				if (obj == null) return;
				if (obj.DicingData == null) return;

				DicingTextureData patternData = obj.PatternData;
				if (patternData == null) return;

				OnPreviewGUISub(r, patternData);
			}

			void OnPreviewGUISub(Rect r, DicingTextureData patternData)
			{
				DicingImage obj = this.target as DicingImage;
				obj.DicingData.OnPreviewGUI(patternData, r);
			}
		}
#endif
	}
}

