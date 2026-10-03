using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// テストプレイ基盤 (TestPlaySpeed / VirtualPlayerInput / TestPlayRecorder / TestPlayActions) の動作確認。
// プレイ中に run_script で呼ぶ (Docs/TestPlay/README.md「検証の手順」)。値の受け渡しは PlayerPrefs。
public static class PlayTP
{
    public static string Status()
    {
        Application.runInBackground = true;
        return $"{TestPlayActions.GetPlayerState()} | speed={TestPlaySpeed.Scale} frozen={TestPlaySpeed.Frozen} " +
               $"timeScale={Time.timeScale} paused={GamePause.IsPaused} fdt={Time.fixedDeltaTime} cap={Time.captureDeltaTime} " +
               $"override={(PlayerInputSource.Override == null ? "device" : PlayerInputSource.Override.GetType().Name)} " +
               $"rec='{TestPlayRecorder.Status}'";
    }

    public static string Speed(string s)
    {
        TestPlaySpeed.SetScale(float.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
        return Status();
    }

    // 速度の実測: Mark → (実時間で数秒待つ) → Rate
    public static string Mark()
    {
        PlayerPrefs.SetFloat("tp_t0", Time.time);
        PlayerPrefs.SetFloat("tp_r0", Time.realtimeSinceStartup);
        PlayerPrefs.SetInt("tp_f0", Time.frameCount);
        return "mark";
    }

    public static string Rate()
    {
        var game = Time.time - PlayerPrefs.GetFloat("tp_t0");
        var real = Time.realtimeSinceStartup - PlayerPrefs.GetFloat("tp_r0");
        var frames = Time.frameCount - PlayerPrefs.GetInt("tp_f0");
        return $"game {game:F2}s / real {real:F2}s = {game / real:F2}x ({frames} frames, {frames / real:F0} fps) | {Status()}";
    }

    // ポーズメニューを仮想入力で開閉する (Esc 相当 / メニューの戻る)
    private static VirtualPlayerInput _menuInput;

    public static string PauseOpen()
    {
        _menuInput = new VirtualPlayerInput { MergeDevice = true };
        PlayerInputSource.Override = _menuInput;
        _menuInput.Tap(PlayerButton.Pause);
        return "tap pause";
    }

    public static string PauseClose()
    {
        _menuInput ??= new VirtualPlayerInput { MergeDevice = true };
        PlayerInputSource.Override = _menuInput;
        _menuInput.MenuCancel();
        return "menu cancel";
    }

    public static string DeviceInput()
    {
        PlayerInputSource.Override = null;
        return Status();
    }

    // 実デバイス経路の確認: キーボードの D を押しっぱなし / 離す (QueueStateEvent)
    public static string KeyD(string down)
    {
        var kb = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        var state = down == "1" ? new KeyboardState(Key.D) : new KeyboardState();
        InputSystem.QueueStateEvent(kb, state);
        return Status();
    }

    // 実デバイス経路の確認 (毎フレーム D を流す。エディタ非フォーカスでは 1 回だけでは押しっぱなしにならない)
    public static string DeviceWalk()
    {
        PlayerInputSource.Override = null;
        var go = new GameObject("DeviceWalk");
        go.AddComponent<DeviceWalkSeq>();
        return "walk " + Status();
    }

    public static string DeviceWalkLog() => PlayerPrefs.GetString("tp_walk", "(none)");

    // 仮想入力の台本 (記録の検証用): 記録の取り込みが始まってからのフレーム数で操作を決める
    private sealed class Scripted : IPlayerInputSource
    {
        private readonly VirtualPlayerInput _v = new();
        private int _start = -1;

        public PlayerInputFrame Read()
        {
            if (!TestPlayRecorder.IsCapturing)
            {
                _v.ReleaseAll();
                return _v.Read();
            }

            if (_start < 0)
                _start = Time.frameCount;
            var t = (Time.frameCount - _start) % 600; // 約 10 秒周期 (60fps 時)
            _v.Move = t < 150 ? 1f : t < 300 ? -1f : t < 420 ? 1f : 0f;
            _v.SetHeld(PlayerButton.Jump, (t >= 40 && t < 70) || (t >= 200 && t < 215) || (t >= 330 && t < 380));
            if (t == 100 || t == 260 || t == 450) _v.Tap(PlayerButton.Attack);
            if (t == 120 || t == 400) _v.Tap(PlayerButton.Dash);
            if (t == 500) _v.Tap(PlayerButton.Special);
            return _v.Read();
        }
    }

    public static string RecordStart()
    {
        PlayerInputSource.Override = new Scripted();
        return TestPlayRecorder.StartRecording(true) ? "recording (reload)" : "failed";
    }

    public static string RecordStop()
    {
        var path = TestPlayRecorder.StopRecording();
        PlayerInputSource.Override = null;
        var rec = path != null ? TestPlayRecorder.Load(path) : null;
        return $"saved {path} frames={rec?.frames.Count} dur={rec?.Duration:F2} checkpoints={rec?.checkpoints.Count}";
    }

    public static string Replay()
    {
        return TestPlayRecorder.ReplayLast() ? "replay started" : "failed";
    }

    public static string Report()
    {
        var r = TestPlayRecorder.LastReport;
        if (r == null) return "no report";
        return $"{r}\n" + string.Join("\n", r.lines.Take(40));
    }

    // オーバーレイ・ワープ・フラグ・能力
    public static string Overlay(string on)
    {
        TestPlayOverlay.Visible = on == "1";
        return "overlay " + TestPlayOverlay.Visible;
    }

    public static string Spawns(string scene) => string.Join(", ", TestPlayActions.SpawnIds(scene).Select(s => s == "" ? "(default)" : s));

    public static string Warp(string scene, string id) => TestPlayActions.Warp(scene, id == "-" ? null : id) + " | " + Status();

    public static string Abilities()
    {
        TestPlayActions.SetAllAbilities(true);
        var a = string.Join(",", System.Enum.GetValues(typeof(ScissorUpgrade)).Cast<ScissorUpgrade>().Select(u => $"{u}={TestPlayActions.HasUpgrade(u)}"));
        var b = string.Join(",", System.Enum.GetValues(typeof(PlayerSkill)).Cast<PlayerSkill>().Select(u => $"{u}={TestPlayActions.HasSkill(u)}"));
        TestPlayActions.SetAllAbilities(false);
        var c = TestPlayActions.HasUpgrade(ScissorUpgrade.Red) || TestPlayActions.HasSkill(PlayerSkill.GroundSlam);
        return $"granted: {a} {b} | after revoke any={c}";
    }

    public static string Presets()
    {
        var log = "";
        foreach (var p in TestPlayActions.FlagPresets)
        {
            TestPlayActions.ApplyPreset(p);
            log += $"{p.Name}: {p.Flags.All(GameProgress.Has)} red={TestPlayActions.HasUpgrade(ScissorUpgrade.Red)}\n";
        }

        TestPlayActions.ClearArea1Flags();
        TestPlayActions.SetUpgrade(ScissorUpgrade.Red, false);
        log += $"cleared: A1 flags left={GameProgress.Collect().Count(f => f.StartsWith("A1_"))}";
        return log;
    }

    public static string Cheats()
    {
        TestPlayActions.Invincible = true;
        var hp0 = TestPlayActions.GetPlayerState().Hp;
        var p = TestPlayActions.Player;
        p.Health.TakeDamage(new DamageInfo(3, 0, p.transform.position + Vector3.right, null));
        var hp1 = TestPlayActions.GetPlayerState().Hp;
        TestPlayActions.Invincible = false;
        var th0 = TestPlayActions.GetPlayerState().Thread;
        TestPlayActions.AddThread(100);
        var th1 = TestPlayActions.GetPlayerState().Thread;
        return $"invincible hp {hp0}->{hp1} | thread {th0}->{th1}";
    }
}

public class DeviceWalkSeq : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        var kb = Keyboard.current;
        var p = TestPlayActions.Player;
        var x0 = p.transform.position.x;
        var t = 0f;
        while (t < 1f)
        {
            InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D));
            t += Time.deltaTime;
            yield return null;
        }

        InputSystem.QueueStateEvent(kb, new KeyboardState());
        PlayerPrefs.SetString("tp_walk", $"walked {p.transform.position.x - x0:F2} in {t:F2}s game time (move={p.MoveInput})");
        Destroy(gameObject);
    }
}
