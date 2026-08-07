// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// Sample LoadErrorのコールバック関数を書き換え
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleLoadError")]
	public class SampleLoadError : MonoBehaviour
	{
		void Awake()
		{
			AssetFileManager.SetLoadErrorCallBack(CustomCallbackFileLoadError);
		}

		bool isWaitingRetry;

		void CustomCallbackFileLoadError(AssetFile file)
		{
			string errorMsg = "インターネットに接続した状況でプレイしてください";
			if (SystemUi.GetInstance() != null)
			{
				if (isWaitingRetry)
				{
					StartCoroutine(CoWaitRetry(file));
				}
				else
				{
					isWaitingRetry = true;
					//リロードを促すダイアログを表示
					SystemUi.GetInstance().OpenDialog1Button(
						errorMsg, LanguageSystemText.LocalizeText(SystemText.Retry),
						() =>
						{
							isWaitingRetry = false;
							OnRetry(file);
						});
				}
			}
			else
			{
				OnRetry(file);
			}
		}


		//リトライダイアログからの応答を待つ
		IEnumerator CoWaitRetry(AssetFile file)
		{
			while (isWaitingRetry)
			{
				yield return null;
			}

			OnRetry(file);
		}


		void OnRetry(AssetFile file)
		{
			AssetFileManager.ReloadFile(file);
		}
	}
}
