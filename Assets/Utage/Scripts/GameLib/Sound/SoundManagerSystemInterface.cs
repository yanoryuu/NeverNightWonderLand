// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace Utage
{
	public enum SoundPlayMode
	{
		Add,                //追加して鳴らす（SEなど）
		Replay,             //同一ラベルのサウンドが鳴っている場合は、直前のをフェードアウトし、同時に先頭から鳴らしなおす（一部のSEなど）
		NotPlaySame,        //同じサウンドが鳴っている場合は、そのままにしてなにもしない（一部のSEなど）
	};

	/// <summary>
	/// サウンド管理
	/// </summary>
	public interface SoundManagerSystemInterface
	{
		void Init(SoundManager soundManager, List<string> saveStreamNameList);

		/// <summary>
		/// サウンドの再生
		/// </summary>
		void Play(string groupName, string label, SoundData soundData, float fadeInTime, float fadeOutTime);

		/// <summary>
		/// フェードアウトして曲を停止
		/// </summary>
		/// <param name="type">タイプ</param>
		/// <param name="fadeTime">フェードする時間</param>
		void Stop(string groupName, string label, float fadeTime);

		/// <summary>
		/// 指定のサウンドが鳴っているか
		/// </summary>
		/// <param name="type">タイプ</param>
		/// <returns>鳴っていればtrue、鳴っていなければfalse</returns>
		bool IsPlaying(string groupName, string label);

		/// <summary>
		/// 指定のオーディオを取得
		/// </summary>
		AudioSource GetAudioSource(string groupName, string label);

		/// <summary>
		/// 現在のボリュームを波形から計算して取得
		/// </summary>
		float GetSamplesVolume(string groupName, string label);

		/// <summary>
		/// 指定のグループすべて停止
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		void StopGroup(string groupName, float fadeTime);

		/// <summary>
		/// 指定のグループのループ以外をすべて停止
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		void StopGroupIgnoreLoop(string groupName, float fadeTime);

		/// <summary>
		/// フェードアウトして曲全てを停止
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		void StopAll(float fadeTime);


		/// <summary>
		/// フェードアウトしてループしているものを全てを停止
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		void StopAllLoop(float fadeTime);

		/// <summary>
		/// マスターボリュームの設定
		/// </summary>
		float GetMasterVolume(string groupName);
		void SetMasterVolume(string groupName, float volume);

		/// <summary>
		/// グループボリュームの設定
		/// </summary>
		float GetGroupVolume(string groupName);
		void SetGroupVolume(string groupName, float volume, float fadeTime = 0);

		/// <summary>
		/// グループ内で複数のオーディオを再生するかどうか
		/// </summary>
		bool IsMultiPlay(string groupName);
		void SetMultiPlay(string groupName, bool multiPlay);


		/// <summary>
		/// セーブデータ用のバイナリ変換
		/// 再生中のBGMのファイル情報などをバイナリ化
		/// </summary>
		void WriteSaveData(BinaryWriter writer);

		/// <summary>
		/// セーブデータを読みこみ
		/// </summary>
		void ReadSaveDataBuffer(BinaryReader reader);

		/// ロード中か
		bool IsLoading { get; }

	}
}