// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Utage
{
	//喋っていないキャラクターをグレーアウトする処理にSendMessageする場合の制御
	[AddComponentMenu("Utage/ADV/Examples/SampleCharacterGrayOutControllerRecieveMessage")]
	public class SampleCharacterGrayOutControllerRecieveMessage : MonoBehaviour
	{
		void ChangeNoGrayoutCharancter(AdvCommandSendMessageByName message)
		{
			AdvCharacterGrayOutController Controller = this.GetComponent<AdvCharacterGrayOutController>();
			string[] characters = message.RowData.ParseCellOptionalArray<string>("Arg3",new string[] { });
			Controller.NoGrayoutCharacters = new List<string>(characters);
		}
	}
}

