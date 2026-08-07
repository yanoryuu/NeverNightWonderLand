using UnityEngine;
using System.Collections;
using UtageExtensions;

namespace Utage
{
	// TIPSの詳細表示画面を独自拡張するサンプル
	public class SampleTipsDetailCustom : MonoBehaviour
	{
		public AdvUguiTipsDetail TipsDetail => this.GetComponentCache<AdvUguiTipsDetail>(ref tipsDetail);
		[SerializeField] AdvUguiTipsDetail tipsDetail;
		[SerializeField] AdvUguiLoadGraphicFile tipsImage;

		//TIPSの詳細画面が初期化されるときに呼ばれるように、AdvUguiTipsDetailのOnInitに、インスペクター上で設定
		public void OnInitDetail()
		{
			var tipsInfo = TipsDetail.TipsInfo;
			var tipsData = tipsInfo.Data;
			if (tipsImage != null)
			{
				var path = tipsData.ImageFilePath;
				if (!string.IsNullOrEmpty(path))
				{
					//画像ファイルパスが設定されている場合
					//データシートのOption列を取得
					var option = tipsData.RowData.ParseCellOptional<string>("Option", "");
					//Option行がHideDetailの場合は画像を表示しない
					if (option == "HideDetail")
					{
						//表示しない
						tipsImage.gameObject.SetActive(false);
					}
				}
			}
		}
	}
}
