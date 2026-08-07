#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Utage;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using TextAsset = UnityEngine.TextAsset;

namespace Utage
{
    //文字コレクションのジェネレーター
    [CreateAssetMenu(menuName = "Utage/Font/"+nameof(CharacterCollectionGeneratorSetting))]
    public class CharacterCollectionGeneratorSetting : ScriptableObject
    {
        //入力要素
        //ここにある文字情報を基本的に含める
        [Serializable]
        class Input
        {
            //テキストファイル内の文字をすべて含める
            public List<TextAsset> Texts => texts; 
            [SerializeField] List<TextAsset> texts = new();

            //フォントファイル内の文字をすべて含める
            //通常はフォント内の文字を全部入れるために使う。
            //一応複数設定可能にしておく
            public List<Font> Fonts => fonts;
            [SerializeField] List<Font> fonts = new();

            //特殊文字追加文字（除外要素にあっても関係なく追加する文字）
            public string ExtraCharacters => extraCharacters;
            [SerializeField] string extraCharacters = "_…";
        }
        [SerializeField, UnfoldedSerializable] Input input = new();

        //除外要素
        //ここにある文字情報を含まないようにする
        [Serializable]
        class Remove
        {
            //テキストファイル内の文字をすべて除外する
            public List<TextAsset> Texts => texts;
            [SerializeField] List<TextAsset> texts = new();

            //対象のTMPフォントアセット内の文字をすべて除外する
            //フォールバックで組み合わせるフォントを設定することを想定
            public List<TMP_FontAsset> FontAssets => fontAssets;
            [SerializeField] List<TMP_FontAsset> fontAssets = new();
            
            //サロゲートペア文字を除外する
            public bool SurrogatePare => surrogatePare;
            [SerializeField] bool surrogatePare = false;

            //組み文字を除外する
            public bool CombiningMark => combiningMark;
            [SerializeField] bool combiningMark = false;
        }
        [SerializeField, UnfoldedSerializable] Remove remove = new();

        //対象の情報
        //フォントがサポートしてない文字は含めないようにする
        [Serializable]
        class Target
        {
            //対象のフォントファイル
            public Font Font => font;
            [SerializeField] Font font;
        }
        [SerializeField, UnfoldedSerializable] Target target = new();


        //出力先の情報
        [Serializable]
        class Output
        {
            //出力先のフォルダ
            public UnityEngine.Object OutputDir => outputDir;
            [SerializeField, Folder] UnityEngine.Object outputDir;
            
            //指定の文字数を超えたら分割する
            public int MaxGlyphCount => maxGlyphCount;
            [SerializeField] int maxGlyphCount;
        }
        [SerializeField,UnfoldedSerializable] Output output = new();


        [SerializeField, Button(nameof(Generate), nameof(DisableGenerate), false)]
        string generate;

        bool DisableGenerate()
        {
            if (this.output.OutputDir == null) return true;
            if(this.output.MaxGlyphCount>0 && target.Font == null) return true;
            return false;
        }

        void Generate()
        {
            CharacterCollectionGenerator generator = new CharacterCollectionGenerator(this);
            if (generator.TryGenerateCharacterCollection())
            {
                generator.OutputFile();
            }
        }

        public class CharacterCollectionGenerator
        {
            CharacterCollectionGeneratorSetting Settings { get; }
            UnicodeCharacterCollection CharacterCollection { get; } = new();
            UnicodeCharacterCollection RemovedCharacterCollection { get; } = new();
            HashSet<uint> GlyphIndexSet { get; } = new();

            public CharacterCollectionGenerator(CharacterCollectionGeneratorSetting settings)
            {
                Settings = settings;
            }

            public bool TryGenerateCharacterCollection()
            {
                CharacterCollection.Clear();
                RemovedCharacterCollection.Clear();
                GlyphIndexSet.Clear();

                //Inputにある文字を重複を除いて追加する
                MergeInputSettings();
                //Removeにある文字を削除する
                MergeRemoveSettings();
                //Targetにある文字を削除し、グリフ数をカウント
                MergeTargetSettings();
                return true;
            }

            //Inputにある文字を重複を除いて追加する
            void MergeInputSettings()
            {
                var inputSettings = Settings.input;
                //特殊文字は先頭に置く
                CharacterCollection.AddCharacters(inputSettings.ExtraCharacters);
                
                //テキスト内の文字を収録
                foreach (var text in inputSettings.Texts)
                {
                    if(text==null) continue;
                    CharacterCollection.AddCharacters(text.text);
                }

                //フォントファイル内の文字を収録
                foreach (var font in inputSettings.Fonts)
                {
                    if (font == null) continue;
                    var fontEngine = new FontEngineService(font);
                    if(fontEngine.Error) continue;

                    foreach (var (unicode, glyphIndex) in fontEngine.GetAllUnicodeAndGlyphIndex())
                    {
                        CharacterCollection.AddCharacter(unicode);
                    }
                }
            }

