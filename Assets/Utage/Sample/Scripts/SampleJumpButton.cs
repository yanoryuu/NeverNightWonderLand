// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// 任意のシナリオにジャンプするサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleJumpButton")]
	public class SampleJumpButton : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		public string scenarioLabel;


		public void OnClickJump()
		{
			Engine.JumpScenario(scenarioLabel);
		}
	}
}
