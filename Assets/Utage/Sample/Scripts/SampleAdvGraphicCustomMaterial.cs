// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;

namespace Utage
{

	//宴のグラフィックオブジェクトのマテリアルを変更するサンプル
	//GraphicManagerのOnInitGraphicObjectイベントを使う
	public class SampleAdvGraphicCustomMaterial : MonoBehaviour
	{
		public AdvEngine engine = null;
		public string targetName = "hoge";
		public Material material;

		void Awake()
		{
			if (engine != null)
			{
//				engine.GraphicManager.OnInitGraphicObject.AddListener(OnInitGraphicObject);
				engine.GraphicManager.OnDrawGraphicObject.AddListener(OnDrawGraphicObject);				
			}
		}


		//グラフィックオブジェクトの描画時によばれるイベント。AdvGraphicInfoは、キャラクターシートのパターンごとの情報が入っている
		public void OnDrawGraphicObject(AdvGraphicObject graphicObject, AdvGraphicInfo graphicInfo)
		{
			Debug.Log($"{nameof(OnDrawGraphicObject)} {graphicObject.name}");
			if (graphicInfo.FileType == AdvGraphicInfo.FileTypeVideo)
			{
				if (graphicObject.TargetObject.TryGetComponent(out RawImage rawImage))
				{
					Debug.Log("Change material");
					rawImage.material = material;
				}
			}
		}
	}
}