            //Removeにある文字を削除する
            void MergeRemoveSettings()
            {
                var removeSettings = Settings.remove;
                UnicodeCharacterCollection removeCollection = new();

                //テキスト内の文字を削除
                foreach (var text in removeSettings.Texts)
                {
                    if (text == null) continue;

                    var removeText = text.text;
                    //削除対象の文字を追加
                    removeCollection.AddCharacters(removeText);
                }

                //フォントアセット内の文字を削除
                foreach (var fontAsset in removeSettings.FontAssets)
                {
                    if (fontAsset == null) continue;
                    
                    foreach (TMP_Character tmpCharacter in fontAsset.characterTable)
                    {
                        //削除対象の文字を追加
                        removeCollection.AddCharacter(tmpCharacter.unicode);
                    }
                }
              
                //設定に従って削除
                foreach (var character in CharacterCollection.Characters)
                {
                    //サロゲートペア文字を削除
                    if (removeSettings.SurrogatePare && character.Value.IsSurrogatePair)
                    {
                        removeCollection.AddCharacter(character.Key);
                    }

                    //組み文字を削除
                    if (removeSettings.CombiningMark && character.Value.IsCombiningMark)
                    {
                        removeCollection.AddCharacter(character.Key);
                    }
                }

                //特種文字は削除できないので除外
                foreach (uint unicode in FontUtil.ToUnicodeCharacters(Settings.input.ExtraCharacters))
                {
                    removeCollection.RemoveCharacter(unicode);
                }

                //削除対象の文字を削除
                CharacterCollection.RemoveCharacters(removeCollection,RemovedCharacterCollection);
            }

            //対象のフォントにない文字を削除し、グリフ数をカウント
            void MergeTargetSettings()
            {
                var targetSettings = Settings.target;
                UnicodeCharacterCollection removeCollection = new();
                if (targetSettings.Font != null)
                {
                    FontEngineService fontEngineService = new FontEngineService(targetSettings.Font);
                    if ( !fontEngineService.Error)
                    {
                        foreach (var keyValue in CharacterCollection.Characters)
                        {
                            var unicode = keyValue.Value.Unicode;
                            if (FontEngine.TryGetGlyphIndex(unicode, out uint glyphIndex))
                            {
                                GlyphIndexSet.Add(glyphIndex);
                            }
                            else
                            {
                                removeCollection.AddCharacter(unicode);
                            }
                        }
                    }
                }
                
                //削除対象の文字を削除
                CharacterCollection.RemoveCharacters(removeCollection, RemovedCharacterCollection);
            }


            public void OutputFile()
            {
                var outputDirAsset = Settings.output.OutputDir;
                var outputDir = AssetDatabase.GetAssetPath(outputDirAsset);
                var outputLogDir = Path.Combine(outputDir, "Log~/");
                //通常の全出力
                CharacterCollection.Output(Path.Combine(outputDir, $"{Settings.name}.txt"));
                //分割ファイルの出力
                OutputSplitCharacterCollection(outputDir, outputLogDir);

                var logHeader = $"{Settings.name}\n";
                if (GlyphIndexSet.Count > 0)
                {
                    logHeader += $"glyphs count : {GlyphIndexSet.Count}\n";
                }
                CharacterCollection.OutputLog(Path.Combine(outputLogDir, $"{Settings.name}_log.txt"),logHeader,true);
                RemovedCharacterCollection.OutputLog(Path.Combine(outputLogDir, $"{Settings.name}_removed_log.txt"), $"{Settings.name}\n");

                EditorUtility.SetDirty(outputDirAsset);
                AssetDatabase.Refresh();
            }
            
            void OutputSplitCharacterCollection(string outputDir, string outputLogDir)
            {

                //分割ファイルの出力
                int countFile = 1;
                int maxGlyph = Settings.output.MaxGlyphCount;
                int glyphCount = GlyphIndexSet.Count;
                if (maxGlyph > 0 && glyphCount > maxGlyph)
                {
                    countFile = ((glyphCount -1) / maxGlyph)+1;
                }
                if(countFile==1)
                {
                    return;
                }
                
                //出力済みのキャラクターコレクション
                UnicodeCharacterCollection characterCollectionOutput = new();

                for (int i = 0; i < countFile; ++i)
                {
                    (UnicodeCharacterCollection characterCollection, HashSet<uint > glyphIndexSet) = SplitCharacterCollection(maxGlyph, characterCollectionOutput);
                    string name = $"{Settings.name}_{i}of{countFile}";
                    string path = Path.Combine(outputDir, $"{name}.txt");

                    characterCollection.Output(path);
                    var logHeader = $"{name}\n"
                                    + $"glyphs count : {glyphIndexSet.Count}\n";
                    characterCollection.OutputLog(Path.Combine(outputLogDir, $"{name}_log.txt"), logHeader, true);

                    characterCollectionOutput.AddCharacters(characterCollection);
                }
                if(characterCollectionOutput.Characters.Count != CharacterCollection.Characters.Count)
                {
                    Debug.LogError($"Failed output character collection {Settings.name}");
                }
            }
            
            //分割されたキャラクターコレクションを作成する
            (UnicodeCharacterCollection, HashSet<uint> )SplitCharacterCollection(int maxGlyph, UnicodeCharacterCollection characterCollectionOutput)
            {
                UnicodeCharacterCollection characterCollection = new();
                HashSet<uint> glyphIndexSet = new HashSet<uint>();
                foreach (var keyValue in CharacterCollection.Characters)
                {
                    var unicode = keyValue.Value.Unicode;
                    //既に出力済みの文字は追加しない
                    if(characterCollectionOutput.Characters.ContainsKey(keyValue.Key)) continue;

                    if (FontEngine.TryGetGlyphIndex(unicode, out uint glyphIndex))
                    {
                        if (glyphIndexSet.Contains(glyphIndex))
                        {
                            //既に存在するグリフなら、文字は無条件で追加
                            characterCollection.AddCharacter(unicode);
                        }
                        else
                        {
                            //新たなグリフなら、最大グリフ数を超えなければ追加
                            if (glyphIndexSet.Count < maxGlyph)
                            {
                                characterCollection.AddCharacter(unicode);
                                glyphIndexSet.Add(glyphIndex);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed get glyph index : {unicode} : {keyValue.Value.Character}");
                    }
                }
                return (characterCollection,glyphIndexSet);
            }
        }
    }
}

#endif
