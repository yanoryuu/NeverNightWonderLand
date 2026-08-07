// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	
	[System.Serializable]
	public class SelectionEvent : UnityEvent<AdvSelectionManager>{}

	/// <summary>
	/// 選択肢管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvSelectionManager")]
	public class AdvSelectionManager : MonoBehaviour, IAdvSaveData
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine { get { return this.GetComponentCache(ref engine ); } }
		AdvEngine engine;

		/// <summary>
		/// 選択肢データ
		/// </summary>
		public List<AdvSelection> Selections { get { return selections; } }
		List<AdvSelection> selections = new List<AdvSelection>();

		/// <summary>
		/// スプライト選択肢データ
		/// </summary>
		public List<AdvSelection> SpriteSelections { get { return spriteSelections; } }
		List<AdvSelection> spriteSelections = new List<AdvSelection>();

		/// <summary>
		/// 選択されたか
		/// </summary>
		public bool IsSelected { get { return selected != null; } }

		/// <summary>
		/// 表示中か
		/// </summary>
		public bool IsShowing { get; set; }

		//クリア
		public SelectionEvent OnClear { get { return onClear; } }
		[SerializeField]
		SelectionEvent onClear = new SelectionEvent();
		
		//表示開始
		public SelectionEvent OnBeginShow { get { return onBeginShow; } }
		[SerializeField]
		SelectionEvent onBeginShow = new SelectionEvent();

		//選択入力待ち開始
		public SelectionEvent OnBeginWaitInput { get { return onBeginWaitInput; } }
		[SerializeField]
		SelectionEvent onBeginWaitInput = new SelectionEvent();

		//選択入力待ち
		public SelectionEvent OnUpdateWaitInput { get { return onUpdateWaitInput; } }
		[SerializeField]
		SelectionEvent onUpdateWaitInput = new SelectionEvent();

		//選択された
		public SelectionEvent OnSelected { get { return onSelected; } }
		[SerializeField]
		SelectionEvent onSelected = new SelectionEvent();

		/// <summary>
		/// 選択されたデータ
		/// </summary>
		public AdvSelection Selected
		{
			get { return selected; }
			set { selected = value; }
		}
		AdvSelection selected = null;


		/// <summary>
		/// 選択肢追加
		/// </summary>
		/// <param name="label">ジャンプ先のラベル</param>
		/// <param name="text">表示テキスト</param>
		/// <param name="exp">選択時に実行する演算式</param>
		public void AddSelection(string label, string text, ExpressionParser exp, string prefabName, float? x, float? y, StringGridRow row)
		{
			selections.Add(new AdvSelection(label, text, exp, prefabName, x, y, row));
		}

		/// <summary>
		/// 選択肢追加
		/// </summary>
		/// <param name="label">ジャンプ先のラベル</param>
		/// <param name="name">クリックを有効にするオブジェクト名</param>
		/// <param name="isPolygon">ポリゴンコライダーを使うか</param>
		/// <param name="exp">選択時に実行する演算式</param>
		public void AddSelectionClick(string label, string name, bool isPolygon, ExpressionParser exp, StringGridRow row)
		{
			AdvSelection select = new AdvSelection(label, name, isPolygon, exp, row);
			spriteSelections.Add(select);
			AddSpriteClickEvent(select);
		}

		void AddSpriteClickEvent(AdvSelection select)
		{
			Engine.GraphicManager.AddClickEvent(select.SpriteName, select.IsPolygon, select.RowData, (eventData) => OnSpriteClick(eventData, select));
		}

		void OnSpriteClick(BaseEventData eventData, AdvSelection select)
		{
			PointerEventData data = eventData as PointerEventData;
			//左クリックのみに反応
			if(data !=null && data.button != PointerEventData.InputButton.Left) return;
			Select(select);
		}


		/// <summary>
		/// 選択の表示
		/// </summary>
		public void Show()
		{
			selected = null;
			IsShowing = true;
			OnBeginShow.Invoke(this);
		}

		/// <summary>
		/// 選択の入力待ち
		/// </summary>
		public bool IsWaitInput { get; set; }

		//選択肢表示中なら入力待ち開始
		internal bool TryStartWaitInputIfShowing()
		{
			if (selections.Count > 0 || spriteSelections.Count > 0)
			{
				IsWaitInput = true;
				OnBeginWaitInput.Invoke(this);
				return true;
			}
			else
			{
				return false;
			}
		}

		void Update()
		{
			if (IsWaitInput)
			{
				this.OnUpdateWaitInput.Invoke(this);
			}
		}

		/// <summary>
		/// 選択
		/// </summary>
		/// <param name="index">選択肢のインデックス</param>
		public virtual void Select(int index)
		{
			Select(selections[index]);
		}

		/// <summary>
		/// 選択
		/// </summary>
		/// <param name="selected">選んだ選択肢</param>
		public virtual void Select(AdvSelection selected)
		{
			this.selected = selected;

			string label = selected.JumpLabel;
			if (selected.Exp != null)
			{
				Engine.Param.CalcExpression(selected.Exp);
			}
			this.OnSelected.Invoke(this);
			Engine.SystemSaveData.SelectionData.AddData(selected);
			Clear();
			Engine.ScenarioPlayer.MainThread.JumpManager.RegistoreLabel(label);
		}

		/// <summary>
		/// 選択肢データをクリア
		/// </summary>
		public void Clear()
		{
			ClearSub();
			OnClear.Invoke(this);
		}

		void ClearSub()
		{
			selected = null;
			selections.Clear();
			foreach (AdvSelection selection in spriteSelections)
			{
				Engine.GraphicManager.RemoveClickEvent(selection.SpriteName);
			}
			spriteSelections.Clear();
			IsShowing = false;
			IsWaitInput = false;
		}

		//全ての選択肢の総数を取得
		public int TotalCount
		{
			get
			{
				return Selections.Count + SpriteSelections.Count;
			}
		}

		//全ての選択肢の総数からのインデックスで選択する
		public void SelectWithTotalIndex(int index)
		{
			if (index < Selections.Count)
			{
				Select(index);
			}
			else
			{
				index -= Selections.Count;
				Select(SpriteSelections[index]);
			}
		}

		//クリアする
		void IAdvSaveData.OnClear()
		{
			Clear();
		}

		public string SaveKey { get { return "AdvSelectionManager"; } }

		const int VERSION = 1;
		const int VERSION_0 = 0;
		//バイナリ書き込み
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(VERSION);
			writer.Write(Selections.Count);
			foreach (var selection in Selections)
			{
				selection.Write(writer);
			}
			writer.Write(SpriteSelections.Count);
			foreach (var selection in SpriteSelections)
			{
				selection.Write(writer);
			}
		}
		//バイナリ読み込み
		public void OnRead(BinaryReader reader)
		{
			this.ClearSub();
			int version = reader.ReadInt32();
			if (version == VERSION)
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					AdvSelection selection = new AdvSelection(reader, engine);
					selections.Add(selection);
				}
				count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					AdvSelection selection = new AdvSelection(reader, engine);
					spriteSelections.Add(selection);
					AddSpriteClickEvent(selection);
				}
			}
			else if (version == VERSION_0)
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					AdvSelection selection = new AdvSelection(reader, engine);
					selections.Add(selection);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}
