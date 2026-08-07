// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// ADV用SendMessageコマンドから送られたメッセージを受け取る処理のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/UtageRecieveMessageFromAdvComannd")]
	public class UtageRecieveMessageFromAdvComannd : MonoBehaviour
	{
		public GameObject root3d;
		public GameObject rotateRoot;
		public GameObject[] models;

		float rotSpped;

		//ADV用SendMessageコマンドが実行されたタイミング
		void OnDoCommand(AdvCommandSendMessage command)
		{
			switch (command.Name)
			{
				case "3DOn":
					TreedOn(command);
					break;
				case "3DOff":
					TreedOff(command);
					break;
				case "RotateOn":
					RotateOn(command);
					break;
				case "RotateOff":
					RotateOff(command);
					break;
				case "Model":
					ModelOn(command);
					break;
				case "ModelOff":
					ModelOff(command);
					break;
				default:
					Debug.Log("Unknown Message:" + command.Name);
					break;
			}
		}

		//ADV用SendMessageコマンドの処理待ちタイミング
		void OnWait(AdvCommandSendMessage command)
		{
			switch (command.Name)
			{
				default:
					command.IsWait = false;
					break;
			}
		}

		//3D表示ON
		void TreedOn(AdvCommandSendMessage command)
		{
			root3d.SetActive(true);
		}

		//3D表示OFF
		void TreedOff(AdvCommandSendMessage command)
		{
			root3d.SetActive(false);
			StopAllCoroutines();
		}


		//回転ON
		void RotateOn(AdvCommandSendMessage command)
		{
			if (!WrapperUnityVersion.TryParseFloatGlobal(command.Arg2, out rotSpped))
			{
				rotSpped = 15;
			}

			//演出としてカメラを回す
			StartCoroutine("CoRotate3D");
		}

		//回転ON
		void RotateOff(AdvCommandSendMessage command)
		{
			StopCoroutine("CoRotate3D");
		}

		IEnumerator CoRotate3D()
		{
			while (true)
			{
				rotateRoot.transform.Rotate(Vector3.up * rotSpped * Time.deltaTime);
				yield return null;
			}
		}

		//モデルの表示
		void ModelOn(AdvCommandSendMessage command)
		{
			GameObject model = FindModel(command.Arg2);
			if (model != null)
			{
				model.SetActive(true);
				if (!string.IsNullOrEmpty(command.Arg3)) model.GetComponent<Animation>().CrossFade(command.Arg3);
			}
		}

		//モデルの表示
		void ModelOff(AdvCommandSendMessage command)
		{
			GameObject model = FindModel(command.Arg2);
			if (model != null)
			{
				model.SetActive(false);
			}
		}

		//モデルの検索
		GameObject FindModel(string name)
		{
			return (Array.Find(models, s => (s.name == name)));
		}
	}

}
