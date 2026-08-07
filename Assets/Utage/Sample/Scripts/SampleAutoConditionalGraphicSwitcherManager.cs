// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;

namespace Utage
{

	//特定のオブジェクトの色をカスタムする（各オブジェクト設定）
	public class SampleAutoConditionalGraphicSwitcherManager : MonoBehaviour
	{
		AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		public AdvEngine engine = null;

		void Awake()
		{
			Engine.GraphicManager.OnInitGraphicObject.AddListener(OnInit);
			Engine.GraphicManager.OnDrawGraphicObject.AddListener(OnDraw);
		}

		//グラフィックオブジェクトが新しく作成されて初期化されたとき呼ばれるイベント
		void OnInit(AdvGraphicObject graphicObject)
		{ 
			graphicObject.gameObject.AddComponent<SampleAutoConditionalGraphicSwitcher>();
		}

		//グラフィックオブジェクトの描画時によばれるイベント。AdvGraphicInfoは、キャラクターシートのパターンごとの情報が入っている
		void OnDraw(AdvGraphicObject graphicObject, AdvGraphicInfo graphicInfo)
		{
			var pattern = graphicObject.GetComponentInChildren<SampleAutoConditionalGraphicSwitcher>(true);
			if (pattern != null)
			{
				pattern.OnDraw(graphicInfo);
			}
		}
	}
}
