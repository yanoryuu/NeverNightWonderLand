// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// 文字送りの音を鳴らす
/// </summary>
namespace Utage
{
	[AddComponentMenu("Utage/ADV/Extra/AdvTextSound")]
	public class AdvTextSound : MonoBehaviour
	{
		//無効化フラグ
		[SerializeField]
		bool disable = false;
		public bool Disable
		{
			get { return disable; }
			set { disable = value; }
		}

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField]
		protected AdvEngine engine;

		public enum Type
		{
			Time,
			CharacterCount,
		};
		public Type type;

		public AudioClip defaultSound;

		//サウンドを鳴らす条件
		[System.Serializable]
		public class SoundInfo
		{
			public string key;
			public AudioClip sound;
		}
		public List<SoundInfo> soundInfoList = new List<SoundInfo>();

		public int intervalCount = 3;
		public float intervalTime = 0.1f;
		float lastTime;
		int lastCharacterCount;

		void Awake()
		{
			Engine.Page.OnBeginPage.AddListener(OnBeginPage);
			Engine.Page.OnUpdateSendChar.AddListener(OnUpdateSendChar);
		}

		void OnBeginPage(AdvPage page)
		{
			lastTime = 0;
			lastCharacterCount = -intervalCount;
		}

		void OnUpdateSendChar(AdvPage page)
		{
			//テキストの文字送り音を鳴らす
			if (CheckPlaySe(page))
			{
				AudioClip sound = GetSe(page);
				if (sound!=null)
				{
					SoundManager.GetInstance().PlaySe(sound);
					lastCharacterCount = page.CurrentTextLength;
					lastTime = page.Engine.Time.Time;
				}
			}
		}

		bool CheckPlaySe(AdvPage page)
		{
			if (Disable) return false;
			if (page.CurrentTextLength == lastCharacterCount) return false;

			switch(type)
			{
				case Type.Time:
					if (page.Engine.Time.Time - lastTime > intervalTime)
					{
						return true;
					}
					break;
				case Type.CharacterCount:
					if (page.CurrentTextLength >= lastCharacterCount + intervalCount)
					{
						return true;
					}
					break;
			}
			return false;
		}

		AudioClip GetSe(AdvPage page)
		{
			//キャラごとにSEを変える場合はキャラクターラベルを使う
			SoundInfo info = soundInfoList.Find(x => x.key == page.CharacterLabel);

			return (info != null) ? info.sound : defaultSound;
		}
	}
}

