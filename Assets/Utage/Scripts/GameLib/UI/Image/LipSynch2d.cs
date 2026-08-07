// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Utage
{
	/// <summary>
	/// 2D用のまばたき処理の基底クラス
	/// </summary>
	public abstract class LipSynch2d : LipSynchBase
	{
		public float Duration { get { return duration; } set { duration = value; } }
		[SerializeField]
		float duration = 0.2f;

		public float Interval { get { return interval; } set { interval = value; } }
		[SerializeField]
		float interval = 0.2f;

		//ボイス音量に合わせて口パクする際のスケール値
		public float ScaleVoiceVolume { get { return scaleVoiceVolume; } set { scaleVoiceVolume = value; } }
		[SerializeField]
		float scaleVoiceVolume = 1;

		//口のパターンタグ
		public string LipTag { get { return lipTag; } set { lipTag = value; } }
		[SerializeField]
		string lipTag = "lip";

		//アニメーションデータ
		public MiniAnimationData AnimationData { get { return animationData; } set { animationData = value; } }
		[SerializeField]
		MiniAnimationData animationData = new MiniAnimationData();

		//リップシンクのボリューム(0～1。0以下の場合は無効)
		public float LipSyncVolume { get; set; }
		

		public GameObject Target
		{
			get { if (target == null) { target = this.gameObject; } return target; }
			set { target = value; }
		}
		[SerializeField]
		GameObject target;

		protected Coroutine coLypSync;

		protected override void OnStartLipSync()
		{
			if (coLypSync == null)
			{
				coLypSync = StartCoroutine(CoUpdateLipSync());
			}
		}

		protected override void OnUpdateLipSync()
		{
			if (CheckVoiceLipSync())
			{
				LipSyncVolume = (SoundManager.GetInstance().GetVoiceSamplesVolume(CharacterLabel) * ScaleVoiceVolume);
			}
			else
			{
				LipSyncVolume = -1;
			}
		}

		protected override void OnStopLipSync()
		{
			if (coLypSync != null)
			{
				StopCoroutine(coLypSync);
				coLypSync = null;
			}
		}

		protected abstract IEnumerator CoUpdateLipSync();
	}
}
