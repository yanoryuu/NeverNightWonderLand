#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

/// <summary>仮想入力のボタン (VirtualPlayerInput 用)。</summary>
[Flags]
public enum PlayerButton
{
    None = 0,
    Jump = 1 << 0,
    Dash = 1 << 1,
    Attack = 1 << 2,
    Special = 1 << 3,
    Finisher = 1 << 4,
    Heal = 1 << 5,
    Interact = 1 << 6,
    Grapple = 1 << 7,   // 糸移動 (青ハサミ)
    Pause = 1 << 8,
    ItemMenu = 1 << 9,  // アイテム画面 (Tab / Select)
}

/// <summary>
/// プログラムから操作する仮想入力 (自動テストプレイ・テスト用)。
/// <code>
/// var input = new VirtualPlayerInput();
/// PlayerInputSource.Override = input;   // 以後キーボード/パッドの代わりにこれを読む
/// input.Move = 1f;                       // 右へ歩き続ける
/// input.Tap(PlayerButton.Jump);          // 次のフレームに 1 回だけ押す (押して離す)
/// input.Hold(PlayerButton.Jump);         // 押しっぱなし (押した瞬間のフレームは Pressed も立つ)
/// input.Release(PlayerButton.Jump);
/// PlayerInputSource.Override = null;     // 実デバイスへ戻す
/// </code>
/// Tap は「次に入力が読まれるフレーム」の 1 フレームだけ有効 (ポーズ中で PlayerController が読まなかった場合は消える = 実機と同じ)。
/// MergeDevice = true にすると実デバイスの入力も重ねる (人が横から割り込める)。
/// </summary>
public class VirtualPlayerInput : IPlayerInputSource, IMenuInputSource
{
    /// <summary>-1..1 の水平入力。</summary>
    public float Move;

    /// <summary>-1..1 の垂直入力 (上 = +1)。</summary>
    public float Vertical;

    /// <summary>-1..1 のカメラの上下見回し (右スティックの縦, 上 = +1)。</summary>
    public float Look;

    /// <summary>true なら実デバイスの入力も重ねる。</summary>
    public bool MergeDevice;

    private PlayerButton _held;
    private PlayerButton _heldAtLastFrame;
    private PlayerButton _taps;
    private ItemSlot? _itemTap;
    private int _menuMoveTap;
    private int _menuMoveXTap;
    private int _menuTabTap;
    private bool _menuSubmitTap;
    private bool _menuCancelTap;

    private int _lastReadFrame = -1;
    private PlayerInputFrame _lastFrame;

    /// <summary>押しっぱなしにする。</summary>
    public void Hold(PlayerButton buttons) => _held |= buttons;

    /// <summary>離す。</summary>
    public void Release(PlayerButton buttons) => _held &= ~buttons;

    /// <summary>押しっぱなしかどうかを設定する。</summary>
    public void SetHeld(PlayerButton buttons, bool held)
    {
        if (held) Hold(buttons);
        else Release(buttons);
    }

    public bool IsHeld(PlayerButton button) => (_held & button) == button;

    /// <summary>次のフレームに 1 回押して離す。</summary>
    public void Tap(PlayerButton buttons) => _taps |= buttons;

    /// <summary>アイテムスロットを使う (L1/[I] ホールド + 方向 と同じ)。</summary>
    public void UseItem(ItemSlot slot) => _itemTap = slot;

    /// <summary>メニュー操作 (-1 = 上, +1 = 下)。</summary>
    public void MenuMove(int direction) => _menuMoveTap = Math.Sign(direction);

    /// <summary>メニューの左右 (-1 = 左, +1 = 右)。</summary>
    public void MenuMoveX(int direction) => _menuMoveXTap = Math.Sign(direction);

    /// <summary>タブ切替 (-1 = Q / L1, +1 = E / R1)。</summary>
    public void MenuTab(int direction) => _menuTabTap = Math.Sign(direction);

    public void MenuSubmit() => _menuSubmitTap = true;
    public void MenuCancel() => _menuCancelTap = true;

