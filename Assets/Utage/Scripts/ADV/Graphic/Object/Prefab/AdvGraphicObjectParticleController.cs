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

	// パーティクルのコントローラー
	[AddComponentMenu("Utage/ADV/GraphicObject/AdvGraphicObjectParticleController")]
	public class AdvGraphicObjectParticleController : MonoBehaviour
		,IAdvGraphicObjectParticleController
	{
		//セーブが有効か
		[SerializeField] 
		protected bool enableSave = true;

		[SerializeField] 
		protected bool stopWithChildren = true;

		[SerializeField]
		protected ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting;
		
		public bool EnableSave
		{
			get { return enableSave; }
		}

		public virtual void Stop(AdvParticleStopType stopType)
		{
			ParticleSystem particle = this.GetComponentInChildren<ParticleSystem>();
			if(particle==null) return;
			var main = particle.main;
			main.loop = false;
			switch (stopType)
			{
				case AdvParticleStopType.StopEmitting:
					particle.Stop(stopWithChildren,ParticleSystemStopBehavior.StopEmitting);
					break;
				case AdvParticleStopType.Clear:
					particle.Stop(stopWithChildren,ParticleSystemStopBehavior.StopEmittingAndClear);
					break;
				case AdvParticleStopType.Default:
				default:
					particle.Stop(stopWithChildren,stopBehavior);
					break;
			}
		}
	}
}
