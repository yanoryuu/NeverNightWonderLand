using UnityEngine.InputSystem;
using Utage;
using Utage.InputSystem;

/// <summary>
/// 宴 (Utage) の入力の差し替え (宴の公式の口 <see cref="InputUtil.SetInputStrategy"/>)。
/// 宴の既定 (Input System 版: Enter で文字送り、マウスクリックは UI 側で処理) に加えて、
/// 会話の再生中 (<see cref="DialogueService.IsPlaying"/>) はゲームの 決定・攻撃・インタラクト・ジャンプ で文字送り/改ページする:
/// キーボード = Enter / テンキー Enter / Space / J / E、パッド = South (×/A) / West (□/X) / インタラクト (上入力)。
/// テストプレイの仮想入力 (PlayerInputSource.Override) の Attack / Interact / Jump のタップも同じ扱い。
/// 会話中以外は宴の既定のまま (ゲームのボタンを宴が拾わない)。
/// </summary>
public class DialogueInputStrategy : InputSystemStrategy
{
    private static readonly DialogueInputStrategy Instance = new();

    /// <summary>宴の入力処理をこのクラスにする (宴の初期化 (BeforeSceneLoad) より後に呼ぶ: UtageDialogueScene が会話のたびに呼ぶ)。</summary>
    public static void Install()
    {
        InputUtil.SetInputStrategy(Instance);
    }

    public override bool IsInputNextButton()
    {
        if (base.IsInputNextButton())
            return true;

        if (!DialogueService.IsPlaying)
            return false;

        return IsAdvancePressed();
    }

    /// <summary>このフレームに「会話を進める」入力があったか (宴の Enter とマウスクリック以外)。</summary>
    public static bool IsAdvancePressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.numpadEnterKey.wasPressedThisFrame)
            return true;

        // ゲームの入力のハブ: Space/J/E と パッド South/West/上入力 (テストプレイの仮想入力を含む)
        var frame = PlayerInputSource.Current;
        if (frame.JumpPressed || frame.AttackPressed || frame.InteractPressed)
            return true;

        // 仮想入力が差し込まれていても実デバイスでは送れるように
        var gamepad = Gamepad.current;
        if (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame))
            return true;

        return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame
                                    || keyboard.jKey.wasPressedThisFrame
                                    || keyboard.eKey.wasPressedThisFrame);
    }
}
