// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;

namespace Utage
{
    //パラメーター変更イベントのサンプル
	public class SampleParamTrigger : MonoBehaviour
	{
		public AdvEngine advEngine;
		public string paramName = "";
		public string paramNameBool = "";
		public string paramNameFloat = "";
		public string paramNameInt= "";
		public string paramNameString = "";

		private void Awake()
		{
			if (advEngine == null) return;
			//イベントリスナーの登録
			advEngine.ParameterEventTrigger.OnChanged.AddListener(OnChanged);
			advEngine.ParameterEventTrigger.AddEventByName(paramName, OnChangedByName);
			advEngine.ParameterEventTrigger.AddBoolEvent(paramNameBool, OnChangedBool);
			advEngine.ParameterEventTrigger.AddFloatEvent(paramNameFloat, OnChangedFloat);
			advEngine.ParameterEventTrigger.AddIntEvent(paramNameInt, OnChangedInt);
			advEngine.ParameterEventTrigger.AddStringEvent(paramNameString, OnChangedString);
		}

		void OnDestroy()
		{
			//イベントリスナーの解除
			//特に動的に生成・削除されるものに対しては、忘れずに解除すること
			if(advEngine==null) return;
			advEngine.ParameterEventTrigger.OnChanged.RemoveListener(OnChanged);
			advEngine.ParameterEventTrigger.RemoveEventByName(paramName, OnChangedByName);
			advEngine.ParameterEventTrigger.RemoveBoolEvent(paramNameBool, OnChangedBool);
			advEngine.ParameterEventTrigger.RemoveFloatEvent(paramNameFloat, OnChangedFloat);
			advEngine.ParameterEventTrigger.RemoveIntEvent(paramNameInt, OnChangedInt);
			advEngine.ParameterEventTrigger.RemoveStringEvent(paramNameString, OnChangedString);
		}
		
		//パラメーターが変更されたとき呼ばれる
		void OnChanged(AdvParamData param)
		{
			Debug.Log($"OnChanged {param.Key}  {param.Parameter}" );
		}

		//指定の名前のパラメーターが変更されたとき呼ばれる
		void OnChangedByName(AdvParamData param)
		{
			switch (param.Type)
			{
				case AdvParamData.ParamType.Bool:
					Debug.Log($"OnChangedByName {param.Key}  {param.BoolValue}");
					break;
				case AdvParamData.ParamType.Float:
					Debug.Log($"OnChangedByName {param.Key}  {param.FloatValue}");
					break;
				case AdvParamData.ParamType.Int:
					Debug.Log($"OnChangedByName {param.Key}  {param.IntValue}");
					break;
				case AdvParamData.ParamType.String:
					Debug.Log($"OnChangedByName {param.Key}  {param.StringValue}");
					break;
			}
		}


		//指定の名前のbool値が変更されたとき呼ばれる
		void OnChangedBool(bool value)
		{
			Debug.Log($"OnChangedBool {paramNameBool}  {value}");
		}

		//指定の名前のfloat値が変更されたとき呼ばれる
		void OnChangedFloat(float value)
		{
			Debug.Log($"OnChangedFloat {paramNameFloat}  {value}");
		}

		//指定の名前のint値が変更されたとき呼ばれる
		void OnChangedInt(int value)
		{
			Debug.Log($"OnChangedInt {paramNameInt}  {value}");
		}

		//指定の名前のstring値が変更されたとき呼ばれる
		public void OnChangedString(string value)
		{
			Debug.Log($"OnChangedString {paramNameString}  {value}");
		}
	}
}
