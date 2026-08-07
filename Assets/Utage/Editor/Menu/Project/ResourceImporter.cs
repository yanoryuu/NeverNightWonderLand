// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;

namespace Utage
{
	public sealed class ResourceImporter : AssetPostprocessor
	{
		//Audioファイルのインポート設定
		void OnPreprocessAudio()
		{
			//インポート時のAudioファイルを設定するクラス
			AudioImporter importer = assetImporter as AudioImporter;

			//宴のリソースかチェック
			if (!IsCustomImportAudio(importer))
			{
				return;
			}
			//各設定
			WrapperUnityVersion.SetAudioImporterThreeD(importer, false);
		}

		//Textureファイルのインポート設定 Textureファイルがインポートされる直前に呼び出される
		void OnPreprocessTexture()
		{
			//インポート時のTextureファイルを設定するクラス
			TextureImporter importer = assetImporter as TextureImporter;

			//宴のリソースかチェック
			AdvScenarioDataProject.TextureType textureType = ParseCustomImportTextureType(importer);
			if (textureType == AdvScenarioDataProject.TextureType.Unknown)
			{
				return;
			}

#if UNITY_5_5_OR_NEWER
			importer.textureType = TextureImporterType.Default;
#else
			importer.textureType = TextureImporterType.Advanced;
#endif
			importer.spriteImportMode = SpriteImportMode.None;
			/*			switch (textureType)
						{
							case AdvScenarioDataProject.TextureType.Character:
							case AdvScenarioDataProject.TextureType.Sprite:
								importer.isReadable = true;
								break;
							default:
								importer.isReadable = false;
								break;
						}
			*/
			//各設定
			//			importer.textureType = TextureImporterType.Sprite;					//スプライトに設定
			importer.mipmapEnabled = false;                                     //MipMapはオフに

#if UNITY_5_5_OR_NEWER
			importer.textureCompression = TextureImporterCompression.Uncompressed;  //True Color
#else
			importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;	//True Color
#endif

			importer.maxTextureSize = 4096;                                     //テクスチャサイズの設定
			importer.alphaIsTransparency = true;                                //アルファの透明設定
			importer.wrapMode = TextureWrapMode.Clamp;                          //Clamp設定
			importer.npotScale = TextureImporterNPOTScale.None;                 //Non Power of 2
			AssetDatabase.WriteImportSettingsIfDirty(AssetDatabase.GetAssetPath(importer));
		}

		//カスタムインポート対象のオーディオか
		bool IsCustomImportAudio(AssetImporter importer)
		{
			AdvScenarioDataProject project = AdvScenarioDataBuilderWindow.ProjectData;
			if (project)
			{
				return project.IsCustomImportAudio(importer);
			}
			return false;
		}

		//カスタムインポート対象のテクスチャか
		AdvScenarioDataProject.TextureType ParseCustomImportTextureType(AssetImporter importer)
		{
			AdvScenarioDataProject project = AdvScenarioDataBuilderWindow.ProjectData;
			if (project)
			{
				return project.ParseCustomImportTextureType(importer);
			}
			return AdvScenarioDataProject.TextureType.Unknown;
		}
	}
}
