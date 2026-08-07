// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
	//キャラクターラベルによって、ImageコンポーネントのSpriteを入れ替えるサンプル
	public class SampleImageControllerByCharacterLabel : MonoBehaviour
	{
		public Image target;	//対象のImage
		[Serializable]
		public class SpriteInfo
		{
			public string characterLabel;	//キャラクターラベル
			public Sprite sprite;			//キャラクターラベルに対応するスプライト
		}
		//キャラクターラベルごとのスプライト情報のリスト
		[SerializeField]
		List<SpriteInfo> spriteInfoList = new List<SpriteInfo>();
		public Sprite defaultSprite; //対応するキャラクターラベルがない場合のデフォルトのスプライト

		void Awake()
		{
			//サンプルなので、イベントの設定はここで自動的に行う
			//AdvEngineが親オブジェクトになかったり、イベントの登録をAwakeのタイミング以外にしたい場合は
			//インスペクター経由で手動設定するなどして、適宜変更してください
			AdvEngine engine = this.GetComponentInParent<AdvEngine>();
			if (engine != null)
			{
				engine.Page.OnBeginPage.AddListener(OnBeginPage);
				engine.Page.OnChangeText.AddListener(OnChangeText);
			}
		}

		//ページの開始のタイミングで呼ばれる
		public void OnBeginPage(AdvPage page)
		{
			UpdateImage(page.CharacterLabel);
		}

		//テキストに変更があった場合に呼ばれる
		public void OnChangeText(AdvPage page)
		{
			UpdateImage(page.CharacterLabel);
		}
		
		//キャラクターラベルに対応するスプライトをImageにセットする
		void UpdateImage(string characterLabel)
		{
			var sprite = FindSpriteByCharacterLabel(characterLabel);
			target.sprite = sprite;
		}
		
		//キャラクターラベルに対応するスプライトを探す
		Sprite FindSpriteByCharacterLabel(string characterLabel)
		{
			var spriteInfo = spriteInfoList.Find(x => x.characterLabel == characterLabel);
			if (spriteInfo != null)
			{
				//キャラクターラベルに対応したスプライトを返す
				return spriteInfo.sprite;
			}
			//見つからない場合はデフォルトスプライトを
			return defaultSprite;
		}
	}
}
