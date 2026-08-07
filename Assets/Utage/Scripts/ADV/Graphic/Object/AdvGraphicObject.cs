
// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// グラフィックオブジェクトのデータ
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvGraphicObject")]
	[RequireComponent(typeof(RectTransform))]
	public class AdvGraphicObject : MonoBehaviour, IAdvFadeAnimation
	{
		//ローダー
		public AdvGraphicLoader Loader { get { return this.GetComponentCacheCreateIfMissing<AdvGraphicLoader>(ref loader); } }
		AdvGraphicLoader loader;

		//このオブジェクトが属するグラフィックレイヤー
		public AdvGraphicLayer Layer
		{
			get { return layer; }
			internal set { layer = value; }
		}
		protected AdvGraphicLayer layer;

		//ADVエンジン本体への参照
		public AdvEngine Engine { get { return Layer.Manager.Engine; } }
		//最後に描画したグラフィックの情報
		public AdvGraphicInfo LastResource { get; private set; }

		//ピクセルとUnits単位の変換係数
		public float PixelsToUnits { get { return Layer.Manager.PixelsToUnits; } }

		//テクスチャ描き込みが有効か
		public bool EnableRenderTexture { get { return LastResource !=null && LastResource.RenderTextureSetting.EnableRenderTexture; } }

		//ターゲットとなるオブジェクト（グラッフィックの本体）
		public AdvGraphicBase TargetObject { get; private set; }

		//実際に描画するオブジェクト（RenderTexture使用時は、RenderTextureImageのほう）
		public AdvGraphicBase RenderObject { get; private set; }

		//RenderTexture使用時の描画空間
		public AdvRenderTextureSpace RenderTextureSpace { get; private set; }

		//フェード用のタイマー
		Timer FadeTimer { get; set; }

		//カラーエフェクト（フェードアルファ等）の制御コンポーネント
		public AdvEffectColor EffectColor { get { return this.GetComponentCacheCreateIfMissing<AdvEffectColor>(ref effectColor); } }
		AdvEffectColor effectColor;

		//このオブジェクトのRectTransform
		public RectTransform rectTransform { get; private set; }

		readonly List<AdvGraphicObject> swapFadeObjects = new List<AdvGraphicObject>();

		protected string ChangedAnimationPattern { get; set; }

		//このオブジェクトが指定のグラフィックタイプ（キャラクター・背景等）かをチェック
		public bool CheckType(AdvGraphicObjectType type)
		{
			if (LastResource == null) return false;
			switch (type)
			{
				case AdvGraphicObjectType.Character:
				{
					AdvCharacterSettingData settingData = LastResource.SettingData as AdvCharacterSettingData;
					return (settingData!=null);
				}
				case AdvGraphicObjectType.Bg:
				{
					AdvTextureSettingData settingData = LastResource.SettingData as AdvTextureSettingData;
					if(settingData==null) return false;
					return (settingData.TextureType == AdvTextureSettingData.Type.Bg || settingData.TextureType == AdvTextureSettingData.Type.Sprite);
				}
				default:
				case AdvGraphicObjectType.Sprite:
				{
					AdvTextureSettingData settingData = LastResource.SettingData as AdvTextureSettingData;
					if(settingData==null) return false;
					return (settingData.TextureType == AdvTextureSettingData.Type.Sprite);
				}
			}
		}


		//********初期化********//
		//レイヤーとグラフィック情報を元にオブジェクトを初期化する
		public virtual void Init(AdvGraphicLayer layer, AdvGraphicInfo graphic)
		{
			this.layer = layer;
			this.rectTransform = this.transform as RectTransform;
			this.rectTransform.SetStretch();
			this.rectTransform.pivot = graphic.Pivot0;

			if (graphic.RenderTextureSetting.EnableRenderTexture)
			{
				InitRenderTextureImage(graphic);
			}
			else
			{
				if (graphic.IsOverridePrefab())
				{
					GameObject child = this.transform.AddChildPrefab(graphic.File.UnityObject as GameObject);
					this.TargetObject = this.RenderObject = child.GetComponent<AdvGraphicBase>();
				}
				else
				{
					GameObject child = this.transform.AddChildGameObject(graphic.Key);
					this.TargetObject = this.RenderObject = child.AddComponent(graphic.GetComponentType()) as AdvGraphicBase;
				}
				this.TargetObject.Init(this);
			}

			//リップシンクのキャラクターラベルを設定
			LipSynchBase lipSync = TargetObject.GetComponentInChildren<LipSynchBase>();
			if (lipSync != null)
			{
				lipSync.CharacterLabel = this.gameObject.name;
				lipSync.OnCheckTextLipSync.AddListener(
					(x) =>
					{
						x.EnableTextLipSync = Engine.ScenarioSound.CheckTextLipSync(x.CharacterLabel);
					});
				lipSync.OnCheckUpdateingText.AddListener(
					(x) =>
					{
						x.UpdatingText = Engine.Page.UpdatingText;
					});
			}

			this.FadeTimer = this.gameObject.AddComponent<Timer>();
			this.effectColor = this.GetComponentCreateIfMissing<AdvEffectColor>();
			this.effectColor.OnValueChanged.AddListener(RenderObject.OnEffectColorsChange);
			this.Layer.Manager.OnInitGraphicObject.Invoke(this);
		}

		void InitRenderTextureImage(AdvGraphicInfo graphic)
		{
			AdvGraphicManager graphicManager = this.Layer.Manager;
			this.RenderTextureSpace = graphicManager.RenderTextureManager.CreateSpace();
			if (graphicManager.RenderTextureManager.EnableChangeLayer)
			{
				this.RenderTextureSpace.gameObject.ChangeLayerDeep(this.gameObject.layer);
			}
			this.RenderTextureSpace.Init(graphic, graphicManager.PixelsToUnits);

			GameObject child = this.transform.AddChildGameObject(graphic.Key);
			AdvGraphicObjectRenderTextureImage renderTextureImage = child.AddComponent<AdvGraphicObjectRenderTextureImage>();
			this.RenderObject = renderTextureImage;
			renderTextureImage.Init(RenderTextureSpace);
			this.RenderObject.Init(this);

			if (graphic.IsOverridePrefab())
			{
				this.TargetObject = RenderTextureSpace.RenderRoot.transform.AddChildPrefab(graphic.File.UnityObject as GameObject).GetComponent<AdvGraphicBase>();
			}
			else
			{
				this.TargetObject = RenderTextureSpace.RenderRoot.transform.AddChildGameObject(graphic.Key).AddComponent(graphic.GetComponentType()) as AdvGraphicBase;
			}
			this.TargetObject.Init(this);
		}

		//********描画開始********//
		//グラフィックを描画する（フェード時間付き）
		public virtual void Draw(AdvGraphicOperationArg arg, float fadeTime)
		{
			DrawSub(arg.Graphic, fadeTime);
		}
		void DrawSub(AdvGraphicInfo graphic, float fadeTime)
		{
			TargetObject.name = graphic.File.FileName;
/*			if (LastResource != graphic)
			{
				TargetObject.ChangeResourceOnDraw(graphic, fadeTime);
			}*/
			TargetObject.ChangeResourceOnDraw(graphic, fadeTime);
			if (RenderObject != TargetObject)
			{
				//テクスチャ書き込みをしている
				RenderObject.ChangeResourceOnDraw(graphic, fadeTime);
				if (graphic.IsUguiComponentType)
				{
					//UGUI系は、描画するImageにスケール値を適用
					RenderObject.Scale(graphic);
				}
			}
			else
			{
				TargetObject.Scale(graphic);
			}
			RenderObject.Alignment(Layer.SettingData.Alignment, graphic);
			RenderObject.Flip(Layer.SettingData.FlipX, Layer.SettingData.FlipY);
			foreach (var flip in TargetObject.GetComponentsInChildren<IAdvGraphicEventFlip>(true))
			{
				flip.Flip(Layer.SettingData.FlipX, Layer.SettingData.FlipY);
			}
			
			this.LastResource = graphic;
			this.Layer.Manager.OnDrawGraphicObject.Invoke(this, graphic);
		}
		
		//本来のとは違う流れで描画処理を外部から呼ぶ処理
		//AdvGraphicInfoはロード済みであること
		public virtual void DrawSubExplicit(AdvGraphicInfo graphic, float fadeTime)
		{
			DrawSub(graphic, fadeTime);
		}


		//コマンドによる位置設定を適用
		internal virtual void SetCommandPostion(AdvCommand command)
		{
			//位置情報を反映
			bool parsed = false;
			Vector3 pos = transform.localPosition;
			float x;
			if (command.TryParseCell<float>(AdvColumnName.Arg4, out x))
			{
				pos.x = x;
				parsed = true;
			}
			float y;
			if (command.TryParseCell<float>(AdvColumnName.Arg5, out y))
			{
				pos.y = y;
				parsed = true;
			}

			if (parsed)
			{
				transform.localPosition = pos;
			}
		}

		
		//文字列指定でのパターンチェンジ（キーフレームアニメーションに使う）
		public virtual void ChangePattern(string pattern)
		{
			if (TargetObject != null)
			{
				TargetObject.ChangePattern(pattern);
			}
		}

		//フェードインを試みる。TargetObjectが存在する場合のみ実行される
		public virtual bool TryFadeIn(float time)
		{
			if (TargetObject != null )
			{
				FadeIn(time);
				return true;
			}
			else
			{
				return false;
			}
		}
		
		//フェードイン処理
		public void FadeIn(float fadeTime)
		{
			FadeIn(fadeTime, () => { });
		}

		//フェードイン処理
		public void FadeIn(float fadeTime, Action onComplete)
		{
			float begin = 0;
			float end = 1;
			FadeTimer.StartTimer(
				fadeTime,
				Engine.Time.Unscaled,
				x =>
				{
					this.EffectColor.FadeAlpha = x.GetCurve(begin, end);
				},
				x =>
				{
					if (onComplete != null) onComplete();
				}
				);
		}

		//フェードアウトしてオブジェクトをクリアする
		public virtual void FadeOut(float time)
		{
			FadeOut(time, Clear);
		}

		//フェードアウト処理
		public void FadeOut(float time, Action onComplete)
		{
			if (TargetObject == null)
			{
				if (onComplete != null) onComplete();
				return;
			}

			float begin = this.EffectColor.FadeAlpha;
			float end = 0;
			FadeTimer.StartTimer(
				time,
				Engine.Time.Unscaled,
				x =>
				{
					this.EffectColor.FadeAlpha = x.GetCurve(begin, end);
				},
				x =>
				{
					if (onComplete != null) onComplete();
				}
				);
		}
		
		internal bool IsFading
		{
			get
			{
				IAdvCrossFadeImageObject crossFadeImage = TargetObject as IAdvCrossFadeImageObject;
				if (crossFadeImage!=null && crossFadeImage.IsCrossFading)
				{
					return true;
				}
				swapFadeObjects.RemoveAll(x => x == null);
				return FadeTimer.IsPlaying || swapFadeObjects.Count>0;
			}
		}

		//フェードをスキップする
		public void SkipFade()
		{
			IAdvCrossFadeImageObject crossFadeImage = TargetObject as IAdvCrossFadeImageObject;
			if (crossFadeImage!=null && crossFadeImage.IsCrossFading)
			{
				crossFadeImage.SkipCrossFade();
			}
			FadeTimer.SkipToEnd();
			foreach (var obj in swapFadeObjects)
			{
				if (obj != null)
				{
					obj.Clear();
				}
			}
			swapFadeObjects.Clear();
		}

		//ルール画像つきのフェードインの初期化のみ行う
		public IAnimationRuleFade BeginRuleFade(AdvEngine engine, AdvTransitionArgs data)
		{
			if ( TargetObject == null)
			{
				return null;
			}
			return RenderObject.BeginRuleFade(engine, data);
		}

		//ルール画像つきのフェードイン
		public void RuleFadeIn(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			if ( TargetObject == null)
			{
				if (onComplete != null) onComplete();
				return;
			}

			RenderObject.RuleFadeIn(engine, data, onComplete);
		}

		//ルール画像つきのフェードアウト
		public void RuleFadeOut(AdvEngine engine, AdvTransitionArgs data, Action onComplete)
		{
			if (TargetObject == null)
			{
				if (onComplete != null) onComplete();
				Clear();
				return;
			}

			RenderObject.RuleFadeOut(
				engine,
				data,
				() =>
				{
					if (onComplete != null) onComplete();
					Clear();
				});
		}

		//ルール画像付きのフェードをスキップする
		public void SkipRuleFade()
		{
			if(RenderObject==null) return;
			RenderObject.SkipRuleFade();
		}
		
		//********クリア********//
		//オブジェクトをレイヤーから除去してDestroyする
		public virtual void Clear()
		{
			RemoveFromLayer();
			//パーティクルのDestroy対策
			this.gameObject.SetActive (false);
			GameObject.Destroy(gameObject);
		}

		protected virtual void OnDestroy()
		{
			RemoveFromLayer();
			if (RenderTextureSpace)
			{
				GameObject.Destroy(RenderTextureSpace.gameObject);
			}
		}
		//レイヤーからこのオブジェクトを取り除く（Destroyはしない）
		public virtual void RemoveFromLayer()
		{
			foreach (var obj in swapFadeObjects)
			{
				if (obj != null)
				{
					obj.Clear();
				}
			}
			swapFadeObjects.Clear();
			if (this.Layer)
			{
				this.Layer.Remove(this);
			}
		}

		//スワップフェード対象オブジェクトを登録する
		public void AddSwapFadeObject(AdvGraphicObject swapFadeObject)
		{
			swapFadeObjects.Add(swapFadeObject);
		}

		internal void SetPivot(float pivotX, float pivotY, float offsetX, float offsetY, AdvGraphicObjectPivotType pivotType)
		{
			if (TargetObject == null) return;

			if (pivotType == AdvGraphicObjectPivotType.Direct)
			{
				//直接指定する場合
				rectTransform.SetPivotKeepRect(new Vector2(pivotX,pivotY));
				return;
			}
			
			//その他の場合は、いったん目標のポイントのワールド座標を取得して、それをpivot座標に変換する
			Vector3 worldPoint = GetPivotTargetWorldPoint(pivotX, pivotY, offsetX, offsetY,pivotType);
			Vector2 pivot = rectTransform.WorldPointToPivot(worldPoint);
			rectTransform.SetPivotKeepRect(pivot);
		}
		
		//ピボットの中心となるポイントをワールド座標で取得
		Vector3 GetPivotTargetWorldPoint(float pivotX, float pivotY, float offsetX, float offsetY, AdvGraphicObjectPivotType pivotType)
		{
			switch (pivotType)
			{
				case AdvGraphicObjectPivotType.WorldSpace:
					//ワールドスペースの場合のピボット点を取得
					return GetPivotTargetInWorldSpace( pivotX, pivotY, offsetX, offsetY);
				default:
					return GetPivotTargetInSpriteSpace( pivotX, pivotY, offsetX, offsetY,pivotType);
			}
		}

		//ピボットの中心となるポイントをワールド座標で取得
		Vector3 GetPivotTargetInWorldSpace(float pivotX, float pivotY, float offsetX, float offsetY)
		{
			var cam = Engine.CameraManager.FindCameraByLayer(this.layer.Canvas.gameObject.layer);
			if (cam == null)
			{
				Debug.LogError("Cant find camera");
				cam = Engine.CameraManager.FindCameraByLayer(0);
			}
			var letterBoxCamera = cam.GetComponent<LetterBoxCamera> ();
			Vector3 screenPos = new Vector3(
				(pivotX-0.5f) * letterBoxCamera.Width + offsetX,
				(pivotY-0.5f) * letterBoxCamera.Height + offsetY,
					0);
			screenPos /= letterBoxCamera.PixelsToUnits;
			var worldPoint = cam.transform.position + screenPos;
			return worldPoint;
		}

		//ピボットの中心となる対象のローカル座標で取得
		Vector3 GetPivotTargetInSpriteSpace(float pivotX, float pivotY, float offsetX, float offsetY, AdvGraphicObjectPivotType pivotType)
		{
			var childTransform = RenderObject.transform;
			var childRectTransform = childTransform as RectTransform;
			if (childRectTransform == null)
			{
				Debug.LogError( this.gameObject.name + "is not RectTransform type");
				return Vector3.zero;
			}
			//ピボットを対象のローカル座標に返還
			Vector3 local = childRectTransform.PivotToLocalPoint(new Vector2(pivotX, pivotY));
			switch (pivotType)
			{
				case AdvGraphicObjectPivotType.SpritePos:
					{
						//子オブジェクトのスケール値を無視する形に
						Vector2 scale = childRectTransform.localScale;
						if (Mathf.Approximately(0, scale.x)) scale.x = 1;
						if (Mathf.Approximately(0, scale.y)) scale.y = 1;
						local.x += offsetX / scale.x;
						local.y += offsetY / scale.y;
					}
					break;
				case AdvGraphicObjectPivotType.SpritePosLocal:
					{
						//指定の値だけずらす
						local.x += offsetX;
						local.y += offsetY;
					}
					break;
				case AdvGraphicObjectPivotType.SpritePosNoSize:
					//ここではオフセット適用しない
					break;
				default:
					Debug.LogError( pivotType + " is Failed");;
					break;
			}
			var world = childTransform.LocalPointToWorldPoint(local);
			switch (pivotType)
			{
				case AdvGraphicObjectPivotType.SpritePosNoSize:
					//ここでオフセット適用
					world.x += offsetX / Layer.Manager.PixelsToUnits;
					world.y += offsetY / Layer.Manager.PixelsToUnits;
					break;
				default:
					break;
			}
			return world;
		}

		//ピボットをLastResourceの初期値にリセットする
		public void ResetPivot()
		{
			if (LastResource == null) return;
			rectTransform.SetPivotKeepRect(LastResource.Pivot0);
		}

		//キャラクターのパターン変更をキーフレームアニメーションから呼ぶためのメソッド
		public virtual void ChangePatternAnimation(string paraString)
		{
			//記録しておいてLateUpdateで呼ぶ
			ChangedAnimationPattern = paraString;
		}

		protected void LateUpdate()
		{
			if (string.IsNullOrEmpty(ChangedAnimationPattern)) return;
			ChangePatternAnimationOnLateUpdate(ChangedAnimationPattern);
			ChangedAnimationPattern = "";
		}

		protected virtual void ChangePatternAnimationOnLateUpdate(string paraString)
		{
			if (LastResource == null)
			{
				Debug.LogError("ChangePatternAnimationError  LastResource is null");
				return;
			}

			AdvCharacterSettingData characterData = LastResource.SettingData as AdvCharacterSettingData;
			if (characterData == null)
			{
				Debug.LogError("ChangePatternAnimationError  characterData is null");
				return;
			}

			string[] after = paraString.Split(new char[] { ',' });
			float fadeTime;
			if (after.Length <= 0)
			{
				Debug.LogError("ChangePatternAnimationError  argString = " + paraString);
				return;
			}
			else if (after.Length == 1)
			{
				fadeTime = 0;
			}
			else
			{
				if (!WrapperUnityVersion.TryParseFloatGlobal(after[1], out fadeTime))
				{
					Debug.LogError("ChangePatternAnimationError  " + after[1] + " is not float string");
					return;
				}
			}

			//0秒だと、DestroyImmediateが呼ばれてしまうが
			//AnimationClip中でそれが許可されていないため
			fadeTime = Mathf.Max(fadeTime, 0.001f);

			string pattern = after[0];
			AdvCharacterSettingData newPatternData =
				Engine.DataManager.SettingDataManager.CharacterSetting.GetCharacterData(characterData.Name, pattern);
			if (newPatternData == null)
			{
				Debug.LogError("ChangePatternAnimationError  pattern is not pattern name");
				return;
			}

			//Graphicのロードは考慮しない
			var graphic = newPatternData.Graphic.Main;
			//描画
			DrawSub(graphic, Engine.Page.ToSkippedTime(fadeTime));
			//モーション変更
			if (!string.IsNullOrEmpty(graphic.AnimationState))
			{
				TargetObject.ChangeAnimationState(graphic.AnimationState, fadeTime);
			}
		}
		
		//セーブが有効なオブジェクトかをチェック
		public virtual bool EnableSaveObject()
		{
			if (LastResource == null) return false;
			if (LastResource.DataType == AdvGraphicInfo.TypeCapture)
			{
				return false;
			}
			
			AdvGraphicObjectParticle particle = this.TargetObject as AdvGraphicObjectParticle; 


			if (particle!=null)
			{
				return particle.EnableSave();
			}

			return true;
		}


		const int Version = Version3;
		const int Version3 = 3;
		const int Version2 = 2;
		const int Version1 = 1;
		const int Version0 = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.WriteRectTransfom(this.rectTransform);
			writer.WriteBuffer(this.EffectColor.Write);
			writer.WriteBuffer((x)=>AdvITweenPlayer.WriteSaveData (x,this.gameObject));
			writer.WriteBuffer((x) => AdvAnimationPlayer.WriteSaveData(x, this.gameObject));
			writer.WriteBuffer((x) => this.TargetObject.Write(x));
			foreach (var item in this.TargetObject.GetComponentsInChildren<IAdvGraphicObjectCustomSave>())
			{
				writer.WriteBuffer((x) => item.WriteSaveDataCustom(x));
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(byte[] buffer, AdvGraphicInfo graphic)
		{
			this.TargetObject.gameObject.SetActive(false);
			Loader.LoadGraphic(
				graphic,
				() =>
				{
					this.TargetObject.gameObject.SetActive(true);
					SetGraphicOnSaveDataRead(graphic);
					BinaryUtil.BinaryRead(buffer, Read);
				}
			);
		}
		//セーブデータ用のバイナリ読み込み
		void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			if (version <= Version1)
			{
				reader.ReadLocalTransform(this.transform);
			}
			else
			{
				reader.ReadRectTransfom(rectTransform);
			}
			reader.ReadBuffer(this.EffectColor.Read);
			reader.ReadBuffer(
				(x) => 
				{
					AdvITweenPlayer.ReadSaveData(x,this.gameObject,true, this.PixelsToUnits, Engine.Time.Unscaled);
				});
			reader.ReadBuffer(
				(x) =>
				{
					AdvAnimationPlayer.ReadSaveData(x, this.gameObject, Engine);
				});

			if (version <= Version0) return;

			reader.ReadBuffer(
				(x) =>
				{
					this.TargetObject.Read(x);
				});
			
			if (version>=Version3)
			{
				foreach (var item in this.TargetObject.GetComponentsInChildren<IAdvGraphicObjectCustomSave>())
				{
					reader.ReadBuffer((x) => item.ReadSaveDataCustom(x));
				}
			}
		}


		//キャプチャーイメージとして初期化
		internal void InitCaptureImage(AdvGraphicInfo grapic, Camera cachedCamera)
		{
			this.LastResource = grapic;
			AdvGraphicObjectRawImage captureObjectRawImage = this.gameObject.GetComponentInChildren<AdvGraphicObjectRawImage>();
			captureObjectRawImage.CaptureCamera(cachedCamera);
		}

		void SetGraphicOnSaveDataRead(AdvGraphicInfo graphic)
		{
			this.DrawSub(graphic, 0);
		}
	}
}
