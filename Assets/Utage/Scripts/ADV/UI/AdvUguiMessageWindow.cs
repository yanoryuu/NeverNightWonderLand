// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UtageExtensions;

namespace Utage
{

	/// メッセージウィドウ処理
	[AddComponentMenu("Utage/ADV/AdvUguiMessageWindow")]
	public class AdvUguiMessageWindow : MonoBehaviour, IAdvMessageWindow, IAdvMessageWindowCaracterCountChecker
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing( ref engine);
		[SerializeField]
		protected AdvEngine engine;

		/// <summary>既読済みのテキスト色を変えるか</summary>
		protected enum ReadColorMode
		{
			None,		//既読済みでも変えない
			Change,		//既読済みで色を変える
			ChangeIgnoreNameText,		//既読済みで色を変えるが、NameTextは変更しない
		}
		[SerializeField]
		protected ReadColorMode readColorMode = ReadColorMode.None;

		/// <summary>既読済みのテキスト色</summary>
		[SerializeField]
		protected Color readColor = new Color(0.8f, 0.8f, 0.8f);

		protected Color defaultTextColor = Color.white;
		protected Color defaultNameTextColor = Color.white;

		/// <summary>本文テキスト</summary>
		public UguiNovelText Text { get { return text; } }
		[SerializeField, HideIfTMP] protected UguiNovelText text=null;
		[SerializeField, HideIfLegacyText] protected TextMeshProNovelText textPro = null;

		/// <summary>名前表示テキスト</summary>
		[SerializeField, HideIfTMP] protected Text nameText;
		[SerializeField, HideIfLegacyText] protected TextMeshProNovelText nameTextPro = null;

		//キャラ名のルート（背景オブジェクトなどを表示、非表示するときに）
		[SerializeField] protected GameObject characterNameRoot;

		/// <summary>ウインドウのルート</summary>
		[SerializeField]
		protected GameObject rootChildren;

		/// <summary>コンフィグの透明度を反映させるUIのルート</summary>
		[SerializeField,FormerlySerializedAs("transrateMessageWindowRoot")]
		protected CanvasGroup translateMessageWindowRoot;

		/// <summary>改ページ以外の入力待ちアイコン</summary>
		[SerializeField]
		protected GameObject iconWaitInput;

		/// <summary>改ページ待ちアイコン</summary>
		[SerializeField]
		protected GameObject iconBrPage;

		[SerializeField]
		protected bool isLinkPositionIconBrPage = true;

		public bool IsCurrent { get; protected set; }

		//テキストの変更処理が始まる前に呼ばれる
		public AdvMessageWindowEvent OnPreChangeText => onPreChangeText;
		[SerializeField] AdvMessageWindowEvent onPreChangeText = new();

		//テキストの変更処理が終わった後に呼ばれる
		public AdvMessageWindowEvent OnPostChangeText => onPostChangeText;
		[SerializeField]
		AdvMessageWindowEvent onPostChangeText = new ();


		//ゲーム起動時の初期化
		public virtual void OnInit(AdvMessageWindowManager windowManager)
		{
			if (engine == null)
			{
				engine = windowManager.Engine;
			}
			defaultTextColor = NovelTextComponentWrapper.GetColor(text, textPro);
			defaultNameTextColor = NovelTextComponentWrapper.GetColor(nameText, nameTextPro);
			Clear();
		}

		protected virtual void Clear()
		{
			NovelTextComponentWrapper.Clear(text, textPro);
			NovelTextComponentWrapper.Clear(nameText, nameTextPro);
			if (iconWaitInput) iconWaitInput.SetActive(false);
			if (iconBrPage) iconBrPage.SetActive(false);
			rootChildren.SetActive(false);
		}

		//初期状態にもどす
		public virtual void OnReset()
		{
			Clear();
		}

		//現在のウィンドウかどうかが変わった
		public virtual void OnChangeCurrent(bool isCurrent)
		{
			this.IsCurrent = isCurrent;
		}

		//アクティブ状態が変わった
		public virtual void OnChangeActive(bool isActive)
		{
			this.gameObject.SetActive(isActive);
			if (!isActive)
			{
				Clear();
			}
			else
			{
				rootChildren.SetActive(true);
			}
		}

