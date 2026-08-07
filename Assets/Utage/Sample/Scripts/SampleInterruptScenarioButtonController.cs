using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
	//シナリオ割り込み機能を呼び出すボタンの制御コンポーネント
    public class SampleInterruptScenarioButtonController : MonoBehaviour
    {
	    // ADVエンジン
		public AdvInterruptScenario AdvInterruptScenario => this.GetComponentCacheInParent(ref interruptScenario);
		[SerializeField] protected AdvInterruptScenario interruptScenario;
		
		//中断シナリオを起動するボタン等のGameObject
		[SerializeField] GameObject target;
		
		void Update()
		{
			if(AdvInterruptScenario==null) return;

			if(target==null) return;

			//割り込みシナリオを実行中なら、ボタンを非表示にする
			target.gameObject.SetActive(!AdvInterruptScenario.InterruptingScenario()); 
		}
    }
}
