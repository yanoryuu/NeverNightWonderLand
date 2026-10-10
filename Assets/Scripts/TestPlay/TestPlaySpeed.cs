#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// テストプレイの速度調整 (手動テスト・自動テストプレイ共通。エディタ/Development Build のみ)。
///
/// - 倍率 (0.1〜8) は GamePause の「ポーズしていない時の timeScale」(BaseTimeScale) として設定する。
///   ポーズメニュー等の GamePause.Push/Pop はそのまま効き、閉じると選んだ倍率に戻る。
/// - 一時停止 (Frozen) は GamePause.Push を 1 つ積む = メニューを開いた時と同じ止まり方 (プレイヤーも入力を読まない)。
/// - コマ送り (Step) は 1 フレームだけ止めを外し、そのフレームのゲーム時間をちょうど固定ステップ 1 回分
///   (Time.captureDeltaTime) にして、終わったら再び止める。
/// - 物理: 既定では Time.fixedDeltaTime を変えない (= 何倍速でも 1 ステップの計算は等倍と同一。高速時はフレームあたりの
///   ステップ数が増えるだけで安定)。SmoothSlowMotion を有効にすると 1 倍未満の時だけ fixedDeltaTime を倍率に比例して
///   縮め、スローでもカクつかない (代わりに積分の刻みが変わるので挙動はわずかに等倍と異なる)。
/// - Time.maximumDeltaTime (プロジェクト設定 0.333) は変えない。8 倍で描画が約 24fps を下回ると、1 フレームの
///   ゲーム時間が上限で切られて実効倍率が下がる (物理の暴走よりも遅くなる方を選ぶ)。
/// </summary>
public static class TestPlaySpeed
{
    public const float Min = 0.1f;
    public const float Max = 8f;

    /// <summary>オーバーレイ・ホットキーの段階。</summary>
    public static readonly float[] Presets = { 0.1f, 0.25f, 0.5f, 1f, 2f, 4f, 8f };

    private static float _scale = 1f;
    private static bool _frozen;
    private static bool _smoothSlowMotion;
    private static float _baseFixedDeltaTime = -1f;
    private static int _stepFramesLeft;   // コマ送りで残っているフレーム数
    private static int _stepReleaseFrame = -1;
    private static bool _stepPendingRelease;  // 次の LateTick で止めを外す

    /// <summary>現在の倍率 (一時停止中も「再開後の倍率」を返す)。</summary>
    public static float Scale => _scale;

    /// <summary>テストプレイの一時停止中か (ポーズメニューとは別)。</summary>
    public static bool Frozen => _frozen;

    /// <summary>コマ送り中か。</summary>
    public static bool Stepping => _stepFramesLeft > 0;

    /// <summary>プロジェクト設定の固定ステップ (秒)。</summary>
    public static float BaseFixedDeltaTime
    {
        get
        {
            if (_baseFixedDeltaTime <= 0f)
                _baseFixedDeltaTime = Time.fixedDeltaTime;
            return _baseFixedDeltaTime;
        }
    }