		//テキストに変更があった場合
		public virtual void OnTextChanged(AdvMessageWindow window)
		{
			//テキスト変更が始まる前の拡張イベント
			OnPreChangeText.Invoke(window);
		
			//表示テキストの設定
			NovelTextComponentWrapper.SetNovelTextData(text, textPro, window.Text,window.TextLength);

			//名前テキストの設定			
			if (nameText!=null)
			{
				//旧ノベルテキストは、パラメーターのタグの反映のためにいったん空文字を設定
				nameText.text = "";
			}
			NovelTextComponentWrapper.SetText(nameText, nameTextPro, window.NameText);
			if (characterNameRoot != null)
			{
				characterNameRoot.SetActive(!string.IsNullOrEmpty(window.NameText));
			}

			switch (readColorMode)
			{
				case ReadColorMode.Change:
					NovelTextComponentWrapper.SetColor(text, textPro, Engine.Page.CheckReadPage() ? readColor : defaultTextColor);
					NovelTextComponentWrapper.SetColor(nameText, nameTextPro, Engine.Page.CheckReadPage() ? readColor : defaultNameTextColor);
					break;
				case ReadColorMode.ChangeIgnoreNameText:
					NovelTextComponentWrapper.SetColor(text, textPro, Engine.Page.CheckReadPage() ? readColor : defaultTextColor);
					break;
				case ReadColorMode.None:
				default:
					break;
			}

			LinkIcon();

			//テキスト変更が始まった後の拡張イベント
			OnPostChangeText.Invoke(window);
		}


		//子オブジェクトのAwakeが間に合わないと、
		//イベントリストナーが登録されないのでいったんここでアクティブ状態にする
		protected virtual void Awake()
		{
			if (!this.rootChildren.activeSelf)
			{
				rootChildren.SetActive(true);
				rootChildren.SetActive(false);
			}
		}

		//毎フレームの更新
		protected virtual void LateUpdate()
		{
			if (Engine.UiManager.Status == AdvUiManager.UiStatus.Default)
			{
				rootChildren.SetActive(Engine.UiManager.IsShowingMessageWindow);
				if (Engine.UiManager.IsShowingMessageWindow)
				{
					//ウィンドのアルファ値反映
					if (translateMessageWindowRoot!=null)
					{
						translateMessageWindowRoot.alpha = Engine.Config.MessageWindowAlpha;
					}
				}
			}

			UpdateCurrent();
		}

		//現在のメッセージウィンドウの場合のみの更新
		protected virtual void UpdateCurrent()
		{
			if (!IsCurrent) return;

			if (Engine.UiManager.Status == AdvUiManager.UiStatus.Default)
			{
				if (Engine.UiManager.IsShowingMessageWindow)
				{
					//テキストの文字送り
					NovelTextComponentWrapper.SetMaxVisibleCharacters(text, textPro, Engine.Page.CurrentTextLength);
				}
				LinkIcon();
			}
		}

		//アイコンの場所をテキストの末端にあわせる
		protected virtual void LinkIcon()
		{
			if (iconWaitInput == null)
			{
				//ページ途中の入力待ちアイコンが設定されてない場合(古いバージョン）対応
				//改ページ待ちと入力待ちを同じ扱い
				LinkIconSub(iconBrPage, Engine.Page.IsWaitInputInPage || Engine.Page.IsWaitBrPage);
			}
			else
			{
				//入力待ち
				LinkIconSub(iconWaitInput, Engine.Page.IsWaitInputInPage);
				//改ページ待ち
				LinkIconSub(iconBrPage, Engine.Page.IsWaitBrPage);
			}
		}

		//アイコンの場所をテキストの末端にあわせる
		protected virtual void LinkIconSub(GameObject icon, bool isActive)
		{
			if (icon == null) return;

			if (!Engine.UiManager.IsShowingMessageWindow)
			{
				icon.SetActive(false);
			}
			else
			{
				icon.SetActive(isActive);
				if (isActive && isLinkPositionIconBrPage)
				{
					icon.transform.localPosition = NovelTextComponentWrapper.GetCurrentEndPosition(text,textPro);
				}
			}
		}

		//ウインドウ閉じるボタンが押された
		public virtual void OnTapCloseWindow()
		{
			Engine.UiManager.Status = AdvUiManager.UiStatus.HideMessageWindow;
		}

		//バックログボタンが押された
		public virtual void OnTapBackLog()
		{
			Engine.UiManager.Status = AdvUiManager.UiStatus.Backlog;
		}

		//表示文字数チェック開始（今設定されているテキストを返す）
		public virtual string StartCheckCaracterCount()
		{
			return NovelTextComponentWrapper.GetText(text, textPro);
		}

		//指定テキストに対する表示文字数チェック
		public virtual bool TryCheckCaracterCount(string str, out int count, out string errorString)
		{
			return NovelTextComponentWrapper.TryCheckCharacterCount(text, textPro, str, out count, out errorString);
		}

		//Startで設定されていたテキストに戻す
		public virtual void EndCheckCaracterCount(string str)
		{
			//コンポーネントの表示文字を戻すので、直接設定（SetTextDirect）する
			NovelTextComponentWrapper.SetTextDirect(text, textPro, str);
		}
	}

}
