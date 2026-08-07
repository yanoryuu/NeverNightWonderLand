using System;
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;

namespace Utage
{

    public class SampleReplayVoice : MonoBehaviour
    {
        /// <summary>ADVエンジン</summary>
        public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
        [SerializeField] protected AdvEngine engine;

        bool EnableVoice { get; set; }

        void Start()
        {
            Engine.Page.OnBeginPage.AddListener(OnBeginPage);
            Engine.Page.OnEndPage.AddListener(OnEndPage);
            Engine.BacklogManager.OnPostAddData.AddListener(OnAddBackLogData);
        }

        //ページ開始
        void OnBeginPage(AdvPage page)
        {
            EnableVoice = false;
        }

        //ページ終了
        void OnEndPage(AdvPage page)
        {
            EnableVoice = false;
        }

        //現在のページのログデータ追加後に呼ばれる
        void OnAddBackLogData(AdvBacklogManager backlogManager)
        {
            EnableVoice = TryGetLogVoiceFileName(out string fileName, out string characterLabel);
            this.gameObject.SetActive(EnableVoice);
        }

        //今のログデータからボイス名とそのキャラクター名を取得
        bool TryGetLogVoiceFileName(out string fileName, out string characterLabel)
        {
            fileName = characterLabel = null;
            if (Engine == null || !Engine.IsStarted)
            {
                return false;
            }

            var log = Engine.BacklogManager.LastLog;
            if (log == null)
            {
                return false;
            }

            fileName = log.MainVoiceFileName;
            if (fileName.IsNullOrEmpty())
            {
                return false;
            }

            characterLabel = log.FindCharacerLabel(fileName);
            return true;
        }

        //ボタンが押された
        public void OnClick()
        {
            if (TryGetLogVoiceFileName(out string fileName, out string characterLabel))
            {
                StartCoroutine(CoPlayVoice(fileName, characterLabel));
            }
        }


        //ボイスの再生
        IEnumerator CoPlayVoice(string voiceFileName, string characterLabel)
        {
            AssetFile file = AssetFileManager.Load(voiceFileName, this);
            if (file == null)
            {
                Debug.LogError("Backlog voiceFile is NULL");
                yield break;
            }

            while (!file.IsLoadEnd)
            {
                yield return null;
            }

            SoundManager manager = SoundManager.GetInstance();
            if (manager)
            {
                manager.PlayVoice(characterLabel, file);
                if (Engine != null)
                {
                    Engine.ScenarioSound.ClearVoiceInScenario(characterLabel);
                }
            }

            file.Unuse(this);
        }

    }
}
