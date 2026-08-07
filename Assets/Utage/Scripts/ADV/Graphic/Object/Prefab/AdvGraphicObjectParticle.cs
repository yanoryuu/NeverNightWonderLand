// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// フェード切り替え機能つきのスプライト表示
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectParticle")]
	public class AdvGraphicObjectParticle : AdvGraphicObjectPrefabBase
	{
		protected ParticleSystem[] ParticleArray { get; set; }

		//初期化処理
		public override void Init(AdvGraphicObject parentObject)
		{
			base.Init(parentObject);
			parentObject.gameObject.AddComponent<ParticleAutomaticDestroyer>();
		}

		//********描画時のリソース変更********//
		protected override void ChangeResourceOnDrawSub(AdvGraphicInfo grapic)
		{
			SetSortingOrder(this.Layer.Canvas.sortingOrder, this.Layer.Canvas.sortingLayerName);
		}

		protected void SetSortingOrder(int sortingOrder, string sortingLayerName)
		{
			ParticleArray = currentObject.GetComponentsInChildren<ParticleSystem>(true);
			foreach (var item in ParticleArray)
			{
				Renderer render = item.GetComponent<Renderer>();
				render.sortingOrder += sortingOrder;
				render.sortingLayerName += sortingLayerName;
			}
		}

		//エフェクト用の色が変化したとき
		public override void OnEffectColorsChange(AdvEffectColor color)
		{
			if (currentObject)
			{
			}
		}

		void FadInOut(ParticleSystem particle, float alpha)
		{
			var mainMudle = particle.main;
			var startColor = mainMudle.startColor;
			var color = startColor.color;
			color.a = alpha;
			mainMudle.startColor = color;
		}

		internal void Stop(AdvParticleStopType stopType)
		{
			IAdvGraphicObjectParticleController controller =
				GetComponentInChildren<IAdvGraphicObjectParticleController>();
			if (controller == null)
			{
				//コントローラー未設定
				switch (stopType)
				{
					case AdvParticleStopType.StopEmitting:
						this.GetComponentInChildren<ParticleSystem>().Stop(true,ParticleSystemStopBehavior.StopEmitting);
						break;
					case AdvParticleStopType.Clear:
					default:
						//即座に消しておわり
						ParentObject.FadeOut(0);
						break;
				}
			}
			else
			{
				controller.Stop(stopType);
			} 
		}
		
		internal bool EnableSave()
		{
			IAdvGraphicObjectParticleController controller =
				GetComponentInChildren<IAdvGraphicObjectParticleController>();
			if (controller == null) return true;

			return controller.EnableSave;
		}
	}
}
