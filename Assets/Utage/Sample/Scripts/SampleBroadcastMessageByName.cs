// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// ADV用SampleBroadcastMessageByNameコマンドから送られたメッセージを受け取る処理のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleBroadcastMessageByName")]
	public class SampleBroadcastMessageByName : MonoBehaviour
	{
		//TestBroadcastMessageという処理を呼ぶ。引数にはAdvCommandSendMessageByNameを持つ
		void TestBroadcastMessage(AdvCommandBroadcastMessageByName command)
		{
			Debug.Log(command.ParseCellOptional(AdvColumnName.Arg4, ""));
		}
	}
}
