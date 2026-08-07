// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	[AddComponentMenu("Utage/ADV/Examples/SampleCheckUnity56Newer")]
	public class SampleCheckUnity56Newer : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		bool IsInit { get; set; }

		void Start()
		{
			Engine.OnClear.AddListener(OnClear);
		}

		bool Unity56OrNewer
		{
#if UNITY_5_6_OR_NEWER
			get { return true; }
#else
		get { return false; }
#endif
		}

		void OnClear(AdvEngine engine)
		{

			bool unity56OrNewer = Unity56OrNewer;
			engine.Param.TrySetParameter("unity56OrNewer", unity56OrNewer);
		}
	}
}
