using UnityEngine;

/// <summary>
/// インタラクト (E キー) で Utage の会話シナリオを再生する NPC・看板など。
/// <see cref="DialogueService"/> 経由で DialogueScene を起動する。
/// </summary>
public class NpcTalkPoint : MonoBehaviour, IInteractable
{
    [Tooltip("再生するシナリオラベル (先頭の * は不要)")]
    [SerializeField] private string _scenarioLabel = "";

    [Tooltip("プロンプトに表示するテキスト")]
    [SerializeField] private string _promptText = "話す";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    public string PromptText => _promptText;
    public bool CanInteract => !DialogueService.IsPlaying && !string.IsNullOrEmpty(_scenarioLabel);
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        DialogueService.Play(_scenarioLabel);
    }
}
