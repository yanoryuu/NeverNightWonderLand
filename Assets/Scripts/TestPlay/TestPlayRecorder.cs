#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>入力記録のファイル形式 (JSON)。</summary>
[Serializable]
public class InputRecording
{
    public int version = 1;
    public string createdAt;
    public string scene;
    public float startX;
    public float startY;
    public int randomSeed;
    public bool invincible;

    /// <summary>true = 記録開始時にステージを読み直した (再生と同じ条件から始まる)。</summary>
    public bool restartedScene;

    /// <summary>開始時の状態 (セーブデータ形式: 能力・スキル・アイテム・糸・装備・進行フラグ・位置)。</summary>
    public SaveData snapshot;

    public List<InputRecordingFrame> frames = new();
    public List<InputRecordingCheckpoint> checkpoints = new();

    public float Duration => frames.Sum(f => f.dt);
}

/// <summary>PlayerController が入力を読んだ 1 フレーム分。</summary>
[Serializable]
public struct InputRecordingFrame
{
    public float dt;    // そのフレームの Time.deltaTime (スケール後)
    public float fdt;   // そのフレームの Time.fixedDeltaTime
    public float r;     // 入力を読んだ時点の物理の端数 (Time.time - Time.fixedTime)。再生で物理ステップの位相を合わせる
    public float ts;    // そのフレームの Time.timeScale (unscaled 時間で動く演出 = 暗転・通知などの進み方を記録とそろえる)
    public float u;     // そのフレームの Time.unscaledDeltaTime (実時間。DOTween の timeScale 非依存 tween = 暗転の進みを再生でそろえる)
    public bool l;      // ステージ遷移中 (暗転 + 読み込み) だったか。読み込みにかかるフレーム数は実時間次第なので再生で合わせる
    public float m;     // Move
    public float v;     // Vertical
    public int b;       // ボタン (InputRecordingFrame.Bit*)
    public int i;       // アイテムスロット (-1 = なし)

    private const int BitJumpHeld = 1 << 0, BitJumpPressed = 1 << 1, BitDashHeld = 1 << 2, BitDashPressed = 1 << 3,
        BitAttack = 1 << 4, BitSpecial = 1 << 5, BitFinisher = 1 << 6, BitHeal = 1 << 7, BitInteract = 1 << 8,
        BitGrapple = 1 << 9;

    public static InputRecordingFrame From(in PlayerInputFrame f, float dt, float fdt, float residual, float timeScale, bool loading, float unscaledDt)
    {
        var bits = 0;
        if (f.JumpHeld) bits |= BitJumpHeld;
        if (f.JumpPressed) bits |= BitJumpPressed;
        if (f.DashHeld) bits |= BitDashHeld;
        if (f.DashPressed) bits |= BitDashPressed;
        if (f.AttackPressed) bits |= BitAttack;
        if (f.SpecialPressed) bits |= BitSpecial;
        if (f.FinisherPressed) bits |= BitFinisher;
        if (f.HealPressed) bits |= BitHeal;
        if (f.InteractPressed) bits |= BitInteract;
        if (f.GrapplePressed) bits |= BitGrapple;
        // ポーズ (PausePressed) は記録しない: ポーズ中のメニュー操作は再生できないため

        return new InputRecordingFrame
        {
            dt = dt, fdt = fdt, r = residual, ts = timeScale, l = loading, u = unscaledDt, m = f.Move, v = f.Vertical, b = bits,
            i = f.ItemSlot.HasValue ? (int)f.ItemSlot.Value : -1,
        };
    }

    public PlayerInputFrame ToInput() => new()
    {
        Move = m,
        Vertical = v,
        JumpHeld = (b & BitJumpHeld) != 0,
        JumpPressed = (b & BitJumpPressed) != 0,
        DashHeld = (b & BitDashHeld) != 0,
        DashPressed = (b & BitDashPressed) != 0,
        AttackPressed = (b & BitAttack) != 0,
        SpecialPressed = (b & BitSpecial) != 0,
        FinisherPressed = (b & BitFinisher) != 0,
        HealPressed = (b & BitHeal) != 0,
        InteractPressed = (b & BitInteract) != 0,
        GrapplePressed = (b & BitGrapple) != 0,
        ItemSlot = i >= 0 ? (ItemSlot?)(ItemSlot)i : null,
    };
}

