using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// インタラクトで別ステージへ運ぶ移動装置 (ジップライン・非常用リフト・搬出エレベーター)。
/// 実行すると StageLoader.TransitionTo で _sceneName の _entranceId の入り口へ遷移する。
/// _requiredFlags が全て立つまでは使えず、インタラクトすると _lockedMessage を通知する。
/// _activateFlag を設定すると初回使用時にそのフラグを立てる (「リフトを起動した」の永続化。
/// 反対側の装置の _requiredFlags に同じフラグを入れると、以後は双方向に使える)。
/// 配置: SpriteRenderer (任意) + トリガー Collider2D を Interactable レイヤー (11) に置く。
/// </summary>
public class TransportInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("遷移先のシーン名 (Build Settings に登録されていること)")]
    [SerializeField] private string _sceneName = "";

    [Tooltip("遷移先ステージの入り口 (PlayerSpawnPoint) の Id。空ならデフォルト入口")]
    [SerializeField] private string _entranceId = "";

    [Tooltip("使うのに必要な進行フラグ (全て立っている必要がある)。空なら常に使える")]
    [SerializeField] private string[] _requiredFlags = new string[0];

    [Tooltip("使えない時にインタラクトした時の通知")]
    [SerializeField] private string _lockedMessage = "動かない。どこかで動力を入れる必要がありそうだ";

    [Tooltip("初回使用時に立てる進行フラグ (任意)。例: 装置の起動")]
    [SerializeField] private string _activateFlag = "";

    [Tooltip("初回使用時 (_activateFlag を立てた時) の通知 (任意)")]
    [SerializeField] private string _activateMessage = "";

    [Tooltip("プロンプトに表示するテキスト")]
    [SerializeField] private string _promptText = "乗る";

    [Tooltip("使えない時のプロンプト (空なら _promptText と同じ)")]
    [SerializeField] private string _lockedPromptText = "調べる";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    /// <summary>必要フラグが全て立っていて使えるか。</summary>
    public bool IsUnlocked
    {
        get
        {
            if (_requiredFlags == null)
                return true;

            foreach (var flag in _requiredFlags)
            {
                if (!string.IsNullOrEmpty(flag) && !GameProgress.Has(flag))
                    return false;
            }

            return true;
        }
    }

    public string PromptText =>
        !IsUnlocked && !string.IsNullOrEmpty(_lockedPromptText) ? _lockedPromptText : _promptText;

    public bool CanInteract => !string.IsNullOrEmpty(_sceneName);
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        if (!IsUnlocked)
        {
            Notifier.Notify(_lockedMessage);
            return;
        }

        if (!string.IsNullOrEmpty(_activateFlag) && !GameProgress.Has(_activateFlag))
        {
            GameProgress.Set(_activateFlag);
            Notifier.Notify(_activateMessage);
        }

        GamePause.Reset();

        if (StageLoader.Instance != null)
            StageLoader.Instance.TransitionTo(_sceneName, _entranceId);
        else
            SceneManager.LoadScene(_sceneName); // 旧方式 (PlayerScene を使わないテストシーン)
    }
}
