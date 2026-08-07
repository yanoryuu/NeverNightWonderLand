// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;

namespace Utage
{

	// メッセージウィドウの顔アイコン処理のサンプル
	[AddComponentMenu("Utage/ADV/AdvUguiMessageWindowFaceIcon")]
	public class AdvUguiMessageWindowFaceIcon : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField]
		protected AdvEngine engine;


		//キャラクター表示がオフになったときに自動的にアイコンも非表示にする
		//キャラクター表示をオフにしたのにアイコンが表示されるのが不自然だと思う場合はtrue
		//イベントCGなどで自動的に表示がオフになっているときもアイコンを表示したいときはfalse
		public bool AutoHideIconCharacterOff { get { return autoHideIconCharacterOff; } set { this.autoHideIconCharacterOff = value; } }
		[SerializeField]
		protected bool autoHideIconCharacterOff = false;

		protected AdvGraphicObject targetObject;
		protected GameObject iconRoot;

		protected virtual void Awake()
		{
			Engine.Page.OnChangeText.AddListener(OnChangeText);
			Engine.MessageWindowManager.OnReset.AddListener(OnReset);
		}

		protected virtual void Update()
		{
			if (targetObject == null && autoHideIconCharacterOff)
			{
				HideIcon();
			}
		}

		protected virtual void OnReset(AdvMessageWindowManager window )
		{
			if (iconRoot != null)
			{
				GameObject.Destroy(iconRoot);
				iconRoot = null;
			}
		}

		//テキストに変更があった場合
		protected virtual void OnChangeText(AdvPage page)
		{
			if (!TyrSetIcon(page))
			{
				targetObject = null;
				HideIcon();
			}
		}

		protected virtual void HideIcon()
		{
			if (iconRoot != null && iconRoot.activeSelf)
			{
				iconRoot.SetActive(false);
			}
		}


		//テキストに変更があった場合
		protected virtual bool TyrSetIcon(AdvPage page)
		{
			this.targetObject = null;
			AdvCharacterInfo info = page.CharacterInfo;
			if (info == null || info.Graphic == null || info.Graphic.Main == null )
			{
				return false;
			}

			AdvGraphicInfo graphic = info.Graphic.Main;
			AdvCharacterSettingData settingData = graphic.SettingData as AdvCharacterSettingData;
			if (settingData == null) return false;

			var iconInfo = settingData.Icon;
			if (iconInfo.IconType == AdvFaceIconInfo.Type.None) return false;

			this.targetObject = Engine.GraphicManager.FindObject(info.Label);

			bool SetIcon()
			{
				switch (iconInfo.IconType)
				{
					case AdvFaceIconInfo.Type.IconImage:
						SetIconImage(iconInfo.File);
						return true;
					case AdvFaceIconInfo.Type.DicingPattern:
						SetIconDicingPattern(iconInfo.File, iconInfo.IconSubFileName);
						return true;
					case AdvFaceIconInfo.Type.RectImage:
						IconGraphicType type = ParseIconGraphicType(graphic, info.Label);
						switch (type)
						{
							case IconGraphicType.Default:
								SetIconRectImage(graphic, iconInfo.IconRect);
								return true;
							case IconGraphicType.Dicing:
								SetIconDicing(graphic, iconInfo.IconRect);
								return true;
							case IconGraphicType.RenderTexture:
								SetIconRenderTexture(iconInfo.IconRect);
								return true;
							case IconGraphicType.NotSupport:
							default:
								return false;
						}
					case AdvFaceIconInfo.Type.None:
					default:
						return false;
				}
			}
			
			//アイコンを設定
			bool result = SetIcon();
			
			//アイコンの自動反転を更新する
			RefreshFlip(targetObject, iconInfo);
			return result;
		}
		
		//アイコンの自動反転を更新する
		void RefreshFlip(AdvGraphicObject graphicObject, AdvFaceIconInfo iconInfo)
		{
			var flip = iconRoot.gameObject.GetComponentCreateIfMissing<UguiFlip>();
			if( !iconInfo.AutoFlip)
			{
				flip.enabled = false;
			}
			else
			{
				flip.enabled = true;
				//イベントCG等でグラフィックが非表示の場合はここまで
				if(graphicObject==null) return;
				
				//アイコンの自動反転を更新
				var layerSettings = graphicObject.Layer.SettingData;
				flip.FlipX = layerSettings.FlipX;
				flip.FlipY = layerSettings.FlipY;
			}
		}

		protected virtual void SetIconImage(AssetFile file)
		{
			AssetFileManager.Load(file,
				x=>
				{
					RawImage rawImage = ChangeIconComponent<RawImage>();
					rawImage.material = null;
					Texture2D texture = x.Texture;
					rawImage.texture = texture;
					rawImage.uvRect = new Rect(0, 0, 1, 1);
					ChangeReference(file, rawImage.gameObject);
				});
		}

		protected virtual void SetIconDicingPattern(AssetFile file, string pattern)
		{
			DicingImage dicing = ChangeIconComponent<DicingImage>();
			DicingTextures dicingTexture = file.UnityObject as DicingTextures;

			dicing.DicingData = dicingTexture;
			dicing.ChangePattern(pattern);
			dicing.UvRect = new Rect(0, 0, 1, 1);
			ChangeReference(file, dicing.gameObject);
		}


		protected enum IconGraphicType
		{
			Default,
			Dicing,
			RenderTexture,
			NotSupport,
		};

		protected virtual IconGraphicType ParseIconGraphicType(AdvGraphicInfo info, string characterLabel)
		{
			switch (info.FileType)
			{
				case AdvGraphicInfo.FileTypeDicing:
					return IconGraphicType.Dicing;
				case AdvGraphicInfo.FileType2D:
				case "":
					return IconGraphicType.Default;
				default:
					AdvGraphicObject obj = Engine.GraphicManager.FindObject(characterLabel);
					if (obj != null && obj.EnableRenderTexture)
					{
						return IconGraphicType.RenderTexture;
					}
					else
					{
						return IconGraphicType.NotSupport;
					}
			}
		}


		//アイコンのイメージを表示
		protected virtual void SetIconRectImage(AdvGraphicInfo graphic, Rect rect)
		{
			RawImage rawImage = ChangeIconComponent<RawImage>();
			rawImage.material = null;
			Texture2D texture = graphic.File.Texture;
			rawImage.texture = texture;
			float w = texture.width;
			float h = texture.height;
			rawImage.uvRect = rect.ToUvRect(w, h);

			ChangeReference(graphic.File, rawImage.gameObject);
		}

		//ダイシングで表示する
		protected virtual void SetIconDicing(AdvGraphicInfo graphic, Rect rect)
		{
			DicingImage dicing = ChangeIconComponent<DicingImage>();
			DicingTextures dicingTexture = graphic.File.UnityObject as DicingTextures;
			string pattern = graphic.SubFileName;

			dicing.DicingData = dicingTexture;
			dicing.ChangePattern(pattern);

			float w = dicing.PatternData.Width;
			float h = dicing.PatternData.Height;
			dicing.UvRect = rect.ToUvRect(w, h);

			ChangeReference(graphic.File, dicing.gameObject);
		}


		//RenderTextureの一部を表示
		protected virtual void SetIconRenderTexture(Rect rect)
		{
			AdvGraphicObject obj = targetObject;
			if (obj.RenderTextureSpace == null)
			{
				return;
			}

			RawImage rawImage = ChangeIconComponent<RawImage>();
			if (obj.RenderTextureSpace.RenderTextureType == AdvRenderTextureMode.Image)
			{
				rawImage.material = new Material(ShaderManager.DrawByRenderTexture);
			}
			Texture texture = obj.RenderTextureSpace.RenderTexture;
			rawImage.texture = texture;
			float w = texture.width;
			float h = texture.height;

			Transform t = obj.TargetObject.transform;
//			RectTransform rectTransform = obj.TargetObject.transform as RectTransform;
//			Rect clipRect = new Rect( w/2+ imageRect.xMin, h/2-imageRect.yMax, imageRect.width, imageRect.height);

			float scaleX = t.localScale.x;
			float scaleY = t.localScale.y;
			rect.position = new Vector2(rect.position.x * scaleX, rect.position.y * scaleY);
			rect.size = new Vector2(rect.size.x*scaleX, rect.size.y * scaleY);
//			clipRect = new Rect(clipRect.xMin + rect.xMin, clipRect.yMin + rect.yMin, rect.width, rect.height);
			rawImage.uvRect = rect.ToUvRect(w, h);
		}


		protected virtual T ChangeIconComponent<T>() where T : Component
		{
			T component = null;
			if (iconRoot != null)
			{
				component = iconRoot.GetComponent<T>();
				if (component != null)
				{
					iconRoot.SetActive(true);
					return component;
				}
			}
			if (iconRoot != null)
			{
				GameObject.Destroy(iconRoot);
			}

			component = this.transform.AddChildGameObjectComponent<T>("Icon");
			iconRoot = component.gameObject;
			RectTransform rect = iconRoot.transform as RectTransform;
			if (rect != null)
			{
				rect.SetStretch();
			}
			return component;
		}

		protected virtual void ChangeReference( AssetFile file, GameObject go )
		{
			//直前のファイルがあればそれを削除
			go.RemoveComponents<AssetFileReference>();
			file.AddReferenceComponent(go);
		}
	}
}
