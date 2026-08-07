// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
	//喋っていないキャラクターをグレーアウトする処理
	//AdvEngineのOnPageTextChangeから呼び出す、このコンポーネントの同名メソッドを登録すると使えるようになる
	[AddComponentMenu("Utage/ADV/Extra/AdvCharacterGrayOutController")]
	public class AdvCharacterGrayOutController : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField]
		protected AdvEngine engine;

		//ライティング（グレーアウトしない）するフラグ
		[System.Flags]
		public enum LightingMask
		{
			Talking = 0x1,					//しゃべっているキャラは
			NewCharacerInPage = 0x1 << 1,	//ページ内の新しいキャラクター
			NoChanageIfTextOnly = 0x1 << 2,	//テキストのみ表示のときは、変化しない
		}

		[SerializeField, EnumFlags]
		LightingMask mask = LightingMask.Talking;
		public LightingMask Mask
		{
			get { return mask; }
			set { mask = value; }
		}

		//グレーアウトいないほうの色。白以外の色も任意に設定可能
		[SerializeField]
		Color mainColor = Color.white;
		public Color MainColor
		{
			get { return mainColor; }
			set { mainColor = value; }
		}

		//グレーアウトするほうの色　グレー以外の色も任意に設定可能
		[SerializeField]
		Color subColor = Color.gray;
		public Color SubColor
		{
			get { return subColor; }
			set { subColor = value; }
		}

		//フェード時間
		[SerializeField]
		float fadeTime = 0.2f;
		public float FadeTime
		{
			get { return fadeTime; }
			set { fadeTime = value; }
		}

		//グレーアウトしないキャラクター名のリスト
		public List<string> NoGrayoutCharacters
		{
			get { return noGrayoutCharacters; }
			set { noGrayoutCharacters = value; }
		}
		[SerializeField]
		List<string> noGrayoutCharacters = new List<string>();


		bool isChanged = false;
		List<AdvGraphicLayer> pageBeginLayer;

		//描画順を変更する
		public bool EnableChangeOrder { get { return enableChangeOrder; } }
		[SerializeField]
		bool enableChangeOrder = false;

		//描画順を変更する場合のオフセット値
		public int OrderOffset { get { return orderOffset; } }
		[SerializeField]
		public int orderOffset = 100;
		Dictionary<AdvGraphicLayer, int> defaultOrders = new Dictionary<AdvGraphicLayer, int>();

		//テキストに変更があった場合
		void Awake()
		{
			if (Engine != null)
			{
				Engine.Page.OnBeginPage.AddListener(OnBeginPage);
				Engine.Page.OnChangeText.AddListener(OnChangeText);
			}
		}


		//ページの冒頭
		void OnBeginPage(AdvPage page)
		{
			this.pageBeginLayer = page.Engine.GraphicManager.CharacterManager.AllGraphicsLayers();
			if (this.mask == 0)
			{
				//表示なしなのでリセット
				if (isChanged)
				{
					foreach (AdvGraphicLayer layer in pageBeginLayer)
					{
						ChangeColor(layer, MainColor);
					}
					isChanged = false;
				}
			}
		}

		//テキストに変更があった場合
		void OnChangeText(AdvPage page)
		{
			if (this.mask == 0) return;
			isChanged = true;
			AdvEngine engine = page.Engine;

			//テキストのみ表示で、前のキャラをそのまま表示
			if (string.IsNullOrEmpty(page.CharacterLabel) && (Mask & LightingMask.NoChanageIfTextOnly) == LightingMask.NoChanageIfTextOnly)
			{
				return;
			}

			List<AdvGraphicLayer> layers = engine.GraphicManager.CharacterManager.AllGraphicsLayers();
			foreach (AdvGraphicLayer layer in layers)
			{
				bool isLighting = IsLightingCharacter(page, layer);
				ChangeColor(layer, isLighting ? MainColor : SubColor);
				ChangeOrder(layer, isLighting);
			}
		}


		void ChangeOrder(AdvGraphicLayer layer, bool isLighting)
		{
			if (!EnableChangeOrder) return;
			int defaultOrder;
			if (!defaultOrders.TryGetValue(layer, out defaultOrder))
			{
				defaultOrder = layer.Canvas.sortingOrder;
				defaultOrders.Add(layer, layer.Canvas.sortingOrder);
			}
			layer.Canvas.sortingOrder = isLighting ? defaultOrder + orderOffset : defaultOrder;
		}

		//強調表示（グレーアウト無視）するか
		bool IsLightingCharacter(AdvPage page, AdvGraphicLayer layer)
		{
			if (layer.DefaultObject == null)
			{
				//キャラクターがいない
				return false;
			}
			//しゃべっているキャラ
			if( (Mask & LightingMask.Talking) == LightingMask.Talking)
			{
				if (layer.DefaultObject.name == page.CharacterLabel) return true;
			}

			//ページ内の新規キャラ
			if ((Mask & LightingMask.NewCharacerInPage) == LightingMask.NewCharacerInPage)
			{
				if (pageBeginLayer.Find(x => (x !=null && x.DefaultObject!=null) && (x.DefaultObject.name == layer.DefaultObject.name) ) == null) return true;
			}

			//名前指定のあるキャラ
			if (NoGrayoutCharacters.Exists(x=>x== layer.DefaultObject.name))
			{
				return true;
			}
			return false;
		}

		//カラーを取得
		void ChangeColor(AdvGraphicLayer layer, Color color)
		{
			foreach ( var keyValue in layer.CurrentGraphics )
			{
				AdvGraphicObject obj = keyValue.Value;
				AdvEffectColor effect = obj.gameObject.GetComponent<AdvEffectColor>();
				if (effect == null) continue;

				if (FadeTime > 0)
				{
					Color from = effect.CustomColor;
					StartCoroutine(FadeColor(effect, from, color));
				}
				else
				{
					effect.CustomColor = color;
				}
			}
		}

		IEnumerator FadeColor(AdvEffectColor effect, Color from, Color to)
		{
			float elapsed = 0f;
			while(true)
			{
				yield return new WaitForEndOfFrame();
				elapsed += Engine.Time.DeltaTime;
				if (elapsed >= fadeTime)
				{
					elapsed = fadeTime;
				}
				effect.CustomColor = Color.Lerp(from, to, elapsed / FadeTime);
				if (elapsed >= fadeTime) yield break;
			}
		}
	}
}

