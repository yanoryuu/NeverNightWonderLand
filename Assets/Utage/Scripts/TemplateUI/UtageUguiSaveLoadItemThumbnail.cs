// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// セーブロード用のUI画像に、キャプチャ画像ではなく設定された名前のサムネイル画像を表示する
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSaveLoadItemThumbnail")]
	public class UtageUguiSaveLoadItemThumbnail : MonoBehaviour
	{
		public AdvUguiLoadGraphicFile texture;
		public virtual bool TrySetThumbnail(AdvSaveData saveData)
		{
			//親オブジェクトより上にあるはずのセーブロード画面を取得
			var view = this.GetComponentInParent<UtageUguiSaveLoad>();
			//AdvEngineを取得
			var engine = view.Engine;
			//サムネイル画像のパラメーター名が設定されているか
			if (engine.SaveManager.DisableThumbnailParam) return false;
			
			//セーブデータのパラメーターを取得
			var paramManager = saveData.ReadParam(engine);
			
			//サムネイル名を取得
			var thumbnailName = paramManager.GetParameterString(engine.SaveManager.ThumbnailParamName);
			if (string.IsNullOrEmpty(thumbnailName)) return false;

			if (!engine.SaveManager.TryGetUiTextureData(thumbnailName, engine, out var textureData)) return false;
			texture.LoadTextureFile(textureData.FilePath);
			return true;
		}
	}
}
