#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>ボットの設定 (エディタのウィンドウ・オーバーレイから変える)。</summary>
[Serializable]
public class BotSettings
{
    public float Speed = 1f;            // ゲームの速度 (0.1〜8)
    public float ThinkSpeed = -1f;      // Jev の応答待ちの速度。0 = 停止, -1 = 自動 (等倍以上は停止 / スローは min(速度, 0.25))
    public bool Invincible = true;
    public bool UseJev = true;          // false = プログラムの推奨 (ローカル方針) だけで動く
    public bool AutoSkipSingle = true;  // 選択肢が 1 つしか無い時は Jev に聞かない
    public float StuckSeconds = 30f;    // この秒数 (ゲーム時間) 進展が無ければ「止まった」
    public float HopTimeout = 300f;     // 1 区間の上限 (ゲーム秒)
    public float LowConfidence = 0.5f;  // これ未満の確信度は「迷った場所」として記録
    public bool Screenshots = true;
    public bool ContinueOnFail = true;  // 失敗した区間はワープで次へ進む
    public bool ApplyPresets = true;    // 途中から始める時、前の区間のフラグ・能力を付ける
    public bool Objectives = true;      // ボス・スピーカー・ピックアップの目的をこなす
    public int MaxDeaths = 3;
    public int MaxScreenshots = 40;
}

/// <summary>
/// 自動テストプレイボット (エディタ/Development Build のみ)。
/// 判断 = Jev (TypeSafe System One): 床に着くたび / 行動が終わるたびに、プログラムが物理で確かめた行動の候補を
/// 状態 (コンパクトな JSON) と一緒に送り、次の行動を 1 つ選ばせる。実行 = BotExecutor: 選ばれた行動を物理予測
/// (BotPhysics) どおりのフレーム入力に変えて VirtualPlayerInput から流す。
/// Jev の応答待ちの間はゲームを止める/遅くする (ThinkSpeed)。止まった場所・迷った場所 (低い確信度) を
/// スクリーンショット付きで TestPlay/Reports/ に書く。
/// </summary>
[DefaultExecutionOrder(-9000)]
public class TestPlayBot : MonoBehaviour
{
    public static TestPlayBot Instance { get; private set; }
    public static readonly BotSettings Settings = new();

    public static TestPlayBot Ensure()
    {
        if (Instance != null) return Instance;
        var runner = TestPlayRunner.Ensure();
        Instance = runner.gameObject.GetComponent<TestPlayBot>();
        if (Instance == null) Instance = runner.gameObject.AddComponent<TestPlayBot>();
        return Instance;
    }

    private enum Phase { Idle, Prepare, WaitLoad, Settle, Act, Thinking, Dead, Finished }

    // ------------------------------------------------------------------ 公開状態 (UI 用)

    public readonly JevClient Jev = new();
    public bool Running => _phase != Phase.Idle && _phase != Phase.Finished;
    public string Status = "待機中";
    public string GoalText = "-";
    public BotDecision LastDecision;
    public string LastReportPath;
    public BotReport Report;
    public string ExecutorStep => _exec != null ? _exec.Step : "-";
    public int StepIndex => _index;
    public int StepCount => _steps.Count;
    public string CurrentStepLabel => _index < _steps.Count ? _steps[_index].ToString() : "-";
    public BotWorld World => _world;

    // ------------------------------------------------------------------ 内部

    private Phase _phase = Phase.Idle;
    private readonly List<BotRouteStep> _steps = new();
    private int _index;
    private VirtualPlayerInput _input;
    private IPlayerInputSource _previousOverride;
    private BotWorld _world;
    private BotPhysics _physics;
    private BotExecutor _exec;

    private BotHopResult _hop;
    private float _hopGameT0, _hopRealT0, _runRealT0, _runGameT0;
    private float _phaseT0;
    private string _expectedScene;
    private bool _forceWarp;

    // ゴール
    private string _goalKind;            // exit / transport / boss / speaker / pickup / reach
    private BotEntity _goalEntity;
    private Rect _goalRect;
    private string _goalEntrance;        // 出口の遷移先の入口 (失敗時のワープ用)
    private string _goalNextScene;

    // 進捗
    private float _bestCost = float.PositiveInfinity;
    private float _lastProgressT;
    private bool _stuckMode;
    private int _stuckCount;
    private bool _rescanRequested;
    private float _rescanAt = -1f;
    private float _lastScanT;
    private readonly List<string> _recent = new();
    private readonly List<BotDecision> _hopDecisions = new();
    private readonly Dictionary<string, int> _failures = new();
    private readonly HashSet<string> _hesitationShots = new();
    private readonly HashSet<string> _visited = new();
    private readonly Dictionary<string, int> _visitCount = new();
    private string SegKey(BotSegment s) => $"{s.CenterX:F0},{s.YCenter:F0}";
    private int _screenshots;
    private string _lastActionKey;
    private int _sameFailStreak;
    private Vector2 _lastPos;
    private float _pausedSince = -1f;
    private int _thinkFrozen;
    private List<BotOption> _options;
    private BotDecision _pending;
    private float _thinkRealT0;
    private bool _deathLogged;
    private float _deadRealT0;
    private List<ScissorUpgrade> _upgradeSnapshot = new();
    private List<PlayerSkill> _skillSnapshot = new();
    private float _lastAppliedScale = -1f;
    private float _noSegmentSince = -1f;

    private class BotOption
    {
        public string Key;
        public BotAction Action;
        public Dictionary<string, object> Description;
        public float Score;
        public bool PhysicsOk = true;
        public bool OnRoute;
        public string Danger = "none";
    }

    // ------------------------------------------------------------------ 開始・停止

    /// <summary>ルートの [from, to] 区間を走らせる。</summary>
    public void StartRoute(BotRouteStep[] route, int from, int to, string title)
    {
        var steps = new List<BotRouteStep>();
        for (var i = Mathf.Max(0, from); i <= Mathf.Min(to, route.Length - 1); i++) steps.Add(route[i]);
        Begin(steps, title, () =>
        {
            if (!Settings.ApplyPresets) return;
            // 「前の区間まで終えた」状態にそろえる: エリア1 のフラグを外してから前の区間のフラグを立てる
            TestPlayActions.ClearArea1Flags();
            foreach (var flag in BotRoutes.FlagsBefore(route, from)) GameProgress.Set(flag);
            // 同じシーンにいる時は読み直さないとフラグ (倒したボス等) が反映されない
            _reloadFirst = TestPlayActions.CurrentStageName == route[Mathf.Max(0, from)].Scene;
            foreach (var up in BotRoutes.UpgradesBefore(route, from)) TestPlayActions.SetUpgrade(up, true);
        });
    }

    /// <summary>シーンごとに「全入口 × 全出口」を確かめる。</summary>
    public void StartAllExits(IEnumerable<string> scenes, string title)
    {
        var steps = new List<BotRouteStep>();
        foreach (var s in scenes) steps.AddRange(BotRoutes.AllExitsFromAllSpawns(s));
        Begin(steps, title, null);
    }

    private Action _prepareOnce;

    private void Begin(List<BotRouteStep> steps, string title, Action prepare)
    {
        if (Running) Stop("restart");
        _steps.Clear();
        _steps.AddRange(steps);
        _index = 0;
        _prepareOnce = prepare;
        _failures.Clear();
        _screenshots = 0;
        Jev.ResetStats();
        Jev.ReloadKey();
        Report = new BotReport { Title = title };
        Report.Settings = SettingsText();
        _runRealT0 = Time.realtimeSinceStartup;
        _runGameT0 = Time.time;
        _input = new VirtualPlayerInput();
        _previousOverride = PlayerInputSource.Override;
        PlayerInputSource.Override = _input;
        _world = new BotWorld();
        _physics = new BotPhysics(_world);
        _exec = new BotExecutor(_world, _physics, _input);
        GameProgress.Changed += OnFlagChanged;
        Application.runInBackground = true;
        TestPlaySpeed.SetScale(Settings.Speed);
        TestPlayActions.Invincible = Settings.Invincible;
        if (Settings.UseJev && !Jev.HasKey)
            Debug.LogWarning("[TestPlayBot] " + JevClient.MissingKeyMessage);
        Debug.Log($"[TestPlayBot] 開始: {title} ({steps.Count} 区間) / Jev キー: {Jev.KeySource}");
        _forceWarp = true;
        StartHop();
    }

    private string SettingsText() =>
        $"速度 {Settings.Speed:0.##}x, 思考中 {(Settings.ThinkSpeed < 0 ? "自動" : Settings.ThinkSpeed == 0 ? "停止" : Settings.ThinkSpeed.ToString("0.##") + "x")}, " +
        $"無敵 {(Settings.Invincible ? "ON" : "OFF")}, Jev {(Settings.UseJev ? (Jev.HasKey ? "ON" : "ON (キー無し→ローカル)") : "OFF (ローカル方針)")}, " +
        $"止まり判定 {Settings.StuckSeconds:0}s, 区間上限 {Settings.HopTimeout:0}s, 迷い閾値 {Settings.LowConfidence:0.00}";

    /// <summary>止める (レポートを書く)。</summary>
    public void Stop(string reason = "stopped")
    {
        if (_phase == Phase.Idle) return;
        Jev.Cancel();
        EndThinking();
        _exec?.Abort(reason);
        if (_hop != null && _hop.Result == "running") EndHop("stopped", reason);
        _phase = Phase.Finished;
        FinishRun();
    }

