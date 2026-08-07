#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utage
{
    //宴のユーザーごとのエディター設定
    //プロジェクトのUserSettingsフォルダ以下に置く
    [FilePath("UserSettings/UtageEditorUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UtageEditorUserSettings : EditorSettingsSingleton<UtageEditorUserSettings>
    {
        //宴のシーンが切り替わったら、自動で宴プロジェクトを切り替える
        public bool AutoChangeProject { get { return autoChangeProject; } }
        [SerializeField]
        bool autoChangeProject = true;

        //現在のプロジェクト
        public AdvScenarioDataProject CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;
                this.OnSave();
            }
        }
        [SerializeField] AdvScenarioDataProject currentProject = null;

        [Serializable]
        public class ImportSettings
        {
            //自動インポートタイプ
            public enum AutoImportType
            {
                Always, //常に
                OnUtageScene, //宴のシーンのみ
                None, //行わない
            };

            /// <summary>
            /// インポートタイプ
            /// </summary>
            [SerializeField] AutoImportType autoImportType = AutoImportType.OnUtageScene;


            /// 簡易インポートをするか
            public enum QuickImportType
            {
                None,
                Quick,
                QuickChapter,
                QuickChapterWithZeroChapter,
            }

            [SerializeField] QuickImportType quickImportType = QuickImportType.None;
            public QuickImportType QuickImport => quickImportType;

            //インポート時に空白をチェックする
            public bool CheckWhiteSpace => checkWhiteSpace;
            [SerializeField] bool checkWhiteSpace = true;

            //テキストを検証する際の設定
            public AdvTextValidatorUserSettings TextValidator => textValidator;
            [SerializeField, UnfoldedSerializable] AdvTextValidatorUserSettings textValidator = new();

            //シナリオログファイルを出力するか
            public bool EnableScenarioLogFile => enableScenarioLogFile;
            [SerializeField] bool enableScenarioLogFile = true;
            
            public bool CheckAutoImportType()
            {
                switch (autoImportType)
                {
                    case AutoImportType.None:
                        return false;
                    case AutoImportType.OnUtageScene:
                        if (WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngine>() == null)
                        {
                            return false;
                        }

                        return true;
                    case AutoImportType.Always:
                    default:
                        return true;
                }
            }

            public bool EnableQuickImport => QuickImport != QuickImportType.None;

        }
        [SerializeField,UnfoldedSerializable]
        ImportSettings importSettings = new();

        public ImportSettings ImportSetting => importSettings;

    }
}
#endif
