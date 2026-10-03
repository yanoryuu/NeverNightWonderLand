using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// メニュー共通の入力の直読み (MenuPanelView / ImageMenuView / SaveSlotView が共用)。
/// W/S・↑↓・十字キー = 上下、A/D・←→・十字キー = 左右 (左右に並ぶ画面だけが使う)、
/// Q/E・L1/R1 = タブ切替 (アイテム画面)、Enter/Space/J・Aボタン = 決定、Esc・Bボタン = 戻る。
/// Time.timeScale = 0 中でも動作する (ポーリングのため)。
/// </summary>
public static class MenuInput
{
    public readonly struct Frame
    {
        /// <summary>-1 = 上, +1 = 下, 0 = なし。</summary>
        public readonly int Move;
        public readonly bool Submit;
        public readonly bool Cancel;

        /// <summary>-1 = 左, +1 = 右, 0 = なし。</summary>
        public readonly int MoveX;

        /// <summary>タブ切替: -1 = 前 (Q / L1), +1 = 次 (E / R1), 0 = なし。</summary>
        public readonly int Tab;

        public Frame(int move, bool submit, bool cancel)
            : this(move, submit, cancel, 0, 0)
        {
        }

        public Frame(int move, bool submit, bool cancel, int moveX, int tab)
        {
            Move = move;
            Submit = submit;
            Cancel = cancel;
            MoveX = moveX;
            Tab = tab;
        }
    }

    /// <summary>このフレームのメニュー入力を読む (デバイス切替の追跡も行う)。</summary>
    public static Frame Read()
    {
        // プレイヤー不在のシーン (タイトル等) でもデバイス切替を追跡できるようにする
        InputDeviceTracker.Poll();

        var move = 0;
        var moveX = 0;
        var tab = 0;
        var submit = false;
        var cancel = false;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) move -= 1;
            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) move += 1;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) moveX -= 1;
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) moveX += 1;
            if (keyboard.qKey.wasPressedThisFrame) tab -= 1;
            if (keyboard.eKey.wasPressedThisFrame) tab += 1;
            submit |= keyboard.enterKey.wasPressedThisFrame
                      || keyboard.numpadEnterKey.wasPressedThisFrame
                      || keyboard.spaceKey.wasPressedThisFrame
                      || keyboard.jKey.wasPressedThisFrame;
            cancel |= keyboard.escapeKey.wasPressedThisFrame;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame) move -= 1;
            if (gamepad.dpad.down.wasPressedThisFrame) move += 1;
            if (gamepad.dpad.left.wasPressedThisFrame) moveX -= 1;
            if (gamepad.dpad.right.wasPressedThisFrame) moveX += 1;
            if (gamepad.leftShoulder.wasPressedThisFrame) tab -= 1;
            if (gamepad.rightShoulder.wasPressedThisFrame) tab += 1;
            submit |= gamepad.buttonSouth.wasPressedThisFrame;
            cancel |= gamepad.buttonEast.wasPressedThisFrame;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // テストプレイの仮想入力 (PlayerInputSource.Override がメニュー入力も持つ場合) を重ねる
        if (PlayerInputSource.Override is IMenuInputSource virtualMenu)
        {
            var v = virtualMenu.ReadMenu();
            move += v.Move;
            moveX += v.MoveX;
            tab += v.Tab;
            submit |= v.Submit;
            cancel |= v.Cancel;
        }
#endif

        return new Frame(move, submit, cancel, Math.Sign(moveX), Math.Sign(tab));
    }

    /// <summary>「何かボタン」: キーボードの任意キー・パッドの任意ボタン・マウスクリックが押されたか。</summary>
    public static bool AnyButtonPressed()
    {
        InputDeviceTracker.Poll();

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        var mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            return true;

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            foreach (var control in gamepad.allControls)
            {
                if (control is ButtonControl button && !control.synthetic && button.wasPressedThisFrame)
                    return true;
            }
        }

        return false;
    }
}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>テストプレイ用: メニュー入力も差し込める仮想入力 (VirtualPlayerInput が実装)。</summary>
public interface IMenuInputSource
{
    MenuInput.Frame ReadMenu();
}
#endif
