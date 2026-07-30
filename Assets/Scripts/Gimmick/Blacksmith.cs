using UnityEngine;

/// <summary>
/// 鍛冶師 NPC。インタラクトで会話し、対応する色のハサミ強化を授ける。
/// 黄=カブトムシ、青=クモ、赤=トビムシ (企画書)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Blacksmith : MonoBehaviour, IInteractable
{
    [Tooltip("授けるハサミ強化の色")]
    [SerializeField] private ScissorUpgrade _upgrade = ScissorUpgrade.Yellow;

    [Tooltip("鍛冶師の名前 (会話表示用)")]
    [SerializeField] private string _smithName = "カブトムシの鍛冶師";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("これらの進行フラグ (GameProgress) が全て立つまで強化を渡さない。空なら常時渡す")]
    [SerializeField] private string[] _requiredFlags = new string[0];

    [Tooltip("解禁前の台詞")]
    [SerializeField] private string _lockedMessage = "「今は渡せるものがない」";

    [Header("鍛冶強化 (能力とは別の有償強化)")]
    [Tooltip("この鍛冶師の鍛冶強化ID (1人1回まで)。空なら有償強化は行わない。例: CarouselHall")]
    [SerializeField] private string _forgeId = "";

    [Tooltip("鍛冶強化に必要な糸の数")]
    [SerializeField] private int _forgeCost = 30;

    public string PromptText => "話す";
    public bool CanInteract => true;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    private string ForgeFlag => $"Forge_{_forgeId}";

    public void Interact(GameObject interactor)
    {
        var progression = interactor.GetComponent<PlayerProgression>();
        if (progression == null)
            return;

        // 進行条件 (中ボス全撃破など) を全て満たすまでは渡さない
        if (!progression.Has(_upgrade) && _requiredFlags != null)
        {
            foreach (var flag in _requiredFlags)
            {
                if (string.IsNullOrEmpty(flag) || GameProgress.Has(flag))
                    continue;

                Notifier.Notify($"{_smithName}{_lockedMessage}");
                return;
            }
        }

        // 1回目の会話: 能力の強化 (ハサミの色)
        if (!progression.Has(_upgrade))
        {
            Notifier.Notify($"{_smithName}「いいハサミだ。強化してやろう」");
            progression.Grant(_upgrade);
            return;
        }

        // 2回目以降: 鍛冶強化 (糸を払って攻撃力+コンボ数、1人1回)
        if (TryForge(interactor, progression))
            return;

        Notifier.Notify($"{_smithName}「{_upgrade.DisplayName()}の調子はどうだ?」");
    }

    /// <summary>
    /// 有償の鍛冶強化。糸 (_forgeCost) を消費して攻撃力と最大コンボ数を1段階伸ばす。
    /// この鍛冶師で強化済みなら false (通常会話へ)。
    /// </summary>
    private bool TryForge(GameObject interactor, PlayerProgression progression)
    {
        if (string.IsNullOrEmpty(_forgeId) || GameProgress.Has(ForgeFlag))
            return false;

        var inventory = interactor.GetComponent<PlayerItemInventory>();
        if (inventory == null)
            return false;

        if (!inventory.TrySpendThread(_forgeCost))
        {
            Notifier.Notify($"{_smithName}「刃を研ぎ直してやれるが、糸が{_forgeCost}いる」");
            return true;
        }

        GameProgress.Set(ForgeFlag);
        progression.AddForgeLevel();
        Notifier.Notify($"{_smithName}「研ぎ上げたぞ」攻撃力と最大コンボ数が上がった! (糸 -{_forgeCost})");
        return true;
    }
}