    private void FinishRun()
    {
        GameProgress.Changed -= OnFlagChanged;
        _input?.ReleaseAll();
        if (PlayerInputSource.Override == _input) PlayerInputSource.Override = _previousOverride;
        GamePause.SetBaseTimeScale(TestPlaySpeed.Scale);
        if (Report != null)
        {
            Report.JevCalls = Jev.Calls;
            Report.JevFailures = Jev.Failures;
            Report.JevRetries = Jev.Retries;
            Report.JevRequests = Jev.Requests;
            Report.JevRequestCap = Jev.MaxRequestsPerRun;
            Report.InputTokens = Jev.InputTokens;
            Report.OutputTokens = Jev.OutputTokens;
            Report.JevLatency = Jev.TotalLatency;
            Report.Model = Jev.ModelVersion;
            Report.RealSeconds = Time.realtimeSinceStartup - _runRealT0;
            Report.GameSeconds = Time.time - _runGameT0;
            try
            {
                LastReportPath = Report.Write();
                Debug.Log($"[TestPlayBot] 終了。レポート: {LastReportPath}\n{Summary()}");
            }
            catch (Exception e)
            {
                Debug.LogError("[TestPlayBot] レポートを書けませんでした: " + e.Message);
            }
        }

        Status = "終了";
    }

    /// <summary>結果の要約 (1 行ずつ)。</summary>
    public string Summary()
    {
        if (Report == null) return "-";
        var ok = Report.Hops.Count(h => h.Result == "ok");
        return $"区間 {ok}/{Report.Hops.Count} 成功, Jev {Jev.Calls} 回 / トークン {Jev.InputTokens}+{Jev.OutputTokens}, " +
               $"止まり {Report.Incidents.Count(i => i.Kind == "stuck")}, 迷い {Report.Incidents.Count(i => i.Kind == "hesitation")}, " +
               $"死亡 {Report.Incidents.Count(i => i.Kind == "death")}\n" +
               string.Join("\n", Report.Hops.Select(h => $"  {h.Label}: {h.Result} {h.Reason} ({h.GameSeconds:0.0}s, Jev {h.JevCalls})"));
    }

    private void OnDestroy()
    {
        if (Running) Stop("destroyed");
        if (Instance == this) Instance = null;
    }

    private void OnFlagChanged(string flag)
    {
        // 扉が開く (スライドに約 0.8 秒) のを待ってから地形を調べ直す
        _rescanAt = Time.time + 1.2f;
    }

    // ------------------------------------------------------------------ 区間

    private void StartHop()
    {
        if (_index >= _steps.Count)
        {
            _phase = Phase.Finished;
            FinishRun();
            return;
        }

        var step = _steps[_index];
        _hop = new BotHopResult { Label = step.ToString(), Scene = step.Scene };
        Report.Hops.Add(_hop);
        _hopDecisions.Clear();
        _hesitationShots.Clear();
        _visited.Clear();
        _visitCount.Clear();
        _recent.Clear();
        _stuckMode = false;
        _stuckCount = 0;
        _sameFailStreak = 0;
        _bestCost = float.PositiveInfinity;
        _bestBossHp = int.MaxValue;
        _hopGameT0 = Time.time;
        _hopRealT0 = Time.realtimeSinceStartup;
        _phase = Phase.Prepare;
        _phaseT0 = Time.realtimeSinceStartup;
        Status = "準備: " + step;
    }

    private void EndHop(string result, string reason)
    {
        if (_hop == null) return;
        _hop.Result = result;
        _hop.Reason = reason;
        _hop.GameSeconds = Time.time - _hopGameT0;
        _hop.RealSeconds = Time.realtimeSinceStartup - _hopRealT0;
        Debug.Log($"[TestPlayBot] 区間 {_hop.Label}: {result} {reason} ({_hop.GameSeconds:0.0}s, 判断 {_hop.Decisions}, Jev {_hop.JevCalls})");
    }

    private void NextHop(bool success, string reason)
    {
        var step = _steps[_index];
        EndHop(success ? "ok" : "failed", reason);
        _index++;
        _exec.Abort("hop end");
        _input.ReleaseAll();

        if (!success && _index < _steps.Count)
        {
            if (!Settings.ContinueOnFail)
            {
                _phase = Phase.Finished;
                FinishRun();
                return;
            }

            // 失敗した区間は飛ばす: 次の区間の入口へワープ
            _hop.WarpedPast = true;
            _forceWarp = true;
            var next = _steps[_index];
            if (next.Entrance == null && step.Next == next.Scene && !string.IsNullOrEmpty(_goalEntrance))
                _warpEntrance = _goalEntrance;
        }

        StartHop();
    }

    private string _warpEntrance;

    // ------------------------------------------------------------------ 毎フレーム

    // ------------------------------------------------------------------ 単発の跳躍テスト (調整用)

    /// <summary>直前の単発テストの結果。</summary>
    public string TestResult;
    private bool _testing;
    private BotSegment _testFrom, _testTo;
    private float _testT0;

    /// <summary>
    /// 今のシーンで床 from (P12 など) に立ち、to へ跳ぶ行動を 1 回だけ実行する (ボットの実行部の調整用)。
    /// 結果は TestResult。ルートの実行中は使えない。
    /// </summary>
    public string TestJump(string from, string to)
    {
        if (Running) return "running";
        var p = TestPlayActions.Player;
        if (p == null) return "no player";
        _input ??= new VirtualPlayerInput();
        if (PlayerInputSource.Override != _input) { _previousOverride = PlayerInputSource.Override; PlayerInputSource.Override = _input; }
        _world = new BotWorld();
        _physics = new BotPhysics(_world);
        _exec = new BotExecutor(_world, _physics, _input);
        _world.Scan(p);
        _physics.ReadPlayer(p);
        _testFrom = _world.Segments.FirstOrDefault(s => s.Name == from);
        _testTo = _world.Segments.FirstOrDefault(s => s.Name == to);
        if (_testFrom == null || _testTo == null) return "segment not found";
        TestPlayActions.Teleport(new Vector2(_testFrom.CenterX, _testFrom.YAt(_testFrom.CenterX) + _world.PivotToFeet + 0.05f));
        _testT0 = Time.time;
        _testing = true;
        TestResult = "running";
        return $"{_testFrom} -> {_testTo}";
    }

    private void TickTest()
    {
        var p = TestPlayActions.Player;
        if (p == null) return;
        if (_testFrom != null)
        {
            if (Time.time - _testT0 < 0.4f || !p.IsGrounded) return;
            var target = new BotTarget { Segment = _testTo };
            var plan = _physics.Plan(_testFrom, p.transform.position, target);
            _exec.Begin(new BotAction { Key = "test", Kind = BotActionKind.JumpTo, Target = target, Plan = plan }, p);
            TestResult = "plan: " + plan.Describe();
            _testFrom = null;
            return;
        }

        if (_exec.Busy)
        {
            ApplySpeed();
            _exec.Tick(p);
            return;
        }

        TestResult += " => " + _exec.Status + " " + _exec.Result;
        _exec.Clear();
        _testing = false;
        _input.ReleaseAll();
        PlayerInputSource.Override = _previousOverride;
        GamePause.SetBaseTimeScale(TestPlaySpeed.Scale);
    }

    private void Update()
    {
        if (_testing && !Running)
        {
            TickTest();
            return;
        }

        if (!Running) return;
        Jev.Tick();
        if (TickDialogue()) return;

        var player = TestPlayActions.Player;
        ApplySpeed();

        switch (_phase)
        {
            case Phase.Prepare: TickPrepare(); break;
            case Phase.WaitLoad: TickWaitLoad(player); break;
            case Phase.Settle: TickSettle(player); break;
            case Phase.Act: TickAct(player); break;
            case Phase.Thinking: TickThinking(player); break;
            case Phase.Dead: TickDead(player); break;
        }
    }

    private float _dialogueSince = -1f;
    private float _dialogueNextTap;

    /// <summary>
    /// 会話 (Utage) の再生中は 0.3 秒ごとに攻撃ボタンをタップして読み進める (DialogueInputStrategy が会話送りとして拾う)。
    /// 会話中は true を返し、区間の処理 (ポーズからの強制脱出を含む) を止める。60 秒抜けなければ記録して停止する。
    /// </summary>
    private bool TickDialogue()
    {
        if (!DialogueService.IsPlaying)
        {
            _dialogueSince = -1f;
            return false;
        }

        var now = Time.realtimeSinceStartup;
        if (_dialogueSince < 0f)
        {
            _dialogueSince = now;
            _dialogueNextTap = now + 0.5f;
            Status = "会話を読み進め中";
        }

        if (now >= _dialogueNextTap)
        {
            _input.Tap(PlayerButton.Attack);
            _dialogueNextTap = now + 0.3f;
        }

        if (now - _dialogueSince > 60f)
        {
            Incident("error", "60 秒以上会話から抜けられないので停止", TestPlayActions.Player, true);
            _dialogueSince = -1f;
            Stop("dialogue_stuck");
            return true;
        }

        return true;
    }