/// <summary>位置の照合点 (そのフレームの入力を読んだ時点のプレイヤー位置)。</summary>
[Serializable]
public struct InputRecordingCheckpoint
{
    public int frame;
    public string scene;
    public float x;
    public float y;
    public float vx;
    public float vy;
}

/// <summary>再生結果 (ずれの報告)。</summary>
public class ReplayReport
{
    public string file;
    public float speed;
    public int framesPlayed;
    public int framesTotal;
    public bool completed;
    public string abortReason;
    public float maxDrift;
    public int maxDriftFrame = -1;
    public int firstDivergedFrame = -1;   // DriftThreshold を初めて超えた照合点
    public float realSeconds;
    public float gameSeconds;
    public readonly List<string> lines = new();

    public override string ToString()
    {
        var head = completed ? $"再生完了 {framesPlayed}/{framesTotal} フレーム"
            : abortReason != null ? $"再生中断 {framesPlayed}/{framesTotal} フレーム ({abortReason})"
            : $"再生中 {framesPlayed}/{framesTotal} フレーム";
        return $"{head} / 速度 {speed:0.##}x (実効 {(realSeconds > 0 ? gameSeconds / realSeconds : 0):0.##}x) / " +
               $"最大ずれ {maxDrift:F3} (frame {maxDriftFrame})" +
               (firstDivergedFrame >= 0 ? $" / {TestPlayRecorder.DriftThreshold} 超え初回 frame {firstDivergedFrame}" : " / 閾値超えなし");
    }
}

/// <summary>
/// プレイヤー入力の記録と再生 (エディタ/Development Build のみ)。
///
/// 記録: PlayerController が入力を読んだフレームごとに、入力・deltaTime・fixedDeltaTime を保存し、
///       CheckpointInterval フレームごとにプレイヤー位置を照合点として残す。
///       restartScene = true (既定) なら、開始時の状態 (セーブデータ形式) を取ってからステージを読み直し、
///       その位置から記録を始める (= 再生と同じ条件)。
/// 再生: 記録の開始状態でステージを読み直し (GameSession.PendingLoad + LoadWithPlayerScene = 「つづきから」と同じ経路)、
///       記録した入力を 1 フレームずつ流す。各フレームのゲーム時間は Time.captureDeltaTime で記録と同じにする
///       (= 物理ステップの回数と順序が記録と一致する)。速度 (TestPlaySpeed.Scale) は実時間の進み方だけを変え、
///       速すぎて間に合わない時は描画の許す限り速く進む。照合点ごとに位置のずれを計測して ReplayReport に残す。
/// ファイル: エディタ = プロジェクト直下の TestPlay/Recordings/*.json、Development Build = persistentDataPath/TestPlay/Recordings。
/// </summary>
public static class TestPlayRecorder
{
    public const int CheckpointInterval = 30;
    public const float DriftThreshold = 0.5f;

    /// <summary>
    /// 記録中のフレームレート上限 (0 = 上限なし)。再生は記録の 1 フレーム = 実際の 1 フレームで進むので、
    /// 記録のフレーム数が少ないほど速く再生できる (例: 60fps で記録 → 描画 240fps の環境で 4 倍速)。
    /// </summary>
    public static int RecordFrameRateCap = 60;

    private static int _savedRecordTargetFrameRate;

    private enum Phase { Idle, RecordLoading, Recording, ReplayLoading, Replaying }

    private static Phase _phase;
    private static InputRecording _recording;      // 記録中 or 再生中のデータ
    private static string _replayFile;
    private static int _cursor;
    private static ReplayReport _report;
    private static IPlayerInputSource _previousOverride;
    private static ReplaySource _replaySource;
    private static double _replayRealStart;
    private static double _replayGameElapsed;
    private static double _pacingRealStart;   // 速度合わせの基準 (速度変更・大きな遅れで取り直す)
    private static double _pacingGameStart;
    private static float _pacingScale;
    private static bool _stallPushed;          // 読み込み待ちで GamePause を積んでいるか
    private static float _phaseStartRealtime;
    private static int _savedVSync;
    private static int _savedTargetFrameRate;
    private static Vector2 _lastReadPos;
    private static string _lastReadScene;

    public static bool IsRecording => _phase == Phase.Recording || _phase == Phase.RecordLoading;
    public static bool IsReplaying => _phase == Phase.Replaying || _phase == Phase.ReplayLoading;
    public static bool IsBusy => _phase != Phase.Idle;

