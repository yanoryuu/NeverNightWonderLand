// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// タイトル表示のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiBoot")]
	public class UtageUguiBoot : UguiView
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;


		public UguiFadeTextureStream fadeTextureStream;

		public UtageUguiTitle title;
		public UtageUguiLoadWait loadWait;

		public bool isWaitBoot;
		public bool isWaitDownLoad;
		public bool isWaitSplashScreen = true;

		///最初の画面なので自分でオープンする
		public virtual void Start()
		{
			title.gameObject.SetActive(false);
			StartCoroutine(CoUpdate());
		}

		///
		protected virtual IEnumerator CoUpdate()
		{
#if UNITY_5_3_OR_NEWER
			if (isWaitSplashScreen)
			{
				while (!WrapperUnityVersion.IsFinishedSplashScreen()) yield return null;
			}
#endif
			//BGMなどを鳴らすために追加
			Open();

			if (fadeTextureStream)
			{
				fadeTextureStream.gameObject.SetActive(true);
				fadeTextureStream.Play();
				while (fadeTextureStream.IsPlaying) yield return null;
			}

			if (isWaitBoot)
			{
				while (Engine.IsWaitBootLoading) yield return null;
			}

			this.Close();
			if (isWaitDownLoad && loadWait != null)
			{
				loadWait.OpenOnBoot();
			}
			else
			{
				title.Open();
			}
		}
	}
}
