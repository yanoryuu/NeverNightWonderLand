using TMPro;
using UnityEngine;

/// <summary>
/// HUD の裁断プロンプト。ブレイク中の敵がいる間表示され、
/// 発動可能 (範囲内・同じ高さ・向いている方向) なら「裁断!」を明るく点滅、
/// 範囲外なら「近づいて裁断!」を控えめに表示する。
/// ボタン表記は最後に使った入力デバイスに追従する。
/// </summary>
public class FinisherPromptView : MonoBehaviour
{
    // 点滅周期 (sec)
    private const float BlinkPeriod = 0.6f;

    [Tooltip("参照するプレイヤー (発動可否の判定用)")]
    [SerializeField] private PlayerController _player;

    [Tooltip("表示の親 (点滅と表示切替の対象)")]
    [SerializeField] private GameObject _root;

    [Tooltip("プロンプトのテキスト")]
    [SerializeField] private TMP_Text _label;

    private void Start()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private void Update()
    {
        if (_root == null)
            return;

        var anyBroken = false;
        foreach (var enemy in EnemyController.Active)
        {
            if (enemy != null && enemy.IsBroken.CurrentValue)
            {
                anyBroken = true;
                break;
            }
        }

        _root.SetActive(anyBroken);
        if (!anyBroken || _label == null)
            return;

        var canFinish = _player != null && _player.CanFinisher();
        var button = InputDeviceTracker.Current == InputDeviceKind.Gamepad ? "[R1]" : "[L]";
        _label.text = canFinish ? $"{button} 裁断!" : "近づいて裁断!";

        var c = _label.color;
        if (canFinish)
        {
            // 発動可能: 明るく点滅して目を引く
            var t = Mathf.PingPong(Time.time * 2f / BlinkPeriod, 1f);
            c.a = Mathf.Lerp(0.4f, 1f, t);
        }
        else
        {
            // 範囲外: 控えめに表示
            c.a = 0.45f;
        }

        _label.color = c;
    }
}
