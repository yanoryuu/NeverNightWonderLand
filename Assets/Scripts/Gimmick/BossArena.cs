using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 中ボス戦エリア。プレイヤーがトリガーに入るとゲート (壁) を閉じてエリアから出られなくし、
/// ボスの撃破 (または撤退 = EnemyController.OnRetreated) でゲートを開放する。撃破は GameProgress のフラグで永続化されるため、
/// 再訪時 (拠点で休んだ後も) はボスが出現せずゲートも開いたまま。
/// トリガーの Collider2D はゲートより内側のエリアを覆うように配置する。
/// ゲートは非アクティブで配置しておき、入場時に有効化される。
/// 任意: _followUpMessage を設定すると、撃破通知に続けてもう 1 つ通知を出す (世界観の一文。例: 鍛冶屋への誘導)。
/// _followUpUnlessUpgrade を true にすると、プレイヤーが _followUpUpgrade のハサミを既に持っている時は出さない。
/// 入場から撃破 (撤退) までの間、BossBattleModel にボスを登録して HUD の画面上にボス HP バーを出す (枠は _hpFrame)。
/// HP は HUD に出すので、ボス本人の頭上の HP ゲージ (赤) は隠す (防御値ゲージは残す。手下の頭上ゲージはそのまま)。
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

    [Tooltip("撃破通知に続けて出す通知 (任意, 世界観の一文)。空なら出さない")]
    [SerializeField] private string _followUpMessage = "";

    [Tooltip("true なら、プレイヤーが _followUpUpgrade を既に持っている時は _followUpMessage を出さない")]
    [SerializeField] private bool _followUpUnlessUpgrade;

    [Tooltip("_followUpUnlessUpgrade の判定に使うハサミの色")]
    [SerializeField] private ScissorUpgrade _followUpUpgrade = ScissorUpgrade.Red;

    [Tooltip("HUD のボス HP バーの枠 (1戦目 = First / 2戦目 = Last)")]
    [SerializeField] private BossHpFrame _hpFrame = BossHpFrame.First;

    private bool _started;
    private PlayerController _player;
    private BossBattleModel _battle;

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
        // 撤退 (倒れずに飛び去る。グラミネ第1のボスの 1 戦目) も撃破と同じく勝ち扱いにする
        _boss.OnRetreated += OnBossDied;

        // ボスの HP は HUD のバーに出すので、頭上の赤いゲージは二重になる。ボス本人のゲージだけ隠す
        foreach (var gauge in _boss.GetComponentsInChildren<EnemyStatusGaugeView>(true))
        {
            if (gauge.Target == _boss)
                gauge.HideHpGauge();
        }

        // ボスの糸は闘技場 (このトリガーの範囲) いっぱいに散らす (PendingChanges 25)
        var b = GetComponent<Collider2D>().bounds;
        _boss.LootArea = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_started || !enabled || _boss == null)
            return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        _player = player;
        _started = true;
        SetGatesClosed(true);

        _battle = ResolveBattleModel();
        _battle?.Begin(_boss, _hpFrame);
    }

    private void OnBossDied(EnemyController _)
    {
        GameProgress.Set(_flagId);
        SetGatesClosed(false);
        EndBattle();
        Notifier.Notify(_defeatMessage);

        if (string.IsNullOrEmpty(_followUpMessage))
            return;
        if (_followUpUnlessUpgrade && _player != null
            && _player.TryGetComponent<PlayerProgression>(out var progression) && progression.Has(_followUpUpgrade))
            return;
        Notifier.Notify(_followUpMessage);
    }

    private void OnDestroy()
    {
        // 戦闘中にシーンを離れた (力尽きて再ロード等) 時もバーを下げる
        EndBattle();
    }

    private void EndBattle()
    {
        _battle?.End(_boss);
        _battle = null;
    }

    /// <summary>ルート (GameLifetimeScope) から BossBattleModel を引く。DI が無いシーンでは null (バーを出さない)。</summary>
    private static BossBattleModel ResolveBattleModel()
    {
        var settings = VContainerSettings.Instance;
        var root = settings != null ? settings.GetOrCreateRootLifetimeScopeInstance() : null;
        if (root == null || root.Container == null)
            return null;

        return root.Container.TryResolve(typeof(BossBattleModel), out var model) ? (BossBattleModel)model : null;
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
