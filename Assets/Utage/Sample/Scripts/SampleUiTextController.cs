// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using System.Collections;
using System.IO;
using TMPro;
using UtageExtensions;

namespace Utage
{

	/// シーン内のUIテキストを制御するコンポーネント
	/// SendMessageByNameでシナリオから呼ばれる
	public class SampleUiTextController : MonoBehaviour
		,IAdvSaveData
	{
		[SerializeField] GameObject root;
		[SerializeField] TextMeshProUGUI text;

		//UIテキストを表示
		void OpenTextUi(AdvCommandSendMessageByName command)
		{
			root.SetActive(true);
			text.text = command.ParseCellLocalizedText();
		}

		//UIテキストを非表示
		void CloseTextUi(AdvCommandSendMessageByName command)
		{
			root.SetActive(false);
		}

		//セーブデータのキー。重複しないように注意
		//コンポーネントを複数使う場合などで、重複する場合はGameObjectの名前などを付加して区別する
		public string SaveKey => nameof(SampleUiTextController);

		//クリア(初期化時等に呼ばれる）
		public void OnClear()
		{
			root.SetActive(false);
			text.text =  "";
		}
		
		//セーブデータの書き込み
		public void OnWrite(BinaryWriter writer)
		{
			writer.Write(root.activeSelf);
			writer.Write(text.text);
		}

		//セーブデータの読み込み
		public void OnRead(BinaryReader reader)
		{
			root.SetActive(reader.ReadBoolean());
			text.text = reader.ReadString();
		}
	}

}
