using UnityEngine;

namespace Utage
{
	//モザイク描画用のコンポーネント
	[AddComponentMenu("Utage/Lib/Effect/MosaicRenderer")]
	public class MosaicRenderer : MonoBehaviour
	{
		//モザイクのサイズ
		[Range(1,20)]
		public float mosaicSize = 10;

		//Zテスト
		public UnityEngine.Rendering.CompareFunction zTest = UnityEngine.Rendering.CompareFunction.Always;

		//描画順（Live2Dオブジェクトの手前に来るように）
		public string sortingLayerName = "";
		public int sortingOrder = 1000;

		//スクリーンの解像度によってモザイクのサイズが変わらないようにするための処理
		public bool autoScale = false;


		[Utage.Hide("IgnoreAutoScale")]
		public int gameScreenWidth = 800;
		[Utage.Hide("IgnoreAutoScale")]
		public int gameScreenHeight = 600;

		public bool IgnoreAutoScale { get { return !autoScale; } }


		void OnValidate()
		{
			Renderer render = this.GetComponent<Renderer>();
			if (render == null) return;
			render.sortingLayerName = sortingLayerName;
			render.sortingOrder = sortingOrder;
		}

		void LateUpdate()
		{
			Renderer render = this.GetComponent<Renderer>();
			if (render == null) return;

			render.sortingLayerName = sortingLayerName;
			render.sortingOrder = sortingOrder;
			render.enabled = true;

			//現在のカメラの描画サイズと、実際のスクリーンの描画ピクセルサイズに合わせて
			//モザイクのサイズをかえる
			float scaleSize = 1;
			if (autoScale)
			{
				scaleSize = Mathf.Min(1.0f * Screen.width / gameScreenWidth, 1.0f*Screen.height / gameScreenHeight);
			}

			//モザイクのサイズ設定
			render.material.SetFloat("_Size", Mathf.CeilToInt(mosaicSize * scaleSize));

			//Z比較の設定
			render.material.SetInt("_ZTest", (int)zTest);
		}
	}
}
