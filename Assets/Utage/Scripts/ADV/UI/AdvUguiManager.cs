// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;


namespace Utage
{
	/// <summary>
	/// UI全般の入力処理。
	/// 独自のキーボード入力などが必要な場合は
	/// これ（AdvUguiManager）かAdvUiManagerを継承して処理を書きかえること
	/// 3.10.0以降、DefaultExecutionOrder(-1)を設定。Update内で入力処理をするので早めに設定
	/// </summary>
	[DefaultExecutionOrder(-1)]	
	[AddComponentMenu("Utage/ADV/AdvUguiManager")]
	public class AdvUguiManager : AdvUiManager
	{
		// メッセージウィンドウ
		public AdvUguiMessageWindowManager MessageWindow{ get { return Engine.MessageWindowManager.UiMessageWindowManager as AdvUguiMessageWindowManager; }}

		//デフォルトの選択肢
		[SerializeField] protected AdvUguiSelectionManager selection;
		//埋め込み選択肢
		protected AdvUguiSelectionManager EmbedSelection { get; set; }
		//埋め込み選択肢が有効か
		protected bool EnableEmbedSelection { get; set; }
		//現在の選択肢
		public AdvUguiSelectionManager CurrentSelection => EnableEmbedSelection ? EmbedSelection : selection;

		//デフォルトのバックログ
		[SerializeField] protected AdvUguiBacklogManager backLog;
		//埋め込みバックログ
		protected AdvUguiBacklogManager EmbedBackLog { get; set; }
		//埋め込みバックログが有効か
		protected bool EnableEmbedBackLog { get; set; }
		//現在のバックログ
		public AdvUguiBacklogManager CurrentBacklog => EnableEmbedBackLog ? EmbedBackLog : backLog;


		//マウスホイールによるバックログの有効・無効
		public bool DisableMouseWheelBackLog { get { return disableMouseWheelBackLog; } set { disableMouseWheelBackLog = value; } }
		[SerializeField]
		protected bool disableMouseWheelBackLog = false;
		
		
		[Flags]
		public enum InputUtilDisableFilter 
		{
			Update =  0x01 << 0,
			OnInput = 0x01 << 1,
		};
		
		//InputUtilが無効の時に、UpdateやOnInputを無視する
		//宴4から初期値を-1（EveryThing）に設定
		public InputUtilDisableFilter FilterInputUtilDisable { get { return filterInputUtilDisable; } set { filterInputUtilDisable = value; } }
		[EnumFlags,SerializeField]
		protected InputUtilDisableFilter filterInputUtilDisable = (InputUtilDisableFilter)(-1);

		//メッセージウィンドウを非表示にした時にも、キー入力を有効にするか
		[SerializeField] protected bool enableInputKeyOnHideMessage = false;
		
		//現在のステータスが変更されたときのイベント
		public UnityEvent OnChangeStatus => onChangeStatus;
		[SerializeField] UnityEvent onChangeStatus = new ();
		

		//InputUtilが無効の時の設定されたフィルターをチェック
		protected bool CheckInputUtilDisable(InputUtilDisableFilter flag)
		{
			if (InputUtil.EnableInput) return false;
			return (FilterInputUtilDisable & flag) == flag;
		}
		
		//埋め込み選択肢の設定
		//nullが設定された場合は、選択肢が無効になる
		public void SetEmbedSelection(AdvUguiSelectionManager embedSelection)
		{
			EnableEmbedSelection = true;
			EmbedSelection = embedSelection;
			if (EmbedSelection != null)
			{
				EmbedSelection.InitEngine(this.Engine);
			}
		}
		//埋め込み選択肢を解除して、デフォルトの選択肢に戻す
		public void ReleaseEmbedSelection()
		{
			EnableEmbedSelection = false;
			if (EmbedSelection != null)
			{
				EmbedSelection.ReleaseEngine();
			}
			EmbedSelection = null;
		}

		//埋め込みバックログの設定
		//nullが設定された場合は、バックログが無効になる
		public void SetEmbedBackLog(AdvUguiBacklogManager backlog)
		{
			EnableEmbedBackLog = true;
			EmbedBackLog = backlog;
			if (EmbedBackLog != null)
			{
				EmbedBackLog.InitEngine(this.Engine);
			}
		}

		//埋め込みバックログを解除して、デフォルトのバックログに戻す
		public void ReleaseEmbedBackLog()
		{
			EnableEmbedBackLog = false;
			if (EmbedBackLog != null)
			{
				EmbedBackLog.ReleaseEngine();
			}
			EmbedBackLog = null;
		}

