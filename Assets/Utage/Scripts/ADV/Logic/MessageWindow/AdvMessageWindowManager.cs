// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;
using System;

namespace Utage
{
	// メッセージウィンドウ管理
	// AdvEngineと実際のUIを繋ぐ管理コンポーネント
	[AddComponentMenu("Utage/ADV/Internal/AdvMessageWindowManager")]
	public class AdvMessageWindowManager : MonoBehaviour, IAdvSaveData
	{
		public IAdvMessageWindowManager UiMessageWindowManager
		{
			get
			{
				if (uiMessageWindowManager == null)
				{
					uiMessageWindowManager = this.GetComponentInChildren<IAdvMessageWindowManager>(true) as MonoBehaviour;
				}
				return uiMessageWindowManager as IAdvMessageWindowManager;
			}
		}
		[SerializeField, Interface(typeof(IAdvMessageWindowManager))]
		MonoBehaviour uiMessageWindowManager;

		//リセット時の処理
		public MessageWindowEvent OnReset { get { return onReset; } }
		[SerializeField]
		MessageWindowEvent onReset = new MessageWindowEvent();

		//アクティブなウィンドウが変わった
		public MessageWindowEvent OnChangeActiveWindows { get { return onChangeActiveWindows; } }
		[SerializeField]
		MessageWindowEvent onChangeActiveWindows = new MessageWindowEvent();

		//現在ページのウィンドウが変わった
		public MessageWindowEvent OnChangeCurrentWindow { get { return onChangeCurrentWindow; } }

		[SerializeField]
		MessageWindowEvent onChangeCurrentWindow = new MessageWindowEvent();

		//現在ページのテキストが変わった
		public MessageWindowEvent OnTextChange { get { return onTextChange; } }
		[SerializeField]
		MessageWindowEvent onTextChange = new MessageWindowEvent();		

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine { get { return this.GetComponentCache(ref engine); } }
		AdvEngine engine;

		[System.NonSerialized]
		bool isInit = false;
		//管理対象のウィンドウ
		public Dictionary<string, AdvMessageWindow> AllWindows
		{
			get
			{
				if (!isInit)
				{
					InitWindows();
				}
				return allWindows;
			}
		}
		Dictionary<string, AdvMessageWindow> allWindows = new Dictionary<string, AdvMessageWindow>();

		//起動時にアクティブにするウィンドウ
		List<string> DefaultActiveWindowNameList
		{
			get
			{
				if (!isInit)
				{
					InitWindows();
				}
				return defaultActiveWindowNameList;
			}
		}
		//起動時にアクティブにするウィンドウ
		List<string> defaultActiveWindowNameList = new List<string>();

		//現在アクティブになっているウィンドウ
		public Dictionary<string,AdvMessageWindow> ActiveWindows { get { return activeWindows; } }
		Dictionary<string, AdvMessageWindow> activeWindows = new Dictionary<string, AdvMessageWindow>();

		//現在ページのウィンドウ
		public AdvMessageWindow CurrentWindow { get; private set; }

		//切り替わる前のページウィンドウ
		public AdvMessageWindow LastWindow{ get; private set; }

		//指定の名前が現在ページのウィンドウか
		public bool IsCurrent(string name)
		{
			return CurrentWindow.Name == name;
		}

		//指定の名前がアィティブなウィンドウか
		public bool IsActiveWindow(string name)
		{
			return ActiveWindows.ContainsKey(name);
		}

		//ゲーム起動時の初期化
		void InitWindows()
		{
			allWindows.Clear();
			foreach (var keyValue in UiMessageWindowManager.AllWindows)
			{
				AddWindowSub(keyValue.Value);
			}
			if (allWindows.Count <= 0)
			{
				Debug.LogError("No windows were found in the scene..");
			}
			foreach (var keyValue in allWindows)
			{
				var window = keyValue.Value.MessageWindow;
				if (window.gameObject.activeSelf) defaultActiveWindowNameList.Add(window.gameObject.name);
			}
			isInit = true;
			foreach (var keyValue in allWindows)
			{
				if (Application.isPlaying)
				{
					keyValue.Value.MessageWindow.OnInit(this);
				}
			}
		}

		//管理対象のウィンドウUIオブジェクトを追加
		public void AddWindow( IAdvMessageWindow window )
		{
			AddWindowSub(window);
			if (Application.isPlaying)
			{
				window.OnInit(this);
			}
		}
		//管理対象のウィンドウUIオブジェクトを追加
		void AddWindowSub( IAdvMessageWindow window )
		{
			string windowName = window.gameObject.name;
			if (allWindows.ContainsKey(windowName))
			{
				Debug.LogError(windowName + ". The same name already exists. Please change to a different name.");
				return;
			}
			allWindows.Add(windowName, new AdvMessageWindow(window));
		}

		//管理対象のウィンドウのUIオブジェクトを削除
		public void RemoveWindow(IAdvMessageWindow window)
		{
			string windowName = window.gameObject.name;
			if (!AllWindows.ContainsKey(windowName))
			{
				return;
			}
			AllWindows.Remove(window.gameObject.name);
		}

