// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using System.IO;
using TMPro;
using Utage;

namespace Utage
{

	/// SkipSpeedGUIのサンプル
	public class SampleCustomConfig : MonoBehaviour, IAdvSystemSaveDataCustom
	{
		//ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		//コンフィグ画面
		UtageUguiConfig Config => this.GetComponentCache(ref config);
		[SerializeField] UtageUguiConfig config;
		
		// スキップ速度のスライダー
		[SerializeField] protected Slider sliderSkipSpeed;

		void OnInitEngine()
		{
			
		}

		void OnLoadValues()
		{
			sliderSkipSpeed.value = Engine.Config.SkipSpped;
		}

		public void SetSipSpeed(float v)
		{
			Engine.Config.SkipSpped = v;
		}

		public void OnSliderSkipSpeed(float value)
		{
			Engine.Config.SkipSpped = sliderSkipSpeed.value;
		}
		
		//セーブデータの拡張
		public string SaveKey => nameof(SampleCustomConfig);
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(Engine.Config.SkipSpped);
		}
		public void OnRead(BinaryReader reader)
		{
			Engine.Config.SkipSpped = reader.ReadSingle();
		}
	}
}
