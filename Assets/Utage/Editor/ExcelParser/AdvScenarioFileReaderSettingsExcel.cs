using System.Collections.Generic;
using UnityEngine;

namespace Utage.ExcelParser
{
	//エクセル形式のシナリオリーダーの設定
	[CreateAssetMenu(menuName = "Utage/ScenarioFileReader/Excel", fileName = "ExcelFileReaderSettings")]
	public class AdvScenarioFileReaderSettingsExcel : ScenarioFileReaderSettings, IScenarioFileReaderSettingsExcel
	{
		/// エクセルの数式解析するか
		[SerializeField] bool parseFormula;

		public bool ParseFormula
		{
			get => parseFormula;
			set => parseFormula = value;
		}

		/// エクセルの数字解析（桁区切り対策など）
		[SerializeField] bool parseNumeric;

		public bool ParseNumeric
		{
			get => parseNumeric;
			set => parseNumeric = value;
		}

		/// エクセルのヘッダ解析
		[SerializeField] bool parseHeader = true;
		public bool ParseHeader
		{
			get => parseHeader;
			set => parseHeader = value;
		}

		// 無視するファイルの接頭辞
		[SerializeField] List<string> ignorePrefixes = new() { @"~$" };

		public List<string> IgnorePrefixes
		{
			get => ignorePrefixes;
			set => ignorePrefixes = value;
		}

		// シートのコメント記号（この文字で始まるシート名はインポートから除外する）
		// '\0' を設定するとコメントアウト除外を行わない
		[SerializeField] char sheetCommentPrefix = '#';

		public char SheetCommentPrefix
		{
			get => sheetCommentPrefix;
			set => sheetCommentPrefix = value;
		}

		public override IAdvScenarioFileReader CreateReader() => new AdvScenarioFileReaderExcel(this);
	}
}
