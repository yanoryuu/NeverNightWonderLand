using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 裁断プロンプトの Presenter (MonoBehaviour 非依存)。
/// ブレイク中の敵がいる間 View を表示し、発動可能 (範囲内・同じ高さ・向いている方向) なら
/// 「裁断!」を明るく点滅、範囲外なら「近づいて裁断!」を控えめに表示する。
/// ボタン表記は最後に使った入力デバイスに追従する。
/// プレイヤーへの参照は PlayerRuntime (GameLifetimeScope) 経由で解決する。
/// </summary>
public sealed class FinisherPromptPresenter : IStartable, ITickable
{
    // 点滅周期 (sec)
    private const float BlinkPeriod = 0.6f;

    private readonly PlayerRuntime _playerRuntime;
    private readonly IFinisherPromptView _view;

    public FinisherPromptPresenter(PlayerRuntime playerRuntime, IFinisherPromptView view)
    {
        _playerRuntime = playerRuntime;
        _view = view;
    }

    public void Start()
    {
        _view.SetVisible(false);
    }

    public void Tick()
    {
        var anyBroken = false;
        foreach (var enemy in EnemyController.Active)
        {
            if (enemy != null && enemy.IsBroken.CurrentValue)
            {
                anyBroken = true;
                break;
            }
        }

        _view.SetVisible(anyBroken);
        if (!anyBroken)
            return;

        var player = _playerRuntime.Current.CurrentValue;
        var canFinish = player != null && player.CanFinisher();
        var button = InputDeviceTracker.Current == InputDeviceKind.Gamepad ? "[R1]" : "[L]";

        float alpha;
        if (canFinish)
        {
            // 発動可能: 明るく点滅して目を引く
            var t = Mathf.PingPong(Time.time * 2f / BlinkPeriod, 1f);
            alpha = Mathf.Lerp(0.4f, 1f, t);
        }
        else
        {
            // 範囲外: 控えめに表示
            alpha = 0.45f;
        }

        _view.SetPrompt(canFinish ? $"{button} 裁断!" : "近づいて裁断!", alpha);
    }
}
