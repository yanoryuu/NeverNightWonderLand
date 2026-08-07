// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


/// <summary>
/// SendMessageByNameコマンド用の拡張命令
/// Arg3のシーンをロードする
/// </summary>
namespace Utage
{
	[AddComponentMenu("Utage/ADV/Extra/AdvLoadScene")]
	public class AdvLoadScene : MonoBehaviour
	{
		//LoadSceneという処理を呼ぶ。引数にはAdvCommandSendMessageByNameを持つ
		void LoadScene(AdvCommandSendMessageByName command)
		{
			//Arg3の文字列を取得
			string sceneName = command.ParseCell<string>(AdvColumnName.Arg3);
			//Arg3に書いてあるシーンをロード
			SceneManager.LoadScene(sceneName);
		}
	}
}

