// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// ボイス系コマンドの終了時に拡張処理を行うサンプル
	/// AdvScenarioPlayerのOnEndCommandイベントに登録して使用する
	/// </summary>
	public class SampleOnEndCommandVoicePan : MonoBehaviour
	{
		AdvEngine AdvEngine => this.GetAdvEngineCacheFindIfMissing(ref advEngine);
		[SerializeField] AdvEngine advEngine;

		void Awake()
		{
			AdvEngine.ScenarioPlayer.OnEndCommand.AddListener(OnEndCommand);
		}

		public void OnEndCommand(AdvCommand command)
		{
			switch (command)
			{
				case AdvCommandVoice voice:
					CustomVoiceCommand(voice);
					break;
				case AdvCommandText text:
					CustomTextVoice(text);
					break;
			}
			return;

			//ボイスコマンドの処理
			void CustomVoiceCommand(AdvCommandVoice voiceCommand)
			{
				//ボイスコマンドのキャラクターラベル
				var characterLabel = voiceCommand.ParseCell<string>(AdvColumnName.Arg1);
				
				//PanStereoの値を「Arg4」列から取得して反映する例
				//「Arg4」列が数値でない場合は0.0fになる
				string cellName = "Arg4";
				float v = voiceCommand.ParseCellOptional(cellName, 0.0f);
				
				CustomVoicePan(characterLabel, v);
			}

			//テキストにボイスが設定されている場合
			void CustomTextVoice(AdvCommandText textCommand)
			{
				//ボイスないならなにもしない
				if(textCommand.VoiceFile == null) return;
				
				//一応PanStereoの値をデフォルトに戻しておく（ファイル名が違えば、AudioSourceも変わるはずなので必要ないかも）
				CustomVoicePan(AdvEngine.Page.CharacterLabel, 0.0f);
			}

			void CustomVoicePan(string characterLabel, float panStereo)
			{
				foreach (var audioSource in FindVoiceAudioSource(characterLabel))
				{
					if(audioSource == null) continue;
					//パンの値を反映
					audioSource.panStereo = panStereo;
				}
				
	//				Debug.Log($"CustomVoice: characterLabel={characterLabel}, panStereo={v}");
			}
			
			//ボイスのAudioSourceを取得する
			IEnumerable<AudioSource> FindVoiceAudioSource(string characterLabel)
			{
				var soundManager = SoundManager.GetInstance();
				if (soundManager.VoicePlayMode == SoundPlayMode.Replay)
				{
					//再生モードがReplayなら、SoundManagerSystemのGetAudioSourceから直接取得できるはず
					yield return soundManager.System.GetAudioSource(SoundManager.IdVoice, characterLabel);
					yield break;
				}

				if (soundManager.System is not SoundManagerSystem soundManagerSystem)
				{
					//システムを変えているので対応できない
					Debug.LogWarning($"SoundManagerSystem not found or incompatible. type={soundManager.System.GetType()}");
					yield break;
				}

				//再生モードがReplay以外の場合、SoundManagerSystemの実装によってはAudioSourceは複数を許容する前提なので、
				//GetAudioSourceで直接取得できないので、グループ名とキャラ名以下の全てのAudioSourceを返す

				SoundGroup group = soundManagerSystem.GetGroup(SoundManager.IdVoice);
				if(group == null)
				{
					//見つからない（SoundManagerSystemの実装によってはグループが存在しないケースもある）
					yield break;
				}

				var characterRoot = group.transform.Find(characterLabel);
				if (characterRoot==null)
				{
					yield break;
				}
	/*			//指定キャラの再生中のボイス全部にパンを付けるなら、コメントアウトを解除
				foreach (var audioSource in characterRoot.GetComponentsInChildren<AudioSource>())
				{
					yield return audioSource;
				}
	*/			
				//直前のコマンドのボイスのみにパンを付けるなら、最も再生時間が短いものを返す
				yield return characterRoot.GetComponentsInChildren<AudioSource>(true).OrderBy(s => s.time).FirstOrDefault();
			} 
		}
	}
}
