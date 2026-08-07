// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Utage;

namespace UtageExtensions
{
	//AdvEngineを取得可能なコンポーネントにつける共通インターフェース
	//GetAdvEngineCacheFindIfMissingでAdvEngineを取得するために使う
	public interface IAdvEngineGetter
	{
		//AdvEngineを取得
		//AdvEngineGetter内で、GetAdvEngineCacheFindIfMissingを使うと無限ループになるので、
		//そうならない範囲（単純な参照を返す）でのみ使用すること
		AdvEngine AdvEngineGetter { get; }
	}
	
	public static class AdvEngineExtensions
	{
		//AdvEngineコンポーネントを探してくる処理
		public static AdvEngine GetAdvEngineCacheFindIfMissing(this GameObject go, ref AdvEngine advEngine)
		{
			//キャッシュがあればそれを返す
			if (advEngine != null) return advEngine;
			
			//キャッシュがない場合は、自分以上の階層からAdvEngineを探す
			advEngine = go.GetComponentInParent<AdvEngine>(true);
			if (advEngine != null) return advEngine;
			
			//見つからない場合は、AdvEngineを取得可能なコンポーネントからAdvEngineを取得
			foreach (var advEngineGetter in go.GetComponentsInParent<IAdvEngineGetter>(true))
			{
				advEngine = advEngineGetter.AdvEngineGetter;
				if (advEngine != null) return advEngine;
			}
			
			//見つからなかった場合は、同一シーン内で探す
			advEngine = go.scene.GetComponentInScene<AdvEngine>(true);
			if (advEngine != null) return advEngine;
			
			//それでも見つからなかったら、全シーン内から探す
			return go.GetComponentCacheFindIfMissing(ref advEngine);
		}

		public static AdvEngine GetAdvEngineCacheFindIfMissing(this Component target, ref AdvEngine advEngine)
		{
			return target.gameObject.GetAdvEngineCacheFindIfMissing(ref advEngine);
		}
	}
}
