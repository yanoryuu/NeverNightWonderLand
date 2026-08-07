// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using UtageExtensions;


namespace Utage
{

	/// <summary>
	/// 動的にグラフィック系ファイルをロードしてUIに描画する
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/AdvUguiLoadGraphicFile")]
	public class AdvUguiLoadGraphicFile : MonoBehaviour
	{
		//ローダー
		public AdvGraphicLoader Loader { get { return this.GetComponentCacheCreateIfMissing<AdvGraphicLoader>(ref loader); } }
		AdvGraphicLoader loader;

		/// <summary>
		/// 描画コンポーネント
		/// </summary>
		protected virtual Graphic GraphicComponent { get; set; }

		/// <summary>
		/// ロードするファイル
		/// </summary>
		protected virtual AssetFile File { get; set; }

		//グラフィック情報
		protected virtual AdvGraphicInfo GraphicInfo { get; set; }

		//テクスチャの
		public enum SizeSetting
		{
			RectSize,       //RectTransformの矩形のサイズ
			TextureSize,    //テクスチャサイズに合わせる
			GraphicSize,    //グラフィックのサイズに合わせる
		};
		public SizeSetting RectSizeSetting { get { return sizeSetting; } set { sizeSetting = value; } }
		[SerializeField]
		SizeSetting sizeSetting = SizeSetting.RectSize;

		public UnityEvent OnLoadEnd;

		/// <summary>
		/// テクスチャファイルを設定
		/// </summary>
		/// <param name="graphic">グラフィック情報</param>
		public virtual void LoadFile(AdvGraphicInfo graphic)
		{
			this.GraphicInfo = graphic;
			this.Loader.LoadGraphic(
				graphic, () =>
				{
					switch (graphic.FileType)
					{
						case AdvGraphicInfo.FileType2D:
						case "":
							RawImage rawImage = ChangeGraphicComponent<RawImage>();
							rawImage.texture = graphic.File.Texture;
							InitSize(new Vector2(rawImage.texture.width, rawImage.texture.height));
							break;
						case AdvGraphicInfo.FileTypeDicing:
							DicingImage dicingImage = ChangeGraphicComponent<DicingImage>();
							dicingImage.DicingData = graphic.File.UnityObject as DicingTextures;
							string pattern = graphic.SubFileName;
							dicingImage.ChangePattern(pattern);
							InitSize(new Vector2(dicingImage.PatternData.Width, dicingImage.PatternData.Height));
							break;
						default:
							Debug.LogError( graphic.FileType + " is not support ");
							break;
					}
					OnLoadEnd.Invoke();
				});
		}

		/// <summary>
		/// テクスチャファイルを設定
		/// </summary>
		/// <param name="path">ファイルパス</param>
		public virtual void LoadTextureFile(string path)
		{
			//直前のファイルがあればそれを削除
			ClearFile();			
			this.File = AssetFileManager.Load(path, this);
			File.AddReferenceComponent(this.gameObject);
			File.Unuse(this);
			StartCoroutine(CoWaitTextureFileLoading());
		}

		protected virtual IEnumerator CoWaitTextureFileLoading()
		{
			while (!File.IsLoadEnd) yield return null;

			if (File.Texture != null)
			{
				RawImage rawImage = ChangeGraphicComponent<RawImage>();
				rawImage.texture = File.Texture;
				InitSize(new Vector2 (rawImage.texture.width, rawImage.texture.height));
			}
			OnLoadEnd.Invoke();
		}

		protected virtual T ChangeGraphicComponent<T>() where T : Graphic
		{
			//まだ設定されてないならGetする
			if (GraphicComponent == null)
			{
				GraphicComponent = GetComponent<Graphic>();
			}
			if (GraphicComponent != null)
			{
				if (GraphicComponent is T)
				{
					//型があってるならそれを
					return GraphicComponent as T;
				}
				else
				{
					//型があってないならいったん削除して追加
					//(DestroyImmediateで「即時」に消す)
					Object.DestroyImmediate(GraphicComponent);
				}
			}
			GraphicComponent = this.gameObject.AddComponent<T>();
			return GraphicComponent as T;
		}

		protected virtual void InitSize(Vector2 resouceSize)
		{
			switch( RectSizeSetting )
			{
				case SizeSetting.TextureSize:
					(GraphicComponent.transform as RectTransform).SetSize(resouceSize.x, resouceSize.y);
					break;
				case SizeSetting.GraphicSize:
					if (GraphicInfo == null)
					{
						Debug.LogError("graphic is null");
					}
					else
					{
						float w = resouceSize.x * GraphicInfo.Scale.x;
						float h = resouceSize.y * GraphicInfo.Scale.y;
						(GraphicComponent.transform as RectTransform).SetSize(w, h);
					}
					break;
				case SizeSetting.RectSize:
				default:
					break;
			}
		}

		/// <summary>
		/// ファイルをクリア
		/// </summary>
		public virtual void ClearFile()
		{
			GraphicComponent.RemoveComponentMySelf();
			GraphicComponent = null;
			this.gameObject.RemoveComponent<AssetFileReference>();
			this.File = null;
			this.Loader.Unload();
			this.GraphicInfo = null;
		}
	}
}
