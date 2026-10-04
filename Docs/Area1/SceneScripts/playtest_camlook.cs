// 2026-10-04 PendingChanges 37 (右スティックでカメラを上下に見回す) の再生中の確認。Play 中に run_script で呼ぶ。
//   CamLook.Stick(y) : テスト用パッドを足して右スティックの縦を y にし、ReadDevice の Look (デッドゾーン・割り当て直し) を返す。
//                      エディタが非フォーカスだと Input System がデバイスの状態を戻し、実パッドが current を取り返すため、
//                      カメラの動きの確認には Look(y) を使う
//   CamLook.Look(y)  : 仮想入力 (VirtualPlayerInput.Look, ボットと同じ経路, 実デバイスも重ねる) の見回しを y (-1..1) にする
//   CamLook.State    : カメラとプレイヤーの y・見回しのずらし量・下限・見回しできる状態か
//   CamLook.Menu(open): メニューが開いている扱いにする/戻す (MenuPanelView の開閉カウント) — 見回しが戻るか
//   CamLook.MinY(y)  : いまのステージの CameraLimit の下限を一時的に y にする (再生中だけ。下限で見下ろしても下がらないか)
//   CamLook.Track / TrackResult : 空中にいたフレームの見回しのずらし量 (最小/最大) を数える (ジャンプ中も見回せるか)
//   CamLook.Off      : 仮想入力とテスト用パッドを外す
// シーン移動・テレポート・スクショは playtest_boxfix.cs (PlayBox.NewGame / Go / Tp / Shot)。
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CamLook
{
    const string PadName = "CamLookTestPad";
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Stick(float y)
    {
        Application.runInBackground = true;
        var pad = InputSystem.devices.OfType<Gamepad>().FirstOrDefault(d => d.name == PadName)
                  ?? InputSystem.AddDevice<Gamepad>(PadName);
        InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = new Vector2(0f, y) });
        InputSystem.Update();
        pad.MakeCurrent();
        return $"stick {y:F2} → ReadDevice().Look={PlayerInputSource.ReadDevice().Look:F2}";
    }

    public static string Look(float y)
    {
        Application.runInBackground = true;
        if (PlayerInputSource.Override is not VirtualPlayerInput v)
        {
            v = new VirtualPlayerInput { MergeDevice = true };
            PlayerInputSource.Override = v;
        }

        v.Look = y;
        return $"look {y:F2}";
    }

    public static string State()
    {
        var follow = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (follow == null || player == null) return "no camera/player";
        var look = (float)typeof(CameraFollow).GetField("_lookOffset", F).GetValue(follow);
        var cam = follow.transform.position.y;
        var py = player.transform.position.y;
        return $"camY={cam:F2} playerY={py:F2} cam-player={cam - py:F2} lookOffset={look:F2} minY={CameraLimit.OverrideMinY?.ToString("F2") ?? "-"} " +
               $"canLook={typeof(CameraFollow).GetMethod("CanLook", F).Invoke(follow, null)} input.Look={PlayerInputSource.Current.Look:F2} " +
               $"grounded={player.IsGrounded} paused={GamePause.IsPaused} menu={MenuPanelView.AnyOpen} dialogue={DialogueService.IsBusy}";
    }

    public static string Off()
    {
        PlayerInputSource.Override = null;
        var pad = InputSystem.devices.OfType<Gamepad>().FirstOrDefault(d => d.name == PadName);
        if (pad != null) InputSystem.RemoveDevice(pad);
        return "off";
    }

    public static string Menu(bool open)
    {
        if (open) MenuPanelView.NotifyOpened();
        else MenuPanelView.NotifyClosed();
        return "menu open=" + MenuPanelView.AnyOpen;
    }

    public static string MinY(float y)
    {
        var limit = UnityEngine.Object.FindAnyObjectByType<CameraLimit>();
        if (limit == null) return "no CameraLimit";
        typeof(CameraLimit).GetField("_minY", F).SetValue(limit, y);
        return "minY=" + CameraLimit.OverrideMinY;
    }

    public static string Track()
    {
        AppDomain.CurrentDomain.SetData("camlook_track", "");
        System.Action<PlayerInputFrame> h = _ =>
        {
            var follow = UnityEngine.Object.FindAnyObjectByType<CameraFollow>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (follow == null || player == null || player.IsGrounded) return;
            var look = (float)typeof(CameraFollow).GetField("_lookOffset", F).GetValue(follow);
            var cur = (string)AppDomain.CurrentDomain.GetData("camlook_track");
            AppDomain.CurrentDomain.SetData("camlook_track", cur + $"{look:F2},");
        };
        AppDomain.CurrentDomain.SetData("camlook_track_h", h);
        PlayerInputSource.ConsumedByPlayer += h;
        return "tracking";
    }

    public static string TrackResult()
    {
        if (AppDomain.CurrentDomain.GetData("camlook_track_h") is System.Action<PlayerInputFrame> h)
            PlayerInputSource.ConsumedByPlayer -= h;
        AppDomain.CurrentDomain.SetData("camlook_track_h", null);
        var v = ((string)AppDomain.CurrentDomain.GetData("camlook_track") ?? "").Split(',', System.StringSplitOptions.RemoveEmptyEntries)
            .Select(float.Parse).ToList();
        return v.Count == 0 ? "no airborne frames" : $"airborne frames={v.Count} lookOffset min={v.Min():F2} max={v.Max():F2}";
    }
}
