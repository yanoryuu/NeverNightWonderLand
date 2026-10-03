using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー入力の 1 フレーム分 (PlayerController が毎フレーム読む値)。
/// Pressed = そのフレームに押された、Held = 押しっぱなし。
/// </summary>
public struct PlayerInputFrame
{
    /// <summary>-1..1 の水平入力。</summary>
    public float Move;

    /// <summary>-1..1 の垂直入力 (上 = +1。グラップルの狙い・落下攻撃・溜めスキルの方向)。</summary>
    public float Vertical;

    public bool JumpHeld;
    public bool JumpPressed;
    public bool DashHeld;
    public bool DashPressed;
    public bool AttackPressed;
    public bool SpecialPressed;
    public bool FinisherPressed;
    public bool HealPressed;
    public bool InteractPressed;

    /// <summary>糸移動 (青ハサミのグラップル)。</summary>
    public bool GrapplePressed;

    /// <summary>ポーズメニューを開く (Esc / Start)。PauseMenuView が読む。</summary>
    public bool PausePressed;

    /// <summary>アイテム画面を開く/閉じる (Tab / Select(View))。ItemMenuView が読む。</summary>
    public bool ItemMenuPressed;

    /// <summary>アイテム使用 (L1/[I] ホールド + 方向) で押されたスロット。無ければ null。</summary>
    public ItemSlot? ItemSlot;
}

/// <summary>
/// プレイヤー入力の差し替え口 (テストプレイの自動操作・入力の記録/再生用)。
/// 既定 (Override = null) では <see cref="PlayerInputSource.ReadDevice"/> がキーボード/パッドを直接読む。
/// </summary>
public interface IPlayerInputSource
{
    /// <summary>このフレームの入力を返す (同一フレームに複数回呼ばれうる。結果はハブがフレーム単位でキャッシュする)。</summary>
    PlayerInputFrame Read();
}

/// <summary>
/// プレイヤー入力のハブ。PlayerController / PauseMenuView はここから入力を読む。
/// 通常はキーボード/ゲームパッドの直読み (従来の PlayerController.ReadInput と同じ割り当て)。
/// エディタと Development Build に限り、<see cref="Override"/> に仮想入力を差し込める
/// (Docs/TestPlay/README.md)。
/// </summary>
public static class PlayerInputSource
{
    // 慣性なしの移動に合わせ、スティックはデッドゾーンを超えたら最大入力として扱う
    private const float StickDeadZone = 0.3f;

    private static int _cachedFrame = -1;
    private static PlayerInputFrame _cached;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static IPlayerInputSource _override;

    /// <summary>
    /// 入力の差し替え (null = 実デバイス)。テストプレイ専用 (リリースビルドには存在しない)。
    /// 差し替えは次に読まれるフレームから有効。
    /// </summary>
    public static IPlayerInputSource Override
    {
        get => _override;
        set
        {
            _override = value;
            _cachedFrame = -1;
        }
    }

    /// <summary>
    /// PlayerController がそのフレームの入力を消費した直後に呼ばれる (記録・再生のフレーム送り用)。
    /// ポーズ中など PlayerController が入力を読まないフレームでは呼ばれない。
    /// </summary>
    public static event Action<PlayerInputFrame> ConsumedByPlayer;
#endif

