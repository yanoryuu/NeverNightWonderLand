// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;

namespace Utage
{
	//自作のゲーム処理から、プレハブから作成した宴のキャラクターオブジェクトがもつコンポーネントをコントロールするためのサンプル
	public class SampleAdvGraphicObjectController : MonoBehaviour
	{
		//ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;
		
		
		//指定の名前のAdvGraphicObjectを探し、描画オブジェクト（プレハブならプレハブインスタンス）のコンポーネントを取得
		public T FindAdvGraphicTargetComponent<T>(string objectLabel)
			where T : class
		{
			//指定の名前のAdvGraphicObjectを探す
			AdvGraphicObject obj = Engine.GraphicManager.FindObject(objectLabel);
			if (obj == null)
			{
				//見つからない
				return null;
			}

			//AdvGraphicObject以下に描画オブジェクトがない可能性があるので
			//obj.TargetObjectで取得
			var target = obj.TargetObject;
			return target.GetComponentInChildren<T>();
		}
		
		
		//クリック処理のサンプル
		public void SampleOnClick()
		{
			//ログを出力（不要なら消すこと）
			Debug.Log($"SampleOnClick");
			//指定の名前のキャラクター名のオブジェクト探し、そのコンポーネントを取得
			string label = "うたこ";
			var component = Engine.GraphicManager.FindObjectTargetComponent<SampleAdvGraphicObjectComponent>(label);
			if (component == null)
			{
				//見つからない
				Debug.LogError($"{label} is not found");
				return;
			}

			//ボタンクリック処理を呼び出す
			component.OnClick();
		}

		//AdvEngine以下に一つしかSamplePrefabComponentを持つオブジェクトがないとわかっている場合に、
		//そのコンポーネントを取得して呼び出す処理
		void SampleSingleComponent()
		{
			var singleComponent = Engine.GetComponentInChildren<SampleAdvGraphicObjectComponent>();
			singleComponent.OnClick();
		}

		//AdvEngine以下の全SamplePrefabComponentに処理をするなら
		void SampleAllComponents()
		{
			foreach (var component in Engine.GetComponentsInChildren<SampleAdvGraphicObjectComponent>(true))
			{
				component.OnClick();
			}
		}
	}
		
}
