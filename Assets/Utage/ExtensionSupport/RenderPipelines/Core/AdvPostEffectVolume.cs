#if UTAGE_RENDER_PIPELINE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UtageExtensions;

namespace Utage.RenderPipeline
{
	//ポストエフェクト用のボリュームの制御
	public class AdvPostEffectVolume : MonoBehaviour,
		IPostEffectVolumeObject
	{
		public float Strength
		{
			get => Volume.weight;
			set => Volume.weight = value;
		}
		
		public Volume Volume => this.GetComponentCache(ref volume);
		Volume volume;

		List<AdvVolumeComponentController> VolumeControllers
		{
			get
			{
				if (volumeControllers == null)
				{
					volumeControllers = this.GetComponents<AdvVolumeComponentController>().ToList();
				}
				return volumeControllers;
			}
		}
		[NonSerialized] List<AdvVolumeComponentController> volumeControllers;


		public bool TryGetVolumeComponent<T>(out T component)
			where T : VolumeComponent
		{
			return Volume.profile.TryGet<T>(out component);
		}

		public VolumeComponent SetActiveVolume(string typeName)
		{
			VolumeComponent target = null;
			foreach (var volumeComponent in Volume.profile.components)
			{
				var type = volumeComponent.GetType().Name; 
				if (type == typeName)
				{
					target = volumeComponent;
				}
				volumeComponent.active = false;
			}

			if (target == null)
			{
				Debug.LogError($"{typeName} is not found",this);
				return null;
			}
			target.active = true;
			return target;
		}
		
		//指定のボリュームのみアクティブにする
		public void SetActiveVolume(VolumeComponent target)
		{
			foreach (var volumeComponent in Volume.profile.components)
			{
				volumeComponent.active = false;
			}

			if (target == null)
			{
				return;
			}
			target.active = true;
		}

		//少なくとも1つでもアクティブなVolumeComponentがあるか
		public bool IsAnyActive()
		{
			foreach (var volumeComponent in Volume.profile.components)
			{
				if (volumeComponent.active)
				{
					return true;
				}
			}
			return false;
		}

		public bool TryGetVolumeController<T>(out T component)
			where T : AdvVolumeComponentController
		{
			component = VolumeControllers.Find(x => x is T) as T;
			return component != null;
		}

		//指定の型名のVolumeComponentを探す
		public VolumeComponent FindVolumeController(string typeName)
		{
			foreach (var volumeComponent in Volume.profile.components)
			{
				var type = volumeComponent.GetType().Name; 
				if (type == typeName)
				{
					return volumeComponent;
				}
			}
			return null;
		}

		public void OnClear()
		{
			this.Strength = 0;
			foreach (var volumeComponent in Volume.profile.components)
			{
				volumeComponent.active = false;
			}
			foreach (var volumeController in VolumeControllers)
			{
				volumeController.OnClear();
			}
		}
		const int Version = 0;

		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			//ボリュームの強度を保存
			writer.Write(Strength);
			
			//各VolumeComponentのactiveを保存
			var volumeComponents = this.volume.profile.components;
			writer.Write(volumeComponents.Count);
			foreach (var volumeComponent in volumeComponents)
			{
				writer.Write(volumeComponent.name);
				writer.Write(volumeComponent.active);
			}

			//各VolumeControllerがある場合セーブ
			writer.Write(VolumeControllers.Count);
			foreach (var volumeController in VolumeControllers)
			{
				writer.Write(volumeController.SaveName);
				writer.WriteBuffer(volumeController.Write);
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

			//ボリュームの強度を読み込み
			Strength = reader.ReadSingle();

			//各VolumeComponentを読み込み
			{
				var volumeComponents = this.volume.profile.components;
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string volumeName = reader.ReadString();
					bool active = reader.ReadBoolean();
					var volumeComponent = volumeComponents.Find(x=>x.name == volumeName);
					if (volumeComponent != null)
					{
						volumeComponent.active = active;
					}
					else
					{
						Debug.LogError($"{volumeName} is not found",this);
					}
				}
			}

			//各VolumeControllerを読み込み
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string volumeControllerName = reader.ReadString();
					var volumeComponent = VolumeControllers.Find(x => x.SaveName == volumeControllerName);
					if (volumeComponent != null)
					{
						reader.ReadBuffer(volumeComponent.Read);
					}
					else
					{
						Debug.LogError($"{volumeControllerName} is not found", this);
						//セーブされていたが、消えているので読み込まない
						reader.SkipBuffer();
					}
				}
			}

		}
	}
}
#endif