    /// <summary>1 倍未満の時に物理の刻みも縮めて滑らかにする (既定 false = 物理は常に等倍と同一)。</summary>
    public static bool SmoothSlowMotion
    {
        get => _smoothSlowMotion;
        set
        {
            _smoothSlowMotion = value;
            ApplyFixedDeltaTime();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _scale = 1f;
        _frozen = false;
        _smoothSlowMotion = false;
        _baseFixedDeltaTime = -1f;
        _stepFramesLeft = 0;
        _stepReleaseFrame = -1;
        _stepPendingRelease = false;
        GamePause.SetBaseTimeScale(1f);
    }

    /// <summary>倍率を設定する (0.1〜8 に丸める)。</summary>
    public static void SetScale(float scale)
    {
        _ = BaseFixedDeltaTime; // 変更前の値を覚えておく
        _scale = Mathf.Clamp(scale, Min, Max);
        // 入力の再生中は timeScale を 1 に固定し、速度は再生側が実時間の待ちで表す (TestPlayRecorder.LateTick)
        if (!TestPlayRecorder.IsFeeding)
            GamePause.SetBaseTimeScale(_scale);
        ApplyFixedDeltaTime();
    }

    /// <summary>段階を 1 つ上げる / 下げる。</summary>
    public static void StepPreset(int direction)
    {
        var index = NearestPresetIndex(_scale);
        index = Mathf.Clamp(index + (direction > 0 ? 1 : -1), 0, Presets.Length - 1);
        SetScale(Presets[index]);
    }

    public static void Faster() => StepPreset(+1);
    public static void Slower() => StepPreset(-1);
    public static void ResetScale() => SetScale(1f);

    /// <summary>一時停止 / 再開。</summary>
    public static void SetFrozen(bool frozen)
    {
        if (_frozen == frozen)
            return;

        var wasStepping = _stepFramesLeft > 0 && !_stepPendingRelease;
        _frozen = frozen;
        _stepFramesLeft = 0;
        _stepPendingRelease = false;
        if (wasStepping && !TestPlayRecorder.IsReplaying)
            Time.captureDeltaTime = 0f;
        if (frozen) GamePause.Push();
        else if (!wasStepping) GamePause.Pop(); // コマ送り中は止めを外してあるので下ろさない
        TestPlayRunner.Ensure();
    }

    public static void ToggleFrozen() => SetFrozen(!_frozen);

    /// <summary>
    /// コマ送り: 一時停止中に frames フレームだけ進めて再び止める (1 フレーム = 固定ステップ 1 回分のゲーム時間)。
    /// 一時停止していなければ一時停止する。
    /// </summary>
    public static void Step(int frames = 1)
    {
        if (!_frozen)
        {
            SetFrozen(true);
            return;
        }

        if (frames <= 0 || _stepFramesLeft > 0)
            return;

        // 止めを外すのは LateUpdate で行う (Update の途中で外すと、残りの Update が deltaTime 0 で 1 回余計に動く)
        _stepFramesLeft = frames;
        _stepPendingRelease = true;
        TestPlayRunner.Ensure();
    }

    /// <summary>TestPlayRunner の LateUpdate から毎フレーム呼ばれる。</summary>
    internal static void LateTick()
    {
        // シーン開始時の GamePause.Reset などで止めが外れたら積み直す
        if (_frozen && _stepFramesLeft == 0 && !GamePause.IsPaused)
            GamePause.Push();

        if (_stepPendingRelease)
        {
            _stepPendingRelease = false;
            _stepReleaseFrame = Time.frameCount;
            GamePause.Pop();
            ApplyStepDeltaTime();
            return;
        }

        if (_stepFramesLeft <= 0 || Time.frameCount == _stepReleaseFrame)
            return;

        // 止めを外したフレームの次から 1 フレーム進むごとに数える
        _stepFramesLeft--;
        if (_stepFramesLeft > 0)
        {
            ApplyStepDeltaTime();
            return;
        }

        if (!TestPlayRecorder.IsReplaying)
            Time.captureDeltaTime = 0f;
        if (_frozen)
            GamePause.Push();
    }

    private static void ApplyStepDeltaTime()
    {
        if (TestPlayRecorder.IsReplaying)
            return; // 再生中は記録のフレーム時間で 1 フレームずつ進む

        // deltaTime = captureDeltaTime × timeScale なので、ちょうど固定ステップ 1 回分になるよう割り戻す
        var timeScale = Mathf.Max(GamePause.BaseTimeScale, 0.01f);
        Time.captureDeltaTime = Time.fixedDeltaTime / timeScale;
    }

    private static void ApplyFixedDeltaTime()
    {
        if (TestPlayRecorder.IsReplaying)
            return; // 再生中は記録の値を使う

        var baseStep = BaseFixedDeltaTime;
        Time.fixedDeltaTime = _smoothSlowMotion && _scale < 1f
            ? Mathf.Max(baseStep * _scale, 0.002f)
            : baseStep;
    }

    /// <summary>再生終了時などに固定ステップを倍率に合わせて戻す。</summary>
    internal static void RestoreFixedDeltaTime() => ApplyFixedDeltaTime();

    private static int NearestPresetIndex(float scale)
    {
        var best = 0;
        for (var i = 1; i < Presets.Length; i++)
        {
            if (Mathf.Abs(Mathf.Log(Presets[i] / scale)) < Mathf.Abs(Mathf.Log(Presets[best] / scale)))
                best = i;
        }

        return best;
    }
}
#endif
