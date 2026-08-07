// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
	//リップシンク処理をカスタムする際の標準コンポーネント
	[AddComponentMenu("Utage/ADV/AdvLipSyncCustom")]
	public class AdvLipSyncCustom : MonoBehaviour, IAdvLipSyncCustom
	{
		//リップシンク無視フラグのパラメーター名
		[SerializeField] private string paramNameIgnoreLipSync = "IgnoreLipSync";
		private AdvEngine Engine => this.GetComponentCache(ref engine);
		AdvEngine engine;
		
		//リップシンクをするかのチェック
		//isVoiceはボイス再生中
		//isTextはテキスト送り中
		public bool CheckLipSync(LipSynchBase lipSync, bool isVoice, bool isText)
		{
			//リップシンク無視フラグのパラメーターをチェック
			var param = Engine.Param; 
			if (param.GetParameterBoolean(paramNameIgnoreLipSync))
			{
				return false;
			}
			
			//通常はボイス再生か、テキスト送りが有効な場合にリップシンクをする
			return isVoice || isText;
		}
	}
}
