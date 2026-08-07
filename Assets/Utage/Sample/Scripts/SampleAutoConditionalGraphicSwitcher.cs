// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using Utage;
using UtageExtensions;

namespace Utage
{

	//CharacterシートやTextureシートのConditionalを毎フレームチェックして
	//自動的にオブジェクトの表示変更を行うコンポーネント
	//ロード処理が行われないので、アバターやダイシングなど全Conditionalで同じリソースを使用するものにのみ有効
	//デフォルトの通常テクスチャの表示の場合は、SampleExplicitConditionalGraphicSwitcherで手動タイミングで呼び出すこと
	public class SampleAutoConditionalGraphicSwitcher : MonoBehaviour
	{
		AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		public AdvEngine engine = null;

		public AdvGraphicInfo CurrentGraphicInfo { get; private set; }
		AdvGraphicInfoList CurrentGraphicList { get; set; }
		
		//グラフィックオブジェクトの描画時によばれるイベント。AdvGraphicInfoは、キャラクターシートのパターンごとの情報が入っている
		public void OnDraw(AdvGraphicInfo graphicInfo)
		{
//			Debug.Log("OnDraw");
			CurrentGraphicInfo = graphicInfo;
			CurrentGraphicList = Engine.DataManager.FindGraphicInfoList(graphicInfo);
		}

		//毎フレームチェック（ほかのコマンド処理などが終わったあとなのでLateUpdateで行う）
		void LateUpdate()
		{
			if(CurrentGraphicList==null) return;
			
			//Mainの内部でConditionalの条件チェック
			var main = CurrentGraphicList.Main; 
			if ( main!= CurrentGraphicInfo)
			{
				if (!CurrentGraphicList.InfoList.Contains(CurrentGraphicInfo))
				{
					Debug.LogError($"{CurrentGraphicInfo.Key} のリストが更新されていません");
					return;
				}
				CurrentGraphicInfo = main;
				OnAutoChangeGraphic();
			}
		}

		void OnAutoChangeGraphic()
		{
//			Debug.Log("OnAutoChangeGraphic");
			AdvGraphicObject graphicObject = this.GetComponent<AdvGraphicObject>();
			if (!graphicObject.Layer.CurrentGraphics.ContainsValue(graphicObject))
			{
				//すでにフェードアウトなどで管理外
				return;
			}

			OnAutoChangeGraphic(graphicObject, CurrentGraphicInfo);
		}

		//表示を変える
		//AdvGraphicInfoはロード済みの前提
		//AvatarやダイシングならFileNameが同じならロード済み。
		//（注）デフォルトのテクスチャを使うだけの表示は、新しいテクスチャロードが必要なので、その処理も追記が必要
		void OnAutoChangeGraphic(AdvGraphicObject graphicObject,AdvGraphicInfo graphic)
		{
			//表示を変更

			//Loaderに参照を持たせてからロード
			//Loader.LoadGraphicは、前のリソースを開放可能にしてから次のリソースをロードするため、
			//今のリソースのロード時間がかかると「なにも描画オブジェクトがない」状態になりかねないため、未ロードのものは使用しないこと
			graphicObject.Loader.LoadGraphic(graphic, () => graphicObject.DrawSubExplicit(graphic,0));
/*			
			graphicObject.TargetObject.ChangeResourceOnDraw(graphic,0);
			if (graphicObject.RenderObject != graphicObject.TargetObject)
			{
				//テクスチャ書き込みをしている
				graphicObject.RenderObject.ChangeResourceOnDraw(graphic, 0);
			}
*/		}
	}
}
