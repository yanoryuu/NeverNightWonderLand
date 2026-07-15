using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>最後に入力があったデバイスの種類。</summary>
public enum InputDeviceKind
{
    Keyboard,
    Gamepad,
}

/// <summary>
/// 最後に使われた入力デバイスを追跡する。チュートリアルの操作説明や
/// 入力プロンプトのボタン表記切替 (キーボード⇔パッド) に使う。
/// 毎フレーム <see cref="Poll"/> を呼ぶこと (PlayerController / MenuPanelView が呼ぶ。
/// 同一フレームに複数回呼ばれても安全)。
/// </summary>
public static class InputDeviceTracker
{
    public static InputDeviceKind Current { get; private set; } = InputDeviceKind.Keyboard;

    /// <summary>デバイスが切り替わった時に発火する。</summary>
    public static event Action<InputDeviceKind> OnChanged;

    public static void Poll()
    {
        var detected = Detect();
        if (detected == null || detected.Value == Current)
            return;

        Current = detected.Value;
        OnChanged?.Invoke(Current);
    }

    private static InputDeviceKind? Detect()
    {
        // ゲームパッド優先で判定 (同時入力時はパッドを勝たせる)
        var gamepad = Gamepad.current;
        if (gamepad != null && IsGamepadActive(gamepad))
            return InputDeviceKind.Gamepad;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return InputDeviceKind.Keyboard;

        return null;
    }

    private static bool IsGamepadActive(Gamepad gamepad)
    {
        // スティック (ノイズ対策にデッドゾーンを大きめに)
        if (gamepad.leftStick.ReadValue().magnitude > 0.35f)
            return true;

        return gamepad.buttonSouth.wasPressedThisFrame
               || gamepad.buttonNorth.wasPressedThisFrame
               || gamepad.buttonEast.wasPressedThisFrame
               || gamepad.buttonWest.wasPressedThisFrame
               || gamepad.leftShoulder.wasPressedThisFrame
               || gamepad.rightShoulder.wasPressedThisFrame
               || gamepad.leftTrigger.wasPressedThisFrame
               || gamepad.rightTrigger.wasPressedThisFrame
               || gamepad.startButton.wasPressedThisFrame
               || gamepad.selectButton.wasPressedThisFrame
               || gamepad.dpad.up.wasPressedThisFrame
               || gamepad.dpad.down.wasPressedThisFrame
               || gamepad.dpad.left.wasPressedThisFrame
               || gamepad.dpad.right.wasPressedThisFrame;
    }
}
