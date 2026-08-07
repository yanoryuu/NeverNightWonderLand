// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UtageExtensions;
namespace Utage
{

	/// <summary>
	/// グラフィックのレイヤー管理の基底クラス
	/// </summary>
	public abstract class AdvGraphicLayer : MonoBehaviour
		, IAdvGraphicLayer
		, IAdvEngineGetter
	{
		public AdvLayerSettingData SettingData { get; protected set; }
		abstract internal AdvLayerSettingData.LayerType LayerType { get; }

		public AdvEngine Engine
		{
			get
			{
				if (Manager == null)
				{
					return null;
				}
				return Manager.Engine;
			}
		}
		public AdvEngine AdvEngineGetter => Engine;


		public AdvGraphicManager Manager { get; protected set; }

		protected Transform RootObjects { get; set; }
		public AdvGraphicObject DefaultObject { get; protected set; }
		public Dictionary<string, AdvGraphicObject> CurrentGraphics
		{
			get { return currentGraphics; }
		}
		Dictionary<string, AdvGraphicObject> currentGraphics = new Dictionary<string, AdvGraphicObject>();

		//フェードアウト用のオブジェクト
		List<AdvGraphicObject> fadeOutingObjets = new List<AdvGraphicObject>();

		public Canvas Canvas { get; protected set; }

		abstract internal void Init(AdvGraphicManager manager);

		//　キャンバスのRectTransformをリセットして初期状態に
		abstract internal void ResetCanvasRectTransform();


		internal void Add(AdvGraphicObject obj)
		{
			CurrentGraphics.Add(obj.name,obj);
		}

		internal void Remove(AdvGraphicObject obj)
		{
			if (CurrentGraphics.ContainsValue(obj))
			{
				CurrentGraphics.Remove(obj.name);
			}
			if (DefaultObject == obj)
			{
				DefaultObject = null;
			}
		}

		//オブジェクトを描画する
		internal AdvGraphicObject Draw(string name, AdvGraphicOperationArg arg )
		{
			AdvGraphicObject obj = GetObjectCreateIfMissing(name, arg.Graphic);
			obj.Loader.LoadGraphic(arg.Graphic, () =>
			{
				obj.Draw(arg, arg.GetSkippedFadeTime(Engine));
			});
			return obj;
		}

		//デフォルトオブジェクトとして描画する
		internal AdvGraphicObject DrawToDefault(string name, AdvGraphicOperationArg arg)
		{
			bool changeObject = false;
			bool keepPosition = false;
			Vector3 oldPosition = Vector3.zero;

			if (DefaultObject != null && DefaultObject.LastResource != null)
			{
				if (DefaultObject.name != name)
				{
					//デフォルトオブジェクトの名前が違うなら、そのオブジェクトは変更
					//場所も保持しない
					changeObject = true;
				}
				else
				{
					if (CheckFailedCrossFade(arg))
					{
						//クロスフェードに失敗するだけの場合
						//場所は保持する
						changeObject = true;
						keepPosition = true;
						oldPosition = DefaultObject.transform.localPosition;
					}
					else
					{
						//クロスフェードできるならオブジェクトの変更を行わない
						changeObject = false;
					}
				}
			}

			AdvGraphicObject swapFadeObject = null;
			if (changeObject)
			{
				//すでにあるオブジェクトを消す準備
				swapFadeObject = DefaultObject;
				Remove(DefaultObject);
			}
			DefaultObject = Draw(name,arg);
			if (changeObject)
			{
				//すでにあるオブジェクトをフェードアウトしてクロスフェードとする
				DefaultObject.AddSwapFadeObject(swapFadeObject);
				float fadeTime = arg.GetSkippedFadeTime(Engine);
				if (LayerType == AdvLayerSettingData.LayerType.Bg)
				{
					StartCoroutine(CoDelayOut(swapFadeObject,fadeTime));
				}
				else
				{
					swapFadeObject.FadeOut(fadeTime);
				}
			}

			//前の場所を保持する
			if (keepPosition && !Manager.IgnoreKeepPositionOnCrossFade)
			{
				DefaultObject.transform.localPosition = oldPosition;
			}
			return DefaultObject;
		}
		
		IEnumerator CoDelayOut(AdvGraphicObject obj, float delay)
		{
			yield return Engine.Time.WaitForSeconds(delay);
			if(obj!=null) obj.Clear();
		}

		protected virtual bool CheckFailedCrossFade(AdvGraphicOperationArg arg)
		{
			if (arg.Graphic.CheckFailedCrossFade(DefaultObject.LastResource))
			{
				return true;
			}
			return DefaultObject.TargetObject.CheckFailedCrossFade(arg.Graphic);
		}

		//指定の名前のオブジェクトを取得、なければ作成
		internal AdvGraphicObject GetObjectCreateIfMissing(string name, AdvGraphicInfo grapic)
		{
			if (grapic == null) 
			{
				Debug.LogError ( name + " grapic is null");
				return null;
			}
			AdvGraphicObject obj;
			if (!currentGraphics.TryGetValue(name, out obj))
			{
				//まだ作成されてないから作る
				obj = CreateObject(name, grapic);
			}
			return obj;
		}

		//描画オブジェクトを作成
		protected virtual AdvGraphicObject CreateObject(string name, AdvGraphicInfo grapic, bool resetOnFirst = true)
		{
			AdvGraphicObject obj;
			//IAdvGraphicObjectがAddComponentされたプレハブをリソースに持つかチェック
			GameObject prefab;
			if (grapic.TryGetAdvGraphicObjectPrefab(out prefab))
			{
				//プレハブからリソースオブジェクトを作成して返す
				GameObject go = GameObject.Instantiate(prefab);
				go.name = name;
				obj = go.GetComponent<AdvGraphicObject>();
				RootObjects.AddChild(obj.gameObject);
			}
			else
			{
				obj = RootObjects.AddChildGameObjectComponent<AdvGraphicObject>(name);
			}
			obj.Init(this, grapic);

			//最初の描画時は位置をリセットする
			if (resetOnFirst && currentGraphics.Count == 0)
			{
				this.ResetCanvasRectTransform();
			}

			Add(obj);
			return obj;
		}
		
		public void ChangeLayer(bool isDefaultObject, AdvGraphicObject targetObject, AdvChangeLayerRepositionType repositionType, float fadeOutTime)
		{
			if (isDefaultObject)
			{
				if (DefaultObject != null)
				{
					FadeOut(DefaultObject.name, fadeOutTime);
				}
				DefaultObject = targetObject;
			}

			Transform targetObjectTransform = targetObject.transform;
			//新しいレイヤーに移動
			switch (repositionType)
			{
				case AdvChangeLayerRepositionType.KeepLocal:
					//ローカル座標を保持
					targetObject.transform.SetParent(this.transform,false);
					break;
				case AdvChangeLayerRepositionType.ResetLocal:
					//リセット
					targetObject.transform.SetParent(this.transform);
					targetObjectTransform.localPosition = Vector3.zero;
					targetObjectTransform.localEulerAngles = Vector3.zero;
					targetObjectTransform.localScale = Vector3.one;
					break;
				default:
				case AdvChangeLayerRepositionType.KeepGlobal:
					//何もしない
					targetObject.transform.SetParent(this.transform);  
					break;
			}
			targetObject.gameObject.layer = this.gameObject.layer;
			targetObject.Layer = this;
			this.Add(targetObject);
		}

		//フェードアウト
		internal void FadeOut(string name, float fadeTime)
		{
			AdvGraphicObject obj;
			if (currentGraphics.TryGetValue(name, out obj))
			{
				obj.FadeOut(fadeTime);
				fadeOutingObjets.Add(obj);
				Remove(obj);
			}
		}


		internal void FadeOutAll(float fadeTime)
		{
			List<AdvGraphicObject> values = new List<AdvGraphicObject>(currentGraphics.Values);
			foreach (var obj in values)
			{
				obj.FadeOut(fadeTime);
				fadeOutingObjets.Add(obj);
			}
			currentGraphics.Clear();
			DefaultObject = null;
		}

		//指定名のパーティクルを非表示にする
		internal void FadeOutParticle(string targetName,AdvParticleStopType stopType)
		{
			AdvGraphicObject obj;
			if (currentGraphics.TryGetValue(targetName, out obj))
			{
				FadOutParticle(obj,stopType);
			}
		}

		//パーティクルを全て非表示にする
		internal void FadeOutAllParticle(AdvParticleStopType stopType)
		{
			List<AdvGraphicObject> values = new List<AdvGraphicObject>(currentGraphics.Values);
			foreach (var obj in values)
			{
				FadOutParticle(obj,stopType);
			}
		}

		void FadOutParticle(AdvGraphicObject obj,AdvParticleStopType stopType)
		{
			AdvGraphicObjectParticle particle = obj.TargetObject as AdvGraphicObjectParticle; 
			if (particle!=null)
			{
				particle.Stop(stopType);
				fadeOutingObjets.Add(obj);
				Remove(obj);
			}
		}


		//パーティクルを探索する
		public AdvGraphicObject FindParticle(string targetName)
		{
			AdvGraphicObject obj = Find(targetName);
			if (obj != null && obj.TargetObject is AdvGraphicObjectParticle)
			{
				return obj;
			}
			return null;
		}

		//クリア処理
		internal void Clear()
		{
			List<AdvGraphicObject> values = new List<AdvGraphicObject>(currentGraphics.Values);
			foreach (var obj in values)
			{
				obj.Clear();
			}
			currentGraphics.Clear();
			foreach (var obj in fadeOutingObjets)
			{
				if (obj != null)
				{
					obj.Clear();
				}
			}
			DefaultObject = null;
		}

		//デフォルトグラフィックオブジェクトの名前が指定名と同じかチェック
		internal bool IsEqualDefaultGraphicName(string name)
		{
			if (DefaultObject!=null)
			{
				return DefaultObject.name == name;
			}
			return false;
		}

		//指定名のオブジェクトがあるか
		internal bool Contains(string name)
		{
			return currentGraphics.ContainsKey(name);
		}

		//指定名のオブジェクトがあれば返す
		internal AdvGraphicObject Find(string name)
		{
			AdvGraphicObject obj;
			if(currentGraphics.TryGetValue(name,out obj))
			{
				return obj;
			}
			return null;
		}

		//指定名のオブジェクトがあれば返す
		internal AdvGraphicObject FindFadeOutingObject(string name)
		{
			foreach (var obj in fadeOutingObjets)
			{
				if (obj != null && obj.name == name)
				{
					return obj;
				}
			}
			return null;
		}


		internal void AddAllGraphics(List<AdvGraphicObject> graphics)
		{
			graphics.AddRange(currentGraphics.Values);
		}

		//ロード中かチェック
		internal bool IsLoading
		{
			get
			{
				//プレハブのロードをしている場合に起きうるロード処理のチェック
				if (LoadingPrefabCount>0) return true;
				
				foreach (var keyValue in currentGraphics)
				{
					if (keyValue.Value == null)
					{
						Debug.LogError("");
					}
					if (keyValue.Value.Loader.IsLoading) return true;
				}
				return false;
			}
		}

		//プレハブの場合はロード待ちが入るのでその対策
		internal int LoadingPrefabCount { get; set; }
		string DefaultObjectNameOnPostLoadPrefabs { get; set; } = "";

		internal bool IsFading
		{
			get
			{
				foreach (var keyValue in currentGraphics)
				{
					if (keyValue.Value.IsFading)
					{
						return true;
					}
				}

				fadeOutingObjets.RemoveAll(x => x == null);
				return fadeOutingObjets.Count>0;
			}
		}

		internal void SkipFade()
		{
			foreach (var keyValue in currentGraphics)
			{
				keyValue.Value.SkipFade();
			}
			foreach (var obj in fadeOutingObjets)
			{
				if (obj != null)
				{
					obj.Clear();
				}
			}
			fadeOutingObjets.Clear();
		}
		
				
		//指定のタイプのオブジェクトのみすべてフェードアウトして削除する
		public void FadeOutAllObjects(AdvGraphicObjectType objectType, float fadeTime)
		{
			List<AdvGraphicObject> values = new List<AdvGraphicObject>(currentGraphics.Values);
			foreach (var obj in values)
			{
				if( !obj.CheckType(objectType) ) continue;
				obj.FadeOut(fadeTime);
				fadeOutingObjets.Add(obj);
				Remove(obj);
			}
		}


		//指定のタイプのオブジェクトのうちどれかがフェード中かチェック
		public bool IsFadingObjects(AdvGraphicObjectType objectType)
		{
			foreach (var keyValue in currentGraphics)
			{
				var obj = keyValue.Value;
				if (!obj.CheckType(objectType)) continue;
				if (obj.IsFading) return true;
			}
			fadeOutingObjets.RemoveAll(x => x == null);
			return fadeOutingObjets.Exists(x=>x.CheckType(objectType));
		}

		//指定のタイプのオブジェクト全てのフェードをスキップ
		public void SkipFadeObjects(AdvGraphicObjectType objectType)
		{
			foreach (var keyValue in currentGraphics)
			{
				var obj = keyValue.Value;
				if (!obj.CheckType(objectType)) continue;
				keyValue.Value.SkipFade();
			}
			foreach (var obj in fadeOutingObjets)
			{
				if (obj != null && obj.CheckType(objectType))
				{
					obj.Clear();
				}
			}
		}

		protected virtual void DestroyAllAnimations()
		{
			foreach (var advAnimationPlayer in this.GetComponents<AdvAnimationPlayer>())
			{
				advAnimationPlayer.DestroyComponentImmediate();
				Destroy(advAnimationPlayer);
			}
			foreach (var tweenPlayer in this.GetComponents<AdvITweenPlayer>())
			{
				Destroy(tweenPlayer);
			}
			foreach (var tween in this.GetComponents<iTween>())
			{
				Destroy(tween);
			}
		}


		const int Version0 = 0;
		const int Version = 1;
		//セーブデータ用のバイナリ書き込み
		public virtual void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.WriteLocalTransform(this.transform);
			writer.WriteBuffer((x)=>AdvITweenPlayer.WriteSaveData (x,this.gameObject));
			writer.WriteBuffer((x) => AdvAnimationPlayer.WriteSaveData(x, this.gameObject));

			//いったんセーブ対象のオブジェクトのリストを作成し、描画順にソートする
			var targetObjects = ListPool<AdvGraphicObject>.Get();
			foreach (var keyValue in CurrentGraphics)
			{
				if (!keyValue.Value.EnableSaveObject())
				{
					continue;
				}
				targetObjects.Add(keyValue.Value);
			}
			targetObjects.Sort(SortByRenderOrder);

			writer.Write(targetObjects.Count);
			foreach (var target in targetObjects)
			{
				writer.Write(target.name);
				writer.WriteBuffer(target.LastResource.OnWrite);
				writer.WriteBuffer(target.Write);
			}
			writer.Write(DefaultObject == null ? "" : DefaultObject.name);
			
			ListPool<AdvGraphicObject>.Release(targetObjects);
			return;
			int SortByRenderOrder(AdvGraphicObject a, AdvGraphicObject b)
			{
				return a.rectTransform.GetSiblingIndex() - b.rectTransform.GetSiblingIndex();
			}
		}

