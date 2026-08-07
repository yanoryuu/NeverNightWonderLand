#if UTAGE_URP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UtageExtensions;

namespace Utage.RenderPipeline.Urp
{
	//URPを使ったポストエフェクト処理
	public class AdvPostEffectRenderPipelineUsingUniversal : MonoBehaviour,
		IAdvPostEffectRenderPipelineBridge, IAdvSaveData
	{
		AdvPostEffectManager AdvPostEffectManager => this.GetComponentCache(ref postEffectManager);
		AdvPostEffectManager postEffectManager;
		AdvEngine Engine => AdvPostEffectManager.Engine;
		
		[SerializeField] bool checkRendererFeatures = true;

		void Awake()
		{
			//UniversalRenderPipelineAssetが設定されているかチェック
			UniversalRenderPipelineAsset currentRendererPipeLine = UrpUtil.GetCurrentRendererPipeLine();
			if (currentRendererPipeLine == null)
			{
				Debug.LogError("Not found UniversalRenderPipelineAsset");
				return;
			}
#if URP_17_OR_NEWER
			if (checkRendererFeatures)
			{
				bool isFound = false;
				foreach (var item in currentRendererPipeLine.rendererDataList)
				{
					foreach (var scriptableRendererFeature in item.rendererFeatures)
					{
						if (scriptableRendererFeature is ColorFadeRenderFeature)
						{
							isFound = true;
							break;
						}
					}
				}
				if (!isFound)
				{
					var url = @"https://madnesslabo.net/utage/?page_id=16001";
					var jp = "UniversalRenderPipelineAsset に ColorFadeRenderFeature が見つかりません。\n。URPのセットアップが終わってない可能性がありますので、リンク先のドキュメントを参考に、URPのセットアップを行ってください。";
					var en = "ColorFadeRenderFeature was not found in UniversalRenderPipelineAsset.\nThe URP setup may not be complete. Please refer to the linked documentation and complete the URP setup.";
					
					var msg = SimpleLang.Select(jp, en);
					Debug.LogError($"{msg}\n WebDocument {StringTagUtil.HyperLinkTag(url)}", currentRendererPipeLine);
				}
			}
#endif
		}

		public virtual (IPostEffectStrength effect, float start, float end) DoCommandColorFade(Camera targetCamera, IAdvCommandFade command)
		{
			var volumes = targetCamera.GetComponentInChildren<AdvCameraPostEffectManager>(true);
			var fadeVolume = volumes.FadeVolume;
			float start, end;
			if (command.Inverse)
			{
				//画面全体のフェードイン（つまりカメラのカラーフェードアウト）
				start = fadeVolume.Strength;
				end = 0;
			}
			else
			{
				//画面全体のフェードアウト（つまりカメラのカラーフェードイン）
				start = fadeVolume.Strength;
				end = command.Color.a;
			}

			if (fadeVolume.TryGetVolumeController(
				    out ColorFadeVolumeController colorFadeVolumeController))
			{
				colorFadeVolumeController.SetColor(command.Color);
				//指定のエフェクトのみアクティブにする
				fadeVolume.SetActiveVolume(colorFadeVolumeController.VolumeComponent);
			}
			else
			{
				Debug.LogError($"{nameof(ColorFadeVolumeController)} is not found", this);
			}
			return (fadeVolume, start, end);
		}

		public virtual (IPostEffectStrength effect, float start, float end) DoCommandRuleFade(Camera targetCamera, IAdvCommandFade command)
		{
			var volumes = targetCamera.GetComponentInChildren<AdvCameraPostEffectManager>(true);
			var fadeVolume = volumes.FadeVolume;
			float start, end;
			if (command.Inverse)
			{
				//画面全体のフェードイン（つまりカメラのカラーフェードアウト）
				start = fadeVolume.Strength;
				end = 0;
			}
			else
			{
				//画面全体のフェードアウト（つまりカメラのカラーフェードイン）
				start = fadeVolume.Strength;
				end = 1;
			}

			if (fadeVolume.TryGetVolumeController(
				    out RuleFadeVolumeController ruleFade))
			{
				ruleFade.SetRuleTexture(Engine.EffectManager.FindRuleTexture(command.RuleImage));
				ruleFade.SetVague(command.Vague);
				ruleFade.SetColor(command.Color);
				//指定のエフェクトのみアクティブにする
				fadeVolume.SetActiveVolume(ruleFade.VolumeComponent);
			}
			else
			{
				Debug.LogError($"{nameof(ColorFadeVolumeController)} is not found", this);
			}
			return (fadeVolume, start, end);
		}

		public (IPostEffect effect, Action onComplete) DoCommandImageEffect(Camera targetCamera, IAdvCommandImageEffect command,
			Action onComplete)
		{
			var manager = targetCamera.GetComponentInChildren<AdvCameraPostEffectManager>(true);
			var imageEffectVolume = manager.ImageEffectVolume;
			
			//指定のエフェクトのVolumeComponentを探す
			var effectType = command.ImageEffectType;
			var volumeComponent = imageEffectVolume.FindVolumeController(effectType);
			if (volumeComponent==null)
			{
				volumeComponent = imageEffectVolume.FindVolumeController( $"{effectType}Volume");
			}
			if (volumeComponent ==null)
			{
				Debug.LogError($"Not found ImageEffect {effectType}", this);
			}
			
			//指定のエフェクトのみアクティブにする
			imageEffectVolume.SetActiveVolume(volumeComponent);
			return (imageEffectVolume, onComplete);
		}

		public void DoCommandImageEffectAllOff(Camera targetCamera, IAdvCommandImageEffect command, Action onComplete)
		{
			var manager = targetCamera.GetComponentInChildren<AdvCameraPostEffectManager>(true);
			var imageEffectVolume = manager.ImageEffectVolume;
			foreach (var component in imageEffectVolume.Volume.profile.components)
			{
				component.active = false;
			}
			onComplete();
		}

		public IPostEffect DoCommandPostEffect(Camera targetCamera, AdvCommandPostEffect command)
		{
			var effectVolume = FindVolumeSub(targetCamera,command.VolumeName);
			if(effectVolume==null)
			{
				Debug.LogError($"Not found post effect camera:{targetCamera.name} volume:{command.VolumeName}",targetCamera);
				return null;
			}
			
			if (command.EffectNames.Length <= 0)
			{
				//全てのエフェクトをアクティブにする
				foreach (var volumeComponent in effectVolume.Volume.profile.components)
				{
					volumeComponent.active = true;
				}
			}
			else
			{
				//指定のエフェクトのみアクティブにする
				List<string> effectNames = new (command.EffectNames);
				foreach (var effectName in command.EffectNames)
				{
					var suffixed = $"{effectName}Volume";
					effectNames.Add(suffixed);
#if UNITY_EDITOR
					if (effectVolume.FindVolumeController(effectName) == null && effectVolume.FindVolumeController(suffixed) == null)
					{
						Debug.LogError($"Not found post effect {effectName} or {suffixed} in volume {effectVolume.name}", effectVolume);
					}
#endif
				}
				foreach (var volumeComponent in effectVolume.Volume.profile.components)
				{
					volumeComponent.active = effectNames.Any(effectName => volumeComponent.GetType().Name == effectName);
				}
			}
			return effectVolume;
		}

		public IPostEffectVolumeObject FindVolume(Camera targetCamera, string volumeName)
		{
			return FindVolumeSub(targetCamera, volumeName);
		}

		protected AdvPostEffectVolume FindVolumeSub(Camera targetCamera, string volumeName)
		{
			var manager = targetCamera.GetComponentInChildren<AdvCameraPostEffectManager>(true);
			var effectVolume = manager.PostEffectVolumes.FirstOrDefault(x => x.name == volumeName);
			return effectVolume;
		}
		
		public IEnumerable<IPostEffectVolumeObject> GetAllActiveEffectVolumes(Camera targetCamera)
		{
			var manager = targetCamera.GetComponentInChildren<AdvCameraPostEffectManager>(true);
			foreach (var effectVolume in manager.PostEffectVolumes)
			{
				if(effectVolume == manager.FadeVolume) continue;
				if (effectVolume.IsAnyActive())
				{
					yield return effectVolume;
				}
			}
		}

		AdvCameraPostEffectManager[] PostEffectManagers => Engine.CameraManager.GetComponentsInChildren<AdvCameraPostEffectManager>(true);

		public string SaveKey
		{
			get { return "PostEffectRenderPipelineUsingUniversal"; }
		}
		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void OnWrite(BinaryWriter writer)
		{
			var managers = PostEffectManagers;
			writer.Write(Version);
			writer.Write(managers.Length);
			foreach (var manager in managers)
			{
				writer.Write(manager.TargetCamera.name);
				writer.WriteBuffer(manager.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void OnRead(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			var managers = PostEffectManagers;

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string cameraName = reader.ReadString();
				AdvCameraPostEffectManager manager = managers.FirstOrDefault(x=>x.TargetCamera.name== cameraName);
				if (manager != null)
				{
					reader.ReadBuffer(manager.Read);
				}
				else
				{
					Debug.LogError($"Not found Camera {cameraName}");
					//セーブされていたが、消えているので読み込まない
					reader.SkipBuffer();
				}
			}
		}

		public void OnClear()
		{
			foreach (var manager in PostEffectManagers)
			{
				manager.Clear();
			}
		}
	}
}
#endif
