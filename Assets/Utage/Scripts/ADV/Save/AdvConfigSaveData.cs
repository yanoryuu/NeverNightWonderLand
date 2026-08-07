// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// 音声の止め方の
	/// </summary>
	public enum VoiceStopType
	{
		/// <summary>次の音声まで再生を続ける</summary>
		OnNextVoice,
		/// <summary>クリックで停止</summary>
		OnClick,
	};

	/// <summary>
	/// コンフィグ用のセーブデータ
	/// </summary>
	[System.Serializable]
	public class AdvConfigSaveData
	{
		/// <summary>フルスクリーン切り替え</summary>
		public bool isFullScreen;
		/// <summary>マウスホイールでメッセージ送り切り替えるか</summary>
		public bool isMouseWheelSendMessage = true;
		/// <summary>エフェクトON・OFF切り替え</summary>
		public bool isEffect = true;
		/// <summary>未読スキップON・OFF切り替え</summary>
		public bool isSkipUnread;
		/// <summary>選択肢でスキップ解除ON・OFF切り替え</summary>
		public bool isStopSkipInSelection = true;

		/// <summary>文字送り速度</summary>
		[Range(0,1.0f)]
		public float messageSpeed = 0.5f;

		/// <summary>既読メッセージの表示スピード</summary>
		[Range(0, 1.0f)]
		public float messageSpeedRead = 0.5f;

		/// <summary>オート改ページ速度</summary>
		[Range(0, 1.0f)]
		public float autoBrPageSpeed = 0.5f;

		/// <summary>メッセージウィンドウの透過色</summary>
		[Range(0, 1.0f)]
		public float messageWindowTransparency = 0.3f;

		/// <summary>音量設定 サウンド全体</summary>
		[Range(0, 1.0f)]
		public float soundMasterVolume = 1.0f;

		/// <summary>音量設定 BGM</summary>
		[Range(0, 1.0f)]
		public float bgmVolume = 0.5f;

		/// <summary>音量設定 SE</summary>
		[Range(0, 1.0f)]
		public float seVolume = 0.5f;

		/// <summary>音量設定 環境音</summary>
		[Range(0, 1.0f)]
		public float ambienceVolume = 0.5f;

		/// <summary>音量設定 ボイス</summary>
		[Range(0, 1.0f)]
		public float voiceVolume = 0.75f;

		/// <summary>音声設定</summary>
		public VoiceStopType voiceStopType;
		/// <summary>オート改ページ</summary>
		public bool isAutoBrPage;

		/// <summary>ボイス再生時にメッセージウィンドウを非表示に</summary>
		public bool hideMessageWindowOnPlayingVoice = false;

		[System.Serializable]
		public class TaggedMasterVolume
		{
			public string tag;
			[Range(0, 1.0f)]
			public float volume;
		}
		//キャラ別のボイス設定などのタグつきボリューム
		public List<TaggedMasterVolume> taggedMasterVolumeList = new List<TaggedMasterVolume>()
		{
			new TaggedMasterVolume() { tag = SoundManager.TaggedMasterVolumeOthers, volume = 1.0f },
		};

		public void SetTaggedMasterVolume(string tag, float volume)
		{
			TaggedMasterVolume data = taggedMasterVolumeList.Find(x => x.tag == tag);
			if (data == null)
			{
				data = new TaggedMasterVolume();
				data.tag = tag;
				taggedMasterVolumeList.Add(data);
			}
			data.volume = volume;
		}
		public bool TryGetTaggedMasterVolume(string tag, out float volume)
		{
			TaggedMasterVolume data = taggedMasterVolumeList.Find(x => x.tag == tag);
			if (data == null)
			{
				volume = 0;
				return false;
			}
			else
			{
				volume = data.volume;
				return true;
			}
		}

		const int VERSION0 = 0;
		const int VERSION = 1;

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public virtual void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version <= VERSION)
			{
				isFullScreen = reader.ReadBoolean();
				isMouseWheelSendMessage = reader.ReadBoolean();
				isEffect = reader.ReadBoolean();
				isSkipUnread = reader.ReadBoolean();
				isStopSkipInSelection = reader.ReadBoolean();
				messageSpeed = reader.ReadSingle();
				autoBrPageSpeed = reader.ReadSingle();
				messageWindowTransparency = reader.ReadSingle();
				soundMasterVolume = reader.ReadSingle();
				bgmVolume = reader.ReadSingle();
				seVolume = reader.ReadSingle();
				ambienceVolume = reader.ReadSingle();
				voiceVolume = reader.ReadSingle();
				voiceStopType = (VoiceStopType)(reader.ReadInt32());
				int num = reader.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					reader.ReadBoolean();
				}
				isAutoBrPage = reader.ReadBoolean();
				if (version <= VERSION0) return;

				messageSpeedRead = reader.ReadSingle();
				hideMessageWindowOnPlayingVoice = reader.ReadBoolean();
				int count = reader.ReadInt32();
				taggedMasterVolumeList.Clear();
				for (int i = 0; i < count; i++)
				{
					TaggedMasterVolume item = new TaggedMasterVolume();
					item.tag = reader.ReadString();
					item.volume = reader.ReadSingle();
					taggedMasterVolumeList.Add(item);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public virtual void Write(BinaryWriter writer)
		{
			writer.Write(VERSION);

			writer.Write(isFullScreen);
			writer.Write(isMouseWheelSendMessage);
			writer.Write(isEffect);
			writer.Write(isSkipUnread);
			writer.Write(isStopSkipInSelection);
			writer.Write(messageSpeed);
			writer.Write(autoBrPageSpeed);
			writer.Write(messageWindowTransparency);
			writer.Write(soundMasterVolume);
			writer.Write(bgmVolume);
			writer.Write(seVolume);
			writer.Write(ambienceVolume);
			writer.Write(voiceVolume);
			writer.Write((int)voiceStopType);
			writer.Write((int)0);
			writer.Write(isAutoBrPage);

			writer.Write(messageSpeedRead);
			writer.Write(hideMessageWindowOnPlayingVoice);
			writer.Write(taggedMasterVolumeList.Count);
			foreach ( var item in taggedMasterVolumeList)
			{
				writer.Write(item.tag);
				writer.Write(item.volume);
			}
		}
	}
}