    /// <summary>速度: 利用者の倍率 / 思考中 / 精密な操作中 (高速時はフレームあたりの物理ステップを 2 回までに)。</summary>
    private void ApplySpeed()
    {
        // オーバーレイ・ホットキーで速度を変えたら設定に取り込む
        if (Mathf.Abs(TestPlaySpeed.Scale - Settings.Speed) > 0.001f) Settings.Speed = TestPlaySpeed.Scale;

        var target = Settings.Speed;
        if (_phase == Phase.Thinking && ThinkScale() > 0f) target = Mathf.Min(target, ThinkScale());
        else if (_exec != null && _exec.NeedsPrecision && target > 1f)
            target = Mathf.Min(target, Mathf.Max(1f, 0.04f / Mathf.Max(Time.unscaledDeltaTime, 0.001f)));

        if (!Mathf.Approximately(target, _lastAppliedScale) || !Mathf.Approximately(GamePause.BaseTimeScale, target))
        {
            GamePause.SetBaseTimeScale(target);
            _lastAppliedScale = target;
        }
    }

    private float ThinkScale()
    {
        if (Settings.ThinkSpeed >= 0f) return Settings.ThinkSpeed;
        return Settings.Speed >= 1f ? 0f : Mathf.Min(Settings.Speed, 0.25f);
    }

    private void TickPrepare()
    {
        var step = _steps[_index];
        var player = TestPlayActions.Player;
        // 読み直し直後 (死亡後など) は古いプレイヤーが残っていることがあるので少し待つ
        if (Time.realtimeSinceStartup - _phaseT0 < 0.5f || player == null || player.IsDead || TestPlayActions.IsLoading)
            return;

        if (_prepareOnce != null)
        {
            var once = _prepareOnce;
            _prepareOnce = null;
            once();
        }

        if (_reloadFirst)
        {
            _reloadFirst = false;
            SnapshotAbilities(player);
            GamePause.Reset();
            StageLoader.LoadWithPlayerScene(step.Scene);
            _prepareOnce = RestoreAbilities;
            _forceWarp = true;
            _phaseT0 = Time.realtimeSinceStartup;
            return;
        }

        var current = TestPlayActions.CurrentStageName;
        var entrance = _warpEntrance ?? step.Entrance;
        _warpEntrance = null;
        if (_forceWarp || current != step.Scene)
        {
            Status = $"ワープ: {step.Scene} {entrance}";
            TestPlayActions.Warp(step.Scene, entrance);
            _hopEntrance = entrance;
        }

        _forceWarp = false;
        _phase = Phase.WaitLoad;
        _phaseT0 = Time.realtimeSinceStartup;
        _expectedScene = step.Scene;
    }

    private string _hopEntrance;
    private bool _reloadFirst;

    private void TickWaitLoad(PlayerController player)
    {
        if (Time.realtimeSinceStartup - _phaseT0 > 30f)
        {
            Incident("error", "シーンの読み込みが 30 秒で終わらない", player, false);
            NextHop(false, "load timeout");
            return;
        }

        if (TestPlayActions.IsLoading || player == null) return;
        if (TestPlayActions.CurrentStageName != _expectedScene) return;
        _phase = Phase.Settle;
        _phaseT0 = Time.realtimeSinceStartup;
        _settleGameT0 = Time.time;
    }

    private float _settleGameT0;

    private void TickSettle(PlayerController player)
    {
        if (player == null) return;
        if (Time.time - _settleGameT0 < 0.35f && Time.realtimeSinceStartup - _phaseT0 < 3f) return;

        TestPlayActions.Invincible = Settings.Invincible;
        SnapshotAbilities(player);
        Rescan(player);

        var step = _steps[_index];
        if (step.Next == "*")
        {
            ExpandAllExits(step);
            return;
        }

        _lastProgressT = Time.time;
        _lastPos = player.transform.position;
        _phase = Phase.Act;
        Status = "実行中: " + step;
    }

    private void ExpandAllExits(BotRouteStep step)
    {
        var exits = _world.Entities.Where(e => e.Kind == "exit" || e.Kind == "transport").ToList();
        var concrete = exits.Select(e => new BotRouteStep
        {
            Scene = step.Scene, Entrance = step.Entrance ?? "", Next = e.Label, ViaTransport = e.Kind == "transport",
            Label = $"{step.Scene} [{(string.IsNullOrEmpty(step.Entrance) ? "default" : step.Entrance)}] → {e.Label} ({e.Rect.center.x:F0},{e.Rect.center.y:F0})",
            ExitHint = e.Rect.center,
        }).ToList();

        Report.Hops.Remove(_hop);
        _steps.RemoveAt(_index);
        _steps.InsertRange(_index, concrete);
        _forceWarp = true;
        StartHop();
    }

    // ------------------------------------------------------------------ 行動

    private void TickAct(PlayerController player)
    {
        // 遷移の検出
        if (TestPlayActions.IsLoading || TestPlayActions.CurrentStageName != _hop.Scene)
        {
            if (LastDecision != null && LastDecision.Outcome == null) LastDecision.Outcome = "transition";
            _exec.Abort("transition");
            _input.ReleaseAll();
            _phase = Phase.WaitLoad;
            _phaseT0 = Time.realtimeSinceStartup;
            _transitionPending = true;
            _expectedScene = null;
            return;
        }

        if (player == null) return;

        // 死亡
        if (player.IsDead)
        {
            _exec.Abort("dead");
            _phase = Phase.Dead;
            _deadRealT0 = Time.realtimeSinceStartup;
            _deathLogged = false;
            return;
        }

        // 一時停止中 (テストの一時停止 F5 はそのまま待つ / 会話・メニューは抜ける)
        if (GamePause.IsPaused && TestPlaySpeed.Frozen)
            return;
        if (GamePause.IsPaused)
        {
            if (_pausedSince < 0f) _pausedSince = Time.realtimeSinceStartup;
            var paused = Time.realtimeSinceStartup - _pausedSince;
            if (paused > 1.5f && Mathf.FloorToInt(paused * 2f) % 2 == 0) _input.MenuSubmit();
            if (paused > 8f)
            {
                Incident("error", "8 秒以上ポーズ (会話・メニュー) から抜けられないので強制的に解除", player, true);
                GamePause.Reset();
                _pausedSince = -1f;
            }

            return;
        }

        _pausedSince = -1f;

        // 安全地点への戻し (穴・トゲ) の検出
        var pos = (Vector2)player.transform.position;
        if ((pos - _lastPos).sqrMagnitude > 9f)
        {
            Incident("hazard", $"({_lastPos.x:F1},{_lastPos.y:F1}) から ({pos.x:F1},{pos.y:F1}) へ戻された (穴/トゲ)。直前の行動: {_lastActionKey}", player, false);
            _exec.Abort("returned");
        }

        _lastPos = pos;

        // シーン内のゴール (FP2 など) に着いた
        if (_goalKind == "reach")
        {
            var col = player.GetComponent<Collider2D>();
            if (col != null && BotWorld.ToRect(col.bounds).Overlaps(_goalRect))
            {
                NextHop(true, "reached");
                return;
            }
        }

        // 区間の上限
        if (Time.time - _hopGameT0 > Settings.HopTimeout)
        {
            Incident("timeout", $"区間の上限 {Settings.HopTimeout:0} 秒を超えた (ゴール: {GoalText})", player, true);
            NextHop(false, "timeout");
            return;
        }

        if (_exec.Busy)
        {
            _exec.Tick(player);
            if (_exec.Busy) return;
        }

        if (_exec.Action != null)
            OnActionFinished(player);

        // 地形の調べ直し (扉が開いた・箱が壊れた)
        if (_rescanRequested || (_rescanAt > 0f && Time.time >= _rescanAt))
        {
            _rescanAt = -1f;
            _rescanRequested = false;
            Rescan(player);
        }

        if (!player.IsGrounded && player.CurrentStateName != nameof(WallClingState)) return; // 着地を待つ
        if (player.CurrentStateName == nameof(LedgeClimbState)) return;

        Decide(player);
    }

    private bool _transitionPending;
    private int _bestBossHp = int.MaxValue;
    private int _lastEnemyCount = -1;

    private void OnActionFinished(PlayerController player)
    {
        var action = _exec.Action;
        var ok = _exec.Status == BotStatus.Succeeded;
        var outcome = (ok ? "ok: " : "fail: ") + _exec.Result;
        if (LastDecision != null && LastDecision.Choice == action.Key && LastDecision.Outcome == null)
            LastDecision.Outcome = outcome;
        _recent.Add($"{action.Key} -> {outcome}");
        if (_recent.Count > 5) _recent.RemoveAt(0);

        if (!ok)
        {
            var key = FailureKey(action);
            if (key != null)
            {
                _failures[key] = _failures.TryGetValue(key, out var n) ? n + 1 : 1;
                ApplyPenalties();
            }

            _sameFailStreak = action.Key == _lastActionKey ? _sameFailStreak + 1 : 1;
            if (_exec.Result.StartsWith("blocked")) _rescanRequested = true;
        }
        else _sameFailStreak = 0;

        _lastActionKey = action.Key;
        _exec.Abort("consumed");
        _exec.Clear();
    }

    private string FailureKey(BotAction a)
    {
        if (a.Kind != BotActionKind.JumpTo && a.Kind != BotActionKind.WalkTo && a.Kind != BotActionKind.WaitForPlatform && a.Kind != BotActionKind.RideTo
            && a.Kind != BotActionKind.Climb)
            return null;
        var from = _decisionSegment;
        if (from == null) return null;
        var to = a.Target == null ? "?" : a.Target.IsRect ? "goal" : $"{a.Target.Segment.CenterX:F0},{a.Target.Segment.YCenter:F0}";
        return $"{_world.SceneName}|{from.CenterX:F0},{from.YCenter:F0}|{to}";
    }

