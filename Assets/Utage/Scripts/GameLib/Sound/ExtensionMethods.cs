// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Audio;
using Utage;

namespace UtageExtensions
{
	//サウンド関連の拡張メソッド
	public static class UtageSoundExtensions
	{
		public const float MaxDecibelVolume = 20.0f;
		public const float MinDecibelVolume = -80.0f;
		public static float DecibelVolumeToLinear(float decibel)
		{
			if (decibel <= MinDecibelVolume) return 0;
			if (decibel >= MaxDecibelVolume) return 10.0f;
			if (Mathf.Approximately(decibel,0)) return 1.0f;
			
			return Mathf.Pow(10.0f, decibel / 20.0f); // 音量に変換する式(20dBごとに10倍になる)
		}
		public static float LinearVolumeToDecibel(float volume01)
		{
			if (volume01 <= 0) return MinDecibelVolume;
			if (volume01 >= 10.0f) return MaxDecibelVolume;
			if (Mathf.Approximately(volume01,1)) return 0;
			
			return MaxDecibelVolume * Mathf.Log10(volume01);//dBに変換する式(音量10倍で20dB)
		}
		public static void SetAudioMixerVolume( this AudioMixer target, string exposedNameVolume,float volume01)
		{
			target.SetFloat(exposedNameVolume, LinearVolumeToDecibel(volume01));
		}
		public static void SetAudioMixerVolume( this AudioMixerGroup target, string exposedNameVolume,float volume01)
		{
			target.audioMixer.SetAudioMixerVolume(exposedNameVolume, volume01);
		}
		public static void SetAudioMixerVolumeUsingFormat( this AudioMixerGroup target, string exposedNameVolumeFormat,float volume01)
		{
			target.audioMixer.SetAudioMixerVolume(string.Format(exposedNameVolumeFormat,target.name), volume01);
		}
	}
}
