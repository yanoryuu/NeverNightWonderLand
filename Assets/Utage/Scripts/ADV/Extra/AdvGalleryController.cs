// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;


namespace Utage
{
	[AddComponentMenu("Utage/ADV/Extra/AdvGalleryController")]
	public class AdvGalleryController : MonoBehaviour
	{
		AdvEngine Engine => this.GetComponentCache(ref engine);
		AdvEngine engine;
		
		//シーン回想を実行中か
		public bool IsPlayingSceneGallery { get; protected set; }
		
		//シーン回想開始時に呼ばれるイベント
		public UnityEvent OnStartSceneGalleryOnSceneGallery => onStartSceneGallery;
		[SerializeField] UnityEvent onStartSceneGallery = new UnityEvent();

		//シーン回想実行中のフラグを設定するパラメーター名
		[SerializeField] string paramKeyPlayingSceneGallery = "";
		public string ParamKeyPlayingSceneGallery
		{
			get => paramKeyPlayingSceneGallery;
			set => paramKeyPlayingSceneGallery = value;
		}


		void Awake()
		{
			Engine.ScenarioPlayer.OnBeginScenarioAfterParametersInitialized.AddListener(OnBeginScenarioAfterParametersInitialized);
		}

		//AdvEngineのシナリオ開始時に呼ばれる
		public void StartGame(bool isSceneGallery)
		{
			IsPlayingSceneGallery = isSceneGallery;
		}

		public void OnBeginScenarioAfterParametersInitialized(AdvScenarioPlayer scenarioPlayer)
		{
			if (!string.IsNullOrEmpty(paramKeyPlayingSceneGallery) && Engine.Param.ExistParameter(paramKeyPlayingSceneGallery))
			{
				Engine.Param.SetParameterBoolean(paramKeyPlayingSceneGallery, IsPlayingSceneGallery);
			}
			if(IsPlayingSceneGallery)
			{
				onStartSceneGallery.Invoke();
			}
		}
	}
}

