// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：キャラクター表示
	/// （Ver3.0からテキスト表示、ボイス再生はTextコマンドを自動生成するように変更）
	/// </summary>
	public class AdvCommandCharacter 
		: AdvCommand
			, IAdvCommandTexts
	{
		protected AdvCharacterInfo characterInfo;
		protected string layerName;
		protected float fadeTime;

		public AdvCommandCharacter(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.characterInfo = AdvCharacterInfo.Create(this, dataManager);
			if (this.characterInfo == null)
			{
				Debug.LogError(ToErrorString("Failed to create character info"));
			}
			else if (characterInfo.Graphic != null)
			{
				AddLoadGraphic(characterInfo.Graphic);
			}

			//表示レイヤー
			this.layerName = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			if (!string.IsNullOrEmpty(layerName) && !dataManager.LayerSetting.Contains(layerName, AdvLayerSettingData.LayerType.Character))
			{
				//表示レイヤーが見つからない
				Debug.LogError(ToErrorString(layerName + " is not contained in layer setting"));
			}
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		//キャラクター表示更新
		public override void DoCommand(AdvEngine engine)
		{
			bool checkDraw = false;
			if (this.characterInfo.IsHide)
			{
				//表示オフの指定なので、表示キャラフェードアウト
				engine.GraphicManager.CharacterManager.FadeOut(characterInfo.Label, engine.Page.ToSkippedTime(fadeTime));
			}
			else if (CheckDrawCharacter(engine))
			{
				checkDraw = true;
				//グラフィック表示処理
				engine.GraphicManager.CharacterManager.DrawCharacter(
					layerName
					, characterInfo.Label
					, new AdvGraphicOperationArg(this, this.characterInfo.Graphic.Main, fadeTime));
			}

			if(checkDraw || CheckNewCharacterInfo(engine) )
			{
				//現在のページのキャラクター情報は上書き
				engine.Page.CharacterInfo = characterInfo;
			}

			//基本以外のコマンド引数の適用
			AdvGraphicObject obj = engine.GraphicManager.CharacterManager.FindObject(this.characterInfo.Label);
			if (obj!=null)
			{
				//位置の適用（Arg4とArg5）
				obj.SetCommandPostion(this);
				//その他の適用（モーション名など）
				obj.TargetObject.SetCommandArg(this);
			}
		}

		bool CheckDrawCharacter( AdvEngine engine )
		{
			if (characterInfo.Graphic == null || characterInfo.Graphic.Main == null)
			{
				//表示データない
				return false;
			}
			else if (engine.GraphicManager.IsEventMode)
			{
				//イベントモード
				return false;
			}
			else
			{
				if (string.IsNullOrEmpty(characterInfo.Pattern) && engine.GraphicManager.CharacterManager.IsContians(layerName, characterInfo.Label))
				{
					//パターン指定なしの場合
					//既に同名キャラが同じレイヤーにいるなら改めての描画しない
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		bool CheckNewCharacterInfo( AdvEngine engine )
		{
			if( engine.Page.CharacterLabel != characterInfo.Label)
			{
				return true;
			}

			if( engine.Page.NameText != characterInfo.LocalizeNameText)
			{
				return true;
			}

			if( !string.IsNullOrEmpty (characterInfo.Pattern))
			{
				return true;
			}

			return false;
		}
		
		// 選択肢終了などの特別なコマンドを自動生成する場合、そのIDを返す
		public override string[] GetExtraCommandIdArray(AdvCommand next)
		{
			if (AdvParser.IsEmptyTextCommand(RowData))
			{
				return null;
			}
			else
			{
				return new string[] { AdvCommandParser.IdText};
			}
		}

		public IEnumerable<string> GetTextStrings()
		{
			yield return characterInfo.LocalizeNameText;
		}
	}
}
