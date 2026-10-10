using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 全体マップの入力の直読み (<see cref="MenuInput"/> と同じ方式。Time.timeScale = 0 中でも動く)。
/// 開閉 (M / タッチパッド / R3) は <see cref="PlayerInputSource"/> の MapPressed で読む。
/// - スクロール: WASD・矢印キー / 左スティック・十字キー (押している間)
/// - ズーム: Q = 縮小, E = 拡大 / L1 = 縮小, R1 = 拡大
/// - 現在地に戻る: Space・Enter / ×(A)
/// - 閉じる: Esc / ○(B)
/// </summary>
public static class MapInput
{
    private const float StickDeadZone = 0.2f;

    public readonly struct Frame
    {
        /// <summary>スクロール (-1..1。右・上が +)。</summary>
        public readonly Vector2 Scroll;

        /// <summary>ズーム: -1 = 縮小, +1 = 拡大, 0 = なし。</summary>
        public readonly int Zoom;

        public readonly bool Recenter;
        public readonly bool Close;

        public Frame(Vector2 scroll, int zoom, bool recenter, bool close)
        {
            Scroll = scroll;
            Zoom = zoom;
            Recenter = recenter;
            Close = close;
        }
    }

    public static Frame Read()
    {
        InputDeviceTracker.Poll();

        var scroll = Vector2.zero;
        var zoom = 0;
        var recenter = false;
        var close = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) scroll.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) scroll.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) scroll.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) scroll.y += 1f;
            if (keyboard.qKey.wasPressedThisFrame) zoom -= 1;
            if (keyboard.eKey.wasPressedThisFrame) zoom += 1;
            recenter |= keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                                                              || keyboard.numpadEnterKey.wasPressedThisFrame;
            close |= keyboard.escapeKey.wasPressedThisFrame;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            var stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude > StickDeadZone)
                scroll += stick;
            if (gamepad.dpad.left.isPressed) scroll.x -= 1f;
            if (gamepad.dpad.right.isPressed) scroll.x += 1f;
            if (gamepad.dpad.down.isPressed) scroll.y -= 1f;
            if (gamepad.dpad.up.isPressed) scroll.y += 1f;
            if (gamepad.leftShoulder.wasPressedThisFrame) zoom -= 1;
            if (gamepad.rightShoulder.wasPressedThisFrame) zoom += 1;
            recenter |= gamepad.buttonSouth.wasPressedThisFrame;
            close |= gamepad.buttonEast.wasPressedThisFrame;
        }

        return new Frame(Vector2.ClampMagnitude(scroll, 1f), System.Math.Sign(zoom), recenter, close);
    }
}
