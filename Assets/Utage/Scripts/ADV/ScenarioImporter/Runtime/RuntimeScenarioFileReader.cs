using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //ランタイム（ゲーム実行中）にシナリオファイルを読み込むためのコンポーネント
    //指定フォルダ以下にあるファイルのうち、設定されたリーダーの拡張子に一致するものを読み込む
    public class RuntimeScenarioFileReader : MonoBehaviour
        , IAdvScenarioDataProject
    {
        List<ScenarioFileReaderSettings> ScenarioFileReaderSettings => scenarioFileReaderSettings;
        [SerializeField] List<ScenarioFileReaderSettings> scenarioFileReaderSettings;

        bool DebugLog => debugLog;
        [SerializeField] bool debugLog = false; 

        public List<DebugLogInfo> DebugLogInfoList { get; } = new();

        public bool TryReadScenario(string directory, out StringGridDictionary scenario)
        {
            //ログ出力をハンドルする
            void HandleLog(string logString, string stackTrace, LogType type)
            {
                DebugLogInfoList.Add(new DebugLogInfo(logString, stackTrace, type));
            }

            DebugLogInfoList.Clear();
            Application.logMessageReceived += HandleLog;
            bool result = TryReadScenarioFiles(directory, out scenario);
            Application.logMessageReceived -= HandleLog;
            if (!result) return false;

            //ログ出力にエラーが含まれてるようなら、失敗とする
            if (DebugLogInfoList.Exists(x => x.IsErrorLogType()))
            {
                return false;
            }

            return true;
        }

        bool TryReadScenarioFiles(string directory, out StringGridDictionary scenario)
        {
            try
            {
                scenario = ReadScenarioFiles(directory);
                return true;
            }
            catch (Exception e)
            {
                scenario = null;
                Debug.LogException(e);
                return false;
            }
        }

        StringGridDictionary ReadScenarioFiles(string directory)
        {
            var files = GetFilePaths(directory);
            AdvScenarioFileReaderManager readerManager = new AdvScenarioFileReaderManager(this, files);
            readerManager.DebugLog = DebugLog;
            return readerManager.ReadAllFile();
        }

        public IEnumerable<IAdvScenarioFileReader> CreateScenarioFileReaders(AdvScenarioFileReaderManager manager)
        {
            if (ScenarioFileReaderSettings.Count <= 0)
            {
                Debug.LogWarning("シナリオリーダーの設定が設定されていません",this);
            }
            foreach (var readerSetting in ScenarioFileReaderSettings)
            {
                yield return readerSetting.CreateReader();
            }
        }

        static List<string> GetFilePaths(string directoryPath)
        {
            List<string> filePaths = new List<string>();

            // ディレクトリ内のすべてのファイルを取得
            string[] files = Directory.GetFiles(directoryPath);

            foreach (string file in files)
            {
                filePaths.Add(file);
            }

            // サブディレクトリ内のファイルを再帰的に取得
            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                List<string> subdirectoryFilePaths = GetFilePaths(subdirectory);
                filePaths.AddRange(subdirectoryFilePaths);
            }

            return filePaths;
        }
    }
}
