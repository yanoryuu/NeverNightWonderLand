using UnityEngine;
using System.Collections.Generic;
using Utage;
using UtageExtensions;

namespace Utage
{
	// 指定の名前のBoolパラメーターがオンになったら、TIPSをOpenするサンプル
	public class SampleTipsOpenByParameter : MonoBehaviour
	{
		// ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		// TIPSの管理コンポーネント
		public TipsManager TipsManager => this.GetComponentCache(ref tipsManager);
		[SerializeField] protected TipsManager tipsManager;

		// TIPSの名前と、パラメーターの名前の対応
		class　TipsParam
		{
			public string ParamName { get; }
			public string TipsId { get; }
			public TipsParam(string tipsId, string paramName)
			{
				TipsId = tipsId;
				ParamName = paramName;
			}
		}
		List<TipsParam> TipsParamList { get; } = new();

		void Awake()
		{
			if (Engine == null) return;
			Engine.OnPostInit.AddListener(OnInit);
			Engine.ParameterEventTrigger.OnChanged.AddListener(OnChangedParam);
		}

		void OnInit()
		{
			TipsParamList.Clear();
			foreach (var keyValue in TipsManager.TipsMap)
			{
				var tipsInfo = keyValue.Value;
				var tipsData = tipsInfo.Data;
				
				// TIPSのデータシートのOpenFlag行を取得
				var openFlag = tipsData.RowData.ParseCellOptional("OpenFlag", "");
				if (string.IsNullOrEmpty(openFlag)) continue;
				
				//OpenFlagが設定されているTIPSのIDと、パラメーターの名前を登録
				TipsParamList.Add( new TipsParam( tipsData.Id, openFlag ));
			}
		}

		//パラメーターが変更されたとき呼ばれる
		void OnChangedParam(AdvParamData param)
		{
//			Debug.Log($"OnChangedParam {param.Key}");
			foreach (var tipsParamPair in TipsParamList)
			{
				//設定した名前のパラメーターがTRUEに変更されたら、TIPSをOpenする
				if( tipsParamPair.ParamName != param.Key) continue;
				if (param.Type == AdvParamData.ParamType.Bool && param.BoolValue == true)
				{
//					Debug.Log($"OpenTips{tipsParamPair.TipsId}");
					TipsManager.OpenTips(tipsParamPair.TipsId);
				}
			}
		}
	}
}
