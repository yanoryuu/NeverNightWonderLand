// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;

namespace Utage
{

	//ターゲットに設定されたカメラを回転させるサンプル 
	public class SampleReceiveMessageRotate : MonoBehaviour
	{
		//ターゲットとなるリスト
		public List<CameraRoot> targetList = new List<CameraRoot>();
		
		//AdvEngine
		AdvEngine Engine => this.GetComponentCache(ref engine);
		[SerializeField]AdvEngine engine;
		
		//AdvEngineのTime（時間制御）
		AdvTime Time => Engine.Time;


		//SendMessageコマンドが実行されたタイミングで呼ばれる
		void OnDoCommand(AdvCommandSendMessage command)
		{
			switch (command.Name)
			{
				case "RotateZ":
					//回転処理
					StartCoroutine(RotateZ(command));
					break;
				default:
					Debug.LogError("Unknown Message:" + command.Name);
					break;
			}
		}
		
		IEnumerator RotateZ(AdvCommandSendMessage command)
		{
			string targetName = command.ParseCell<string>(AdvColumnName.Arg2);
			var target = targetList.Find(x => x.name == targetName);
			if(target==null)
			{
				Debug.LogError("Not Found Target:" + targetName);
				yield break;
			}

			var targetTransform = target.transform;
			float angle = command.ParseCell<float>(AdvColumnName.Arg3);
			float time = command.ParseCell<float>(AdvColumnName.Arg6);
			//回転処理
			command.IsWait = true;
			float startTime = Time.Time;
			float startAngle = targetTransform.localRotation.eulerAngles.z;
			float endAngle = angle;
			while (Time.Time - startTime < time)
			{
				float t = (Time.Time - startTime) / time;
				float z = Mathf.Lerp(startAngle, endAngle, t);
				targetTransform.localRotation = Quaternion.Euler(0, 0, z);
				yield return null;
			}
			targetTransform.localRotation = Quaternion.Euler(0, 0, endAngle);
			command.IsWait = false;
		}
	}
}
