using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// テストシーン用のステータス常時表示。プレイヤーの HP・回復メモリ・糸・スタイル・
/// 現在ステート・ハサミ強化・装備アイテムを毎フレーム表示する。
/// </summary>
public class DebugStatusView : MonoBehaviour
{
    [Tooltip("参照するプレイヤー")]
    [SerializeField] private PlayerController _player;

    [Tooltip("表示テキスト")]
    [SerializeField] private TMP_Text _label;

    private readonly StringBuilder _sb = new();

    private void Update()
    {
        if (_player == null || _label == null)
            return;

        _sb.Clear();

        var health = _player.Health;
        var gauge = _player.HealGauge;
        var inventory = _player.Inventory;
        var progression = _player.Progression;

        if (health != null)
            _sb.Append($"HP {health.Hp.CurrentValue}/{health.MaxHp}");
        if (gauge != null)
            _sb.Append($"  メモリ {gauge.Pips.CurrentValue} (+{gauge.Charge.CurrentValue:P0})");
        if (inventory != null)
            _sb.Append($"  糸 {inventory.Thread.CurrentValue}");
        _sb.AppendLine();

        var style = _player.Style == ScissorStyle.DualBlades ? "二刀流" : "両手持ち";
        _sb.AppendLine($"スタイル: {style}  ステート: {_player.CurrentStateName}");

        if (progression != null)
        {
            _sb.AppendLine(
                $"強化: 黄{Mark(progression.Has(ScissorUpgrade.Yellow))} " +
                $"青{Mark(progression.Has(ScissorUpgrade.Blue))} " +
                $"赤{Mark(progression.Has(ScissorUpgrade.Red))}");
        }

        if (inventory != null)
        {
            _sb.Append("スロット:");
            for (var i = 0; i < ItemSlotExtensions.SlotCount; i++)
            {
                var slot = (ItemSlot)i;
                var item = inventory.GetSlotItem(slot);
                _sb.Append($" {slot.Glyph()}{(item != null ? $"{item.DisplayName}x{inventory.GetCount(item)}" : "---")}");
            }
        }

        _sb.Append($"  敵: {EnemyController.Active.Count}体");

        _label.text = _sb.ToString();
    }

    private static string Mark(bool has) => has ? "○" : "×";
}
