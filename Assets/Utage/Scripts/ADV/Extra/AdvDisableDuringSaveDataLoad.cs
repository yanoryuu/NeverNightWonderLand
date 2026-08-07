//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//セーブデータのロード中にオブジェクトを無効化するコンポーネント
	//セーブデータのロード中にAdvEngine>UIオブジェクトなどがオンになったままで、見た目がわるいので無効化したいときなどに
	public class AdvDisableDuringSaveDataLoad : MonoBehaviour
	{
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;
		
		public List<GameObject> Targets => targets;
		[SerializeField] List<GameObject> targets = new ();

		List<GameObject> DisabledObjects { get; } = new();
		
		void Awake()
		{
			Engine.ScenarioPlayer.OnBeforeLoadSaveData.AddListener(OnLoadStarted);
			Engine.ScenarioPlayer.OnAfterLoadSaveData.AddListener(OnLoadFinished);
		}

		//セーブデータのロード開始時の処理
		void OnLoadStarted(AdvScenarioPlayer _)
		{
//			Debug.Log($"{Time.frameCount} セーブデータロード開始");
			DisabledObjects.Clear();
			foreach (var target in Targets)
			{
				if (target.activeSelf)
				{
					//Activeがオンのものだけ無効化して、あとで戻せるようにする
					target.SetActive(false);
					DisabledObjects.Add(target);
				}
			}
		}

		void OnLoadFinished(AdvScenarioPlayer _)
		{
//			Debug.Log($"{Time.frameCount} セーブデータロード終了");
			//無効化していたオブジェクトを元に戻す
			foreach (var target in DisabledObjects)
			{
				target.SetActive(true);
			}
			DisabledObjects.Clear();
		}
	}
}
