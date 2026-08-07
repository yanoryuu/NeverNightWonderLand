// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// パーティクルを拡大縮小する。
	/// 拡大縮小に必要な設定に自動書き換えする
	/// </summary>
	[AddComponentMenu("Utage/Lib/Effect/ParticleScaler")]
	public class ParticleScaler : MonoBehaviour
	{
		public bool UseLocalScale { get { return useLocalScale; } set { useLocalScale = value; HasChanged = true; } }
		[SerializeField]
		bool useLocalScale = false;

		public float Scale { get { return scale; } set { scale = value; HasChanged = true; } }
		[SerializeField,Hide("UseLocalScale")]
		float scale = 1.0f;

		//レンダーモードを変えるか
		public bool ChangeRenderMode { get { return changeRenderMode; } set { changeRenderMode = value; HasChanged = true; } }
		[SerializeField]
		bool changeRenderMode = true;

		//重力を変えるか
		public bool ChangeGravity { get { return changeGravity; } set { changeGravity = value; HasChanged = true; } }
		[SerializeField]
		bool changeGravity = true;

		bool HasChanged { get; set; }

		bool IsInit { get; set; }

		Dictionary<ParticleSystem, float> defaultGravities = new Dictionary<ParticleSystem, float>();

		void Start()
		{
			HasChanged = true;
		}

		void OnValidate()
		{
			HasChanged = true;
		}

		void Update()
		{
			if (HasChanged)
			{
				if (useLocalScale)
				{
				}
				else
				{
					this.transform.localScale = Scale * Vector3.one;
				}
				ChangeSetting();
			}
		}

		void ChangeSetting()
		{
			ParticleSystem[] particles = this.GetComponentsInChildren<ParticleSystem>(true);
			foreach (ParticleSystem particle in particles )
			{
				ChangeSetting(particle);
			}
		}

		void ChangeSetting(ParticleSystem particle)
		{
			var mainModle = particle.main;
			mainModle.scalingMode = ParticleSystemScalingMode.Hierarchy;
			if (particle.velocityOverLifetime.enabled)
			{
				ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particle.velocityOverLifetime;
				velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
			}
			if (particle.forceOverLifetime.enabled)
			{
				ParticleSystem.ForceOverLifetimeModule forceOverLifetime = particle.forceOverLifetime;
				forceOverLifetime.space = ParticleSystemSimulationSpace.Local;
			}
			if (ChangeGravity)
			{
				float defaultGravity;
				if (!defaultGravities.TryGetValue(particle, out defaultGravity))
				{
					defaultGravity = mainModle.gravityModifier.constant;
					defaultGravities.Add(particle, defaultGravity);
				}
				mainModle.gravityModifier = defaultGravity * this.transform.lossyScale.y;
			}

			if (ChangeRenderMode)
			{
				ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
				if (renderer != null)
				{
					if (renderer.renderMode == ParticleSystemRenderMode.Stretch)
					{
						renderer.renderMode = ParticleSystemRenderMode.Billboard;
					}
				}
			}
		}
	}
}
