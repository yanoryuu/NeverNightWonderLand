// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Pool;

namespace Utage
{

	/// <summary>
	/// ノベル用に、禁則処理などを含めたテキスト表示
	/// </summary>
	[RequireComponent(typeof(UguiNovelTextGenerator))]
	[AddComponentMenu("Utage/Lib/UI/UguiNovelText")]
	public class UguiNovelText : Text
		, INovelText
	{
		public int LengthOfView
		{
			get { return TextGenerator.LengthOfView; }
			set { TextGenerator.LengthOfView = value; }
		}

		public UguiNovelTextGenerator TextGenerator { get { return this.GetComponentCache(ref textGenerator); } }
		UguiNovelTextGenerator textGenerator;

		//文字送りをしない場合の文字の最後の座標
		public Vector3 EndPosition { get { return TextGenerator.EndPosition; } }

		//文字送りをする場合の文字の最後の座標
		public Vector3 CurrentEndPosition { get { TextGenerator.RefreshEndPosition(); return TextGenerator.EndPosition; } }


        //頂点情報を作成
        readonly UIVertex[] m_TempVerts = new UIVertex[4];
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (font == null)
                return;
/*
            if (TextGenerator.IsRequestingCharactersInTexture)
            {
                return;
            }

            //フォントの再作成によるものであればその旨を通知
            if (!isDirtyVerts)
            {
                TextGenerator.IsRebuidFont = true;
            }
*/
			var verts = ListPool<UIVertex>.Get();
			TextGenerator.CreateVertex(verts);
            vh.Clear();
            for (int i = 0; i < verts.Count; ++i)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = verts[i];
                if (tempVertsIndex == 3)
                    vh.AddUIVertexQuad(m_TempVerts);
            }
			ListPool<UIVertex>.Release(verts);
        }

        protected override void Awake()
		{
			base.Awake();
			m_OnDirtyVertsCallback += OnDirtyVerts;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Font.textureRebuilt += OnTextureRebuild;
		}

		protected override void OnDisable()
		{
			Font.textureRebuilt -= OnTextureRebuild;
			TextGenerator.ChangeAll();
			base.OnDisable();
		}

		void OnTextureRebuild(Font font)
		{
			if (this==null) return;
			if (!this.IsActive()) return;

			//フォント作成
			TextGenerator.OnTextureRebuild(font);
			if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
			{
				//キャンバスのリビルド中はこっち
				base.UpdateGeometry();
			}
			else
			{
				//通常はこっち
				this.SetVerticesDirty();
			}
		}

		public override void SetAllDirty()
		{
			TextGenerator.ChangeAll();
			base.SetAllDirty();
		}

		internal void RemakeVerts()
		{
			TextGenerator.RemakeVerts();
			base.SetVerticesDirty();
		}

		//頂点情報だけ書き換え（文字数などのみを変更するために）
		internal void SetVerticesOnlyDirty()
		{
			TextGenerator.ChangeVertsOnly();
			base.SetVerticesDirty();
		}

		public override void SetVerticesDirty()
		{
			TextGenerator.ChangeAll();
			base.SetVerticesDirty();
		}

		//頂点変更時に呼ばれる
		void OnDirtyVerts()
		{
			TextGenerator.OnDirtyVerts();
		}

		//行間を含んだ高さを取得
		public int GetTotalLineHeight( int fontSize )
		{
			//uGUIは行間の基本値1=1.2の模様
			return Mathf.CeilToInt(fontSize * (lineSpacing + 0.2f));
		}

		public override float preferredHeight
		{
			get
			{
				return TextGenerator.PreferredHeight;
			}
		}

		public override float preferredWidth
		{
			get
			{
				return TextGenerator.PreferredWidth;
			}
		}
	}
}

