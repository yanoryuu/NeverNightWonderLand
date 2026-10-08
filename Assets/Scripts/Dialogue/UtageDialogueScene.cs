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
/// 実行順を最優先にしているのは、DialogueScene が二重にロードされた時に、同じシーンの EventSystem・宴のエンジンが
/// 有効になる (OnEnable / Awake) より先に止めるため (Docs/DialogueSystem.md §7)。
/// </summary>
[DefaultExecutionOrder(-10000)]
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

    /// <summary>宴のエンジンの起動 (シナリオ・設定の読み込み) が済んでいるか。</summary>
    public bool IsBooted => _advEngine != null && _advEngine.gameObject.activeSelf && !_advEngine.IsWaitBootLoading;

    // この会話シーンのルート (DontDestroyOnLoad へ移した後も「自分の物」を見分けるため。カメラ・キャンバスの判定に使う)
    private readonly List<GameObject> _ownRoots = new();

    /// <summary>
    /// 会話中に隠すゲーム側 UI の Screen Space - Overlay キャンバスの sortingOrder の上限 (これ未満を隠す)。
    /// Overlay キャンバスは全カメラ (宴のカメラスタックを含む) の後に描かれるため、
    /// HUD などが会話のメッセージウィンドウの上に重なる。フェード (FadeCanvas, 500) は隠さない。
    /// </summary>
    private const int HideOverlaySortingOrderBelow = 100;

    private Camera _stackedBaseCamera;
    private readonly List<Canvas> _hiddenCanvases = new();
    private bool _pausePushed;
    private bool _isDuplicate;

    private void Start()
    {
        if (_isDuplicate)
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    private void Awake()
    {
        // DialogueScene が二重にロードされた時の保険 (DialogueService が防ぐが、念のため)。
        // 2 つ目は自分の宴カメラを含めて何もせずに消える (カメラの cullingMask を相互に消し合うと会話が見えなくなる)
        foreach (var other in FindObjectsByType<UtageDialogueScene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other != this && other.gameObject.scene != gameObject.scene)
            {
                Debug.LogWarning($"[{nameof(UtageDialogueScene)}] DialogueScene が二重にロードされたため、後から来た方をアンロードします。", this);
                _isDuplicate = true; // ロード中 (Awake) はアンロードできないので Start で
                // アンロードまでの数フレームも、2 つ目の EventSystem (「There are 2 event systems」の警告が毎フレーム出る)・
                // 宴のエンジンを動かさない。自分 (Start でアンロードする) 以外のルートを止める
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    if (root != gameObject)
                        root.SetActive(false);
                }
                return;
            }
        }

        // EventSystem はシーン全体で1つが原則。ゲーム側 (このシーン以外) に既にあればこちらを無効化する。
        if (_eventSystem != null)
        {
            foreach (var other in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (other.gameObject.scene != gameObject.scene)
                {
                    _eventSystem.gameObject.SetActive(false);
                    break;
                }
            }
        }

        RememberOwnRoots(gameObject.scene.GetRootGameObjects());
        StripUtageLayersFromGameCameras();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>この会話シーンのルートを覚える (DialogueService が DontDestroyOnLoad へ移す前に呼ぶ)。</summary>
    public void RememberOwnRoots(GameObject[] roots)
    {
        _ownRoots.Clear();
        _ownRoots.AddRange(roots);
    }

    private bool IsOwn(GameObject go) => go.scene == gameObject.scene || _ownRoots.Contains(go.transform.root.gameObject);

    /// <summary>
    /// 常駐中に別のシーンがロードされた (ステージ遷移・死亡リトライ・タイトル)。
    /// 新しいゲーム側カメラから宴のレイヤーを外し、EventSystem が 2 つにならないようにする。
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (_isDuplicate || this == null)
            return;

        StripUtageLayersFromGameCameras();
        if (_eventSystem != null)
        {
            var other = false;
            foreach (var es in FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (es != _eventSystem && !IsOwn(es.gameObject))
                {
                    other = true;
                    break;
                }
            }

            // ゲーム側に無い時だけ自分の EventSystem を使う (宴の UI のクリック送り用)
            _eventSystem.gameObject.SetActive(!other);
        }
    }

    /// <summary>
    /// 宴のエンジンを起動しておく (先読み)。表示はしない (宴のカメラはメインカメラのスタックに載せない限り描かれない)。
    /// 起動済み・再生中なら何もしない。
    /// </summary>
    public void Boot()
    {
        if (_isDuplicate || _advEngine == null || IsPlaying)
            return;

        if (!_advEngine.gameObject.activeSelf)
            _advEngine.gameObject.SetActive(true);
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
            // 宴のシーン (DialogueScene) のカメラは除外しない。自分の _overlayCameras だけを見て判定すると、
            // DialogueScene が 2 つある時に互いの宴カメラの cullingMask を 0 にして会話が見えなくなる (2026-10-03 のバグ)
            if (IsOwn(cam.gameObject) || System.Array.IndexOf(_overlayCameras, cam) >= 0)
                continue;
            if (cam.gameObject.scene.name == DialogueService.SceneName)
                continue;
            cam.cullingMask &= ~mask;
        }
    }

    /// <summary>指定ラベルのシナリオを再生する。終了時に onComplete が呼ばれる。</summary>
    public void Play(string scenarioLabel, Action onComplete = null)
    {
        if (_isDuplicate)
            return;

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
        DialogueService.NotifyStarted();
        // キーボード/パッドでも文字送りできるよう宴の入力を差し替える (宴の初期化で戻されることがあるので再生ごとに)
        DialogueInputStrategy.Install();
        GamePause.Push();
        _pausePushed = true;
        AttachCamerasToMain();
        HideGameOverlayCanvases();

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

        EndPlaying();
        onComplete?.Invoke();
    }

    /// <summary>会話の後始末 (カメラ・隠した UI・ポーズを戻す)。onComplete より前に呼ぶ。</summary>
    private void EndPlaying()
    {
        DetachCamerasFromMain();
        RestoreGameOverlayCanvases();
        if (_pausePushed)
        {
            _pausePushed = false;
            GamePause.Pop();
        }
        IsPlaying = false;
        DialogueService.NotifyEnded();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        // 会話中に DialogueScene がアンロードされてもポーズ・カメラスタック・UI を残さない
        if (IsPlaying)
            EndPlaying();
    }

    /// <summary>
    /// ゲーム側の Screen Space - Overlay キャンバス (HUD・StageUI など、名前が UI で終わるシーンの UI) を会話中だけ隠す。
    /// URP では Overlay キャンバスはカメラスタック (宴の会話 UI) より必ず手前に描かれ、sortingOrder でも下に回せないため。
    /// </summary>
    private void HideGameOverlayCanvases()
    {
        _hiddenCanvases.Clear();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || !canvas.enabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;
            if (IsOwn(canvas.gameObject) || canvas.sortingOrder >= HideOverlaySortingOrderBelow)
                continue;
            canvas.enabled = false;
            _hiddenCanvases.Add(canvas);
        }
    }

    private void RestoreGameOverlayCanvases()
    {
        foreach (var canvas in _hiddenCanvases)
        {
            if (canvas != null)
                canvas.enabled = true;
        }
        _hiddenCanvases.Clear();
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
