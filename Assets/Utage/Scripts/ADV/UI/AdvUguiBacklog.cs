// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// バックログ用UI
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvUguiBacklog")]
	public class AdvUguiBacklog : MonoBehaviour
	{
		/// <summary>テキスト</summary>
		[HideIfTMP] public UguiNovelText text;
		[SerializeField, HideIfLegacyText] protected TextMeshProNovelText textMeshProLogText;

		/// <summary>キャラ名</summary>
		[HideIfTMP] public Text characterName;
		[SerializeField, HideIfLegacyText] protected TextMeshProNovelText textMeshProCharacterName;
		
		//キャラ名のルート（背景オブジェクトなどを表示、非表示するときに）
		[SerializeField] protected GameObject characterNameRoot;


		/// <summary>ボイス再生アイコン</summary>
		public GameObject soundIcon;

		/// ボイス再生ボタン
		public Button Button { get { return this.GetComponentCache(ref button); } }
		[SerializeField] protected Button button;

		/// <summary>ページ内に複数行あるか（ログの長さにあわせて変えるたりする）</summary>
		public bool isMultiTextInPage;

		public AdvEngine Engine { get; set; }
		public AdvBacklog Data { get { return data; } }
		protected AdvBacklog data;

		//初期化時に呼ばれるイベント
		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="data">バックログのデータ</param>
		public virtual void Init(Utage.AdvBacklog data)
		{
			this.data = data;

			if (isMultiTextInPage)
			{
				InitTextIfMulti();
			}
			else
			{
				InitTextIfSingle();
			}
			InitCharacterName();
			InitVoice();
			OnInit.Invoke();
		}

		//ページ内に複数テキストがある場合の初期化を行う
		protected virtual void InitTextIfMulti()
		{
			RectTransform textRectTransform = NovelTextComponentWrapper.GetRectTransform(text,textMeshProLogText);
			float defaultHeight = textRectTransform.rect.height;
			NovelTextComponentWrapper.SetText(text, textMeshProLogText, data.Text);
			float height = NovelTextComponentWrapper.GetPreferredHeight(text,textMeshProLogText);

			RectTransform r = (RectTransform)this.transform;
			textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			float baseH = r.rect.height;
			float scale = textRectTransform.lossyScale.y / this.transform.lossyScale.y;
			baseH += (height - defaultHeight) * scale;
			r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseH);
		}

		//ページ内に1つのテキストがある場合の初期化を行う
		protected virtual void InitTextIfSingle()
		{
			NovelTextComponentWrapper.SetText(text, textMeshProLogText, data.Text);
		}

		//キャラ名に関しての初期化を行う
		protected virtual void InitCharacterName()
		{
			NovelTextComponentWrapper.SetText(characterName, textMeshProCharacterName, data.MainCharacterNameText);
			if (characterNameRoot!=null)
			{
				characterNameRoot.SetActive(!string.IsNullOrEmpty(data.MainCharacterNameText));
			}
		}

		//ボイスに関しての初期化を行う
		protected virtual void InitVoice()
		{
			int countVoice = data.CountVoice;
			if (countVoice <= 0)
			{
				soundIcon.SetActive(false);
				Button.interactable = false;
			}
			else
			{
				if (countVoice >= 2 || isMultiTextInPage)
				{
					InitVoiceIfMulti();
				}
				else
				{
					Button.onClick.AddListener(() => OnClicked(data.MainVoiceFileName));
				}
			}
		}

		//ボイスが複数ある場合の初期化を行う
		protected virtual void InitVoiceIfMulti()
		{
			UguiNovelTextEventTrigger trigger =
				text.gameObject.GetComponentCreateIfMissing<UguiNovelTextEventTrigger>();
			trigger.OnClick.AddListener((x) => OnClickHitArea(x, OnClicked));
		}

		protected virtual void OnClickHitArea(UguiNovelTextHitArea hitGroup, Action<string> OnClicked)
		{
			switch (hitGroup.HitEventType)
			{
				case CharData.HitEventType.Sound:
					OnClicked(hitGroup.Arg);
					break;
			}
		}


		/// <summary>
		/// 音声再生ボタンが押された
		/// </summary>
		/// <param name="button">押されたボタン</param>
		protected virtual void OnClicked(string voiceFileName)
		{
			if (!string.IsNullOrEmpty(voiceFileName))
			{
				StartCoroutine(CoPlayVoice(voiceFileName, Data.FindCharacerLabel(voiceFileName)));
			}
		}

		//ボイスの再生
		protected virtual IEnumerator CoPlayVoice(string voiceFileName, string characterLabel)
		{
			AssetFile file = AssetFileManager.Load(voiceFileName, this);
			if (file == null)
			{
				Debug.LogError("Backlog voiceFile is NULL");
				yield break;
			}
			while (!file.IsLoadEnd)
			{
				yield return null;
			}
			SoundManager manager = SoundManager.GetInstance();
			if (manager)
			{
				manager.PlayVoice(characterLabel, file);
				if (Engine != null)
				{
					Engine.ScenarioSound.ClearVoiceInScenario(characterLabel);
				}
			}
			file.Unuse(this);
		}

	}
}

