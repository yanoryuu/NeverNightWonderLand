namespace Utage
{
    public interface IScenarioFileReaderSettingsExcel
    {
        /// エクセルの数式解析するか
        public bool ParseFormula { get; set; }

        /// エクセルの数字解析（桁区切り対策など）
        public bool ParseNumeric { get; set; }

        public IAdvScenarioFileReader CreateReader();

    }
}
