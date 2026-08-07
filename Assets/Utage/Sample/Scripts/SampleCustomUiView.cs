// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using TMPro;
using Utage;

namespace Utage
{

	/// カスタムUI画面のサンプル
	public class SampleCustomUiView : UguiView
	{
		//ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		//メインゲーム画面
		public UtageUguiMainGame mainGame;

		public virtual bool IsInit { get; set; }

		//好感度のバー
		[SerializeField] Image paramBar;
		//好感度のテキスト
		[SerializeField] TextMeshProUGUI paramText;

		/// メインゲーム画面からこの画面を開く
		public void OpenFromMainGame()
		{
			//メインゲーム画面を閉じる
			mainGame.Close();
			//この画面を開く
			this.Open(mainGame);
		}

		/// オープンしたときに呼ばれる
		protected virtual void OnOpen()
		{
			IsInit = false;
			StartCoroutine(CoWaitOpen());
		}


		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading) yield break;
			
			//以下に画面の初期化処理を書く

			//好感度を取得してバーに反映
			var v = Engine.Param.GetParameterInt("好感度");
			if(paramBar) paramBar.fillAmount = v / 100.0f;  //好感度は0～100の範囲

			//好感度の数値をテキストに設定
			if(paramText) paramText.text = v.ToString();
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (IsInit && InputUtil.IsInputGuiClose())
			{
				Back();
			}
		}
	}
}
