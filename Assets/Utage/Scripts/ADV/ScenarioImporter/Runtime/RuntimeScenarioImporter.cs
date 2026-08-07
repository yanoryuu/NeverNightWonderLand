using System;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //ランタイム（ゲーム実行中）にシナリオをインポートし、ロードするためのコンポーネント
    //インポート対象は、StringGridDictionaryとして、オンメモリ上にあるデータになる
    public class RuntimeScenarioImporter : MonoBehaviour
    {
        public AdvEngine Engine => this.GetComponentCacheInParent(ref engine);
        [SerializeField] AdvEngine engine;

        private List<DebugLogInfo> DebugLogInfoList { get; } = new ();

        //ログ出力をハンドルするためのメソッド
        void HandleLog(string logString, string stackTrace, LogType type)
        {
            DebugLogInfoList.Add(new DebugLogInfo(logString, stackTrace, type));
        }

        public bool TryImportAndLoadScenario(StringGrid stringGrid)
        {
            StringGridDictionary scenario = new StringGridDictionary();
            scenario.Add(stringGrid.SheetName, stringGrid);
            return TryImportAndLoadScenario(scenario);
        }

        public bool TryImportAndLoadScenario(StringGridDictionary scenario)
        {
            if (!TryImportScenario(scenario, out AdvChapterData chapterData))
            {
                return false;
            }
            if( !TryLoadChapter(chapterData) )
            {
                return false;
            }
            return true;
        }

        public bool TryImportScenario(StringGridDictionary scenario, out AdvChapterData chapterData)
        {
            DebugLogInfoList.Clear();
            Application.logMessageReceived += HandleLog;
            bool result = false;
            //例外をキャッチして、失敗したらログ出力して、falseを返す
            try
            {
                chapterData = Import(scenario);
                result = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                chapterData = null;
            }
            Application.logMessageReceived -= HandleLog;
            if (!result) return false;

            //ログ出力にエラーが含まれてるようなら、失敗とする
            if (DebugLogInfoList.Exists(x => x.IsErrorLogType()))
            {
                return false;
            }

            return true;
        }

        bool TryLoadChapter(AdvChapterData chapter)
        {
            DebugLogInfoList.Clear();
            bool result = false;
            AdvCommand.IsEditorErrorCheck = true;
            AdvCommand.IsEditorErrorCheckWaitType = true;
            Application.logMessageReceived += HandleLog;
            //シナリオデータの初期化
            try
            {
                Engine.DataManager.BootInitChapter(chapter);
                result = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Application.logMessageReceived -= HandleLog;

            AdvCommand.IsEditorErrorCheck = true;
            AdvCommand.IsEditorErrorCheckWaitType = true;

            if (result)
            {
                //ログ出力にエラーが含まれてるようなら、失敗とする
                if (DebugLogInfoList.Exists(x => x.IsErrorLogType()))
                {
                    result = false;
                }
            }

            if (!result)
            {
                //失敗したら、Engineに追加したシナリオデータを削除
                Engine.DataManager.RemoveChapter(chapter);
            }
            return result;
        }

        AdvChapterData Import(StringGridDictionary scenario)
        {
            var macroManager = Engine.DataManager.MacroManager;
            List<AdvImportBook> books = new List<AdvImportBook>() { };
            {
                AdvImportBook book = ScriptableObject.CreateInstance<AdvImportBook>();
                book.AddSourceBook(scenario);
                book.Reimport = true;
                books.Add(book);
            }
            AdvChapterData chapter = ScriptableObject.CreateInstance<AdvChapterData>();

            //マクロ適用して初期化
            chapter.ImportBooks(books, macroManager);
            chapter.MakeScenarioImportData(Engine.DataManager.SettingDataManager, macroManager);

            return chapter;
        }

    }
}
