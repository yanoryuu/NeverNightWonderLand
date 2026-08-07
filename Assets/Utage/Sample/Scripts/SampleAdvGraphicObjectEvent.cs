// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;

namespace Utage
{

	//特定のオブジェクトの色をカスタムする
	public class SampleAdvGraphicObjectEvent : MonoBehaviour
	{
		public AdvEngine engine = null;
		public string customObjectName = "hoge";
		public Color customColor = Color.white;

		void Awake()
		{
			engine.GraphicManager.OnInitGraphicObject.AddListener(OnInit);
			engine.GraphicManager.OnDrawGraphicObject.AddListener(OnDraw);
		}

		//グラフィックオブジェクトが新しく作成されて初期化されたとき呼ばれるイベント
		void OnInit(AdvGraphicObject graphicObject)
		{
			//			Debug.Log($"OnInit {graphicObject.name}");
			if (graphicObject.name == customObjectName)
			{
				graphicObject.EffectColor.ScriptColor = customColor;
			}
		}

		//グラフィックオブジェクトの描画時によばれるイベント。AdvGraphicInfoは、キャラクターシートのパターンごとの情報が入っている
		void OnDraw(AdvGraphicObject graphicObject, AdvGraphicInfo graphicInfo)
		{
			//			Debug.Log($"OnDraw {graphicInfo.Key}");
			if (graphicInfo.RowData.TryParseCell("Alpha", out float a))
			{
				//				Debug.Log($"OnDraw Alpha = {a}");
				graphicObject.EffectColor.ScriptColor = new Color(1.0f, 1.0f, 1.0f, a);
			}
		}
	}
}
