// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;

namespace Utage
{

	[AddComponentMenu("Utage/ADV/Examples/SampleParam")]
	public class SampleParam : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		public void ParamTest()
		{
			Engine.Param.GetParameter("flag1");
			Engine.Param.TrySetParameter("flag1", true);
		}

		//宴のパラメーターを文字列として書き出す
		//第二引数を、AdvParamData.FileType.Systemにすると、System系のパラメーターを書き出す
		public string WriteParam(AdvParamData.FileType fileType = AdvParamData.FileType.Default)
		{
			if (Engine.IsWaitBootLoading)
			{
				Debug.LogError("宴の起動処理が終わっていません");
			}

			//宴のパラメーターをバイナリ化
			byte[] buffer = BinaryUtil.BinaryWrite((writer) => Engine.Param.Write(writer, fileType));
			//バイナリを文字列化
			string saveData = System.Convert.ToBase64String(buffer);
			return saveData;
		}

		//文字列化したデータから、宴のパラメーターを読み込む
		//第二引数を、AdvParamData.FileType.Systemにすると、System系のパラメーターに書き込む
		public void ReadParam(string saveData, AdvParamData.FileType fileType = AdvParamData.FileType.Default)
		{
			if (Engine.IsWaitBootLoading)
			{
				Debug.LogError("宴の起動処理が終わっていません");
			}

			//文字列をバイナリ化
			byte[] buffer = System.Convert.FromBase64String(saveData);
			//バイナリから宴のパラメーターを読み込み
			BinaryUtil.BinaryRead(buffer, (reader) => Engine.Param.Read(reader, fileType));
		}

		const string UtageSaveKey = "UtageSaveKey";

		//Unity公式のPlayerPrefsを使ってパラメーターのみを書き込むサンプル
		public void LoadSaveDataByPlayerPrefs()
		{
			PlayerPrefs.SetString(UtageSaveKey, WriteParam());
		}

		//Unity公式のPlayerPrefsを使ってパラメーターのみを読み込むサンプル
		public void ReadSaveDataByPlayerPrefs()
		{
			var saveData = PlayerPrefs.GetString(UtageSaveKey);
			if (string.IsNullOrEmpty(saveData))
			{
				//まだセーブされてない
				return;
			}

			ReadParam(saveData);
		}

	}
}
