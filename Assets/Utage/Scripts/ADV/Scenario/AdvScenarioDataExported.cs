// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// シナリオのエクスポートデータ
	/// </summary>
	[System.Serializable]
	public partial class AdvScenarioDataExported : ScriptableObject
	{
		/// <summary>
		/// シナリオデータ
		/// </summary>
		public List<StringGridDictionaryKeyValue> List
		{
			get { return dictionary.List; }
		}
		[SerializeField]
		StringGridDictionary dictionary = null;

		/// <summary>
		/// データクリア
		/// </summary>
		public void Clear()
		{
			dictionary.Clear();
		}

		/// <summary>
		/// エクセルからデータ解析
		/// </summary>
		/// <param name="sheetName">シート名</param>
		/// <param name="grid">エクセルの1シートから作成したStringGrid</param>
		public void ParseFromExcel(string sheetName, StringGrid grid)
		{
			dictionary.Add( sheetName, grid );
		}
	}
}