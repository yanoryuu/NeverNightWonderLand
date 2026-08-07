// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// ゲームのコンフィグ
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvConfig")]
	public class AdvConfig : MonoBehaviour, IBinaryIO
	{
		[SerializeField]
		bool dontUseSystemSaveData = false;     //システムセーブデータを使わない

		[SerializeField]
		bool dontSaveAuto = false;     			//オート機能をセーブしない

		[SerializeField]
		bool ignoreSoundVolume = false;			//サウンドマネージャーに対するボリューム設定等の操作を無視する(サウンドマネージャーのボリューム設定を別に行いたいときに)
		
		[SerializeField]
		float sendCharWaitSecMax = 1.0f / 10;	//一文字送りの待ち時間の最大値
		[SerializeField]
		float autoPageWaitSecMax = 2.5f;		//オート改ページ待ち時間の最大値
		[SerializeField]
		float autoPageWaitSecMin = 0f;          //オート改ページ待ち時間の最小値

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("forceSkipInputCtl")]
		bool forceSkipInputCtrl= true;           //CTRL入力で強制スキップ
		[SerializeField]
		bool useMessageSpeedRead = false;		//既読メッセージの表示スピードを個別に独自に設定するか

		/// <summary>
		/// スキップ中の演出速度の倍率
		/// </summary>
		public float SkipSpped
		{
			get => skipSpeed;
			set => skipSpeed = value;
		}
		[UnityEngine.Serialization.FormerlySerializedAs("skipSpped"), SerializeField]
		float skipSpeed = 20.0f;

		public bool SkipVoiceAndSe { get { return skipVoiceAndSe; } }
		[SerializeField]
		bool skipVoiceAndSe = false;

		public bool DontSkipLoopVoiceAndSe { get { return dontSkipLoopVoiceAndSe; } }
		[SerializeField]
		bool dontSkipLoopVoiceAndSe = false;

		[SerializeField]
		protected AdvConfigSaveData defaultData;

		protected AdvConfigSaveData current = new AdvConfigSaveData();

		protected AdvEngine Engine=> this.GetComponentCache(ref engine);
		AdvEngine engine;

		protected ScreenResolution ScreenResolution => Engine.ScreenResolution;

		/// <summary>
		/// 初回（セーブデータがない場合）のみの初期化
		/// </summary>
		public void InitDefault()
		{
			SetData(defaultData,false);
		}

		//データのキー
		public virtual string SaveKey { get { return "AdvConfig"; } }

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public virtual void OnRead(BinaryReader reader)
		{
			AdvConfigSaveData data = new AdvConfigSaveData();
			data.Read(reader);
			if (!dontUseSystemSaveData)
			{
				SetData(data, false);
			}
			else
			{
				InitDefault();
			}
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライーター</param>
		public virtual void OnWrite(BinaryWriter writer)
		{
			current.Write(writer);
		}

		/// <summary>
		/// 全てデフォルト値で初期化
		/// </summary>
		public void InitDefaultAll()
		{
			SetData(defaultData, true);
		}

		//データを設定
		protected virtual void SetData(AdvConfigSaveData data, bool isSetDefault)
		{
			IsMouseWheelSendMessage = data.isMouseWheelSendMessage;

			//エフェクトON・OFF切り替え
			IsEffect = data.isEffect;
			//未読スキップON・OFF切り替え
			IsSkipUnread = data.isSkipUnread;
			//選択肢でスキップ解除ON・OFF切り替え
			IsStopSkipInSelection = data.isStopSkipInSelection;
			//文字送り速度
			MessageSpeed = data.messageSpeed;
			//オート改ページ速度
			AutoBrPageSpeed = data.autoBrPageSpeed;
			//メッセージウィンドウの透過色
			MessageWindowTransparency = data.messageWindowTransparency;

			if (!ignoreSoundVolume)
			{
				//音量設定 サウンド全体
				SoundMasterVolume = data.soundMasterVolume;
				//音量設定 BGM
				BgmVolume = data.bgmVolume;
				//音量設定 SE
				SeVolume = data.seVolume;
				//音量設定 環境音
				AmbienceVolume = data.ambienceVolume;
				//音量設定 ボイス
				VoiceVolume = data.voiceVolume;
			}
			//音声設定（クリックで停止、次の音声まで再生を続ける）
			VoiceStopType = data.voiceStopType;

			if (!isSetDefault && !dontSaveAuto)
			{
				//コンフィグ例外（コンフィグ画面でのデフォルトに戻すに関与しない）
				IsAutoBrPage = data.isAutoBrPage;						//オート改ページ
			}

			//既読メッセージの表示スピード
			MessageSpeedRead = data.messageSpeedRead;
			//ボイス再生時にメッセージウィンドウを非表示に
			HideMessageWindowOnPlayingVoice = data.hideMessageWindowOnPlayingVoice;

			//キャラ別のボイス設定などのタグつきボリューム
			current.taggedMasterVolumeList.Clear();
			int num = data.taggedMasterVolumeList.Count;
			for (int i = 0; i < num; i++)
			{
				SetTaggedMasterVolume(data.taggedMasterVolumeList[i].tag, data.taggedMasterVolumeList[i].volume);
			}
		}

		[Obsolete("This method is deprecated. Please use ScreenResolution component instead.")]
		public bool IsFullScreen
		{
			get => ScreenResolution.IsFullScreen;
			set => ScreenResolution.IsFullScreen = value;
		}
		[Obsolete("This method is deprecated. Please use ScreenResolution component instead.")]
		public void ToggleFullScreen()
		{
			ScreenResolution.ToggleFullScreen();
		}

		/// <summary>
		/// マウスホイールでメッセージ送り切り替えるか
		/// </summary>
		public bool IsMouseWheelSendMessage
		{
			get { return current.isMouseWheelSendMessage; }
			set	{ current.isMouseWheelSendMessage = value; }
		}
		/// <summary>
		/// マウスホイールでメッセージ送り切り替え
		/// </summary>
		public void ToggleMouseWheelSendMessage()
		{
			IsMouseWheelSendMessage = !IsMouseWheelSendMessage;
		}

		/// <summary>
		/// エフェクトが有効か
		/// </summary>
		public bool IsEffect
		{
			get { return current.isEffect; }
			set { current.isEffect = value; }
		}
		/// <summary>
		/// エフェクトON・OFF切り替え
		/// </summary>
		public void ToggleEffect()
		{
			IsEffect = !IsEffect;
		}


		/// <summary>
		/// 未読スキップが有効か
		/// </summary>
		public bool IsSkipUnread {
			get { return current.isSkipUnread; }
			set { current.isSkipUnread = value; }
		}
		/// <summary>
		/// 未読スキップON・OFF切り替え
		/// </summary>
		public void ToggleSkipUnread()
		{
			IsSkipUnread = !IsSkipUnread;
		}

		/// <summary>
		/// 選択肢でスキップ解除するか
		/// </summary>
		public bool IsStopSkipInSelection
		{
			get { return current.isStopSkipInSelection; }
			set { current.isStopSkipInSelection = value; }
		}
		/// <summary>
		/// 選択肢でスキップ解除ON・OFF切り替え
		/// </summary>
		public void ToggleStopSkipInSelection()
		{
			IsStopSkipInSelection = !IsStopSkipInSelection;
		}

		/// <summary>
		/// 文字送り速度
		/// </summary>
		public float MessageSpeed
		{
			get { return current.messageSpeed; }
			set { current.messageSpeed = value; }
		}

		/// <summary>
		/// 既読メッセージの表示スピード
		/// </summary>
		public float MessageSpeedRead
		{
			get { return current.messageSpeedRead; }
			set { current.messageSpeedRead = value; }
		}

		/// <summary>
		/// 一文字進めるのにかかる時間(既読かどうかもチェック)
		/// </summary>
		public virtual float GetTimeSendChar(bool read)
		{
			if (read && useMessageSpeedRead)
			{
				return (1.0f - MessageSpeedRead) * sendCharWaitSecMax;
			}
			else
			{
				return (1.0f - MessageSpeed) * sendCharWaitSecMax;
			}
		}

		/// <summary>
		/// ボイス再生時にメッセージウィンドウを非表示に
		/// </summary>
		public bool HideMessageWindowOnPlayingVoice
		{
			get { return current.hideMessageWindowOnPlayingVoice; }
			set { current.hideMessageWindowOnPlayingVoice = value; }
		}

		/// <summary>
		/// オート改ページ速度
		/// </summary>
		public float AutoBrPageSpeed
		{
			get { return current.autoBrPageSpeed; }
			set { current.autoBrPageSpeed = value; }
		}
		/// <summary>
		/// オート改ページの待ち時間
		/// </summary>
		public float AutoPageWaitTime
		{
			get { return (1.0f - AutoBrPageSpeed) * (autoPageWaitSecMax - autoPageWaitSecMin) + autoPageWaitSecMin; }
		}

		/// <summary>
		/// メッセージウィンドウの透過度
		/// </summary>
		/// <returns></returns>
		public float MessageWindowTransparency
		{
			get { return current.messageWindowTransparency; }
			set { current.messageWindowTransparency = value; }
		}
		/// <summary>
		/// メッセージウィンドウのアルファ値（不透明度）
		/// </summary>
		public float MessageWindowAlpha { get { return 1.0f - MessageWindowTransparency; } }


		/// <summary>
		/// サウンド全体のボリューム
		/// </summary>
		public float SoundMasterVolume
		{
			get { return current.soundMasterVolume; }
			set
			{
				current.soundMasterVolume = value;
				SoundManager manager = SoundManager.GetInstance();
				if (manager)
				{
					manager.MasterVolume = value;
				}
			}
		}

		/// <summary>
		/// BGMのボリューム
		/// </summary>
		public float BgmVolume
		{
			get { return current.bgmVolume; }
			set
			{
				current.bgmVolume = value;
				SoundManager manager = SoundManager.GetInstance();
				if (manager)
				{
					manager.BgmVolume = value;
//					manager.DuckVolume = bgmVolumeFilterOfPlayingVoice;
				}
			}
		}

		/// <summary>
		/// SEのボリューム
		/// </summary>
		public float SeVolume
		{
			get { return current.seVolume; }
			set
			{
				current.seVolume = value;
				SoundManager manager = SoundManager.GetInstance();
				if (manager)
				{
					manager.SeVolume = value;
				}
			}
		}

		/// <summary>
		/// 環境音のボリューム
		/// </summary>
		public float AmbienceVolume
		{
			get { return current.ambienceVolume; }
			set
			{
				current.ambienceVolume = value;
				SoundManager manager = SoundManager.GetInstance();
				if (manager)
				{
					manager.AmbienceVolume = value;
				}
			}
		}

		/// <summary>
		/// ボイスのボリューム
		/// </summary>
		public float VoiceVolume
		{
			get { return current.voiceVolume; }
			set
			{
				current.voiceVolume = value;
				SoundManager manager = SoundManager.GetInstance();
				if (manager)
				{
					manager.VoiceVolume = value;
				}
			}
		}

		/// <summary>
		/// ボイスの止め方
		/// </summary>
		public VoiceStopType VoiceStopType
		{
			get { return current.voiceStopType; }
			set { current.voiceStopType = value; }
		}

		/// <summary>
		/// タグ付きボリュームの設定
		/// </summary>
		public void SetTaggedMasterVolume(string tag, float volume)
		{
			current.SetTaggedMasterVolume(tag, volume);
			SoundManager manager = SoundManager.GetInstance();
			if (manager)
			{
				manager.SetTaggedMasterVolume( tag, volume);
			}
		}
		public bool TryGetTaggedMasterVolume(string tag, out float volume)
		{
			return current.TryGetTaggedMasterVolume(tag, out volume);
		}

		/// <summary>
		/// スキップ判定をしない
		/// プログラムから書き換える必要があり、セーブデータの対象とならないので注意
		/// </summary>
		public bool NoSkip { get; set; }

		/// <summary>
		/// スキップのチェック
		/// </summary>
		/// <param name="isReadPage">既読のページかどうか</param>
		/// <returns>スキップする</returns>
		public virtual bool CheckSkip( bool isReadPage )
		{
			if (NoSkip)
			{
				//スキップを無視する
				return false;
			}
			else if (forceSkipInputCtrl && InputUtil.IsInputForceSkip())
			{
				//強制スキップ
				return true;
			}
			else if (isSkip)
			{
				if ( IsSkipUnread)
				{	//未読でもスキップ
					return true;
				}
				else
				{	//既読スキップ
					return isReadPage;
				}
			}
			return false;
		}

		//スキップフラグ
		public bool IsSkip
		{
			get { return isSkip; }
			set { isSkip = value; }
		}
		bool isSkip = false;

		/// <summary>
		/// スキップのON・OFF切り替え
		/// </summary>
		public void ToggleSkip()
		{
			isSkip = !isSkip;
		}

		/// <summary>
		/// 選択肢の最後でのスキップ解除処理
		/// </summary>
		public void StopSkipInSelection()
		{
			if (IsStopSkipInSelection && isSkip)
			{
				isSkip = false;
			}
		}

		/// <summary>
		/// 自動メッセージ送り
		/// </summary>
		public bool IsAutoBrPage
		{
			get { return current.isAutoBrPage; }
			set { current.isAutoBrPage = value; }
		}
		/// <summary>
		/// 自動メッセージ送り切り替え
		/// </summary>
		public void ToggleAuto()
		{
			IsAutoBrPage = !IsAutoBrPage;
		}
	}
}
