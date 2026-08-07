// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// UI全般の管理
	/// </summary>
	public abstract class AdvUiManager : MonoBehaviour, IAdvSaveData
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine { get { return this.GetComponentCache( ref engine); } }
		[SerializeField]
		protected AdvEngine engine;

		public AdvGuiManager GuiManager
		{
			get { return this.gameObject.GetComponentCacheCreateIfMissing< AdvGuiManager >(ref guiManager); }
		}
		AdvGuiManager guiManager;

		//状態
		public virtual UiStatus Status
		{
			get { return status; }
			set
			{
				if (this.status == value) return;
				ChangeStatus(value);
			}
		}
		public enum UiStatus
		{
			Default,			//通常
			Backlog,			//バックログ
			HideMessageWindow,	//メッセージウィンドウ非表示
		};
		protected UiStatus status;
		public PointerEventData CurrentPointerData { get; private set; }
		public virtual bool IsPointerDowned
		{
			get { return CurrentPointerData != null; }
		}

		public abstract void Open();

		public abstract void Close();

		protected abstract void ChangeStatus(UiStatus newStatus);

		public virtual bool IsInputTrig{get; set;}

		//カスタム入力
		public virtual bool IsInputTrigCustom { get; set; }

		public virtual void OnPointerDown(PointerEventData data)
		{
			CurrentPointerData = data;
			IsInputTrig = true;
		}
		protected virtual void LateUpdate()
		{
			ClearPointerDown ();
			IsInputTrigCustom = false;
		}
		public virtual void ClearPointerDown()
		{
			CurrentPointerData = null;
			IsInputTrig = false;
		}

		//メッセージウィンドウの表示状態
		public virtual bool IsShowingMessageWindow { get; private set; }
		public virtual void HideMessageWindow()
		{
			IsShowingMessageWindow = false;
		}

		public virtual void ShowMessageWindow()
		{
			IsShowingMessageWindow = true;
		}

		//メニューボタンの非表示状態
		public bool IsShowingMenuButton
		{
			get
			{
				return !IsHideMenuButton && (IsShowingMessageWindow || Engine.SelectionManager.IsShowing);
			}
		}

		//メニューボタンの非表示状態
		public bool IsHideMenuButton { get; private set; }
		internal virtual void ShowMenuButton()
		{
			IsHideMenuButton = false;
		}
		internal virtual void HideMenuButton()
		{
			IsHideMenuButton = true;
		}

		public virtual void OnBeginPage()
		{
			IsShowingMessageWindow = false;
		}

		public virtual void OnEndPage()
		{
			IsShowingMessageWindow = false;
		}

		//メッセージウィンドウの表示を隠すか
		[System.Obsolete]
		public bool IsHide{ get{ return !IsShowingMessageWindow; }}

		//メニュー系UIの表示状態
		[System.Obsolete]
		public bool IsShowingUI { get { return (IsShowingMessageWindow || Engine.SelectionManager.IsShowing); } }

		//データのキー
		public string SaveKey{ get { return "UiManager"; } }

		//クリアする(初期状態に戻す)
		public virtual void OnClear()
		{
			IsHideMenuButton = false;
			IsShowingMessageWindow = false;
		}

		const int Version = 0;
		//書き込み
		public virtual void OnWrite(System.IO.BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(IsHideMenuButton);
			writer.Write(IsShowingMessageWindow);
		}

		//読み込み
		public virtual void OnRead(System.IO.BinaryReader reader)
		{
			//バージョンチェック
			int version = reader.ReadInt32();
			if (version == Version)
			{
				IsHideMenuButton = reader.ReadBoolean();
				IsShowingMessageWindow = reader.ReadBoolean();
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}