		public override void Open()
		{
			this.gameObject.SetActive(true);
			ChangeStatus(UiStatus.Default);
		}

		public override void Close()
		{
			this.gameObject.SetActive(false);
			MessageWindow.Close();
			if (CurrentSelection != null) CurrentSelection.Close();
			if (CurrentBacklog != null) CurrentBacklog.Close();
		}

		protected override void ChangeStatus(UiStatus newStatus)
		{
			switch (newStatus)
			{
				case UiStatus.Backlog:
					if (CurrentBacklog == null) return;

					MessageWindow.Close();
					if (CurrentSelection != null) CurrentSelection.Close();
					if (CurrentBacklog != null) CurrentBacklog.Open();
					Engine.Config.IsSkip = false;
					break;
				case UiStatus.HideMessageWindow:
					MessageWindow.Close();
					if (CurrentSelection != null) CurrentSelection.Close();
					if (CurrentBacklog != null) CurrentBacklog.Close();
					Engine.Config.IsSkip = false;
					break;
				case UiStatus.Default:
					MessageWindow.Open();
					if (CurrentSelection != null) CurrentSelection.Open();
					if (CurrentBacklog != null) CurrentBacklog.Close();
					break;
			}
			this.status = newStatus;
			OnChangeStatus.Invoke();
		}

		//ウインドウ閉じるボタンが押された
		protected virtual void OnTapCloseWindow()
		{
			Status = UiStatus.HideMessageWindow;
		}

		protected virtual void Update()
		{
			if(CheckInputUtilDisable(InputUtilDisableFilter.Update)) return;
			
			//読み進みなどの入力
			bool isInput = (Engine.Config.IsMouseWheelSendMessage && InputUtil.IsInputNexByMouse())
								|| InputUtil.IsInputNextButton();
			switch (Status)
			{
				case UiStatus.Backlog:
					break;
				case UiStatus.HideMessageWindow:	//メッセージウィンドウが非表示
					//右クリック
					if (InputUtil.IsInputGuiClose())
					{	//通常画面に復帰
						Status = UiStatus.Default;
					}
					else if (!DisableMouseWheelBackLog && InputUtil.IsInputOpenBackLog())
					{
						//バックログ開く
						Status = UiStatus.Backlog;
					}
					break;
				case UiStatus.Default:
					if (IsShowingMessageWindow)
					{
						//テキストの更新
						Engine.Page.UpdateText();
					}
					if (IsShowingMessageWindow || Engine.SelectionManager.IsWaitInput)
					{	//入力待ち
						if (InputUtil.IsInputGuiClose())
						{	//右クリックでウィンドウ閉じる
							Status = UiStatus.HideMessageWindow;
						}
						else if (!DisableMouseWheelBackLog && InputUtil.IsInputOpenBackLog())
						{	//バックログ開く
							Status = UiStatus.Backlog;
						}
						else
						{
							if (isInput)
							{
								//メッセージ送り
								Engine.Page.InputSendMessage();
								base.IsInputTrig = true;
							}
						}
					}
					else
					{
						//enableInputKeyOnHideMessageがtrueの時は、
						//メッセージウィンドウが表示されていないときもキーによる入力を有効にする
						if (isInput && enableInputKeyOnHideMessage)
						{
							base.IsInputTrig = true;
						}
					}
					break;
			}
		}

		/// <summary>
		/// タッチされたとき
		/// </summary>
		public virtual void OnPointerDown(BaseEventData data)
		{
			if (data !=null && data is PointerEventData)
			{
				//左クリック入力のみ
				if((data as PointerEventData).button != PointerEventData.InputButton.Left) return;
			}

			OnInput(data);
		}

		/// <summary>
		/// クリックなどの入力があったとき（キーボード入力による文字送りなどを拡張するときに）
		/// </summary>
		public virtual void OnInput(BaseEventData data = null)
		{
			if(CheckInputUtilDisable(InputUtilDisableFilter.OnInput)) return;

			switch (Status)
			{
				case UiStatus.Backlog:
					break;
				case UiStatus.HideMessageWindow:    //メッセージウィンドウが非表示
					Status = UiStatus.Default;
					break;
				case UiStatus.Default:
					if (Engine.Config.IsSkip)
					{
						//スキップ中ならスキップ解除
						Engine.Config.ToggleSkip();
					}
					else
					{
						if (IsShowingMessageWindow)
						{
							if (!Engine.Config.IsSkip)
							{
								//文字送り
								Engine.Page.InputSendMessage();
							}
						}
						if (data != null && data is PointerEventData)
						{
							base.OnPointerDown(data as PointerEventData);
						}
					}
					break;
			}
		}
	}
}