		//セーブデータ用のバイナリ読み込み
		public virtual void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}
			DestroyAllAnimations();
			reader.ReadLocalTransform(this.transform);
			if (version > Version0)
			{
				reader.ReadBuffer((x) => AdvITweenPlayer.ReadSaveData(x,this.gameObject,true, Manager.PixelsToUnits, Engine.Time.Unscaled));
				reader.ReadBuffer((x) => AdvAnimationPlayer.ReadSaveData(x, this.gameObject, Engine));
			}
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string key = reader.ReadString();
				AdvGraphicInfo graphic = null;
				reader.ReadBuffer(x => graphic = AdvGraphicInfo.ReadGraphicInfo(Engine, x));
				byte[] buffer = reader.ReadBuffer();
				if (!graphic.IsOverridePrefab())
				{
					AdvGraphicObject obj = CreateObject(key, graphic,false);
					obj.Read(buffer, graphic);
				}
				else
				{
					LoadPrefab(buffer, key, graphic);
				}
			}
			string defaultObjectName = reader.ReadString();
			if (LoadingPrefabCount>0)
			{
				DefaultObjectNameOnPostLoadPrefabs = defaultObjectName;
			}
			else
			{
				DefaultObject = Find(defaultObjectName);
			}
		}
		
		//プレハブの場合はロード待ちしてからオブジェクトを作成
		protected virtual void LoadPrefab(byte[] buffer, string key, AdvGraphicInfo graphic)
		{
			AdvGraphicLoader loader = this.gameObject.AddComponent<AdvGraphicLoader>();
			++LoadingPrefabCount;
			loader.LoadGraphic(graphic, () =>
			{
				AdvGraphicObject obj = CreateObject(key, graphic,false);
				obj.Read(buffer, graphic);
				Destroy(loader);
				--LoadingPrefabCount;
				if (LoadingPrefabCount <= 0)
				{
					OnPostLoadPrefabs();
				}
			});
		}

		protected virtual void OnPostLoadPrefabs()
		{
			DefaultObject = Find(DefaultObjectNameOnPostLoadPrefabs);
			DefaultObjectNameOnPostLoadPrefabs = "";
		}
	}
}
