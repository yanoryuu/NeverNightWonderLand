// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// ダイシングキャラクター表示サンプル
	/// </summary>
	public class UtageSampleLoadCaharacter : MonoBehaviour
	{
		//ローダー
		public AdvGraphicLoader Loader
		{
			get { return this.GetComponentCacheCreateIfMissing<AdvGraphicLoader>(ref loader); }
		}

		AdvGraphicLoader loader;

		/// <summary>
		/// CG表示画面
		/// </summary>
		public AdvUguiLoadGraphicFile texture;

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		[SerializeField] DicingImage dicingImage = null;


		[SerializeField] string testName = "";
		[SerializeField] string testPattern = "";

		void Start()
		{
			Load(testName, testPattern);
		}

		public void Load(string name, string pattern)
		{
			AdvGraphicInfo graphicInfo = Engine.DataManager.SettingDataManager.CharacterSetting
				.KeyToGraphicInfo(AdvCharacterSetting.ToDataKey(name, pattern)).Main;
			Loader.LoadGraphic(graphicInfo, () => OnLoaded(graphicInfo));
		}

		void OnLoaded(AdvGraphicInfo graphic)
		{
			switch (graphic.FileType)
			{
				case AdvGraphicInfo.FileTypeDicing:
					dicingImage.DicingData = graphic.File.UnityObject as DicingTextures;
					string pattern = graphic.SubFileName;
					dicingImage.ChangePattern(pattern);
					break;
				default:
					Debug.LogError(graphic.FileType + " is not support ");
					break;
			}
		}
	}
}
