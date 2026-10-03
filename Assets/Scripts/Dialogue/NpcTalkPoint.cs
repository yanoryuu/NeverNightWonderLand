using UnityEngine;

/// <summary>
/// インタラクト (E キー) で Utage の会話シナリオを再生する NPC・看板など。
/// <see cref="DialogueService"/> 経由で DialogueScene を起動する。
/// 任意: _talkedFlag を設定すると「話したことがある」が永続化され (GameProgress)、2回目以降は
/// _repeatLabel (空なら _scenarioLabel) を再生する。_rewardThread を設定すると初回の会話の終わりに糸を渡す
/// (_talkedFlag があれば一度きり)。未設定なら従来どおり毎回 _scenarioLabel を再生するだけ。
/// </summary>
public class NpcTalkPoint : MonoBehaviour, IInteractable
{
    [Tooltip("再生するシナリオラベル (先頭の * は不要)")]
    [SerializeField] private string _scenarioLabel = "";

    [Tooltip("2回目以降に再生するシナリオラベル (任意, _talkedFlag が必要)。空なら毎回 _scenarioLabel")]
    [SerializeField] private string _repeatLabel = "";

    [Tooltip("話したことの永続化フラグ (GameProgress, 任意)。例: A1_Npc_Doll")]
    [SerializeField] private string _talkedFlag = "";

    [Tooltip("初回の会話の終わりに渡す糸の数 (0 なら無し)")]
    [SerializeField] private int _rewardThread;

    [Tooltip("プロンプトに表示するテキスト")]
    [SerializeField] private string _promptText = "話す";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    private bool _rewardGiven; // _talkedFlag なしの時の、このシーン内での一度きり判定

    public string PromptText => _promptText;
    // 会話の再生中・ロード中・閉じた直後 (連打で再び開くのを防ぐ) は話しかけられない
    public bool CanInteract => DialogueService.CanStartFromInteract && !string.IsNullOrEmpty(_scenarioLabel);
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        var talked = !string.IsNullOrEmpty(_talkedFlag) && GameProgress.Has(_talkedFlag);
        var label = talked && !string.IsNullOrEmpty(_repeatLabel) ? _repeatLabel : _scenarioLabel;
        if (talked || (_rewardThread <= 0 && string.IsNullOrEmpty(_talkedFlag)))
        {
            DialogueService.Play(label);
            return;
        }

        DialogueService.Play(label, () => OnFirstTalkFinished(interactor));
    }

    private void OnFirstTalkFinished(GameObject interactor)
    {
        if (_rewardThread > 0 && !_rewardGiven && interactor != null
            && interactor.TryGetComponent<PlayerItemInventory>(out var inventory))
        {
            inventory.AddThread(_rewardThread); // 通知は AddThread が出す
            _rewardGiven = true;
        }

        if (!string.IsNullOrEmpty(_talkedFlag))
            GameProgress.Set(_talkedFlag);
    }
}
