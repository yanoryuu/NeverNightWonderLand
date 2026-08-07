using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// デバッグメニュー表示
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDebugMenu")]
	public class SystemUiDebugMenu : MonoBehaviour
	{
		[SerializeField]
		GameObject buttonRoot = null;

		[SerializeField]
		GameObject buttonViewRoot = null;
		
		[SerializeField]
		UguiLocalize buttonText = null;

		[SerializeField]
		GameObject debugInfo = null;

		[SerializeField,HideIfTMP] Text debugInfoText = null;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI debugInfoTextTmp = null;

		[SerializeField]
		GameObject debugLog = null;

		[SerializeField, HideIfTMP] Text debugLogText=null;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI debugLogTextTmp = null;

		[SerializeField, HideIfTMP] Text languageButtonText = null;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI languageButtonTextTMP = null;

		[SerializeField]
		bool autoUpdateLogText = true;
		//	public UILabel debugLogLabel;

		[SerializeField]
		GameObject rootDebugMenu = null;

		[SerializeField]
		GameObject targetDeleteAllSaveData = null;

		public bool EnableReleaseBuild
		{
			get => enabeReleaseBuild;
			set => enabeReleaseBuild = value;
		}
		[SerializeField]
		bool enabeReleaseBuild = false;


		bool Ignore
		{
			get
			{
				return !EnableReleaseBuild && !UnityEngine.Debug.isDebugBuild;
			}
		}
		void Start()
		{
			if (Ignore)
			{
				buttonRoot.SetActive(false);
			}

			ClearAll();
			ChangeMode(currentMode);
		}

		enum Mode
		{
			Hide,
			Info,
			Log,
			Memu,
			Max,
		};

		Mode currentMode = Mode.Hide;
		public void OnClickSwitchButton()
		{
			if (Ignore) return;

			ChangeMode(currentMode + 1);
		}

		void ChangeMode(Mode mode)
		{
			if (currentMode == mode) return;
			if (mode >= Mode.Max) mode = 0;

			currentMode = mode;
			ClearAll();
			StopAllCoroutines();
			switch (currentMode)
			{
				case Mode.Info:
					StartCoroutine(CoUpdateInfo());
					break;
				case Mode.Log:
					StartCoroutine(CoUpdateLog());
					break;
				case Mode.Memu:
					StartCoroutine(CoUpdateMenu());
					break;
				case Mode.Hide:
					break;
			};
		}

		void ClearAll()
		{
			buttonViewRoot.SetActive(false);

			debugInfo.SetActive(false);
			debugLog.SetActive(false);

			rootDebugMenu.SetActive(false);
		}

		IEnumerator CoUpdateInfo()
		{
			buttonViewRoot.SetActive(true);
			buttonText.Key = SystemText.DebugInfo.ToString();

			debugInfo.SetActive(true);
			while (true)
			{
				SetTextDebugInfo(DebugPrint.GetDebugString());
				yield return null;
			}
		}

		IEnumerator CoUpdateLog()
		{
			buttonViewRoot.SetActive(true);
			buttonText.Key = SystemText.DebugLog.ToString();

			debugLog.SetActive(true);
			if (autoUpdateLogText)
			{
				AppendTextDebugLog(DebugPrint.GetLogString());
			}

			yield break;
		}

		IEnumerator CoUpdateMenu()
		{
			buttonViewRoot.SetActive(true);
			buttonText.Key = SystemText.DebugMenu.ToString();
			UpdateLanguageButtonText();
			rootDebugMenu.SetActive(true);
			yield break;
		}

		//セーブデータを消去して終了
		public void OnClickDeleteAllSaveDataAndQuit()
		{
			foreach (var component in targetDeleteAllSaveData.GetComponentsInChildren<IAdvSaveDelete>(true))
			{
				component.OnDeleteAllSaveDataAndQuit();
			}
			PlayerPrefs.DeleteAll();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
		
		//キャッシュファイルを全て削除
		public void OnClickDeleteAllCacheFiles()
		{
			AssetFileManager.GetInstance().AssetBundleInfoManager.DeleteAllCache();
		}

		//言語切り替え
		public void OnClickChangeLanguage()
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager == null) return;
			if (langManager.Languages.Count < 1) return;

			//言語をシフトループ
			int index = langManager.Languages.IndexOf(langManager.CurrentLanguage);
			langManager.CurrentLanguage = langManager.Languages[(index+1) % langManager.Languages.Count];
			UpdateLanguageButtonText();
		}
		
		//言語切り替え
		public void ChangeLanguage(string language)
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager == null) return;
			if (langManager.Languages.Count < 1) return;

			langManager.CurrentLanguage = language;
			UpdateLanguageButtonText();
		}
		
		//ボイスのみ言語切り替え
		public void ChangeVoiceLanguage(string language)
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager == null) return;

			langManager.VoiceLanguage = language;
			UpdateLanguageButtonText();
		}
		
		//言語切り替えボタンのテキストを設定
		void UpdateLanguageButtonText()
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager == null) return;

			string text = LanguageSystemText.LocalizeText(SystemText.Language);
			text += "\n" + langManager.CurrentLanguage;
			SetLanguageButtonText(text);
		}

		//ボイスのみの言語切り替えを元に戻す
		public void ResetVoiceLanguage()
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager == null) return;

			langManager.VoiceLanguage = "";
		}

		protected virtual void SetTextDebugLog(string text)
		{
			TextComponentWrapper.SetText(this.debugLogText, this.debugLogTextTmp, text);
		}

		protected virtual void AppendTextDebugLog(string text)
		{
			TextComponentWrapper.AppendText(this.debugLogText, this.debugLogTextTmp,text);
		}

		protected virtual void SetTextDebugInfo(string text)
		{
			TextComponentWrapper.SetText(this.debugInfoText, this.debugInfoTextTmp, text);
		}

		protected virtual void SetLanguageButtonText(string text)
		{
			TextComponentWrapper.SetText(this.languageButtonText, this.languageButtonTextTMP, text);
		}

	}
}
