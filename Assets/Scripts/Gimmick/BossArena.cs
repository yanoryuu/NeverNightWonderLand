using UnityEngine;

/// <summary>
/// 中ボス戦エリア。プレイヤーがトリガーに入るとゲート (壁) を閉じてエリアから出られなくし、
/// ボスの撃破でゲートを開放する。撃破は GameProgress のフラグで永続化されるため、
/// 再訪時 (拠点で休んだ後も) はボスが出現せずゲートも開いたまま。
/// トリガーの Collider2D はゲートより内側のエリアを覆うように配置する。
/// ゲートは非アクティブで配置しておき、入場時に有効化される。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossArena : MonoBehaviour
{
    [Tooltip("撃破の進行フラグ名 (GameProgress で管理・セーブされる)")]
    [SerializeField] private string _flagId = "MidBoss1";

    [Tooltip("このエリアのボス")]
    [SerializeField] private EnemyController _boss;

    [Tooltip("入場時に閉じるゲート (非アクティブで配置しておく)")]
    [SerializeField] private GameObject[] _gates;

    [Tooltip("撃破時の通知メッセージ")]
    [SerializeField] private string _defeatMessage = "扉が開いた";

    private bool _started;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        // 撃破済みならボスを出さず、ゲートも開いたままにする
        if (GameProgress.Has(_flagId))
        {
            if (_boss != null)
                Destroy(_boss.gameObject);

            SetGatesClosed(false);
            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled || _boss == null)
            return;

        _boss.OnDied += OnBossDied;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_started || !enabled || _boss == null)
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        _started = true;
        SetGatesClosed(true);
    }

    private void OnBossDied(EnemyController _)
    {
        GameProgress.Set(_flagId);
        SetGatesClosed(false);
        Notifier.Notify(_defeatMessage);
    }

    private void SetGatesClosed(bool closed)
    {
        foreach (var gate in _gates)
        {
            if (gate != null)
                gate.SetActive(closed);
        }
    }
}
