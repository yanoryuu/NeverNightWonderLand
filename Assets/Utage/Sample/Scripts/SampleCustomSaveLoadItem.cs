//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
//----------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
	//セーブロードのUI表示を変更するサンプル
	public class SampleSaveLoadItem : UtageUguiSaveLoadItem
	{
		public Text paramText;
		public override void Refresh(bool isSave)
		{
			base.Refresh(isSave);

			if (!data.IsSaved)
			{
				//空のセーブデータの場合・・・
				paramText.text = "";
			}
			else
			{
				//親オブジェクトより上にあるはずのセーブロード画面を取得
				var view = this.GetComponentInParent<UtageUguiSaveLoad>();
				//AdvEngineを取得
				var advEngine = view.Engine;
			
				//セーブデータ内のパラメーターを扱えるようにする
				var paramManager = this.Data.ReadParam(advEngine);
			
				//パラメーター「player_name」を取得して、paramTextに設定
				paramText.text = paramManager.GetParameterString("player_name");
				
				ReadCustomSave();
			}
		}
		
		public SampleSendMessageByName customSave = null;

		void ReadCustomSave()
		{
			this.Data.Buffer.Overrirde(customSave);
		}
	}
}