    /// <summary>このフレームの入力 (フレーム内では同じ値を返す)。</summary>
    public static PlayerInputFrame Current
    {
        get
        {
            if (_cachedFrame != Time.frameCount)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _cached = _override != null ? _override.Read() : ReadDevice();
#else
                _cached = ReadDevice();
#endif
                _cachedFrame = Time.frameCount;
            }

            return _cached;
        }
    }

    /// <summary>PlayerController 用: このフレームの入力を読み、消費を通知する。</summary>
    public static PlayerInputFrame ReadForPlayer()
    {
        var frame = Current;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ConsumedByPlayer?.Invoke(frame);
#endif
        return frame;
    }

    /// <summary>キーボード/ゲームパッドを直接読む (従来の PlayerController.ReadInput の割り当てそのまま)。</summary>
    public static PlayerInputFrame ReadDevice()
    {
        var f = new PlayerInputFrame();

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            // [I] ホールド中は方向キーをアイテム選択に使う (移動・回復には反映しない)
            var itemHeld = keyboard.iKey.isPressed;
            if (itemHeld)
            {
                if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                    f.ItemSlot = global::ItemSlot.Down;
                else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                    f.ItemSlot = global::ItemSlot.Left;
                else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                    f.ItemSlot = global::ItemSlot.Right;
            }
            else
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) f.Move -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) f.Move += 1f;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) f.Vertical -= 1f;
                f.HealPressed = keyboard.sKey.wasPressedThisFrame;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) f.Vertical += 1f;

            f.JumpHeld = keyboard.spaceKey.isPressed;
            f.JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            f.DashHeld = keyboard.leftShiftKey.isPressed;

            // キーボード: Shift = ダッシュ, J = 近接攻撃, K = 特殊攻撃, L = 裁断,
            //             S = 回復, E = インタラクト, I ホールド+方向 = アイテム使用, F = 糸移動 (青), Esc = ポーズ, Tab = アイテム画面
            f.DashPressed = keyboard.leftShiftKey.wasPressedThisFrame;
            f.AttackPressed = keyboard.jKey.wasPressedThisFrame;
            f.SpecialPressed = keyboard.kKey.wasPressedThisFrame;
            f.FinisherPressed = keyboard.lKey.wasPressedThisFrame;
            f.InteractPressed = keyboard.eKey.wasPressedThisFrame;
            f.GrapplePressed = keyboard.fKey.wasPressedThisFrame;
            f.PausePressed = keyboard.escapeKey.wasPressedThisFrame;
            f.ItemMenuPressed = keyboard.tabKey.wasPressedThisFrame;
        }

        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            // L1 ホールド中は十字キーをアイテム選択に使う (移動・インタラクトには反映しない)。
            // スティックでの移動は L1 ホールド中も有効
            var itemHeld = gamepad.leftShoulder.isPressed;

            var stickX = gamepad.leftStick.x.ReadValue();
            if (stickX < -StickDeadZone || (!itemHeld && gamepad.dpad.left.isPressed)) f.Move = -1f;
            else if (stickX > StickDeadZone || (!itemHeld && gamepad.dpad.right.isPressed)) f.Move = 1f;

            var stickY = gamepad.leftStick.y.ReadValue();
            if (stickY > StickDeadZone || (!itemHeld && gamepad.dpad.up.isPressed)) f.Vertical = 1f;
            else if (stickY < -StickDeadZone || (!itemHeld && gamepad.dpad.down.isPressed)) f.Vertical = -1f;

            if (itemHeld)
            {
                if (gamepad.dpad.down.wasPressedThisFrame) f.ItemSlot = global::ItemSlot.Down;
                else if (gamepad.dpad.left.wasPressedThisFrame) f.ItemSlot = global::ItemSlot.Left;
                else if (gamepad.dpad.right.wasPressedThisFrame) f.ItemSlot = global::ItemSlot.Right;
            }

            // ホロウナイト準拠の配置:
            // ジャンプ = ×(A), 近接攻撃 = □(X), 特殊攻撃 = △(Y), 回復 = ○(B/フォーカス枠),
            // ダッシュ = R2, 裁断 = R1(クイックキャスト枠), 糸移動 = L2(スーパーダッシュ枠),
            // アイテム = L1 ホールド+十字, インタラクト = 上入力, ポーズ = Start
            f.JumpHeld |= gamepad.buttonSouth.isPressed;
            f.JumpPressed |= gamepad.buttonSouth.wasPressedThisFrame;
            f.DashPressed |= gamepad.rightTrigger.wasPressedThisFrame;
            f.DashHeld |= gamepad.rightTrigger.isPressed;
            f.AttackPressed |= gamepad.buttonWest.wasPressedThisFrame;
            f.SpecialPressed |= gamepad.buttonNorth.wasPressedThisFrame;
            f.FinisherPressed |= gamepad.rightShoulder.wasPressedThisFrame;
            f.HealPressed |= gamepad.buttonEast.wasPressedThisFrame;
            f.GrapplePressed |= gamepad.leftTrigger.wasPressedThisFrame;
            f.PausePressed |= gamepad.startButton.wasPressedThisFrame;
            f.ItemMenuPressed |= gamepad.selectButton.wasPressedThisFrame;

            // インタラクト = 上入力 (十字キー上 / 左スティック上) — ホロウナイトと同じ
            f.InteractPressed |= (!itemHeld && gamepad.dpad.up.wasPressedThisFrame)
                                 || gamepad.leftStick.up.wasPressedThisFrame;
        }

        f.Move = Mathf.Clamp(f.Move, -1f, 1f);
        return f;
    }
}
