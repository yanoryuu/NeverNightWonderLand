// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UtageExtensions;

namespace Utage
{
	//シナリオ中に再生されているサウンドの管理
	//主にシナリオ中のボイス再生と、バックログからのボイス再生かの区別をつけるために使う
	[AddComponentMenu("Utage/ADV/Internal/AdvScenarioSound")]
	public class AdvScenarioSound : MonoBehaviour
	{
		//現在のシナリオ中のボイスかどうかを区別しない場合（旧仕様のまま）はtrueに
		bool DisableScenarioVoce => disableScenarioVoce; 
		[SerializeField] bool disableScenarioVoce = false;

		//全てのキャラクターのリップシンクが有効となる特別なラベル名
		public string AlmightyLabel => almightyLabel;
		[SerializeField] string almightyLabel = "";
		
		//全てのキャラクターのリップシンクが有効となる特別なラベル名が有効か
		bool EnableAlmightyLabel => !string.IsNullOrEmpty(AlmightyLabel);
		
		//リップシンクが有効となる最低のボリューム値
		public float LipSyncVolumeMin => lipSyncVolumeMin;
		[SerializeField] float lipSyncVolumeMin = 0.3f;
		
		//キャラクターラベルと現在のシナリオ中のボイスファイルの対応
		Dictionary<string, AssetFile> VoiceFilesInScenario { get; } = new Dictionary<string, AssetFile>();

		AdvEngine Engine => this.GetComponentCacheInParent(ref engine);
		AdvEngine engine;
		SoundManager SoundManager => Engine.SoundManager;

		public void Clear()
		{
			VoiceFilesInScenario.Clear();
		}
		//現在のシナリオ中のボイスを再生する時に呼ぶ
		public void SetVoiceInScenario(string characterLabel, AssetFile file )
		{
			if (VoiceFilesInScenario.ContainsKey(characterLabel))
			{
				VoiceFilesInScenario[characterLabel] = file;
			}
			else
			{
				VoiceFilesInScenario.Add(characterLabel,file);
			}
		}
		
		//現在のシナリオ外のボイスを再生する時に呼ぶ
		//バックログなど、シナリオ中のボイスと同じファイルが再生される可能性がある場合、区別をつけるために呼ぶ
		public void ClearVoiceInScenario(string characterLabel)
		{
			if (VoiceFilesInScenario.ContainsKey(characterLabel))
			{
				VoiceFilesInScenario[characterLabel] = null;
			}
		}

		//リップシンク対象となるボイスが鳴っているか
		public bool CheckLipSync(string characterLabel)
		{
			if (characterLabel.IsNullOrEmpty()) return false;
			if (SoundManager==null) return false;
			if (IsPlayingAlmighty() && CheckLipSyncVolume(AlmightyLabel))
			{
				return true;
			}
			if (IsPlayingScenarioVoiceSub(characterLabel) && CheckLipSyncVolume(characterLabel))
			{
				return true;
			}
			return false;
		}
		
		//再生中の音源のボリュームがリップシンクの閾値を超えているか
		protected virtual bool CheckLipSyncVolume(string label)
		{
			var samplesVolume = SoundManager.GetVoiceSamplesVolume(label);
			//			Debug.Log($"samplesVolume={samplesVolume}");
			return samplesVolume > LipSyncVolumeMin;
		}
		

		//現在のシナリオ内で指定のボイスが再生されているか
		public bool IsPlayingScenarioVoice(string characterLabel)
		{
			if (characterLabel.IsNullOrEmpty()) return false;
			if (IsPlayingAlmighty()) return false;
			return IsPlayingScenarioVoiceSub(characterLabel);
		}

		public bool IsPlayingAlmighty()
		{
			if (string.IsNullOrEmpty(AlmightyLabel)) return false;
			return IsPlayingScenarioVoiceSub(AlmightyLabel);
		}
		
		//指定のラベルのボイスが再生されているか
		protected virtual bool IsPlayingScenarioVoiceSub(string label)
		{
			if (label.IsNullOrEmpty()) return false;
			if (SoundManager == null) return false;

			if (DisableScenarioVoce)
			{
				//現在のシナリオ中のボイスかどうかを区別しない場合（旧仕様のまま）
				return SoundManager.IsPlayingVoice(label);
			}
			else
			{
				if (!VoiceFilesInScenario.TryGetValue(label, out AssetFile file))
				{
					return false;
				}

				if (file == null)
				{
					return false;
				}

				var soundManagerSystemEx = SoundManager.SystemEx;
				if (soundManagerSystemEx != null)
				{
					return soundManagerSystemEx.IsPlaying(SoundManager.IdVoice, label, file);
				}
				else
				{
					return SoundManager.IsPlayingVoice(label);
				}
			}
		}

		public AudioSource GetAudioSource(string characterLabel)
		{
			if (characterLabel.IsNullOrEmpty()) return null;
			if (SoundManager == null) return null;
			
			if (IsPlayingAlmighty())
			{
				return SoundManager.System.GetAudioSource(SoundManager.IdVoice, AlmightyLabel);
			}
			return SoundManager.System.GetAudioSource(SoundManager.IdVoice, characterLabel);
		}

		//テキストリップシンクのチェック（サウンドは関係ないが、似たような処理なのでここで行う）
		public bool CheckTextLipSync(string characterLabel)
		{
			if (!Engine.Page.IsSendChar) return false;

			if (characterLabel == Engine.Page.CharacterLabel) return true;
			if (EnableAlmightyLabel && AlmightyLabel == Engine.Page.CharacterLabel) return true;
			return false;
		}

	}
}
