// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
	//メッセージスピードのデモ表示のサンプル
	public class SampleMessageSpeedDemo : MonoBehaviour
	{
		[SerializeField]
		AdvEngine engine = null;

		//既読メッセージのスピードならtrue
		[SerializeField]
		bool readMessage = false;

		//表示するテキスト文字
		[SerializeField]
		string message = "メッセージ速度のデモのテキスト";

		//実際に表示をするTextコンポーネント
		[SerializeField]
		Text text = null;

		bool IsPlaying { get; set; }
		bool IsFirst { get; set; }
		int CurrentTextLength { get; set; }

		int TextLengthMax { get { return message.Length; } }

		float DeltaTime { get; set; }

		public void PlayDemo()
		{
			IsPlaying = true;
			IsFirst = true;
			CurrentTextLength = 0;
			DeltaTime = 0;
		}

		void Update()
		{
			if (IsPlaying)
			{
				UpdateText();
			}
		}

		void UpdateText()
		{
			if (IsFirst)
			{
				//最初のフレームだけ長さ0をいったん表示
				CurrentTextLength = 0;
				IsFirst = false;
			}
			else
			{
				//文字送り
				float timeCharSend = engine.Config.GetTimeSendChar(readMessage);
				SendChar(timeCharSend);
			}

			text.text = message.Substring(0, CurrentTextLength);
			if (CurrentTextLength>= TextLengthMax)
			{
				IsPlaying = false;
			}
		}

		void SendChar(float timeCharSend)
		{
			if (timeCharSend <= 0)
			{
				CurrentTextLength = TextLengthMax;
				return;
			}

			DeltaTime += UnityEngine.Time.deltaTime;
			while (DeltaTime >= 0)
			{
				++CurrentTextLength;
				DeltaTime -= timeCharSend;
				if (CurrentTextLength > TextLengthMax)
				{
					CurrentTextLength = TextLengthMax;
					break;
				}
			}
		}
	}
}
