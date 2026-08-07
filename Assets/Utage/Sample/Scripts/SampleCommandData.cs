using UnityEngine;

namespace Utage
{
    public class SampleCommandData
    {
        public void GetCommandData(AdvCommand command)
        {
            //コマンドのID
            // AdvCommandParser.IdCharacterなど、AdvCommandParser.Idで始まる定数が定義されている
            var id = command.Id;
            
            //シナリオデータ（エクセルデータ）の解析には、基本的にはParseCellかParseCellOptionalを使う。
            var str1 = command.ParseCell<string>(AdvColumnName.Arg1);
            var str2 = command.ParseCell<string>(AdvColumnName.Arg2);
            
            //型を指定して解析することもできる(string、float、intのみ)
            var f = command.ParseCell<float>(AdvColumnName.Arg3);
            var i= command.ParseCell<int>(AdvColumnName.Arg4);
            
            //コマンドのエクセルデータ（行のデータ）
            var row = command.RowData;

            //指定の列名のセルの文字列データを取得（内部で先頭行のインデックスを解析している）
            var cellString = row.ParseCell<string>("Arg1");
            
            //指定の列名がない場合も考慮した文字列データを取得
            var cellString1 = row.ParseCellOptional<string>("Arg1","");


            //エクセルの1シートのデータ
            var grid = row.Grid;

            //エクセルの先頭行のデータ
            var header = grid.HeaderRow;

            //指定の名前の行の列のインデックスを取得（内部で先頭行のインデックスを解析している）
            var columnIndex = grid.GetColumnIndex("Arg1");
            //そのセルの文字列を取得
            var cellStringByIndex = row.Strings[columnIndex];

            //指定の名前の行がない場合も考慮した列のインデックスを取得
            if (grid.TryGetColumnIndex("Arg1",out int columnIndex1))
            {
                //そのセルの文字列を取得
                var cellStringByIndex1 = row.Strings[columnIndex1];
            }
        }
    }
}