    private readonly HashSet<int> _simFail = new();
    private int _simFailVersion = -1;
    private bool _graphDirty;

    private void MarkSimFail(int from, int to)
    {
        if (_simFailVersion != _world.ScanVersion)
        {
            _simFail.Clear();
            _simFailVersion = _world.ScanVersion;
        }

        var key = BotWorld.EdgeKey(from, to);
        if (!_simFail.Add(key)) return;
        _world.EdgePenalty[key] = (_world.EdgePenalty.TryGetValue(key, out var p) ? p : 0f) + 25f;
        _graphDirty = true;
    }

    private void ApplyPenalties()
    {
        _world.EdgePenalty.Clear();
        foreach (var e in _world.Edges.ToList())
        {
            var from = _world.Segments[e.From];
            var to = e.To < 0 ? "goal" : $"{_world.Segments[e.To].CenterX:F0},{_world.Segments[e.To].YCenter:F0}";
            var key = $"{_world.SceneName}|{from.CenterX:F0},{from.YCenter:F0}|{to}";
            if (_failures.TryGetValue(key, out var n))
                _world.EdgePenalty[BotWorld.EdgeKey(e.From, e.To)] = 6f * n;
        }

        if (_simFailVersion == _world.ScanVersion)
            foreach (var k in _simFail)
                _world.EdgePenalty[k] = (_world.EdgePenalty.TryGetValue(k, out var p) ? p : 0f) + 25f;

        ResolveGoal(TestPlayActions.Player);
    }

    private void Rescan(PlayerController player)
    {
        _world.Scan(player);
        _physics.ReadPlayer(player);
        _lastScanT = Time.time;
        ResolveGoal(player);
        ApplyPenalties();
    }

    // ------------------------------------------------------------------ ゴール

    private void ResolveGoal(PlayerController player)
    {
        if (player == null || _index >= _steps.Count) return;
        var step = _steps[_index];
        _world.RefreshEnemies();
        _goalEntity = null;
        _goalEntrance = null;
        _goalNextScene = step.Next;

        // 戦闘が始まった闘技場 (ウェーブ・ボス) ではゲートが閉じている: 敵を倒すのが先
        if (TryFightGoal(player))
            return;

        _goalNextScene = step.Next;

        if (Settings.Objectives)
        {
            foreach (var o in step.Objectives)
            {
                var parts = o.Split(':');
                if (parts.Length != 2 || GameProgress.Has(parts[1])) continue;
                var kind = parts[0];
                BotEntity target = null;
                if (kind == "boss")
                {
                    var arena = _world.Entities.FirstOrDefault(e => e.Kind == "arena" && e.Label == parts[1]);
                    var boss = arena != null ? BotWorld.GetField<EnemyController>(arena.Source, "_boss") : null;
                    target = boss != null && !boss.IsDead ? _world.Enemies.FirstOrDefault(e => e.Source == boss) : null;
                    if (target == null && arena != null) target = arena;
                }
                else if (kind == "speaker")
                    target = _world.Entities.FirstOrDefault(e => e.Kind == "speaker" && e.Label == parts[1] && e.Active);
                else if (kind == "pickup")
                    target = _world.Entities.FirstOrDefault(e => e.Kind == "pickup" && e.Active &&
                                                                 BotWorld.GetField<string>(e.Source, "_pickupId") == parts[1] &&
                                                                 e.Source.gameObject.activeInHierarchy);

                if (target == null) continue;
                _goalKind = kind;
                _goalEntity = target;
                var r = target.Rect;
                _goalRect = kind == "pickup" ? r : new Rect(r.xMin - 0.8f, r.yMin - 0.3f, r.width + 1.6f, r.height + 0.6f);
                GoalText = $"{kind} {parts[1]} ({r.center.x:F0},{r.center.y:F0})";
                _world.BuildGraph(_goalRect);
                return;
            }
        }

        if (step.ReachRect.HasValue)
        {
            _goalKind = "reach";
            _goalRect = step.ReachRect.Value;
            GoalText = $"reach ({_goalRect.center.x:F0},{_goalRect.center.y:F0})";
            _world.BuildGraph(_goalRect);
            return;
        }

        if (string.IsNullOrEmpty(step.Next))
        {
            _goalKind = "none";
            GoalText = "-";
            _world.BuildGraph(null);
            return;
        }

        var kindWanted = step.ViaTransport ? "transport" : "exit";
        var candidates = _world.Entities.Where(e => e.Kind == kindWanted && e.Label == step.Next).ToList();
        if (candidates.Count == 0)
            candidates = _world.Entities.Where(e => (e.Kind == "exit" || e.Kind == "transport") && e.Label == step.Next).ToList();
        if (step.ExitHint.HasValue)
            candidates = candidates.OrderBy(e => (e.Rect.center - step.ExitHint.Value).sqrMagnitude).Take(1).ToList();

        if (candidates.Count == 0)
        {
            _goalKind = "none";
            GoalText = $"出口 {step.Next} が見つからない";
            _world.BuildGraph(null);
            return;
        }

        // 今いる床から見積もりがいちばん小さい出口
        var cur = _world.SegmentUnder(player);
        BotEntity best = null;
        var bestCost = float.PositiveInfinity;
        foreach (var c in candidates)
        {
            _world.BuildGraph(c.Rect);
            var cost = cur != null ? _world.Cost(cur) : 0f;
            if (best == null || cost < bestCost) { best = c; bestCost = cost; }
        }

        _goalKind = best.Kind;
        _goalEntity = best;
        _goalRect = best.Rect;
        _goalEntrance = best.Source is SceneTransitionZone ? BotWorld.GetField<string>(best.Source, "_entranceId") :
            BotWorld.GetField<string>(best.Source, "_entranceId");
        GoalText = $"{best.Kind} → {best.Label} ({best.Rect.center.x:F0},{best.Rect.center.y:F0})";
        _world.BuildGraph(best.Rect);
    }

    /// <summary>始まった (ゲートが閉じた) 闘技場があれば、その敵をゴールにする。</summary>
    private bool TryFightGoal(PlayerController player)
    {
        Rect? arenaRect = null;
        EnemyController boss = null;
        foreach (var e in _world.Entities)
        {
            if (e.Source == null) continue;
            if (e.Kind == "wavearena" && e.Source is WaveArena wa && wa.enabled &&
                BotWorld.GetField<bool>(wa, "_started") && !GameProgress.Has(e.Label))
            {
                arenaRect = e.Rect;
                break;
            }

            if (e.Kind == "arena" && e.Source is BossArena ba && ba.enabled && BotWorld.GetField<bool>(ba, "_started"))
            {
                var b = BotWorld.GetField<EnemyController>(ba, "_boss");
                if (b != null && !b.IsDead)
                {
                    arenaRect = e.Rect;
                    boss = b;
                    break;
                }
            }
        }

        if (!arenaRect.HasValue) return false;

        var pos = (Vector2)player.transform.position;
        var area = new Rect(arenaRect.Value.xMin - 6f, arenaRect.Value.yMin - 6f, arenaRect.Value.width + 12f, arenaRect.Value.height + 12f);
        var target = boss != null
            ? _world.Enemies.FirstOrDefault(e => e.Source == boss)
            : _world.Enemies.Where(e => BotExecutor.EntityAlive(e) && area.Contains(e.Rect.center))
                .OrderBy(e => (e.Rect.center - pos).sqrMagnitude).FirstOrDefault();

        if (target == null)
        {
            // ウェーブの合間: 闘技場の中で待つ
            _goalKind = "fight_wait";
            _goalEntity = null;
            _goalRect = arenaRect.Value;
            GoalText = "闘技場: 次のウェーブを待つ";
            _world.BuildGraph(_goalRect);
            return true;
        }

        _goalKind = "fight";
        _goalEntity = target;
        var r = target.Rect;
        _goalRect = new Rect(r.xMin - 0.8f, r.yMin - 0.3f, r.width + 1.6f, r.height + 0.6f);
        GoalText = $"闘技場: {target.Kind} {target.Id} を倒す ({r.center.x:F0},{r.center.y:F0})";
        _world.BuildGraph(_goalRect);
        return true;
    }

    // ------------------------------------------------------------------ 判断

    private BotSegment _decisionSegment;

