using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using Utage;

/// <summary>
/// DialogueScene (Additive) 内で AdvEngine を制御するコントローラ。
/// 再生中は GamePause で時間を止め、宴のカメラをメインカメラのスタックに
/// Overlay として載せる (URP)。呼び出しは <see cref="DialogueService"/> 経由を想定。
/// </summary>
public class UtageDialogueScene : MonoBehaviour
{
    [Tooltip("制御対象の AdvEngine (初期状態は非アクティブ)")]
    [SerializeField] private AdvEngine _advEngine;

    [Tooltip("再生中にメインカメラのスタックへ Overlay として追加する宴のカメラ (depth 昇順)")]
    [SerializeField] private Camera[] _overlayCameras;

    [Tooltip("DialogueScene 内の EventSystem。ゲーム側に既にあれば重複を避けて無効化する")]
    [SerializeField] private EventSystem _eventSystem;

    /// <summary>会話を再生中か。</summary>
    public bool IsPlaying { get; private set; }

    private Camera _stackedBaseCamera;

    private void Awake()
    {
        // EventSystem はシーン全体で1つが原則。ゲーム側に既にあればこちらを無効化する。
        if (_eventSystem != null &&
            FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length > 1)
        {
            _eventSystem.gameObject.SetActive(false);
        }

        StripUtageLayersFromGameCameras();
    }

    /// <summary>
    /// ゲーム側カメラの cullingMask から宴用レイヤーを除外する。
    /// cullingMask が Everything のカメラが宴のオブジェクトを二重描画するのを防ぐ。
    /// </summary>
    private void StripUtageLayersFromGameCameras()
    {
        int mask = LayerMask.GetMask("Utage", "UtageUI");
        if (mask == 0)
            return;

        foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(_overlayCameras, cam) < 0)
                cam.cullingMask &= ~mask;
        }
    }

    /// <summary>指定ラベルのシナリオを再生する。終了時に onComplete が呼ばれる。</summary>
    public void Play(string scenarioLabel, Action onComplete = null)
    {
        if (IsPlaying)
        {
            Debug.LogWarning($"[{nameof(UtageDialogueScene)}] 会話再生中のため '{scenarioLabel}' を無視しました。", this);
            return;
        }

        if (_advEngine == null)
        {
            Debug.LogWarning($"[{nameof(UtageDialogueScene)}] AdvEngine が設定されていません。", this);
            return;
        }

        StartCoroutine(PlayAsync(scenarioLabel, onComplete));
    }

    /// <summary>
    /// エンジンを起動し、ブート完了後にインポート済みの全シナリオラベルを渡す (デバッグ用)。
    /// シナリオファイルの形式 (xls/tsv) に依存せず、実際に再生できるラベルだけが得られる。
    /// </summary>
    public void CollectScenarioLabels(Action<string[]> onLoaded)
    {
        if (_advEngine == null)
        {
            Debug.LogWarning($"[{nameof(UtageDialogueScene)}] AdvEngine が設定されていません。", this);
            onLoaded?.Invoke(Array.Empty<string>());
            return;
        }

        StartCoroutine(CollectScenarioLabelsAsync(onLoaded));
    }

    private IEnumerator CollectScenarioLabelsAsync(Action<string[]> onLoaded)
    {
        if (!_advEngine.gameObject.activeSelf)
            _advEngine.gameObject.SetActive(true);

        while (_advEngine.IsWaitBootLoading)
            yield return null;

        var labels = new List<string>();
        foreach (var scenario in _advEngine.DataManager.ScenarioDataTbl.Values)
        {
            foreach (var label in scenario.ScenarioLabels.Keys)
            {
                if (!labels.Contains(label))
                    labels.Add(label);
            }
        }

        onLoaded?.Invoke(labels.ToArray());
    }

    private IEnumerator PlayAsync(string scenarioLabel, Action onComplete)
    {
        IsPlaying = true;
        GamePause.Push();
        AttachCamerasToMain();

        // AddToCurrentScene 構成ではエンジンは非アクティブで置かれているため、初回に起動する
        if (!_advEngine.gameObject.activeSelf)
            _advEngine.gameObject.SetActive(true);

        while (_advEngine.IsWaitBootLoading)
            yield return null;

        if (_advEngine.DataManager.FindScenarioData(scenarioLabel) == null)
        {
            Debug.LogWarning($"[{nameof(UtageDialogueScene)}] シナリオラベル '{scenarioLabel}' が見つかりません。再インポート忘れかラベル名の誤りを確認してください。", this);
        }
        else
        {
            _advEngine.JumpScenario(scenarioLabel);

            // JumpScenario は内部でロード待ちを挟むことがあり、その間はアイドル時の IsEndScenario=true が
            // 残ったままになる。先に「シナリオが実際に開始される」まで待たないと、終了待ちが即座に抜けてしまう。
            var waitStart = Time.realtimeSinceStartup;
            while (!_advEngine.ScenarioPlayer.MainThread.IsPlaying)
            {
                if (Time.realtimeSinceStartup - waitStart > 15f)
                {
                    Debug.LogError($"[{nameof(UtageDialogueScene)}] シナリオ '{scenarioLabel}' が15秒待っても開始されませんでした。ロード失敗の可能性があります。", this);
                    break;
                }
                yield return null;
            }

            while (!_advEngine.IsEndOrPauseScenario)
                yield return null;
        }

        DetachCamerasFromMain();
        GamePause.Pop();
        IsPlaying = false;
        onComplete?.Invoke();
    }

    /// <summary>宴のカメラをゲーム側メインカメラのスタックに Overlay として追加する。</summary>
    private void AttachCamerasToMain()
    {
        // シーン遷移で入れ替わったカメラにも対応するため、再生のたびに除外し直す
        StripUtageLayersFromGameCameras();

        var main = Camera.main;
        if (main == null)
        {
            Debug.LogWarning($"[{nameof(UtageDialogueScene)}] MainCamera が見つからないため会話 UI を表示できません。", this);
            return;
        }

        var baseData = main.GetUniversalAdditionalCameraData();
        if (baseData.renderType != CameraRenderType.Base)
        {
            Debug.LogWarning($"[{nameof(UtageDialogueScene)}] MainCamera が Base カメラではありません。", this);
            return;
        }

        foreach (var cam in _overlayCameras)
        {
            if (cam == null)
                continue;

            cam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            if (!baseData.cameraStack.Contains(cam))
                baseData.cameraStack.Add(cam);
        }

        _stackedBaseCamera = main;
    }

    /// <summary>スタックに追加した宴のカメラを取り除く。</summary>
    private void DetachCamerasFromMain()
    {
        if (_stackedBaseCamera == null)
            return;

        var baseData = _stackedBaseCamera.GetUniversalAdditionalCameraData();
        foreach (var cam in _overlayCameras)
        {
            if (cam != null)
                baseData.cameraStack.Remove(cam);
        }

        _stackedBaseCamera = null;
    }
}
