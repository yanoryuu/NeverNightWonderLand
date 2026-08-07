// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// グラフィックの管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvGraphicManager")]
	public class AdvGraphicManager : MonoBehaviour, IBinaryIO
	{
		/// <summary>
		/// スプライトを作成する際の、座標1.0単位辺りのピクセル数
		/// </summary>
		public float PixelsToUnits
		{
			get { return pixelsToUnits; }
		}

		[SerializeField] float pixelsToUnits = 100;

		/// <summary>
		/// Z座標1.0単位辺りのSortingOrderの数
		/// </summary>
		public float SortOderToZUnits
		{
			get { return sortOderToZUnits; }
		}

		[SerializeField] float sortOderToZUnits = 100;

		//背景レイヤーのスプライト名
		public string BgSpriteName
		{
			get { return bgSpriteName; }
		}

		[SerializeField] string bgSpriteName = "BG";

		//表示済みのキャラクターのレイヤー変更時に、ローカルのTransform値をリセットするか引き継ぐか
		public bool ResetCharacterTransformOnChangeLayer
		{
			get { return resetCharacterTransformOnChangeLayer; }
		}

		[SerializeField] bool resetCharacterTransformOnChangeLayer = true;

		//デバッグ用：エディタ実行時にキャンバス位置を自動リセットするか
		public bool DebugAutoResetCanvasPosition
		{
			get
			{
#if UNITY_EDITOR
				return debugAutoResetCanvasPosition;
#else
				return false;

#endif
			}
		}
#if UNITY_EDITOR
		[SerializeField] bool debugAutoResetCanvasPosition = false;
#endif

		//オブジェクトのクロスフェード時に、座標を保持する機能を無効化
		//Version3.6.3で修正した不具合
		//　テクスチャのサイズが違うなどでシェーダーによるクロスフェードができない場合に
		//　オブジェクトを再生成したときに、座標がリセットされてしまう不具合への修正
		//による影響を無視して、前の動作を保ちたい場合にのみONにする
		public bool IgnoreKeepPositionOnCrossFade
		{
			get { return ignoreKeepPositionOnCrossFade; }
		}

		[SerializeField] bool ignoreKeepPositionOnCrossFade = false;


		//2DプレハブのSortingOrderの上書きを無効化
		//Version3.11.7での仕様変更
		//　2DプレハブのSortingOrderをLayerに設定されたOrderで上書きする
		//による影響を無視して、前の動作を保ちたい場合にのみONにする
		public bool IgnoreOverride2DPrefabSortingOrder
		{
			get { return ignoreOverride2DPrefabSortingOrder; }
		}
		[SerializeField] bool ignoreOverride2DPrefabSortingOrder = false;

		/// <summary>
		/// レンダーテクスチャー設定
		/// </summary>
		public AdvGraphicRenderTextureManager RenderTextureManager
		{
			get
			{
				if (renderTextureManager == null)
				{
					renderTextureManager =
						this.transform.parent.AddChildGameObjectComponent<AdvGraphicRenderTextureManager>(
							"GraphicRenderTextureManager");
				}

				return renderTextureManager;
			}
		}

		[SerializeField] AdvGraphicRenderTextureManager renderTextureManager;

		/// <summary>
		/// ビデオ制御
		/// </summary>
		public AdvVideoManager VideoManager
		{
			get
			{
				if (videoManager == null)
				{
					videoManager = this.transform.parent.AddChildGameObjectComponent<AdvVideoManager>("VideoManager");
				}

				return videoManager;
			}
		}

		[SerializeField] AdvVideoManager videoManager;

		//あらかじめシーン内にあるレイヤーリスト
		public List<AdvGraphicLayer> LayerList
		{
			get { return layerList; }
		}

		[SerializeField] List<AdvGraphicLayer> layerList = new List<AdvGraphicLayer>();

		//グラフィックオブジェクトが新しく作成されて初期化されたとき呼ばれるイベント
		public AdvGraphicObjectEvent OnInitGraphicObject
		{
			get { return onInitGraphicObject; }
		}
		[SerializeField] AdvGraphicObjectEvent onInitGraphicObject = new AdvGraphicObjectEvent();
		
		//グラフィックオブジェクトの描画時によばれるイベント。AdvGraphicInfoは、キャラクターシートなどのパターンごとの情報が入っている
		public AdvGraphicInfoEvent OnDrawGraphicObject
		{
			get { return onDrawGraphicObject; }
		}
		[SerializeField] AdvGraphicInfoEvent onDrawGraphicObject = new AdvGraphicInfoEvent();
		
		
		/// <summary>
		/// イベントモード（キャラクター立ち絵非表示）
		/// </summary>
		public bool IsEventMode
		{
			get { return this.isEventMode; }
			set { isEventMode = value; }
		}

		bool isEventMode;

		/// <summary>
		/// キャラクター管理
		/// </summary>
		public AdvGraphicGroup CharacterManager
		{
			get { return this.Groups[AdvLayerSettingData.LayerType.Character]; }
		}

		/// <summary>
		/// スプライト管理
		/// </summary>
		public AdvGraphicGroup SpriteManager
		{
			get { return this.Groups[AdvLayerSettingData.LayerType.Sprite]; }
		}

		/// <summary>
		/// 背景管理
		/// </summary>
		public AdvGraphicGroup BgManager
		{
			get { return this.Groups[AdvLayerSettingData.LayerType.Bg]; }
		}

		/// <summary>
		/// 全てのグループ
		/// </summary>
		protected Dictionary<AdvLayerSettingData.LayerType, AdvGraphicGroup> Groups =
			new Dictionary<AdvLayerSettingData.LayerType, AdvGraphicGroup>();

		internal AdvEngine Engine
		{
			get { return engine; }
		}

		protected AdvEngine engine;

		/// <summary>
		/// 起動時初期化
		/// </summary>
		/// <param name="setting">レイヤー設定データ</param>
		public virtual void BootInit(AdvEngine engine, AdvLayerSetting setting)
		{
			this.engine = engine;
			Groups.Clear();
			foreach (AdvLayerSettingData.LayerType type in Enum.GetValues(typeof(AdvLayerSettingData.LayerType)))
			{
				if (type == AdvLayerSettingData.LayerType.Dummy) continue;
				AdvGraphicGroup group = new AdvGraphicGroup(type, setting, this);
				Groups.Add(type, group);
			}
		}

		//指定のオブジェクトのレイヤー（キャンバス）をAdvEngineに埋め込み
		public void EmbedLayer(AdvGraphicLayer layer)
		{
			foreach (var keyValue in Groups)
			{
				if (keyValue.Value.Type == layer.LayerType)
				{
					keyValue.Value.EmbedLayer(layer);
				}
			}
		}

		//埋め込んだレイヤーを削除
		public void RemoveEmbedLayer(AdvGraphicLayer layer)
		{
			foreach (var keyValue in Groups)
			{
				if (keyValue.Value.Type == layer.LayerType)
				{
					keyValue.Value.RemoveLayer(layer);
				}
			}
		}

		/// <summary>
		/// 章追加時などリメイク
		/// </summary>
		public void Remake(AdvLayerSetting setting)
		{
			foreach (AdvGraphicGroup group in Groups.Values)
			{
				group.DestroyAll();
			}

			Groups.Clear();
			foreach (AdvLayerSettingData.LayerType type in Enum.GetValues(typeof(AdvLayerSettingData.LayerType)))
			{
				AdvGraphicGroup group = new AdvGraphicGroup(type, setting, this);
				Groups.Add(type, group);
			}
		}

		/// <summary>
		/// 全てクリア
		/// </summary>
		internal void Clear()
		{
			IsEventMode = false;
			foreach (AdvGraphicGroup group in Groups.Values)
			{
				group.Clear();
			}
		}

		/// <summary>
		/// 指定のキーのレイヤーを探す
		/// </summary>
		public AdvGraphicLayer FindLayer(string layerName)
		{
			foreach (var keyValue in Groups)
			{
				AdvGraphicLayer layer = keyValue.Value.FindLayer(layerName);
				if (layer != null) return layer;
			}

			return null;
		}

		/// <summary>
		/// 指定のオブジェクト名のレイヤーを探す
		/// </summary>
		public AdvGraphicLayer FindLayerByObjectName(string name)
		{
			foreach (var keyValue in Groups)
			{
				AdvGraphicLayer layer = keyValue.Value.FindLayerFromObjectName(name);
				if (layer != null) return layer;
			}

			return null;
		}
		
		// 全レイヤーのリセット
		internal void ResetAllLayerRectTransform()
		{
			foreach (var keyValue in Groups)
			{
				keyValue.Value.ResetAllLayerRectTransform();
			}
		}
		

		/// <summary>
		/// 指定の名前のグラフィックオブジェクトを検索
		/// </summary>
		public AdvGraphicObject FindObject(string targetName)
		{
			foreach (var keyValue in Groups)
			{
				AdvGraphicObject obj = keyValue.Value.FindObject(targetName);
				if (obj != null) return obj;
			}

			return null;
		}

		//指定の名前のAdvGraphicObjectを探し、そのTargetObjectを取得
		public AdvGraphicBase FindObjectTarget(string objectLabel)
		{
			//指定の名前のAdvGraphicObjectを探す
			AdvGraphicObject obj = FindObject(objectLabel);
			if (obj == null)
			{
				//見つからない
				return null;
			}

			//AdvGraphicObject以下に描画オブジェクトがない可能性があるので
			//obj.TargetObjectで取得
			return obj.TargetObject;
		}

		//指定の名前のAdvGraphicObjectを探し、描画オブジェクト（プレハブならプレハブインスタンス）のコンポーネントを取得
		public T FindObjectTargetComponent<T>(string objectLabel)
			where T : class
		{
			AdvGraphicBase target =  FindObjectTarget(objectLabel);
			if (target == null)
			{
				//見つからない
				return null;
			}

			//AdvGraphicObject以下に描画オブジェクトがない可能性があるので
			//obj.TargetObjectで取得
			return target.GetComponentInChildren<T>();
		}

		/// <summary>
		/// 指定の名前のレイヤーかグラフィックオブジェクトを検索
		/// </summary>
		public GameObject FindObjectOrLayer(string targetName)
		{
			AdvGraphicObject obj = FindObject(targetName);
			if (obj != null)
			{
				return obj.gameObject;
			}

			AdvGraphicLayer layer = FindLayer(targetName);
			if (layer != null)
			{
				return layer.gameObject;
			}

			return null;
		}

		//全てのグラフィックオブジェクトを取得
		public List<AdvGraphicObject> AllGraphics()
		{
			List<AdvGraphicObject> allGraphics = new List<AdvGraphicObject>();
			foreach (var keyValue in Groups)
			{
				keyValue.Value.AddAllGraphics(allGraphics);
			}

			return allGraphics;
		}

		//指定のオブジェクトがレイヤー以下の現在のオブジェクトにあるか？
		//つまり、すでに非表示だったり、フェードアウト中などではないか
		public bool ContainsCurrentGraphic(AdvGraphicObject graphicObject)
		{
			foreach (var layer in LayerList)
			{
				foreach (var keyValue in layer.CurrentGraphics)
				{
					if (keyValue.Value == graphicObject)
					{
						return true;
					}
				}
			}
			return false;
		}

		//ロード中かチェック
		internal bool IsLoading
		{
			get
			{
				foreach (var keyValue in Groups)
				{
					if (keyValue.Value.IsLoading) return true;
				}

				return false;
			}
		}

		//表示する
		internal void DrawObject(string layerName, string label, AdvGraphicOperationArg graphicOperationArg)
		{
			FindLayer(layerName).Draw(label, graphicOperationArg);
		}

		//指定の名前のオブジェクトのレイヤーを変更する
		public void ChangeLayer(string objectName, string layerName, AdvChangeLayerRepositionType repositionType, float fadeOutTime)
		{
			AdvGraphicLayer newLayer = FindLayer(layerName);
			if (newLayer == null)
			{
				Debug.LogErrorFormat("{0} is not found", layerName);
				return;
			}

			AdvGraphicGroup group=null;
			AdvGraphicObject targetObject = null;
			foreach (var keyValue in Groups)
			{
				group = keyValue.Value;
				targetObject = group.FindObject(objectName);
				if (targetObject != null)
				{
					break;
				}
			}
			if (targetObject == null)
			{
				Debug.LogErrorFormat("{0} is not found", objectName);
				return;
			}
			AdvGraphicLayer oldLayer= targetObject.Layer;
			if (oldLayer == newLayer)
			{
				return;
			}
			bool isDefaultObject= (oldLayer.DefaultObject == targetObject);
			if (group.Type == AdvLayerSettingData.LayerType.Sprite)
			{
				isDefaultObject = false;
			}
			newLayer.ChangeLayer(isDefaultObject,targetObject, repositionType, fadeOutTime);	
			oldLayer.Remove(targetObject);
		}

		//指定名のパーティクルを非表示にする
		internal void FadeOutParticle(string targetName,AdvParticleStopType stopType)
		{
			foreach (var keyValue in Groups)
			{
				keyValue.Value.FadeOutParticle(targetName,stopType);
			}
		}

		//パーティクルを全て非表示にする
		internal void FadeOutAllParticle(AdvParticleStopType stopType)
		{
			foreach (var keyValue in Groups)
			{
				keyValue.Value.FadeOutAllParticle(stopType);
			}
		}

		//パーティクルを探索する
		public AdvGraphicObject FindParticle(string targetName)
		{
			foreach (var keyValue in Groups)
			{
				AdvGraphicObject obj = keyValue.Value.FindParticle(targetName);
				if (obj != null)
				{
					return obj;
				}
			}

			return null;
		}


		//指定の名前のオブジェクトがフェード中かどうかチェック
		public bool IsFading(string targetName)
		{
			AdvGraphicObject obj = FindObjectIncludeFadeOuting(targetName);
			if (obj != null)
			{
				return obj.IsFading;
			}

			AdvGraphicLayer layer = FindLayer(targetName);
			if (layer != null)
			{
				return layer.IsFading;
			}

			return false;
		}

		//指定の名前のオブジェクトのフェードをスキップする
		public void SkipFade(string targetName)
		{
			//Bgとキャラクターは、1レイヤーに1オブジェクトしかないのでスキップ可能
			AdvGraphicObject obj = FindObjectIncludeFadeOuting(targetName);
			if (obj != null)
			{
				obj.SkipFade();
				return;
			}

			AdvGraphicLayer layer = FindLayer(targetName);
			if (layer != null)
			{
				layer.SkipFade();
				return;
			}

			Debug.LogError(targetName + " is not found in all objects");
		}

		

		//指定のタイプのオブジェクトのみ全て削除
		internal void FadeOutAllObjects(AdvGraphicObjectType objectType, float fadeTime)
		{
			foreach (var keyValue in Groups)
			{
				keyValue.Value.FadeOutAllObjects(objectType,fadeTime);
			}
		}

		//指定のタイプのオブジェクトのうちどれかがフェード中かチェック
		public bool IsFadingObjects(AdvGraphicObjectType objectType)
		{
			foreach (var keyValue in Groups)
			{
				if (keyValue.Value.IsFadingObjects(objectType)) return true;
			}
			return false;
		}

		//指定のタイプのオブジェクト全てのフェードをスキップ
		public void SkipFadeObjects(AdvGraphicObjectType objectType)
		{
			foreach (var keyValue in Groups)
			{
				keyValue.Value.SkipFadeObjects(objectType);
			}
		}

		/// 指定の名前のグラフィックオブジェクトを、フェードアウト中のものを含めて検索
		AdvGraphicObject FindObjectIncludeFadeOuting(string targetName)
		{
			AdvGraphicObject obj = FindObject(targetName);
			if (obj != null) return obj;
			foreach (var keyValue in Groups)
			{
				obj = keyValue.Value.FindFadeOutingObject(targetName);
				if (obj != null) return obj;
			}
			return null;
		}

		//指定のカメラのキャプチャ画像を撮って、それを表示するオブジェクトを作成
		internal void CreateCaptureImageObject(string name, string cameraName, string layerName)
		{
			AdvGraphicLayer layer = FindLayer(layerName);
			if (layer == null)
			{
				Debug.LogError(layerName + " is not layer name");
				return;
			}

			CameraRoot cameraRoot = Engine.CameraManager.FindCameraRoot(cameraName);
			if (cameraRoot==null)
			{
				Debug.LogError(cameraName + " is not camera name");
				return;
			}

			AdvGraphicInfo grapic = new AdvGraphicInfo(AdvGraphicInfo.TypeCapture, name, AdvGraphicInfo.FileType2D);
			AdvGraphicObject obj = layer.GetObjectCreateIfMissing(name, grapic);
			obj.InitCaptureImage(grapic, cameraRoot.LetterBoxCamera.CachedCamera);
		}

		/// <summary>
		/// クリックイベントを削除
		/// </summary>
		internal void RemoveClickEvent(string name)
		{
			AdvGraphicObject obj = FindObject(name);
			if (obj == null) return;

			IAdvClickEvent clickEvent = obj.gameObject.GetComponentInChildren<IAdvClickEvent>();
			if (clickEvent == null) return;

			clickEvent.RemoveClickEvent();
		}

		/// <summary>
		/// 指定の名前のスプライトにクリックイベントを設定
		/// </summary>
		/// <param name="name"></param>
		internal void AddClickEvent(string name, bool isPolygon, StringGridRow row, UnityAction<BaseEventData> action)
		{
			AdvGraphicObject obj = FindObject(name);
			if (obj == null)
			{
				Debug.LogError("can't find Graphic object" + name);
				return;
			}

			IAdvClickEvent clickEvent = obj.gameObject.GetComponentInChildren<IAdvClickEvent>();
			if (clickEvent == null)
			{
				Debug.LogError("can't find IAdvClickEvent Interface in " + name);
				return;
			}

			clickEvent.AddClickEvent(isPolygon, row, action);
		}

		//IBinaryIO用のセーブキー
		public string SaveKey { get { return "AdvGraphicManager"; } }

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public virtual void OnWrite(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(isEventMode);
			writer.Write(Groups.Count);
			foreach (var keyValue in Groups)
			{
				writer.Write((int)keyValue.Key);
				writer.WriteBuffer(keyValue.Value.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		public virtual void OnRead(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			this.isEventMode = reader.ReadBoolean();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				AdvLayerSettingData.LayerType type = (AdvLayerSettingData.LayerType)reader.ReadInt32();
				reader.ReadBuffer(Groups[type].Read);
			}
		}
	}
}
