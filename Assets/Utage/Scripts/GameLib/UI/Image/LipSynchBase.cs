// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Utage
{
	//リップシンクのタイプ
	[System.Flags]
	public enum LipSynchType
	{
		Text,               //テキストのみ
		Voice,              //ボイスが鳴っている場合は、そのボイスに合わせてリップシンク
		TextAndVoice,       //テキストとボイス
	};

	//現在のリップシンクのモード
	public enum LipSynchMode
	{
		Text,               //テキスト
		Voice,              //ボイス
	};

	[System.Serializable]
	public class LipSynchEvent : UnityEvent<LipSynchBase> { }
	/// <summary>
	/// まばたき処理の基底クラス
	/// </summary>
	public abstract class LipSynchBase : MonoBehaviour
	{
		public LipSynchType Type { get { return type; } set { type = value; } }
		[SerializeField]
		LipSynchType type = LipSynchType.TextAndVoice;

		public bool UnscaledTime { get { return unscaledTime; } set { unscaledTime = value; } }
		[SerializeField]
		bool unscaledTime = false;

		//テキストのリップシンクが現在有効になっているか
		//外部から変更する
		public bool EnableTextLipSync { get; set; }

		public LipSynchMode LipSynchMode { get; set; }

		//テキストのリップシンクチェック
		public LipSynchEvent OnCheckTextLipSync = new LipSynchEvent();

		protected AdvEngine Engine => AdvGraphicBase.Engine;
		protected AdvGraphicBase AdvGraphicBase => this.GetComponentCacheInParent(ref graphicBase);
		AdvGraphicBase graphicBase;

		
		//テキストが今のフレームで更新されているか（falseの場合はリップシンクが止まる）
		public bool UpdatingText
		{
			get
			{
				return updatingText;
			}
			set
			{
				updatingText = value;
			}
		}

		private bool updatingText = false;
		
		//テキストの更新チェック
		public LipSynchEvent OnCheckUpdateingText = new LipSynchEvent();

		//ターゲットのキャラクターラベルを取得
		public string CharacterLabel
		{
			get
			{
				if (string.IsNullOrEmpty(characterLabel))
				{
					return this.gameObject.name;
				}
				else
				{
					return characterLabel;
				}
			}
			set
			{
				characterLabel = value;
			}
		}
		string characterLabel;

		//有効か
		public bool IsEnable { get; set; }

		//再生中か
		public bool IsPlaying { get; set; }

		//ポーズ中か
		public bool IsPausing { get; set; }

		//カスタムする際のインターフェース
		public IAdvLipSyncCustom LipSyncCustom { get; set; }

		//再生
		public void Play()
		{
			IsEnable = true;
		}

		//強制終了
		public void Cancel()
		{
			IsEnable = false;
			IsPlaying = false;
			OnStopLipSync();
		}

		//更新
		//Updateだと、CheckUpdatingTextで取得する判定先(AdvPageのUpdateText)のUpdateがされていなくて早すぎる可能性がある
		//なので、コルーチン（すべてのUpdateの後に実行された後）を使うことでタイミングを遅らせる。
		protected void Start()
		{
			InitLypSyncCustom();
			StartCoroutine(CoUpdate());
		}

		protected virtual void InitLypSyncCustom()
		{
			var graphicBase = this.GetComponentInParent<AdvGraphicBase>(); 
			if (graphicBase!=null)
			{
				if( graphicBase.Engine.TryGetComponent(out IAdvLipSyncCustom custom) )
				{
					LipSyncCustom = custom;
				}
			}
		}

		protected virtual IEnumerator CoUpdate()
		{
			while (true)
			{
				UpdateSub();
				yield return null;
			}
		}
		
		protected virtual void UpdateSub()
		{
			bool isVoice = CheckVoiceLipSync();
			bool isText = CheckTextLipSync();
			this.LipSynchMode = isVoice ? LipSynchMode.Voice : LipSynchMode.Text;
			bool enableLipSync = IsEnable && CheckLipSync(isVoice,isText);
			if (enableLipSync)
			{
				//テキストが更新されてないなら、ポーズをかける
				IsPausing = (LipSynchMode == LipSynchMode.Text && !CheckUpdatingText());
				if (!IsPlaying)
				{
					IsPlaying = true;
					OnStartLipSync();
				}
				OnUpdateLipSync();
			}
			else
			{
				if (IsPlaying)
				{
					IsPlaying = false;
					IsPausing = false;
					OnStopLipSync();
				}
			}
		}

		//リップシンクが有効かチェック
		//テキスト送りやボイス再生中かを設定し、
		protected bool CheckLipSync(bool isVoice,bool isText)
		{
			if (LipSyncCustom == null)
			{
				return (isVoice || isText);
			}
			return LipSyncCustom.CheckLipSync(this,isVoice, isText);
		}

		//ボイスのリップシンクのチェック
		protected bool CheckVoiceLipSync()
		{
			switch (Type)
			{
				case LipSynchType.Voice:
				case LipSynchType.TextAndVoice:
					return Engine.ScenarioSound.CheckLipSync(CharacterLabel);
				default:
					break;
			}
			return false;
		}

		//テキストのリップシンクのチェック
		protected bool CheckTextLipSync()
		{
			switch (Type)
			{
				case LipSynchType.Text:
				case LipSynchType.TextAndVoice:
					{
						OnCheckTextLipSync.Invoke(this);
						return EnableTextLipSync;
					}
				default:
					break;
			}
			return false;
		}

		//テキストのリップシンクのチェック
		protected bool CheckUpdatingText()
		{
			OnCheckUpdateingText.Invoke(this);
			return UpdatingText;
		}

		protected abstract void OnStartLipSync();

		protected abstract void OnUpdateLipSync();

		protected abstract void OnStopLipSync();
	}
}
