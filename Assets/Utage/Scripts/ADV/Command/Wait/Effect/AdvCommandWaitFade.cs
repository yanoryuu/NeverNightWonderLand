// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	// コマンド：オブジェクト単位のフェードアウト
	public class AdvCommandWaitFadeObjects : AdvCommandWaitBase
		, IAdvCommandEffect
		, IAdvCommandUpdateWait
	{
	
		string[] Targets { get; set; }
		AdvEngine Engine { get; set; }
		
		internal AdvCommandWaitFadeObjects(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.Targets = ParseCellOptionalArray<string>(AdvColumnName.Arg1, new string[] { AdvCommandKeyword.All } );
			WaitType = ParseCellOptional(AdvColumnName.WaitType,AdvCommandWaitType.Default);
		}

		//開始時のコールバック
		protected override void OnStart(AdvEngine engine, AdvScenarioThread thread)
		{
			Engine = engine;
		}

		//コマンド終了待ち
		public bool UpdateCheckWait()
		{
			foreach (var target in Targets)
			{
				if (CheckTargetWait(target))
				{
					return true;
				}
			}
			return false;
		}

		bool CheckTargetWait(string targetName)
		{
			if (targetName.IsNullOrEmpty()) return  false;
			
			var graphicManager = Engine.GraphicManager;
			switch (targetName)
			{
				case  AdvCommandKeyword.AllBgLayers:
					return  graphicManager.BgManager.IsFading;
				case  AdvCommandKeyword.AllCharacterLayers:
					return  graphicManager.CharacterManager.IsFading;
				case  AdvCommandKeyword.AllSpriteLayers:
					return  graphicManager.SpriteManager.IsFading;
				case  AdvCommandKeyword.AllBgObjects:
					return  graphicManager.IsFadingObjects(AdvGraphicObjectType.Bg);
				case  AdvCommandKeyword.AllCharacterObjects:
					return  graphicManager.IsFadingObjects(AdvGraphicObjectType.Character);
				case  AdvCommandKeyword.AllSpriteObjects:
					return  graphicManager.IsFadingObjects(AdvGraphicObjectType.Sprite);
				case  AdvCommandKeyword.All:
					return  graphicManager.BgManager.IsFading ||
					        graphicManager.CharacterManager.IsFading ||
					        graphicManager.SpriteManager.IsFading;
				default:
					return graphicManager.IsFading(targetName);
			}
		}

		public void OnEffectFinalize()
		{
			Engine = null;
		}

		public void OnEffectSkip()
		{
			foreach (var target in Targets)
			{
				OnEffectSkip(target);
			}
		}

		void OnEffectSkip(string targetName)
		{
			var graphicManager = Engine.GraphicManager; 
			switch (targetName)
			{
				case  AdvCommandKeyword.AllBgLayers:
					graphicManager.BgManager.SkipFade();
					break;
				case  AdvCommandKeyword.AllCharacterLayers:
					graphicManager.CharacterManager.SkipFade();
					break;
				case  AdvCommandKeyword.AllSpriteLayers:
					graphicManager.SpriteManager.SkipFade();
					break;
				case  AdvCommandKeyword.AllBgObjects:
					graphicManager.SkipFadeObjects(AdvGraphicObjectType.Bg);
					break;
				case  AdvCommandKeyword.AllCharacterObjects:
					graphicManager.SkipFadeObjects(AdvGraphicObjectType.Character);
					break;
				case  AdvCommandKeyword.AllSpriteObjects:
					graphicManager.SkipFadeObjects(AdvGraphicObjectType.Sprite);
					break;
				case  AdvCommandKeyword.All:
					graphicManager.BgManager.SkipFade();
					graphicManager.CharacterManager.SkipFade();
					graphicManager.SpriteManager.SkipFade();
					break;
				default:
					graphicManager.SkipFade(targetName);
					break;
			}
		}
	}
}