    /// <summary>記録中で、読み直しが終わって入力を取り込んでいる段階か (自動操作はこれが true になってから始める)。</summary>
    public static bool IsCapturing => _phase == Phase.Recording;

    /// <summary>再生中で、記録の入力を流している段階か。</summary>
    public static bool IsFeeding => _phase == Phase.Replaying;

    /// <summary>記録/再生の進み具合 (フレーム)。</summary>
    public static int CurrentFrame => _phase == Phase.Recording ? _recording?.frames.Count ?? 0 : _cursor;
    public static int TotalFrames => _recording?.frames.Count ?? 0;

    /// <summary>直前の記録の保存先。</summary>
    public static string LastSavedPath { get; private set; }

    /// <summary>直前の再生結果 (再生中は途中経過)。</summary>
    public static ReplayReport LastReport { get; private set; }

    /// <summary>再生が終わった (完了・中断) 時に呼ばれる。</summary>
    public static event Action<ReplayReport> ReplayFinished;

    public static string Status => _phase switch
    {
        Phase.RecordLoading => "記録: 読み直し中",
        Phase.Recording => $"● 記録中 {_recording.frames.Count}f ({_recording.Duration:F1}s)",
        Phase.ReplayLoading => "再生: 読み直し中",
        Phase.Replaying => $"▶ 再生中 {_cursor}/{_recording.frames.Count}f 最大ずれ {_report?.maxDrift:F3}",
        _ => "",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _phase = Phase.Idle;
        _recording = null;
        _report = null;
        _replaySource = null;
        _previousOverride = null;
        ReplayFinished = null;
        PlayerInputSource.ConsumedByPlayer -= OnConsumed;
        PlayerInputSource.ConsumedByPlayer += OnConsumed;
    }

    /// <summary>記録の保存フォルダ。</summary>
    public static string RecordingsDirectory
    {
        get
        {
#if UNITY_EDITOR
            var root = Path.GetDirectoryName(Application.dataPath);
#else
            var root = Application.persistentDataPath;
#endif
            return Path.Combine(root, "TestPlay", "Recordings");
        }
    }

    /// <summary>保存済みの記録ファイル (新しい順)。</summary>
    public static string[] ListRecordings()
    {
        var dir = RecordingsDirectory;
        if (!Directory.Exists(dir))
            return Array.Empty<string>();
        return Directory.GetFiles(dir, "*.json").OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
    }

    // ------------------------------------------------------------------ 記録

    /// <summary>
    /// 記録を始める。restartScene = true ならステージを読み直してから始める (再生と条件がそろうので推奨)。
    /// false ならその場から始める (敵などの状態は再生時と異なるので、ずれやすい)。
    /// </summary>
    public static bool StartRecording(bool restartScene = true)
    {
        if (IsBusy)
            return false;

        var player = TestPlayActions.Player;
        var bridge = player != null ? player.GetComponent<PlayerSaveBridge>() : null;
        if (bridge == null || StageLoader.Instance == null || TestPlayActions.IsLoading)
        {
            Debug.LogWarning("[TestPlay] 記録を開始できません (PlayerScene 方式のステージでプレイ中に実行してください)。");
            return false;
        }

        var snapshot = bridge.Collect();
        _recording = new InputRecording
        {
            createdAt = DateTime.Now.ToString("o"),
            scene = snapshot.sceneName,
            startX = snapshot.posX,
            startY = snapshot.posY,
            randomSeed = Environment.TickCount & 0x7fffffff,
            invincible = DebugCheats.Invincible,
            restartedScene = restartScene,
            snapshot = snapshot,
        };

        TestPlayRunner.Ensure();
        _savedRecordTargetFrameRate = Application.targetFrameRate;
        if (RecordFrameRateCap > 0 && (Application.targetFrameRate <= 0 || Application.targetFrameRate > RecordFrameRateCap))
            Application.targetFrameRate = RecordFrameRateCap;

        if (restartScene)
        {
            ReloadFromSnapshot(_recording);
            _phase = Phase.RecordLoading;
        }
        else
        {
            UnityEngine.Random.InitState(_recording.randomSeed);
            _phase = Phase.Recording;
        }

        _phaseStartRealtime = Time.realtimeSinceStartup;
        return true;
    }