    private void Decide(PlayerController player)
    {
        // ボスなど動く目的は毎回ゴールを取り直す
        // ゴールは毎回取り直す (ボスなど動く目的・途中で始まる闘技場の戦闘・開いた扉)
        ResolveGoal(player);

        var cur = _world.SegmentUnder(player);
        if (cur == null)
        {
            if (_noSegmentSince < 0f) _noSegmentSince = Time.time;
            if (Time.time - _noSegmentSince > 1.5f && Time.time - _lastScanT > 1f)
                Rescan(player);
            cur = _world.SegmentAt((Vector2)player.transform.position - new Vector2(0f, _world.PivotToFeet), 1.6f);
        }
        else _noSegmentSince = -1f;

        // 目的でない出口には跳び込まない (計画のシミュレーションで避ける)
        _physics.AvoidRects.Clear();
        foreach (var e in _world.Entities)
            if (e.Kind == "exit" && e != _goalEntity) _physics.AvoidRects.Add(e.Rect);

        _decisionSegment = cur;
        if (cur != null) _visited.Add(SegKey(cur));
        if (cur != null) _visitCount[SegKey(cur)] = (_visitCount.TryGetValue(SegKey(cur), out var vc) ? vc : 0) + 1;
        UpdateProgress(player, cur);
        if (_phase != Phase.Act) return; // 進捗の判定で区間が終わった

        _options = BuildOptions(player, cur);
        for (var pass = 0; pass < 3 && _graphDirty; pass++)
        {
            // シミュレーションで跳べなかった辺を重くして経路を引き直す (行ったり来たりを防ぐ)
            _graphDirty = false;
            _world.BuildGraph(_world.HasGoal ? _world.Goal : (Rect?)null);
            _options = BuildOptions(player, cur);
        }

        _graphDirty = false;

        if (_options.Count == 0)
        {
            StartAction(player, new BotAction { Key = "wait", Kind = BotActionKind.Wait, WalkX = 0.5f }, null);
            return;
        }

        var decision = new BotDecision
        {
            Scene = _world.SceneName, Position = player.transform.position, Segment = cur != null ? cur.Name : "-",
            Goal = GoalText, GameTime = Time.time - _runGameT0, Options = _options.Count,
            OptionsText = string.Join(" ", _options.Select(o => $"{o.Key}({o.Score:0.0}{(o.PhysicsOk ? "" : "!")})")),
        };

        if (Settings.UseJev && Jev.HasKey && !(Settings.AutoSkipSingle && _options.Count == 1))
        {
            _pending = decision;
            var questions = BuildQuestions(player);
            var state = BuildState(player, cur);
            _thinkRealT0 = Time.realtimeSinceStartup;
            Jev.Ask(state, questions, OnJevAnswer);
            _phase = Phase.Thinking;
            BeginThinking();
            Status = "Jev に問い合わせ中";
            return;
        }

        var local = LocalChoice(player);
        decision.Choice = local.Key;
        decision.Source = _options.Count == 1 ? "auto" : "local";
        if (decision.Source == "auto") Report.AutoDecisions++;
        else Report.LocalDecisions++;
        Commit(player, decision, local);
    }

    private void BeginThinking()
    {
        if (ThinkScale() <= 0f && !TestPlaySpeed.Frozen)
        {
            TestPlaySpeed.SetFrozen(true);
            _thinkFrozen = 1;
        }
    }

    private void EndThinking()
    {
        if (_thinkFrozen > 0)
        {
            TestPlaySpeed.SetFrozen(false);
            _thinkFrozen = 0;
        }
    }

    private void TickThinking(PlayerController player)
    {
        // 応答待ち (Jev.Tick で callback が来る)。30 秒で諦める
        if (Time.realtimeSinceStartup - _thinkRealT0 > 30f)
        {
            Jev.Cancel();
            EndThinking();
            _phase = Phase.Act;
        }
    }

    private void OnJevAnswer(JevAnswer answer)
    {
        EndThinking();
        if (_phase != Phase.Thinking || _pending == null)
            return;
        if (answer.Error == JevClient.CapError)
        {
            Debug.LogWarning($"[TestPlayBot] Jev のリクエストが上限 {Jev.MaxRequestsPerRun} 回に達したので停止します (費用の歯止め)。");
            Stop("jev_cap");
            return;
        }
        _phase = Phase.Act;
        var player = TestPlayActions.Player;
        if (player == null) return;

        var d = _pending;
        _pending = null;
        d.LatencySeconds = answer.LatencySeconds;
        BotOption chosen = null;
        if (answer.Ok)
        {
            chosen = _options.FirstOrDefault(o => o.Key == answer.Choice);
            d.Source = "jev";
            d.Confidence = answer.Confidence;
            d.TopProbabilities = answer.Probabilities.OrderByDescending(p => p.Value).Take(4).ToList();
            _hop.JevCalls++;
            _hop.Tokens += answer.InputTokens + answer.OutputTokens;
            // 「敵に当たりそうか」(noul) は確率表の末尾に残す
            if (answer.Nouls.TryGetValue("danger", out var danger))
                d.TopProbabilities.Add(new KeyValuePair<string, float>("(danger)", danger));
        }

        if (chosen == null)
        {
            chosen = LocalChoice(player);
            d.Source = answer.Ok ? "fallback" : "local";
            if (!answer.Ok)
            {
                Report.LocalDecisions++;
                if (answer.Error != "no_api_key") Debug.LogWarning("[TestPlayBot] Jev 失敗 → ローカル方針: " + answer.Error);
            }
        }

        d.Choice = chosen.Key;
        if (d.Source == "jev")
        {
            _hop.MinConfidence = Mathf.Min(_hop.MinConfidence, d.Confidence);
            if (d.Confidence < Settings.LowConfidence)
            {
                var segKey = d.Segment + "|" + GoalText;
                var shot = _hesitationShots.Add(segKey);
                var top = string.Join(", ", d.TopProbabilities.Select(p => $"{p.Key} {p.Value:0.00}"));
                Incident("hesitation", $"確信度 {d.Confidence:0.00} で `{d.Choice}` を選んだ (床 {d.Segment}, ゴール {GoalText}, 選択肢 {d.Options})。確率: {top}", player, shot, d);
            }
        }

        Commit(player, d, chosen);
    }

    private void Commit(PlayerController player, BotDecision d, BotOption option)
    {
        LastDecision = d;
        Report.Decisions.Add(d);
        _hopDecisions.Add(d);
        if (_hopDecisions.Count > 60) _hopDecisions.RemoveAt(0);
        _hop.Decisions++;
        StartAction(player, option.Action, option);
        Status = $"実行: {option.Key}" + (d.Confidence >= 0 ? $" (確信度 {d.Confidence:0.00})" : $" ({d.Source})");
    }

    private void StartAction(PlayerController player, BotAction action, BotOption option)
    {
        // 跳躍は計画を今の位置から取り直す (思考中に少し動いた場合)
        _exec.Begin(action, player);
    }

    private BotOption LocalChoice(PlayerController player)
    {
        // 近くの敵がルートを塞いでいれば先に倒す
        var attack = _options.Where(o => o.Action.Kind == BotActionKind.Attack || o.Action.Kind == BotActionKind.Finisher)
            .OrderBy(o => o.Score).FirstOrDefault();
        if (attack != null && attack.Score < 1f) return attack;
        return _options.OrderBy(o => o.Score).First();
    }

    // ------------------------------------------------------------------ 進捗・止まり

    private void UpdateProgress(PlayerController player, BotSegment cur)
    {
        var cost = cur != null ? _world.Cost(cur) : float.PositiveInfinity;
        if (cur != null && _goalKind != "none" && !float.IsInfinity(cost))
        {
            // 同じ床の中ではゴールへの次の跳躍点までの距離も足す
            if (_world.NextHop.TryGetValue(cur.Id, out var hop))
            {
                var tx = hop.To >= 0 ? _world.Segments[hop.To].CenterX : _goalRect.center.x;
                cost += Mathf.Abs(cur.ClampX(tx) - player.transform.position.x) / 8f;
            }
        }

        if (cost < _bestCost - 0.25f)
        {
            _bestCost = cost;
            _lastProgressT = Time.time;
            _stuckMode = false;
        }

        // 闘技場: 敵の数が変わったら進展 (倒した・次のウェーブ)
        if (_goalKind == "fight" || _goalKind == "fight_wait")
        {
            var alive = _world.Enemies.Count(e => BotExecutor.EntityAlive(e));
            if (alive != _lastEnemyCount)
            {
                _lastEnemyCount = alive;
                _lastProgressT = Time.time;
            }
        }

        // ボス戦: 削った HP・防御値も進展とみなす
        if (_goalEntity != null && _goalEntity.Source is EnemyController boss && !boss.IsDead)
        {
            var hp = boss.Hp.CurrentValue + boss.Guard.CurrentValue;
            if (hp < _bestBossHp)
            {
                _bestBossHp = hp;
                _lastProgressT = Time.time;
            }
        }

        var noProgress = Time.time - _lastProgressT;
        if (noProgress > Settings.StuckSeconds || _sameFailStreak >= 4)
        {
            _stuckCount++;
            var why = _sameFailStreak >= 4 ? $"同じ行動 `{_lastActionKey}` が {_sameFailStreak} 回続けて失敗" : $"{Settings.StuckSeconds:0} 秒進展なし";
            Incident("stuck", $"{why} (ゴール: {GoalText}, 床 {(cur != null ? cur.Name : "-")}, 見積もり {(float.IsInfinity(_bestCost) ? "経路なし" : _bestCost.ToString("0.0") + "s")})", player, true);
            _stuckMode = true;
            _sameFailStreak = 0;
            _lastProgressT = Time.time;
            _rescanRequested = true;
            if (_stuckCount >= 2)
            {
                NextHop(false, "stuck: " + why);
            }
        }
    }

    // ------------------------------------------------------------------ 選択肢

