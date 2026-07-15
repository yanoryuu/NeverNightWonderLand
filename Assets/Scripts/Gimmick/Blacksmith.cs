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

    public string PromptText => "話す";
    public bool CanInteract => true;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        var progression = interactor.GetComponent<PlayerProgression>();
        if (progression == null)
            return;

        if (progression.Has(_upgrade))
        {
            Notifier.Notify($"{_smithName}「{_upgrade.DisplayName()}の調子はどうだ?」");
            return;
        }

        Notifier.Notify($"{_smithName}「いいハサミだ。強化してやろう」");
        progression.Grant(_upgrade);
    }
}
