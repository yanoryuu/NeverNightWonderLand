// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// ADV用SendMessageByNameコマンドから送られたメッセージを受け取る処理のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleSendMessageByName")]
	public class SampleSendMessageByName : MonoBehaviour, IAdvSaveData
	{
		//Testという処理を呼ぶ。引数にはAdvCommandSendMessageByNameを持つ
		void Test(AdvCommandSendMessageByName command)
		{
			Debug.Log("SendMessageByName");
		}

		//引数を使った例
		void TestArg(AdvCommandSendMessageByName command)
		{
			Debug.Log(command.ParseCellOptional<string>(AdvColumnName.Arg3, "arg3"));
		}

		//引数を使った例
		void TestWait(AdvCommandSendMessageByName command)
		{
			StartCoroutine(CoWait(command));
		}

		IEnumerator CoWait(AdvCommandSendMessageByName command)
		{
			command.IsWait = true;

			float time = command.ParseCellOptional<float>(AdvColumnName.Arg3, 0);
			while (true)
			{
				Debug.Log(time);
				time -= command.Engine.Time.DeltaTime;
				if (time <= 0) break;
				yield return null;
			}

			command.IsWait = false;
		}

		//広告のオンオフフラグなど
		public bool isAdOpen = false;

		//シナリオからコマンドを呼んで切り替える
		void SetEnableAdvertise(AdvCommandSendMessageByName command)
		{
			this.isAdOpen = command.ParseCellOptional<bool>(AdvColumnName.Arg3, false);
		}



		//データのキー
		public string SaveKey
		{
			get { return "SampleSendMessageByName"; }
		}

		//クリアする
		public void OnClear()
		{
			this.isAdOpen = false;
		}

		//バージョンチェックしたほうが安全
		const int Version = 0;

		//書き込み
		public void OnWrite(System.IO.BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(this.isAdOpen);
		}

		//読み込み
		public void OnRead(System.IO.BinaryReader reader)
		{
			//バージョンチェック
			int version = reader.ReadInt32();
			if (version == Version)
			{
				this.isAdOpen = reader.ReadBoolean();
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}

}