    /// <summary>記録を止めて保存する。保存先を返す (記録が空なら null)。</summary>
    public static string StopRecording(string fileName = null)
    {
        if (!IsRecording)
            return null;

        var recording = _recording;
        _phase = Phase.Idle;
        _recording = null;
        Application.targetFrameRate = _savedRecordTargetFrameRate;
        if (recording.frames.Count == 0)
            return null;

        // 最後のフレームを読んだ時点の位置も照合点にする
        var last = recording.frames.Count - 1;
        if (last % CheckpointInterval != 0)
            recording.checkpoints.Add(new InputRecordingCheckpoint { frame = last, scene = _lastReadScene, x = _lastReadPos.x, y = _lastReadPos.y });
        var path = Save(recording, fileName);
        LastSavedPath = path;
        Debug.Log($"[TestPlay] 記録を保存しました: {path} ({recording.frames.Count} フレーム, {recording.Duration:F1} 秒)");
        return path;
    }

    public static string Save(InputRecording recording, string fileName = null)
    {
        var dir = RecordingsDirectory;
        Directory.CreateDirectory(dir);
        if (string.IsNullOrEmpty(fileName))
            fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{recording.scene}";
        if (!fileName.EndsWith(".json"))
            fileName += ".json";

        var path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(dir, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(recording));
        return path;
    }

    public static InputRecording Load(string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(RecordingsDirectory, path.EndsWith(".json") ? path : path + ".json");
        return JsonUtility.FromJson<InputRecording>(File.ReadAllText(path));
    }

    // ------------------------------------------------------------------ 再生

    /// <summary>記録ファイルを再生する (パスはフルパスか RecordingsDirectory からの相対)。</summary>
    public static bool StartReplay(string path)
    {
        InputRecording recording;
        try
        {
            recording = Load(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TestPlay] 記録を読めません: {path}\n{e.Message}");
            return false;
        }

        return StartReplay(recording, path);
    }

    /// <summary>記録データを再生する。</summary>
    public static bool StartReplay(InputRecording recording, string label = null)
    {
        if (IsBusy || recording == null || recording.frames.Count == 0)
            return false;

        _recording = recording;
        _replayFile = label ?? recording.scene;
        _cursor = 0;
        _report = new ReplayReport { file = _replayFile, framesTotal = recording.frames.Count, speed = TestPlaySpeed.Scale };
        LastReport = _report;

        if (TestPlaySpeed.Frozen)
            TestPlaySpeed.SetFrozen(false);

        TestPlayRunner.Ensure();
        ReloadFromSnapshot(recording);
        _phase = Phase.ReplayLoading;
        _phaseStartRealtime = Time.realtimeSinceStartup;
        return true;
    }

    /// <summary>再生を止める。</summary>
    public static void StopReplay(string reason = "中断")
    {
        if (!IsReplaying)
            return;
        FinishReplay(false, reason);
    }

    /// <summary>直前に保存した記録を再生する。</summary>
    public static bool ReplayLast()
    {
        var path = LastSavedPath ?? ListRecordings().FirstOrDefault();
        return path != null && StartReplay(path);
    }

    // ------------------------------------------------------------------ 内部

    private static void ReloadFromSnapshot(InputRecording recording)
    {
        var data = recording.snapshot ?? new SaveData { sceneName = recording.scene, posX = recording.startX, posY = recording.startY };
        data.sceneName = recording.scene;
        data.posX = recording.startX;
        data.posY = recording.startY;

        DebugCheats.Invincible = recording.invincible;
        UnityEngine.Random.InitState(recording.randomSeed);
        GameSession.PendingLoad = data;
        StageLoader.LoadWithPlayerScene(recording.scene);
    }

    /// <summary>読み直しが終わって操作できる状態になったか (StageLoader が PendingLoad を適用し終えた)。</summary>
    private static bool ReloadFinished(InputRecording recording)
    {
        var loader = StageLoader.Instance;
        return loader != null && !loader.IsLoading && loader.CurrentStageName == recording.scene
               && GameSession.PendingLoad == null && TestPlayActions.Player != null;
    }