    private List<BotOption> BuildOptions(PlayerController player, BotSegment cur)
    {
        var list = new List<BotOption>();
        var pos = (Vector2)player.transform.position;
        var curCost = _world.Cost(cur);

        // 1) ゴールに直接触れられる
        if (cur != null && _goalKind != "none" && (_goalKind == "exit" || _goalKind == "transport" || _goalKind == "reach" || _goalKind == "pickup"))
        {
            var goalEdges = _world.Edges.Where(e => e.From == cur.Id && e.To < 0).ToList();
            if (goalEdges.Count > 0)
            {
                var goalEdge = goalEdges[0];
                var target = new BotTarget { Rect = _goalRect };
                BotAction action;
                if (_goalKind == "transport" || _goalKind == "pickup")
                    action = new BotAction { Kind = BotActionKind.Interact, WalkX = Mathf.Clamp(pos.x, _goalRect.xMin + 0.3f, _goalRect.xMax - 0.3f), Target = target };
                else if (goalEdge.Move == BotMove.Walk)
                    action = new BotAction { Kind = BotActionKind.WalkTo, WalkX = _goalRect.center.x, GoalAction = true, Target = target };
                else
                {
                    var plan = _physics.Plan(cur, pos, target);
                    action = new BotAction { Kind = BotActionKind.JumpTo, Plan = plan, Target = target, GoalAction = true };
                }

                var interactNeedsWalk = action.Kind == BotActionKind.Interact && goalEdge.Move != BotMove.Walk;
                if (!interactNeedsWalk)
                {
                    var key = _goalKind == "transport" ? "use_transport" : _goalKind == "pickup" ? "interact:pickup" : _goalKind == "reach" ? "go_goal" : "go_exit";
                    action.Key = key;
                    var ok = action.Plan == null || action.Plan.Valid;
                    if (!ok) MarkSimFail(cur.Id, -1);
                    list.Add(new BotOption
                    {
                        Key = key, Action = action, Score = ok ? -5f : 20f, PhysicsOk = ok, OnRoute = true,
                        Description = Desc($"{(_goalKind == "transport" ? "use the transport to" : "go into the exit to")} {(_goalNextScene ?? "the goal")} ({Where(pos, _goalRect.center)})",
                            "goal", ok ? (action.Plan == null ? "not needed" : "lands") : "misses", null, 0),
                    });
                }
            }
        }

        // 2) 隣の床へ (グラフの辺)
        if (cur != null)
        {
            var edges = _world.Edges.Where(e => e.From == cur.Id && e.To >= 0).ToList();
            var ranked = edges.Select(e => (e, after: e.Cost + _world.CostToGoal[e.To]))
                .OrderBy(t => t.after).ToList();
            var routeCount = 0;
            var explore = 0;
            foreach (var (e, after) in ranked)
            {
                var finite = !float.IsInfinity(after);
                var progress = finite && after <= curCost + 0.05f; // 最短の辺は after == curCost
                if (finite && routeCount >= 5) continue;
                if (!finite || !progress)
                {
                    if (explore >= (_stuckMode ? 4 : 2) || float.IsInfinity(curCost) == false && !_stuckMode && list.Count >= 5) continue;
                    explore++;
                }
                else routeCount++;

                var opt = SegmentOption(player, cur, e, after, progress, routeCount);
                if (opt != null) list.Add(opt);
            }
        }

        // 4) 敵・壊せる物
        foreach (var e in _world.Enemies.Concat(_world.Entities.Where(x => (x.Kind == "breakable" || x.Kind == "speaker") && x.Active)))
        {
            if (!BotExecutor.EntityAlive(e)) continue;
            var d = e.Rect.center - pos;
            // 近くの敵・物。目的の相手 (ボス・闘技場の敵) は同じ床の上なら遠くても出す
            var goalOnFloor = _goalEntity == e && cur != null && Mathf.Abs(d.x) < 30f && Mathf.Abs(d.y) < 2.5f;
            if (!goalOnFloor && (Mathf.Abs(d.x) > 7f || Mathf.Abs(d.y) > 3.5f)) continue;
            if (e.Kind == "breakable" && e.Label.StartsWith("needs_")) continue; // スキルが要る物は通常攻撃では壊れない
            var isGoal = _goalEntity == e;
            var blocks = Mathf.Abs(d.x) < 2.5f && Mathf.Abs(d.y) < 1.5f;
            var enemy = e.Source as EnemyController;
            var broken = enemy != null && enemy.IsBroken.CurrentValue;
            var score = isGoal ? -6f : blocks && !Settings.Invincible ? 0.5f : blocks ? 2f : 6f + Mathf.Abs(d.x);
            list.Add(new BotOption
            {
                Key = "attack:" + e.Id,
                Action = new BotAction { Key = "attack:" + e.Id, Kind = BotActionKind.Attack, Entity = e },
                Score = score, OnRoute = isGoal,
                Description = Desc($"attack {e.Kind} {e.Id}{(e.Kind == "boss" ? " (boss)" : "")} ({Where(pos, e.Rect.center)})",
                    isGoal ? "this is the current objective" : blocks ? "it is in the way" : "off the route", "melee range about 1.3", null, 0),
            });
            if (broken && Mathf.Abs(d.x) < 2.5f && Mathf.Abs(d.y) < 1.2f)
            {
                list.Add(new BotOption
                {
                    Key = "finisher:" + e.Id,
                    Action = new BotAction { Key = "finisher:" + e.Id, Kind = BotActionKind.Finisher, Entity = e },
                    Score = score - 1f,
                    Description = Desc($"finisher on {e.Id} (it is broken / stunned)", isGoal ? "this is the current objective" : "kills it fast", "in finisher range", null, 0),
                });
            }

            if (blocks && enemy != null && !Settings.Invincible)
            {
                list.Add(new BotOption
                {
                    Key = "dodge",
                    Action = new BotAction { Key = "dodge", Kind = BotActionKind.Dodge, WalkX = pos.x - Mathf.Sign(d.x) * 3f },
                    Score = 4f, Description = Desc("dash away from the nearest enemy (dash avoids contact damage)", "defensive", "n/a", null, 0),
                });
                list.Add(new BotOption
                {
                    Key = "retreat",
                    Action = new BotAction { Key = "retreat", Kind = BotActionKind.Retreat, WalkX = pos.x - Mathf.Sign(d.x) * 3f },
                    Score = 5f, Description = Desc("walk 3 units away from the nearest enemy", "defensive", "n/a", null, 0),
                });
            }
        }

        // 3) 床の端まで歩く (探索: 止まった時・ルートが無い時だけ。ほかの出口には入らない)
        var hasRoute = list.Any(o => o.OnRoute);
        if (cur != null && !cur.Moving && (_stuckMode || !hasRoute))
        {
            var left = AvoidOtherExits(cur.XMin + 0.3f, pos, cur);
            var right = AvoidOtherExits(cur.XMax - 0.3f, pos, cur);
            if (pos.x - left > 1.5f)
                list.Add(WalkOption("explore_left", left, pos, curCost));
            if (right - pos.x > 1.5f)
                list.Add(WalkOption("explore_right", right, pos, curCost));
        }
        else if (cur == null && player.IsGrounded)
        {
            // 床として認識できない所 (狭い足場・段の角など): 少しずつ動いて抜ける
            list.Add(WalkOption("explore_left", AvoidOtherExits(pos.x - 2f, pos, null), pos, curCost));
            list.Add(WalkOption("explore_right", AvoidOtherExits(pos.x + 2f, pos, null), pos, curCost));
        }

        // 5) 回復・レバー
        var hp = player.Health != null ? player.Health.Hp.CurrentValue : 10;
        var maxHp = player.Health != null ? player.Health.MaxHp : 10;
        if (hp <= maxHp / 2 && player.CanHeal() && player.IsGrounded)
        {
            list.Add(new BotOption
            {
                Key = "heal", Action = new BotAction { Key = "heal", Kind = BotActionKind.Heal },
                Score = Settings.Invincible ? 8f : 1f, Description = Desc($"heal (hp {hp}/{maxHp})", "safety", "must be grounded", null, 0),
            });
        }

        var interactor = player.GetComponent<PlayerInteractor>();
        var current = interactor != null ? interactor.Current.CurrentValue : null;
        if (current != null && current.CanInteract && list.All(o => o.Action.Kind != BotActionKind.Interact))
        {
            var label = (current as Component) != null ? ((Component)current).name : "object";
            list.Add(new BotOption
            {
                Key = "interact", Action = new BotAction { Key = "interact", Kind = BotActionKind.Interact, WalkX = pos.x },
                Score = current is LeverSwitch ? 3f : 12f,
                Description = Desc($"interact with {label} ({current.PromptText})", current is LeverSwitch ? "levers open the way" : "optional", "n/a", null, 0),
            });
        }

        // 6) 大ジャンプ (止まった時だけ)
        if (_stuckMode && player.HasSkill(PlayerSkill.SuperJump) && cur != null)
        {
            list.Add(new BotOption
            {
                Key = "use_ability:super_jump", Action = new BotAction { Key = "use_ability:super_jump", Kind = BotActionKind.SuperJump },
                Score = 9f, Description = Desc("charged super jump straight up (about 24 units)", "exploration", "not simulated", null, 0),
            });
        }

        if (_goalKind == "fight" && _goalEntity != null && (_goalEntity.Rect.center.y - pos.y > 2.5f || float.IsInfinity(curCost)))
        {
            // 飛んでいる・届かない敵: 近くに来るのを待って斬る
            list.Add(new BotOption
            {
                Key = "wait", Action = new BotAction { Kind = BotActionKind.Wait, WalkX = 0.8f }, Score = 2f, OnRoute = true,
                Description = Desc("wait here for the enemy to come within reach, then attack", "goal", "not needed", null, 0),
            });
        }

        if (_goalKind == "fight_wait")
        {
            list.Add(new BotOption
            {
                Key = "wait", Action = new BotAction { Kind = BotActionKind.Wait, WalkX = 0.6f }, Score = -1f, OnRoute = true,
                Description = Desc("wait for the next wave of enemies (the arena gates are closed)", "goal", "not needed", null, 0),
            });
        }

        // シミュレーションで着地できない選択肢は、止まっている時以外は出さない
        if (!_stuckMode && list.Any(o => o.PhysicsOk))
            list.RemoveAll(o => !o.PhysicsOk);

        foreach (var o in list) o.Action.Key = o.Key;
        // 同じキーは 1 つ。プログラムの推奨 (いちばん小さいスコア) に印を付ける
        var result = list.GroupBy(o => o.Key).Select(g => g.OrderBy(o => o.Score).First()).OrderBy(o => o.Score).Take(12).ToList();
        if (result.Count > 0) result[0].Description["planner_pick"] = true;
        return result;
    }