		//指定のオブジェクトのメッセージウィンドウをAdvEngineに埋め込み
		public void EmbedWindow(IAdvMessageWindow window)
		{
			string windowName = window.gameObject.name;
			if (!AllWindows.ContainsKey(windowName))
			{
				AddWindow(window);
			}
			AllWindows[windowName].MessageWindow = window;
		}
		
		internal void ChangeActiveWindows(List<string> names)
		{
			//複数ウィンドウの設定
			ActiveWindows.Clear();
			foreach (var name in names)
			{
				AdvMessageWindow window;
				if (!AllWindows.TryGetValue(name, out window))
				{
					Debug.LogError(name + " is not found in message windows");
				}
				else
				{
					if (!ActiveWindows.ContainsKey(name))
					{
						ActiveWindows.Add(name, window);
					}
				}
			}

			//登録されたイベントを呼ぶ
			CalllEventActiveWindows();
		}

		//現在のウィンドウかどうかが変わった
		void CalllEventActiveWindows()
		{
			foreach (var item in AllWindows.Values)
			{
				item.ChangeActive(IsActiveWindow(item.Name));
			}
			OnChangeActiveWindows.Invoke(this);
		}

		//メッセージウィンドを変更
		internal void ChangeCurrentWindow(string name)
		{
			//設定なしならなにもしない
			if (string.IsNullOrEmpty(name)) return;

			if (CurrentWindow != null && CurrentWindow.Name == name)
			{
				//変化なし
				return;
			}
			else
			{
				AdvMessageWindow window;
				if (!ActiveWindows.TryGetValue(name, out window))
				{
					//アクティブなウィンドウにない場合、全ウィンドウから検索
					if (!AllWindows.TryGetValue(name, out window))
					{
						//全ウィンドウにもない場合どうしようもないので、デフォルトウィンドウを
						Debug.LogWarning(name + "is not found in window manager");
						name = DefaultActiveWindowNameList[0];
						window= AllWindows[name];
					}
					//非アクティブなウィンドウと交換
					if (CurrentWindow != null) ActiveWindows.Remove(CurrentWindow.Name);
					ActiveWindows.Add(name, window);

					//登録されたイベントを呼ぶ
					CalllEventActiveWindows();
				}
				LastWindow = CurrentWindow;
				CurrentWindow = window;
				//登録されたイベントを呼ぶ
				if (LastWindow != null) LastWindow.ChangeCurrent(false);
				CurrentWindow.ChangeCurrent(true);
				OnChangeCurrentWindow.Invoke(this);
			}
		}

		//指定の名前のウィンドウを検索
		internal AdvMessageWindow FindWindow(string name)
		{
			AdvMessageWindow window = CurrentWindow;
			if (!string.IsNullOrEmpty(name))
			{
				if (!AllWindows.TryGetValue(name, out window))
				{
					Debug.LogError(name + "is not found in all message windows");
				}
			}
			return window;
		}

		//テキストが変わった
		internal void OnPageTextChange(AdvPage page)
		{
			CurrentWindow.PageTextChange(page);
			OnTextChange.Invoke(this);
		}



		//データのキー
		public string SaveKey { get { return "MessageWindowManager"; } }

		//クリアする(初期状態に戻す)
		public virtual void OnClear()
		{
			if (DefaultActiveWindowNameList.Count <= 0)
			{
				Debug.LogWarning("defaultWindowNameList is zero");
			}
			else
			{
				ChangeActiveWindows(DefaultActiveWindowNameList);
				ChangeCurrentWindow(DefaultActiveWindowNameList[0]);
				//登録されたイベントを呼ぶ
				foreach (var item in AllWindows.Values)
				{
					item.Reset();
				}
				OnReset.Invoke(this);
			}
		}

		const int Version = 0;
		//書き込み
		public virtual void OnWrite(System.IO.BinaryWriter writer)
		{
			writer.Write(Version);

			writer.Write(ActiveWindows.Count);
			foreach( var item in ActiveWindows)
			{
				writer.Write(item.Key);
				writer.WriteBuffer(item.Value.WritePageData);
			}
			string currentWindowName = CurrentWindow == null ? "" : CurrentWindow.Name;
			writer.Write(currentWindowName);
		}

		//読み込み
		public virtual void OnRead(System.IO.BinaryReader reader)
		{
			//バージョンチェック
			int version = reader.ReadInt32();
			if (version == Version)
			{
				List<string> nameList = new List<string>();
				int count = reader.ReadInt32();
				for(int i = 0; i < count; ++i)
				{
					string key = reader.ReadString();
					byte[] buffer = reader.ReadBytes( reader.ReadInt32() );
					nameList.Add(key);
					BinaryUtil.BinaryRead( buffer, FindWindow(key).ReadPageData );
				}
				string currentWindowName = reader.ReadString();

				ChangeActiveWindows(nameList);
				ChangeCurrentWindow(currentWindowName);
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}

	[System.Serializable]
	public class MessageWindowEvent : UnityEvent<AdvMessageWindowManager> { }

}
