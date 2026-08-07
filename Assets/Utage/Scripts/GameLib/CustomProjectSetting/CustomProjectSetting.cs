// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

    /// <summary>
    /// 表示言語切り替え用のクラス
    /// </summary>
    public class CustomProjectSetting : ScriptableObject
    {
        [RuntimeInitializeStaticField] static CustomProjectSetting instance;
        /// <summary>
        /// シングルトンなインスタンスの取得
        /// </summary>
        /// <returns></returns>
        public static CustomProjectSetting Instance
        {
            get
            {
                if (instance == null)
                {
                    BootCustomProjectSetting boot = WrapperFindObject.FindObjectOfType<BootCustomProjectSetting>();
                    if (boot != null)
                    {
                        instance = boot.CustomProjectSetting;
                        if (instance == null)
                        {
                            Debug.LogError("CustomProjectSetting is NONE", boot);
                        }
                    }
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }
		
        /// <summary>
        /// 設定言語
        /// </summary>
        public LanguageManager Language
        {
            get { return language; }
            set { language = value; }
        }
        [SerializeField]
        LanguageManager language;
        
        
        /// <summary>
        /// 設定言語
        /// </summary>
        public bool UseSheetNameToScenarioLabel
        {
            get { return useSheetNameToScenarioLabel; }
            set { useSheetNameToScenarioLabel = value; }
        }
        [SerializeField] bool useSheetNameToScenarioLabel = true;
        
        
        //構造化マクロのパーサー設定(基本はnullで、使用したい場合のみ設定する)
        public StructuredMacroParser StructuredMacroParser => structuredMacroParser;
        [SerializeField] StructuredMacroParser structuredMacroParser = null;

        //カスタムデータの設定
        public AdvCustomDataSettings CustomDataSettings => customDataSettings;
        [SerializeField] AdvCustomDataSettings customDataSettings = new();

    }
}