    /// <summary>歩いて x へ向かう途中に目的でない出口があれば、その手前で止める。</summary>
    private float AvoidOtherExits(float x, Vector2 pos, BotSegment cur)
    {
        var feet = cur != null ? cur.YAt(pos.x) : pos.y - _world.PivotToFeet;
        var halfW = _world.PlayerWidth * 0.5f + 0.35f; // 体の幅 (向きで中心が 0.2 ずれる分も含む)
        var band = new Rect(Mathf.Min(pos.x, x) - halfW, feet, Mathf.Abs(x - pos.x) + halfW * 2f, _world.PlayerHeight);
        foreach (var e in _world.Entities)
        {
            if (e.Kind != "exit" || e == _goalEntity || !e.Rect.Overlaps(band)) continue;
            // 進む向きにある出口だけ (入口のすぐ脇に出た時、背中側の出口で止まらない)
            if (x > pos.x ? e.Rect.center.x < pos.x : e.Rect.center.x > pos.x) continue;
            x = x > pos.x ? Mathf.Max(pos.x, Mathf.Min(x, e.Rect.xMin - halfW)) : Mathf.Min(pos.x, Mathf.Max(x, e.Rect.xMax + halfW));
        }

        return x;
    }

    private BotOption WalkOption(string key, float x, Vector2 pos, float curCost)
    {
        var failed = _recent.Count(r => r.StartsWith(key + " ") && r.Contains("fail"));
        return new BotOption
        {
            Key = key,
            Action = new BotAction { Key = key, Kind = BotActionKind.WalkTo, WalkX = x },
            Score = 40f + failed * 3f,
            Description = Desc($"explore: walk {(key.EndsWith("left") ? "left" : "right")} {Mathf.Abs(x - pos.x):F0} units to the end of this floor",
                "no", "not needed", null, failed),
        };
    }

    private BotOption SegmentOption(PlayerController player, BotSegment cur, BotEdge e, float after, bool progress, int rank)
    {
        var pos = (Vector2)player.transform.position;
        var b = _world.Segments[e.To];
        var target = new BotTarget { Segment = b };
        BotAction action;
        string key;
        var physics = "not needed";
        var ok = true;

        if (b.Moving)
        {
            if (cur.Moving) return null;
            var waitX = cur.ClampX(Mathf.Clamp(pos.x, b.TravelRange.xMin, b.TravelRange.xMax), 0.3f);
            key = "wait_for_platform:" + b.Name;
            action = new BotAction { Kind = BotActionKind.WaitForPlatform, Target = target, WalkX = waitX };
            physics = "checked while waiting";
        }
        else if (cur.Moving)
        {
            key = "ride_to:" + b.Name;
            action = new BotAction { Kind = BotActionKind.RideTo, Target = target };
            physics = "checked while riding";
        }
        else if (e.Move == BotMove.Ladder && e.Ladder != null)
        {
            key = "climb_ladder:" + b.Name;
            action = new BotAction { Kind = BotActionKind.Climb, Target = target, WalkX = e.Ladder.CenterX, Ladder = e.Ladder };
        }
        else if (e.Move == BotMove.Walk)
        {
            key = "walk_to:" + b.Name;
            var walkX = b.ClampX(pos.x, 0.5f);
            if (!Mathf.Approximately(AvoidOtherExits(walkX, pos, cur), walkX))
            {
                // 歩く途中で目的でない出口に入ってしまう
                MarkSimFail(cur.Id, b.Id);
                return null;
            }

            action = new BotAction { Kind = BotActionKind.WalkTo, Target = target, WalkX = walkX };
        }
        else
        {
            var plan = _physics.Plan(cur, pos, target);
            ok = plan.Valid;
            physics = ok ? "lands" : "misses";
            if (!ok) MarkSimFail(cur.Id, b.Id);
            key = plan.Valid && plan.DropThrough ? "drop_through:" + b.Name
                : plan.Valid && plan.Drop ? "drop_to:" + b.Name
                : plan.Valid && plan.Dash ? "dash_jump:" + b.Name
                : "jump_to_platform:" + b.Name;
            action = new BotAction { Kind = BotActionKind.JumpTo, Target = target, Plan = plan };
            if (!ok)
            {
                // 計画が無くても試せるように、近い端から全力で跳ぶ計画にしておく
                plan.TakeoffX = cur.ClampX(b.CenterX, 0.2f);
                plan.HoldTime = 10f;
                plan.Dash = Mathf.Abs(b.CenterX - plan.TakeoffX) > 5f;
            }
        }

        var failKey = $"{_world.SceneName}|{cur.CenterX:F0},{cur.YCenter:F0}|{b.CenterX:F0},{b.YCenter:F0}";
        _failures.TryGetValue(failKey, out var failed);
        var danger = DangerNear(b, out var dangerScore);
        var finite = !float.IsInfinity(after);
        // 経路が分からない時は、まだ行っていない・ゴールに近い床を優先する (探索)
        var visited = _visited.Contains(SegKey(b));
        _visitCount.TryGetValue(SegKey(b), out var visits);
        var score = (finite ? after : 30f + Vector2.Distance(new Vector2(b.CenterX, b.YCenter), _goalRect.center) * 0.3f + (visited ? 15f : 0f)) + (ok ? 0f : 25f) + failed * 6f + dangerScore + Mathf.Max(0, visits - 1) * 2f; // 何度も行った床は少し重く (行ったり来たりを防ぐ)
        var route = !finite ? "no" : progress ? (rank == 1 ? "best" : "alternative") : "no";
        var dy = b.YAt(b.ClampX(pos.x)) - cur.YAt(pos.x);
        return new BotOption
        {
            Key = key, Action = action, Score = score, PhysicsOk = ok, OnRoute = progress, Danger = danger,
            Description = Desc($"{Verb(key)} {b.Name} ({Where(new Vector2(pos.x, cur.YAt(pos.x)), new Vector2(b.ClampX(pos.x), b.YAt(b.ClampX(pos.x))))}{(b.AllOneWay ? ", thin platform" : "")}{(b.Moving ? ", moving" : "")})",
                route, physics, danger == "none" ? null : danger, failed),
        };
    }

    private static string Verb(string key) =>
        key.StartsWith("drop_through") ? "drop through this thin floor to" :
        key.StartsWith("drop_to") ? "walk off the edge down to" :
        key.StartsWith("dash_jump") ? "dash then jump to" :
        key.StartsWith("walk_to") ? "walk onto" :
        key.StartsWith("climb_ladder") ? "climb the ladder to" :
        key.StartsWith("wait_for_platform") ? "wait for and jump onto" :
        key.StartsWith("ride_to") ? "ride, then jump to" : "jump to";

    private string DangerNear(BotSegment b, out float score)
    {
        score = 0f;
        var notes = new List<string>();
        var area = new Rect(b.XMin - 1f, b.YMin - 0.5f, b.Width + 2f, 3f);
        foreach (var e in _world.Entities)
        {
            if (!e.Active || !area.Overlaps(e.Rect)) continue;
            if (e.Kind == "hazard")
            {
                notes.Add($"{e.Label} hazard there");
                score += Settings.Invincible ? 1f : 4f;
            }
            else if (e.Kind == "enemy" || e.Kind == "boss")
            {
                notes.Add($"enemy {e.Id} there");
                score += Settings.Invincible ? 0.3f : 2f;
            }
        }

        return notes.Count == 0 ? "none" : string.Join(", ", notes.Take(2));
    }

    private static Dictionary<string, object> Desc(string what, string route, string physics, string danger, int failed)
    {
        var d = new Dictionary<string, object> { ["what"] = what, ["on_route"] = route, ["simulation"] = physics };
        if (!string.IsNullOrEmpty(danger)) d["danger"] = danger;
        if (failed > 0) d["failed_before"] = failed;
        return d;
    }

    private static string Where(Vector2 from, Vector2 to)
    {
        var d = to - from;
        var parts = new List<string>();
        if (Mathf.Abs(d.x) >= 0.5f) parts.Add($"{Mathf.Abs(d.x):0} {(d.x > 0 ? "right" : "left")}");
        if (Mathf.Abs(d.y) >= 0.5f) parts.Add($"{Mathf.Abs(d.y):0} {(d.y > 0 ? "up" : "down")}");
        return parts.Count == 0 ? "here" : string.Join(", ", parts);
    }