    /// <summary>すべて離す・軸を 0 に戻す。</summary>
    public void ReleaseAll()
    {
        Move = 0f;
        Vertical = 0f;
        Look = 0f;
        _held = PlayerButton.None;
        _taps = PlayerButton.None;
        _itemTap = null;
        _menuMoveTap = 0;
        _menuMoveXTap = 0;
        _menuTabTap = 0;
        _menuSubmitTap = false;
        _menuCancelTap = false;
    }

    public PlayerInputFrame Read()
    {
        var frameCount = Time.frameCount;
        if (frameCount == _lastReadFrame)
            return _lastFrame;

        // タップは新しいフレームで最初に読まれた時に 1 回だけ渡す (そのフレーム内は _lastFrame を返す)
        var taps = _taps;
        var itemTap = _itemTap;
        _taps = PlayerButton.None;
        _itemTap = null;

        var held = _held;
        var pressed = (held & ~_heldAtLastFrame) | taps;
        _heldAtLastFrame = held;

        var f = new PlayerInputFrame
        {
            Move = Mathf.Clamp(Move, -1f, 1f),
            Vertical = Mathf.Clamp(Vertical, -1f, 1f),
            Look = Mathf.Clamp(Look, -1f, 1f),
            JumpHeld = (held & PlayerButton.Jump) != 0,
            DashHeld = (held & PlayerButton.Dash) != 0,
            JumpPressed = (pressed & PlayerButton.Jump) != 0,
            DashPressed = (pressed & PlayerButton.Dash) != 0,
            AttackPressed = (pressed & PlayerButton.Attack) != 0,
            SpecialPressed = (pressed & PlayerButton.Special) != 0,
            FinisherPressed = (pressed & PlayerButton.Finisher) != 0,
            HealPressed = (pressed & PlayerButton.Heal) != 0,
            InteractPressed = (pressed & PlayerButton.Interact) != 0,
            GrapplePressed = (pressed & PlayerButton.Grapple) != 0,
            PausePressed = (pressed & PlayerButton.Pause) != 0,
            ItemMenuPressed = (pressed & PlayerButton.ItemMenu) != 0,
            ItemSlot = itemTap,
        };

        if (MergeDevice)
            f = Merge(f, PlayerInputSource.ReadDevice());

        _lastReadFrame = frameCount;
        _lastFrame = f;
        return f;
    }

    public MenuInput.Frame ReadMenu()
    {
        var frame = new MenuInput.Frame(_menuMoveTap, _menuSubmitTap, _menuCancelTap, _menuMoveXTap, _menuTabTap);
        _menuMoveTap = 0;
        _menuMoveXTap = 0;
        _menuTabTap = 0;
        _menuSubmitTap = false;
        _menuCancelTap = false;
        return frame;
    }

    /// <summary>2 つの入力を重ねる (軸は絶対値の大きい方、ボタンは OR)。</summary>
    public static PlayerInputFrame Merge(PlayerInputFrame a, PlayerInputFrame b)
    {
        return new PlayerInputFrame
        {
            Move = Mathf.Abs(b.Move) > Mathf.Abs(a.Move) ? b.Move : a.Move,
            Vertical = Mathf.Abs(b.Vertical) > Mathf.Abs(a.Vertical) ? b.Vertical : a.Vertical,
            Look = Mathf.Abs(b.Look) > Mathf.Abs(a.Look) ? b.Look : a.Look,
            JumpHeld = a.JumpHeld || b.JumpHeld,
            JumpPressed = a.JumpPressed || b.JumpPressed,
            DashHeld = a.DashHeld || b.DashHeld,
            DashPressed = a.DashPressed || b.DashPressed,
            AttackPressed = a.AttackPressed || b.AttackPressed,
            SpecialPressed = a.SpecialPressed || b.SpecialPressed,
            FinisherPressed = a.FinisherPressed || b.FinisherPressed,
            HealPressed = a.HealPressed || b.HealPressed,
            InteractPressed = a.InteractPressed || b.InteractPressed,
            GrapplePressed = a.GrapplePressed || b.GrapplePressed,
            PausePressed = a.PausePressed || b.PausePressed,
            ItemMenuPressed = a.ItemMenuPressed || b.ItemMenuPressed,
            ItemSlot = a.ItemSlot ?? b.ItemSlot,
        };
    }
}
#endif