    /// <summary>TestPlayRunner.Update (他のスクリプトより先) から毎フレーム呼ばれる。</summary>
    internal static void EarlyTick()
    {
        switch (_phase)
        {
            case Phase.RecordLoading:
                if (ReloadFinished(_recording))
                {
                    UnityEngine.Random.InitState(_recording.randomSeed);
                    _phase = Phase.Recording;
                }
                else if (Time.realtimeSinceStartup - _phaseStartRealtime > 30f)
                {
                    Debug.LogError("[TestPlay] 記録開始の読み直しが終わりません。記録を中止します。");
                    _phase = Phase.Idle;
                    Application.targetFrameRate = _savedRecordTargetFrameRate;
                }

                break;

            case Phase.ReplayLoading:
                if (ReloadFinished(_recording))
                    BeginReplayFrames();
                else if (Time.realtimeSinceStartup - _phaseStartRealtime > 30f)
                    FinishReplay(false, "読み直しが終わらない");
                break;

            case Phase.Replaying:
                if (TestPlayActions.Player == null || StageLoader.Instance == null)
                    FinishReplay(false, "プレイヤー/ステージが無くなった");
                else
                    SyncStageLoading();
                break;
        }

        if (_phase == Phase.Recording)
        {
            SyncDOTweenUnscaledTime(Time.unscaledDeltaTime);
        }
        else if (_phase == Phase.Replaying)
        {
            // Time.captureDeltaTime は unscaledDeltaTime (実時間) を変えないので、記録した値を DOTween に渡す
            var u = !_stallPushed && _cursor < _recording.frames.Count ? _recording.frames[_cursor].u : 0f;
            SyncDOTweenUnscaledTime(u > 0f ? u : Time.unscaledDeltaTime);
        }
    }

    private static void BeginReplayFrames()
    {
        UnityEngine.Random.InitState(_recording.randomSeed);

        _previousOverride = PlayerInputSource.Override;
        _replaySource = new ReplaySource();
        PlayerInputSource.Override = _replaySource;

        // 描画待ちで速度が頭打ちにならないよう、再生中は垂直同期とフレームレート上限を外す (終了時に戻す)
        _savedVSync = QualitySettings.vSyncCount;
        _savedTargetFrameRate = Application.targetFrameRate;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        // 読み直しの直後は、記録時と再生時で「配置から最初のフレームまで」の物理の回数が違いうるので、
        // 最初の照合点 (frame 0 の位置と速度) へ合わせてから流し始める
        var first = _recording.checkpoints.FirstOrDefault(c => c.frame == 0);
        var player = TestPlayActions.Player;
        if (_recording.checkpoints.Count > 0 && first.frame == 0 && first.scene == TestPlayActions.CurrentStageName && player != null)
        {
            player.Teleport(new Vector2(first.x, first.y));
            if (player.Rb != null)
                player.Rb.linearVelocity = new Vector2(first.vx, first.vy);
        }

        // 速度 (TestPlaySpeed) は timeScale ではなく実時間の待ちで表す。timeScale は記録の値に合わせる (ApplyFrameTiming)

        _replayRealStart = Time.realtimeSinceStartupAsDouble;
        _pacingRealStart = _replayRealStart;
        _pacingGameStart = 0;
        _pacingScale = TestPlaySpeed.Scale;
        _replayGameElapsed = 0;
        _phase = Phase.Replaying;
        // 最初のフレームのゲーム時間は既に決まっているので、次のフレームから記録の時間に合わせる
        ApplyFrameTiming(0);
    }

    /// <summary>
    /// frame 番目の入力が読まれるフレームのゲーム時間・固定ステップを記録と同じにする。
    /// 物理 (FixedUpdate) は「前のフレームからの端数 + deltaTime」が固定ステップに達した回数だけ走るので、
    /// deltaTime だけでなく端数 (Time.time - Time.fixedTime) も記録とそろえる (phaseError を次のフレームで埋める)。
    /// これで各フレームの物理ステップの回数と、入力との前後関係が記録と一致する。
    /// </summary>
    private static void ApplyFrameTiming(int frame, float phaseError = 0f)
    {
        if (frame >= _recording.frames.Count)
            return;
        var f = _recording.frames[frame];
        if (f.fdt > 0f)
            Time.fixedDeltaTime = f.fdt;
        var dt = Mathf.Max(f.dt + phaseError, 0.0001f);
        // timeScale は記録時の値 (速度は実時間の待ちで表す = LateTick)。deltaTime = captureDeltaTime × timeScale
        var timeScale = f.ts > 0f ? f.ts : 1f;
        if (!Mathf.Approximately(GamePause.BaseTimeScale, timeScale))
            GamePause.SetBaseTimeScale(timeScale);
        Time.captureDeltaTime = dt / timeScale;
    }

