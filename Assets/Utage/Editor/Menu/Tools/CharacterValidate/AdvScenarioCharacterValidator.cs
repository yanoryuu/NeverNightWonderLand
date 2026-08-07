#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //ADV用の、想定外の文字が含まれていないかチェックするクラス
    public class AdvScenarioCharacterValidator
    {
        AdvScenarioCharacterValidatorSettings Settings { get; }
        
        public AdvScenarioCharacterValidator(AdvScenarioCharacterValidatorSettings settings)
        {
            Settings = settings;
        }

        //検証する
        public void Validate(AdvScenarioImporterInEditor importerInEditor)
        {
            string projectName = importerInEditor.Project.ProjectName;
            List<AdvScenarioData> scenarioList = importerInEditor.ScenarioDataTbl.Values.ToList();

            switch (Settings.Language)
            {
                case AdvScenarioCharacterValidatorSettings.LanguageType.Single:
                    ValidateSingle();
                    break;
                case AdvScenarioCharacterValidatorSettings.LanguageType.Multiple:
                    ValidateMultiple();
                    break;
                default:
                    Debug.LogError($"LanguageType {Settings.Language} is not supported.");
                    break;
            }

            void ValidateSingle()
            {
                var validator = new AdvScenarioCharacterValidatorLanguage(projectName, "", this, scenarioList, MakeValidatorSingle());
                validator.Validate();
            }

            void ValidateMultiple()
            {
                var langManager = Settings.LanguageManager;
                if (langManager == null)
                {
                    Debug.LogError("Not found LanguageManager");
                    return;
                }

                //言語を切り替えながら検証
                string defaultLanguage = langManager.CurrentLanguage;
                foreach (var language in langManager.Languages)
                {
                    langManager.CurrentLanguage = language;
                    var validator =
                        new AdvScenarioCharacterValidatorLanguage(projectName, language, this, scenarioList, MakeValidatorMultiple(language));
                    validator.Validate();
                }

                langManager.CurrentLanguage = defaultLanguage;
            }
            
        }


        CharacterValidator MakeValidatorSingle()
        {
            var font = Settings.Font;
            var validator = new CharacterValidatorFontAsset(font);
            foreach (var fallback in font.fallbackFontAssetTable)
            {
                validator.AddFallbackFont(fallback);
            }
            foreach (var spriteAsset in Settings.SpriteAssets)
            {
                validator.AddSpriteAsset(spriteAsset);
            }
            return validator;
        }

        CharacterValidator MakeValidatorMultiple(string language)
        {
            var fallbackSettings = Settings.FontFallbackSettings;
            var validator = new CharacterValidatorFontAsset(fallbackSettings.Font);
            //フォント言語名一覧にない場合
            if (!fallbackSettings.FontSettings.TyGetFontLanguageName(language, out string fontLanguage))
            {
                //指定言語のフォントがないのでエラー
                Debug.LogError($"{language} : Font not found in {fallbackSettings.FontSettings.name}",
                    fallbackSettings.FontSettings);
            }
            else
            {
                var fallbacks = fallbackSettings.FindFallbackSettings(fontLanguage);
                if (fallbacks == null)
                {
                    Debug.LogError($"{fontLanguage} is not found in {nameof(FontFallbackSettingsInEditor)}",
                        fallbackSettings);
                }
                else if (fallbackSettings.TryLoadFallbackList(fallbacks, out var fallbackList))
                {
                    foreach (var fallback in fallbackList)
                    {
                        validator.AddFallbackFont(fallback);
                    }
                }
            }

            foreach (var spriteAsset in Settings.SpriteAssets)
            {
                validator.AddSpriteAsset(spriteAsset);
            }

            return validator;
        }

        class AdvScenarioCharacterValidatorLanguage
        {
            string ProjectName { get; }
            string Language { get; }
            AdvScenarioCharacterValidator Settings { get; }
            List<AdvScenarioData> ScenarioList { get; }
            UnicodeCharacterCollection UsingCharacters { get; } = new();
            CharacterValidator Validator { get; }

            
            public AdvScenarioCharacterValidatorLanguage(
                string projectName, 
                string language,
                AdvScenarioCharacterValidator settings,
                List<AdvScenarioData> scenarioList,
                CharacterValidator validator)
            {
                ProjectName = projectName;
                Language = language;
                Settings = settings;
                ScenarioList = scenarioList;
                Validator = validator;
            }
            
            public void Validate()
            {
                UsingCharacters.Clear();
                //全シナリオをチェック
                foreach (var scenario in ScenarioList)
                {
                    foreach (var keyValue in scenario.ScenarioLabels)
                    {
                        foreach (AdvScenarioPageData page in keyValue.Value.PageDataList)
                        {
                            foreach (var command in page.CommandList)
                            {
                                ValidateCommand(command);
                            }
                        }
                    }
                }
                
                //ログの出力
                OutputLog();
            }


            //コマンドが使用するテキスト文字の検証
            void ValidateCommand(AdvCommand command)
            {
                string text = GetText(command);
                if (text.Length <= 0) return;
                
                var unicodeList = FontUtil.ToUnicodeCharacters(text).ToList();
                foreach (var unicode in unicodeList)
                {
                    //使用文字として追加
                    UsingCharacters.AddCharacter(unicode);
                }

                bool validate = true;
                HashSet <uint> errorCharacters = null;
                foreach (var unicode in unicodeList)
                {
                    if (!Validator.Validate(unicode))
                    {
                        errorCharacters ??= new HashSet<uint>();
                        errorCharacters.Add(unicode);
                        validate = false;
                    }
                }
                if (!validate)
                {
                    string errorCharacterString = FontUtil.UnicodeCharactersToString(errorCharacters.Select(x=>new UnicodeCharacter(x)));
                    string errorMsg =
                        $"{errorCharacterString} : Characters not allowed in {Validator.TargetAsset.name}";
                    Debug.LogWarning(command.ToErrorString(errorMsg), Validator.TargetAsset);
                }
            }

            //コマンドが使用するテキストを文字列として取得
            string GetText(AdvCommand command)
            {
                if (command is not IAdvCommandTexts texts) return "";
                string text = "";
                foreach (string t in texts.GetTextStrings())
                {
                    text += t;
                }

                return text;
            }

            void OutputLog()
            {
                //不足文字をログ出力
                var missing = Validator.MissingCharacters.Characters.Values;
                int count = missing.Count;
                if (count > 0)
                {
                    var missingCharacters =
                        FontUtil.UnicodeCharactersToString(Validator.MissingCharacters.Characters.Values);
                    string errorMsg =
                        $"{count} Characters not allowed in {Validator.TargetAsset.name}\n{missingCharacters}";
                    Debug.LogError(errorMsg, Validator.TargetAsset);
                }

                //ログファイルの出力
                string path = GetLogOutputPath();
                if(string.IsNullOrEmpty(path)) return;
                
                Debug.Log($"Output Log : {path}");

                //使用文字をログ出力
                UsingCharacters.Output(Path.Combine(path, $"Characters.txt"));
                UsingCharacters.OutputLog(Path.Combine(path, $"_log.txt"),"");
                
                Validator.MissingCharacters.Output(Path.Combine(path, $"{Validator.TargetAsset.name}_MissingCharacters.txt"));
            }


            //ログの出力先パスを取得
            string GetLogOutputPath()
            {
                if (string.IsNullOrEmpty(Settings.Settings.LogOutputPath)) return string.Empty;

                string outputDir = Path.Combine(Settings.Settings.LogOutputPath, $"{ProjectName}/");

                if (!string.IsNullOrEmpty(Language))
                {
                    outputDir = Path.Combine(outputDir, $"{Language}/");
                }

                return outputDir;
            }

        }

    }
}
#endif
