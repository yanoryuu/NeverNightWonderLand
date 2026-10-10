using UnityEngine;

/// <summary>
/// 短いセリフを順に表示する NPC (迷子の人形・地図売りなど)。Utage のシナリオを使う会話は
/// NpcTalkPoint を使い、こちらはシナリオを書くまでもない数行の会話用。
/// インタラクトするたびに次の行を TutorialMessageView (画面上部の案内枠) に出し、
/// 最後の行の次で閉じる。離れたら (_hideDistance) 途中でも閉じる。
/// TutorialMessageView がシーンに無ければ Notifier (トースト) に全行を流す。
/// _talkedFlag を設定すると「話したことがある」が永続化され、2回目以降は _repeatLines (空なら _lines) を話す。
/// _rewardThread を設定すると初回の会話を最後まで聞いた時に糸を渡す (_talkedFlag があれば一度きり)。
/// 配置: SpriteRenderer (任意) + トリガー Collider2D を Interactable レイヤー (11) に置く。
/// </summary>
public class NpcTalk : MonoBehaviour, IInteractable
{
    [Tooltip("話者名 (任意)。設定すると「名前「セリフ」」の形で表示する")]
    [SerializeField] private string _speakerName = "";

    [Tooltip("初回のセリフ (1要素 = 1回の表示)")]
    [SerializeField, TextArea] private string[] _lines = new string[0];

    [Tooltip("2回目以降のセリフ (任意)。空なら毎回 _lines を話す")]
    [SerializeField, TextArea] private string[] _repeatLines = new string[0];

    [Tooltip("話したことの永続化フラグ (GameProgress, 任意)。例: A1_Npc_Doll")]
    [SerializeField] private string _talkedFlag = "";

    [Tooltip("初回の会話を最後まで聞いた時に渡す糸の数 (0 なら無し)")]
    [SerializeField] private int _rewardThread;

    [Tooltip("プロンプトに表示するテキスト")]
    [SerializeField] private string _promptText = "話す";

    [Tooltip("会話中のプロンプト")]
    [SerializeField] private string _nextPromptText = "次へ";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("プレイヤーがこの距離より離れたら会話を閉じる")]
    [SerializeField] private float _hideDistance = 3f;

    [Tooltip("表示先。未設定なら同じシーン → UI シーン (StageUI) の順に検索する")]
    [SerializeField] private TutorialMessageView _message;

    private string[] _current; // 会話中のセリフ (null なら会話していない)
    private int _index;
    private bool _firstTalk;
    private bool _rewardGiven;  // _talkedFlag なしの時の、このシーン内での一度きり判定
    private Transform _listener;

    public string PromptText => _current != null ? _nextPromptText : _promptText;
    public bool CanInteract => _lines != null && _lines.Length > 0;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    /// <summary>
    /// 表示先を解決する (同じシーンの旧 SceneUI → UI シーン StageUI の順)。UI シーンはステージより後に
    /// ロードされることがある (ステージを直接開いて再生した場合) ので、Awake ではなく話しかけた時に探す。
    /// </summary>
    private void ResolveMessage()
    {
        if (_message == null)
            _message = UIViewLookup.Find<TutorialMessageView>(this);
    }

    private void Update()
    {
        if (_current == null || _listener == null)
            return;

        if (Vector2.Distance(_listener.position, transform.position) > _hideDistance)
            EndTalk(completed: false);
    }

    public void Interact(GameObject interactor)
    {
        if (_current == null)
        {
            BeginTalk(interactor);
            return;
        }

        _index++;
        if (_index >= _current.Length)
        {
            EndTalk(completed: true);
            return;
        }

        Show(_current[_index]);
    }

    private void BeginTalk(GameObject interactor)
    {
        var talked = !string.IsNullOrEmpty(_talkedFlag) && GameProgress.Has(_talkedFlag);
        _firstTalk = !talked;
        _current = talked && _repeatLines != null && _repeatLines.Length > 0 ? _repeatLines : _lines;
        _index = 0;
        _listener = interactor.transform;
        ResolveMessage();

        // 表示枠が無いシーンでは全行をトーストで流して終わる
        if (_message == null)
        {
            foreach (var line in _current)
                Notifier.Notify(Format(line));
            EndTalk(completed: true);
            return;
        }

        Show(_current[0]);
    }

    private void EndTalk(bool completed)
    {
        var listener = _listener;
        _current = null;
        _listener = null;

        if (_message != null)
            _message.Hide();

        if (!completed || !_firstTalk)
            return;

        if (_rewardThread > 0 && !_rewardGiven && listener != null
            && listener.TryGetComponent<PlayerItemInventory>(out var inventory))
        {
            inventory.AddThread(_rewardThread); // 通知は AddThread が出す
            _rewardGiven = true;
        }

        if (!string.IsNullOrEmpty(_talkedFlag))
            GameProgress.Set(_talkedFlag);
    }

    private void Show(string line)
    {
        if (_message != null)
            _message.Show(Format(line));
    }

    private string Format(string line) =>
        string.IsNullOrEmpty(_speakerName) ? line : $"{_speakerName}「{line}」";

    private void OnDisable()
    {
        if (_current != null)
            EndTalk(completed: false);
    }
}