    /// <summary>
    /// ステージ遷移の読み込みは実時間で終わるので、終わるまでのフレーム数が記録と再生で違う。
    /// 再生の読み込みが遅ければ (記録では読み込み済みのフレームなのに、まだ読み込み中) 入力を流さずに待ち、
    /// 早ければ (記録ではまだ読み込み中のフレーム) その分の記録を飛ばして、遷移後のフレームをそろえる。
    /// </summary>
    private static void SyncStageLoading()
    {
        var loading = TestPlayActions.IsLoading;
        var recordedLoading = _recording.frames[_cursor].l;

        if (loading && !recordedLoading)
        {
            if (!_stallPushed || !GamePause.IsPaused)
            {
                GamePause.Push();
                _stallPushed = true;
            }

            return;
        }

        ReleaseStall();

        if (!loading && recordedLoading)
        {
            var skipped = 0;
            while (_cursor < _recording.frames.Count - 1 && _recording.frames[_cursor].l)
            {
                _cursor++;
                skipped++;
            }

            _report.framesPlayed = _cursor;
            _report.lines.Add($"frame {_cursor,5}: 読み込みが記録より早く終わったので {skipped} フレーム飛ばした");
            ApplyFrameTiming(_cursor);
        }
    }

    private static void ReleaseStall()
    {
        if (!_stallPushed)
            return;
        _stallPushed = false;
        if (GamePause.IsPaused)
            GamePause.Pop();
    }

    private static System.Reflection.FieldInfo _dotweenUnscaledTime;
    private static Component _dotweenComponent;