    // ------------------------------------------------------------------ Jev の問い

    private Dictionary<string, object> BuildQuestions(PlayerController player)
    {
        var criteria = new Dictionary<string, object>();
        foreach (var o in _options) criteria[o.Key] = o.Description;

        var q = new Dictionary<string, object>
        {
            ["action"] = new Dictionary<string, object>
            {
                ["type"] = "choice",
                ["instructions"] = new Dictionary<string, object>
                {
                    ["question"] = "Pick the next action for a bot that is play-testing a 2D platformer. The bot must reach `goal`.",
                    ["how_to_decide"] = new List<object>
                    {
                        "1. Normally pick the option with `planner_pick` true: the game's planner computed it as the fastest way to the goal and checked it with a physics simulation.",
                        "2. Do not pick the planner_pick option if its `failed_before` is 2 or more, or if `simulation` says it misses; then pick another option whose `on_route` is best or alternative.",
                        "3. If an enemy has between_player_and_goal true, is within 3 units and `player.invincible` is false, attack it first.",
                        "4. If the goal is to defeat a boss or break an object, attacking that target is progress.",
                        "5. Pick an option whose `on_route` is no (explore) only when `progress.stuck` is true.",
                    },
                },
                ["criteria"] = criteria,
            },
        };

        var pos = (Vector2)player.transform.position;
        if (!Settings.Invincible && _world.Enemies.Any(e => (e.Rect.center - pos).sqrMagnitude < 25f))
        {
            q["danger"] = new Dictionary<string, object>
            {
                ["type"] = "noul",
                ["instructions"] = "Will an enemy in `nearby.enemies` probably hit the player within the next second if the player ignores it?",
            };
        }

        return q;
    }

    private Dictionary<string, object> BuildState(PlayerController player, BotSegment cur)
    {
        var pos = (Vector2)player.transform.position;
        var abilities = new List<object>();
        if (_world.CanDoubleJump) abilities.Add("double_jump");
        if (player.HasSkill(PlayerSkill.SuperJump)) abilities.Add("super_jump");
        if (player.HasSkill(PlayerSkill.GroundSlam)) abilities.Add("ground_slam");
        if (player.HasSkill(PlayerSkill.ChargeRush)) abilities.Add("charge_rush");

        var cost = _world.Cost(cur);
        var enemies = _world.Enemies.Where(e => BotExecutor.EntityAlive(e))
            .OrderBy(e => (e.Rect.center - pos).sqrMagnitude).Take(4)
            .Where(e => (e.Rect.center - pos).sqrMagnitude < 144f)
            .Select(e =>
            {
                var enemy = e.Source as EnemyController;
                var toGoal = Mathf.Sign(_goalRect.center.x - pos.x);
                var between = Mathf.Sign(e.Rect.center.x - pos.x) == toGoal && Mathf.Abs(e.Rect.center.x - pos.x) < Mathf.Abs(_goalRect.center.x - pos.x);
                return (object)new Dictionary<string, object>
                {
                    ["id"] = e.Id, ["kind"] = e.Kind, ["where"] = Where(pos, e.Rect.center),
                    ["hp"] = enemy != null ? enemy.Hp.CurrentValue : 0,
                    ["broken"] = enemy != null && enemy.IsBroken.CurrentValue,
                    ["between_player_and_goal"] = between,
                };
            }).ToList();
        var hazards = _world.Entities.Where(e => e.Kind == "hazard" && e.Active && (e.Rect.center - pos).sqrMagnitude < 100f)
            .OrderBy(e => (e.Rect.center - pos).sqrMagnitude).Take(3)
            .Select(e => (object)new Dictionary<string, object> { ["kind"] = e.Label, ["where"] = Where(pos, e.Rect.center) }).ToList();

        var hp = player.Health != null ? player.Health.Hp.CurrentValue : 0;
        var maxHp = player.Health != null ? player.Health.MaxHp : 0;
        return new Dictionary<string, object>
        {
            ["scene"] = _world.SceneName,
            ["goal"] = new Dictionary<string, object>
            {
                ["what"] = GoalDescription(),
                ["where"] = Where(pos, _goalRect.center),
                ["route_known"] = !float.IsInfinity(cost),
                ["estimated_seconds"] = float.IsInfinity(cost) ? -1f : cost,
            },
            ["player"] = new Dictionary<string, object>
            {
                ["standing_on"] = cur != null ? cur.Name : "unknown",
                ["hp"] = $"{hp}/{maxHp}",
                ["invincible"] = Settings.Invincible,
                ["abilities"] = abilities,
            },
            ["progress"] = new Dictionary<string, object>
            {
                ["seconds_without_progress"] = Mathf.Round(Time.time - _lastProgressT),
                ["stuck"] = _stuckMode,
                ["recent"] = _recent.Cast<object>().ToList(),
            },
            ["nearby"] = new Dictionary<string, object> { ["enemies"] = enemies, ["hazards"] = hazards },
        };
    }

    private string GoalDescription() => _goalKind switch
    {
        "exit" => $"leave through the exit to {_goalNextScene}",
        "transport" => $"use the transport (interact) to {_goalNextScene}",
        "boss" => "defeat the boss",
        "speaker" => "break the speaker",
        "pickup" => "pick up the item",
        "reach" => "reach the marked area",
        _ => "explore",
    };

    // ------------------------------------------------------------------ 死亡

    private void TickDead(PlayerController player)
    {
        if (!_deathLogged)
        {
            _deathLogged = true;
            _hop.Deaths++;
            Incident("death", $"死亡 (ゴール: {GoalText}, 直前の行動: {_lastActionKey})", player, true);
        }

        if (Time.realtimeSinceStartup - _deadRealT0 < 2.5f) return;

        if (_hop.Deaths >= Settings.MaxDeaths)
        {
            _forceWarp = true;
            NextHop(false, $"死亡 {_hop.Deaths} 回");
            ReloadAfterDeath();
            return;
        }

        ReloadAfterDeath();
        _forceWarp = true;
        _warpEntrance = _hopEntrance;
        _phase = Phase.Prepare;
        _phaseT0 = Time.realtimeSinceStartup;
    }

    private void ReloadAfterDeath()
    {
        GamePause.Reset();
        StageLoader.LoadWithPlayerScene(_hop.Scene);
        _prepareOnce = RestoreAbilities;
    }

    private void SnapshotAbilities(PlayerController player)
    {
        _upgradeSnapshot = Enum.GetValues(typeof(ScissorUpgrade)).Cast<ScissorUpgrade>().Where(TestPlayActions.HasUpgrade).ToList();
        _skillSnapshot = Enum.GetValues(typeof(PlayerSkill)).Cast<PlayerSkill>().Where(TestPlayActions.HasSkill).ToList();
    }

    private void RestoreAbilities()
    {
        foreach (var u in _upgradeSnapshot) TestPlayActions.SetUpgrade(u, true);
        foreach (var s in _skillSnapshot) TestPlayActions.SetSkill(s, true);
    }

    // ------------------------------------------------------------------ 記録

    private void Incident(string kind, string detail, PlayerController player, bool screenshot, BotDecision extra = null)
    {
        var inc = new BotIncident
        {
            Kind = kind, Scene = TestPlayActions.CurrentStageName,
            Position = player != null ? (Vector2)player.transform.position : Vector2.zero,
            Detail = detail,
        };
        inc.RecentDecisions.AddRange(_hopDecisions.Skip(Math.Max(0, _hopDecisions.Count - 5)));
        if (extra != null && !inc.RecentDecisions.Contains(extra)) inc.RecentDecisions.Add(extra);
        if (screenshot && Settings.Screenshots && _screenshots < Settings.MaxScreenshots)
        {
            _screenshots++;
            inc.Screenshot = Report.Screenshot($"{kind}_{inc.Scene}");
        }

        Report.Incidents.Add(inc);
        Debug.Log($"[TestPlayBot] {kind}: {inc.Scene} ({inc.Position.x:F1},{inc.Position.y:F1}) {detail}");
    }

    // ------------------------------------------------------------------ 遷移の完了

    private void LateUpdate()
    {
        if (!Running || !_transitionPending) return;
        if (TestPlayActions.IsLoading || TestPlayActions.Player == null) return;

        _transitionPending = false;
        var scene = TestPlayActions.CurrentStageName;
        var step = _steps[_index];
        if (scene == step.Next)
        {
            var entrance = _goalEntrance;
            NextHop(true, "→ " + scene);
            _hopEntrance = entrance;
            // 次の区間はこのシーンから (ワープしない)
            _forceWarp = _index < _steps.Count && (_steps[_index].Scene != scene || _steps[_index].Entrance != null && _steps[_index].ExitHint.HasValue);
        }
        else if (scene == step.Scene)
        {
            // 同じシーンの読み直し (死亡など)
            _phase = Phase.WaitLoad;
            _expectedScene = scene;
        }
        else
        {
            Incident("wrong_exit", $"目的と違う出口から {scene} へ出た (目的: {step.Next})", TestPlayActions.Player, true);
            _forceWarp = true;
            NextHop(false, "wrong exit → " + scene);
        }
    }
}
#endif
