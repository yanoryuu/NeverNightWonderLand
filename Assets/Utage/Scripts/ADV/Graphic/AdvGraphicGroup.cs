// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// グラフィックオブジェクトを、キャラクターやBGなどのグループ単位で管理のためのスーパークラス
	/// </summary>
	public class AdvGraphicGroup
	{
		internal AdvLayerSettingData.LayerType Type { get; private set; }
		internal AdvGraphicLayer DefaultLayer { get; set; }
		protected AdvGraphicManager manager;

		//すべての管理対象のレイヤー
		public Dictionary<string, AdvGraphicLayer> Layers => layers;
		Dictionary<string, AdvGraphicLayer> layers = new Dictionary<string, AdvGraphicLayer>();

		//起動時の初期化
		public AdvGraphicGroup(AdvLayerSettingData.LayerType type, AdvLayerSetting setting, AdvGraphicManager manager)
		{
			this.Type = type;
			this.manager = manager;
			foreach (var item in manager.LayerList)
			{
				if (item.LayerType == type)
				{
					item.Init(manager);
					AddLayer(item.name, item);
				}
			}
			foreach (var item in setting.List)
			{
				if (item.Type == type)
				{
					//5.6対策でRectTransformを指定したnewが必要
					var go = new GameObject(item.Name, typeof(RectTransform), typeof(Canvas));
					manager.transform.AddChild(go);
					AdvGraphicLayerDefault layer = go.AddComponent<AdvGraphicLayerDefault>();
					layer.Init(manager);
					layer.Init(item);
					AddLayer( item.Name, layer);
				}
			}
		}

		void AddLayer(string name, AdvGraphicLayer layer )
		{
			if (layers.ContainsKey(name))
			{
				Debug.LogError(name + " is already exists in layers");
				return;
			}
			layers.Add(name, layer);
			if (layer.SettingData.IsDefault) DefaultLayer = layer;
		}

		//指定のオブジェクトのレイヤー（キャンバス）をAdvEngineに埋め込み
		public void EmbedLayer(AdvGraphicLayer layer)
		{
			layer.Init(manager);
			string name = layer.gameObject.name;
			if (layers.ContainsKey(name))
			{
				layers[name] = layer;
			}
			else
			{
				AddLayer(name, layer);
			}
		}
		//指定のレイヤーを削除
		public void RemoveLayer(AdvGraphicLayer layer)
		{
			string name = layer.gameObject.name;
			if (layers.ContainsKey(name))
			{
				layers.Remove(name);
			}
			else
			{
				Debug.LogError( name + " is not find");
			}
		}


		//クリア
		public virtual void Clear()
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.Clear();
			}
		}

		public void DestroyAll()
		{
			foreach (var keyValue in layers)
			{
				var layer = keyValue.Value;
				layer.Clear();
				//動的に作成したものだけ破壊
				if (layer is AdvGraphicLayerDefault)
				{
					GameObject.Destroy(keyValue.Value.gameObject);
				}
			}
			layers.Clear();
			DefaultLayer = null;
		}

		//表示する
		public virtual AdvGraphicObject Draw(string layerName, string name, AdvGraphicOperationArg arg)
		{
			return FindLayerOrDefault(layerName).Draw(name, arg);
		}

		//デフォルトレイヤーのデフォルトオブジェクトとして表示する
		public virtual AdvGraphicObject DrawToDefault(string name, AdvGraphicOperationArg arg)
		{
			return DefaultLayer.DrawToDefault(name, arg);
		}

		//キャラクターオブジェクトとして、特殊な表示をする
		public AdvGraphicObject DrawCharacter(string layerName, string name, AdvGraphicOperationArg arg)
		{
			//既に同名のグラフィックがあるなら、そのレイヤーを取得
			AdvGraphicLayer oldLayer = null;
			foreach ( var keyValue in layers)
			{
				if (keyValue.Value.IsEqualDefaultGraphicName(name))
				{
					oldLayer = keyValue.Value;
					break;
				}
			}

			//レイヤー名の指定がある場合、そのレイヤーを探す
			AdvGraphicLayer layer = FindLayer(layerName);
			if (layer == null)
			{
				//レイヤーがない場合は、旧レイヤーかデフォルトレイヤーを使う
				layer = (oldLayer == null) ? DefaultLayer : oldLayer;
			}

			//レイヤー変更があるか
			bool changeLayer = (oldLayer != layer && oldLayer != null);

			//レイヤー変更ないなら、描画しておわり
			if (!changeLayer)
			{
				//レイヤー上にデフォルトオブジェクトとして表示
				return layer.DrawToDefault(name, arg);
			}

			Vector3 oldScale = Vector3.one;
			Vector3 oldPosition = Vector3.zero;
			Quaternion oldRotation = Quaternion.identity;
			//レイヤーが変わる場合は、昔のほうを消す
			AdvGraphicObject oldObj;
			if (oldLayer.CurrentGraphics.TryGetValue(name, out oldObj))
			{
				oldScale = oldObj.rectTransform.localScale;
				oldPosition = oldObj.rectTransform.localPosition;
				oldRotation = oldObj.rectTransform.localRotation;
				oldLayer.FadeOut(name, arg.GetSkippedFadeTime(manager.Engine));
			}

			//レイヤー上にデフォルトオブジェクトとして表示
			AdvGraphicObject obj = layer.DrawToDefault(name, arg);
			//ローカルTransform値を引き継ぐ処理
			if (!manager.ResetCharacterTransformOnChangeLayer)
			{
				obj.rectTransform.localScale = oldScale;
				obj.rectTransform.localPosition = oldPosition;
				obj.rectTransform.localRotation = oldRotation;
			}
			return obj;
		}

		//現在描画オブジェクトのある全てのレイヤー
		public List<AdvGraphicLayer> AllGraphicsLayers()
		{
			List<AdvGraphicLayer> list = new List<AdvGraphicLayer>();
			foreach (var keyValue in layers)
			{
				if (keyValue.Value.CurrentGraphics.Count>0)
				{
					list.Add(keyValue.Value);
				}
			}
			return list;
		}


		//指定名のオブジェクトを非表示（フェードアウト）する
		public virtual void FadeOut(string name, float fadeTime)
		{
			AdvGraphicLayer layer = FindLayerFromObjectName(name);
			if (layer != null) layer.FadeOut(name, fadeTime);
		}

		//全オブジェクトを非表示（フェードアウト）する
		public virtual void FadeOutAll(float fadeTime)
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.FadeOutAll(fadeTime);
			}
		}

		//指定名のパーティクルを非表示にする
		public void FadeOutParticle(string targetName,AdvParticleStopType stopType)
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.FadeOutParticle(targetName,stopType);
			}
		}

		//パーティクルを全て非表示にする
		public void FadeOutAllParticle(AdvParticleStopType stopType)
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.FadeOutAllParticle(stopType);
			}
		}
		
		//パーティクルを探索する
		public AdvGraphicObject FindParticle(string targetName)
		{
			foreach (var keyValue in layers)
			{
				AdvGraphicObject obj = keyValue.Value.FindParticle(targetName);
				if (obj != null)
				{
					return obj;
				}
			}
			return null;
		}


		//指定名グラフィックオブジェクトを持つか
		public bool IsContians(string layerName, string name)
		{
			if (string.IsNullOrEmpty(layerName))
			{
				return FindObject(name) !=null;
			}
			else
			{
				AdvGraphicLayer layer = FindLayer(layerName);
				return (layer != null && layer.Find(name) != null);
			}
		}

		//指定の名前のグラフィックオブジェクトを持つレイヤーを探す
		public AdvGraphicLayer FindLayerFromObjectName(string name)
		{
			foreach (var keyValue in layers)
			{
				if (keyValue.Value.Contains(name)) return keyValue.Value;
			}
			return null;
		}

		//指定の名前のレイヤーを探す
		public AdvGraphicLayer FindLayer(string name)
		{
			AdvGraphicLayer layer;
			if (layers.TryGetValue(name, out layer))
				return layer;
			return null;
		}

		//指定の名前のレイヤーを探す（見つからなかったらデフォルト）
		public AdvGraphicLayer FindLayerOrDefault(string name)
		{
			AdvGraphicLayer layer = FindLayer(name);
			if (layer == null)
			{
				return DefaultLayer;
			}
			return layer;
		}

		//指定の名前のグラフィックオブジェクトをを探す
		public AdvGraphicObject FindObject(string name)
		{
			foreach (var keyValue in layers)
			{
				AdvGraphicObject obj = keyValue.Value.Find(name);
				if (obj != null) return obj;
			}
			return null;
		}

		public AdvGraphicObject FindFadeOutingObject(string name)
		{
			foreach (var keyValue in layers)
			{
				AdvGraphicObject obj = keyValue.Value.FindFadeOutingObject(name);
				if (obj != null) return obj;
			}
			return null;
		}

		//全てのグラフィックオブジェクトを取得
		public List<AdvGraphicObject> AllGraphics()
		{
			List<AdvGraphicObject> allGraphics = new List<AdvGraphicObject>();
			foreach (var keyValue in layers)
			{
				keyValue.Value.AddAllGraphics(allGraphics);
			}
			return allGraphics;
		}

		public void AddAllGraphics(List<AdvGraphicObject> graphics)
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.AddAllGraphics(graphics);
			}
		}

		//ロード中かチェック
		public bool IsLoading
		{
			get
			{
				foreach (var keyValue in layers)
				{
					if (keyValue.Value.IsLoading) return true;
				}
				return false;
			}
		}
		
		public bool IsFading
		{
			get
			{
				foreach (var keyValue in layers)
				{
					if (keyValue.Value.IsFading) return true;
				}
				return false;
			}
		}

		public void SkipFade()
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.SkipFade();
			}
		}
				
		//指定のタイプのオブジェクトのみ全て削除
		public void FadeOutAllObjects(AdvGraphicObjectType objectType, float fadeTime)
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.FadeOutAllObjects(objectType,fadeTime);
			}
		}

		//指定のタイプのオブジェクトのうちどれかがフェード中かチェック
		public bool IsFadingObjects(AdvGraphicObjectType objectType)
		{
			foreach (var keyValue in layers)
			{
				if (keyValue.Value.IsFadingObjects(objectType)) return true;
			}
			return false;
		}

		//指定のタイプのオブジェクト全てのフェードをスキップ
		public void SkipFadeObjects(AdvGraphicObjectType objectType)
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.SkipFadeObjects(objectType);
			}
		}


		// 全レイヤーのリセット
		public void ResetAllLayerRectTransform()
		{
			foreach (var keyValue in layers)
			{
				keyValue.Value.ResetCanvasRectTransform();
			}
		}

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(layers.Count);
			foreach (var keyValue in layers)
			{
				writer.Write(keyValue.Key);
				writer.WriteBuffer(keyValue.Value.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string layerName = reader.ReadString();
				AdvGraphicLayer layer = FindLayer(layerName);
				if (layer != null)
				{
					reader.ReadBuffer(layer.Read);
				}
				else
				{
					reader.SkipBuffer();
				}
			}
		}
	}
}