    /// <summary>
    /// DOTween の「timeScale に依らない」tween (暗転 ScreenFader など) は実時間 (realtimeSinceStartup) で進むため、
    /// 再生の速さによって暗転の終わるフレームが変わってしまう。記録・再生中はその 1 フレーム分の進みを
    /// 記録時は Time.unscaledDeltaTime、再生時は記録した値にそろえる。DOTween の Update より先 (実行順 -10000) に呼ぶ。
    /// (Time.unscaledDeltaTime を直接使う UI 演出 = 部屋名の表示・通知は実時間のまま。ゲーム進行には影響しない)
    /// </summary>
    private static void SyncDOTweenUnscaledTime(float unscaledDelta)
    {
        if (_dotweenComponent == null)
        {
            var type = typeof(DG.Tweening.DOTween).Assembly.GetType("DG.Tweening.Core.DOTweenComponent");
            if (type == null)
                return;
            _dotweenComponent = UnityEngine.Object.FindAnyObjectByType(type) as Component;
            _dotweenUnscaledTime ??= type.GetField("_unscaledTime",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (_dotweenComponent == null || _dotweenUnscaledTime == null)
                return;
        }

        _dotweenUnscaledTime.SetValue(_dotweenComponent, Time.realtimeSinceStartup - unscaledDelta);
    }

    /// <summary>まだ物理ステップに消化されていない時間 (0〜fixedDeltaTime)。</summary>
    private static float PhysicsResidual() => (float)(Time.timeAsDouble - Time.fixedTimeAsDouble);

    /// <summary>PlayerController が入力を消費した直後 (記録・再生のフレーム送り)。</summary>
    private static void OnConsumed(PlayerInputFrame input)
    {
        if (_phase == Phase.Recording)
        {
            var count = _recording.frames.Count;
            if (count % CheckpointInterval == 0)
                AddCheckpoint(_recording, count);
            var player = TestPlayActions.Player;
            if (player != null)
            {
                _lastReadPos = player.transform.position;
                _lastReadScene = TestPlayActions.CurrentStageName;
            }

            _recording.frames.Add(InputRecordingFrame.From(input, Time.deltaTime, Time.fixedDeltaTime, PhysicsResidual(), Time.timeScale, TestPlayActions.IsLoading, Time.unscaledDeltaTime));
            return;
        }

        if (_phase != Phase.Replaying)
            return;

        CheckDrift(_cursor);

        _replayGameElapsed += Time.deltaTime;
        // 記録時との物理の端数の差 (次のフレームの時間で埋める)
        var phaseError = _recording.frames[_cursor].r - PhysicsResidual();
        _cursor++;
        _report.framesPlayed = _cursor;

        if (_cursor >= _recording.frames.Count)
        {
            FinishReplay(true, null);
            return;
        }

        ApplyFrameTiming(_cursor, phaseError);
    }

    private static void AddCheckpoint(InputRecording recording, int frame)
    {
        var p = TestPlayActions.Player;
        if (p == null)
            return;
        recording.checkpoints.Add(new InputRecordingCheckpoint
        {
            frame = frame,
            scene = TestPlayActions.CurrentStageName,
            x = p.transform.position.x,
            y = p.transform.position.y,
            vx = p.Rb != null ? p.Rb.linearVelocity.x : 0f,
            vy = p.Rb != null ? p.Rb.linearVelocity.y : 0f,
        });
    }

    private static void CheckDrift(int frame)
    {
        var p = TestPlayActions.Player;
        if (p == null)
            return;

        foreach (var cp in _recording.checkpoints)
        {
            if (cp.frame != frame)
                continue;

            var scene = TestPlayActions.CurrentStageName;
            var drift = scene == cp.scene
                ? Vector2.Distance(new Vector2(cp.x, cp.y), p.transform.position)
                : float.PositiveInfinity;
            if (drift > _report.maxDrift || _report.maxDriftFrame < 0)
            {
                _report.maxDrift = drift;
                _report.maxDriftFrame = frame;
            }

            if (drift > DriftThreshold && _report.firstDivergedFrame < 0)
            {
                _report.firstDivergedFrame = frame;
                Debug.LogWarning($"[TestPlay] 再生がずれました: frame {frame} 期待 ({cp.x:F2},{cp.y:F2}) {cp.scene} / " +
                                 $"実際 ({p.transform.position.x:F2},{p.transform.position.y:F2}) {scene} / ずれ {drift:F3}");
            }

            _report.lines.Add($"frame {frame,5}: 期待 ({cp.x,8:F3},{cp.y,8:F3}) 実際 ({p.transform.position.x,8:F3},{p.transform.position.y,8:F3}) ずれ {drift:F3}");
            return;
        }
    }

    private static void FinishReplay(bool completed, string reason)
    {
        var wasPlaying = _phase == Phase.Replaying;
        _phase = Phase.Idle;
        ReleaseStall();

        if (wasPlaying)
        {
            if (PlayerInputSource.Override == _replaySource)
                PlayerInputSource.Override = _previousOverride;
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedTargetFrameRate;
        }

        _replaySource = null;
        _previousOverride = null;
        Time.captureDeltaTime = 0f;
        GamePause.SetBaseTimeScale(TestPlaySpeed.Scale);
        TestPlaySpeed.RestoreFixedDeltaTime();

        _report.completed = completed;
        _report.abortReason = reason;
        _report.realSeconds = (float)(Time.realtimeSinceStartupAsDouble - _replayRealStart);
        _report.gameSeconds = (float)_replayGameElapsed;
        LastReport = _report;
        Debug.Log($"[TestPlay] {_report}");
        ReplayFinished?.Invoke(_report);
    }

    /// <summary>TestPlayRunner.LateUpdate から呼ばれる: 再生の実時間の進みを速度に合わせる。</summary>
    internal static void LateTick()
    {
        if (_phase != Phase.Replaying)
            return;

        var now = Time.realtimeSinceStartupAsDouble;
        var scale = Mathf.Max(TestPlaySpeed.Scale, 0.01f);
        _report.speed = scale;
        if (!Mathf.Approximately(scale, _pacingScale))
        {
            _pacingScale = scale;
            _pacingRealStart = now;
            _pacingGameStart = _replayGameElapsed;
        }

        // 描画が間に合う限り、ゲーム時間 / 速度 の実時間で進む (速すぎる分だけ待つ)
        var targetReal = (_replayGameElapsed - _pacingGameStart) / scale;
        var actualReal = now - _pacingRealStart;
        var waitMs = (int)((targetReal - actualReal) * 1000.0);
        if (waitMs > 0)
        {
            System.Threading.Thread.Sleep(Mathf.Min(waitMs, 100));
        }
        else if (waitMs < -250)
        {
            // 描画が追いつかない (= 記録のフレーム数が多すぎる) 時は、取り返そうとせず基準を取り直す
            _pacingRealStart = now;
            _pacingGameStart = _replayGameElapsed;
        }
    }

    /// <summary>再生中に PlayerInputSource へ差し込む入力。</summary>
    private sealed class ReplaySource : IPlayerInputSource
    {
        public PlayerInputFrame Read() =>
            _recording != null && _cursor < _recording.frames.Count ? _recording.frames[_cursor].ToInput() : default;
    }
}
#endif
