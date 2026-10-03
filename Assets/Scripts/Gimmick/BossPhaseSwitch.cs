using System.Collections;
using UnityEngine;

/// <summary>
/// ボス戦の段階 (前半/後半) を地形で切り替える。ボス (EnemyController) の HP が
/// 最大値 × _threshold 以下になったら、前半の物 (_phase1: 浮遊する足場など) を点滅させてから消し、
/// 後半の物 (_phase2: せり上がる床など) を出す。ボスが倒れたら後半の物も消す。
/// 撃破済み (_defeatedFlag が立っている / ボスがいない) なら最初から両方を消す
/// (撃破後に出す物は FlagActivator で別に出す)。
/// 段階は保存しない (やり直すと前半から)。
/// </summary>
public class BossPhaseSwitch : MonoBehaviour
{
    [Tooltip("見張るボス")]
    [SerializeField] private EnemyController _boss;

    [Tooltip("撃破の進行フラグ (立っていれば最初から両段階の物を消す)")]
    [SerializeField] private string _defeatedFlag = "";

    [Tooltip("後半に移る HP の割合 (最大 HP に対して)")]
    [Range(0f, 1f)]
    [SerializeField] private float _threshold = 0.5f;

    [Tooltip("前半だけの物 (後半に移ると点滅して消える)")]
    [SerializeField] private GameObject[] _phase1 = new GameObject[0];

    [Tooltip("後半だけの物 (後半に移ると現れる。最初は非アクティブで置く)")]
    [SerializeField] private GameObject[] _phase2 = new GameObject[0];

    [Tooltip("前半の物が消える前の点滅時間 (sec)")]
    [SerializeField] private float _blinkTime = 1.5f;

    [Tooltip("後半に移った時の通知 (空なら出さない)")]
    [SerializeField] private string _phase2Message = "";

    private bool _inPhase2;
    private bool _done;
    private Coroutine _switching;

    public bool InPhase2 => _inPhase2;

    private void Start()
    {
        SetActive(_phase2, false);
        if ((!string.IsNullOrEmpty(_defeatedFlag) && GameProgress.Has(_defeatedFlag)) || _boss == null)
        {
            Finish();
            return;
        }

        SetActive(_phase1, true);
    }

    private void Update()
    {
        if (_done)
            return;

        if (_boss == null || _boss.IsDead)
        {
            Finish();
            return;
        }

        if (_inPhase2 || _boss.Consts == null)
            return;

        var limit = Mathf.CeilToInt(_boss.Consts.MaxHp * _threshold);
        if (_boss.Hp.CurrentValue <= limit)
        {
            _inPhase2 = true;
            _switching = StartCoroutine(SwitchToPhase2());
        }
    }

    private IEnumerator SwitchToPhase2()
    {
        if (!string.IsNullOrEmpty(_phase2Message))
            Notifier.Notify(_phase2Message);

        var renderers = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var go in _phase1)
            if (go != null)
                renderers.AddRange(go.GetComponentsInChildren<SpriteRenderer>());

        var t = 0f;
        while (t < _blinkTime)
        {
            var on = Mathf.Repeat(t * 8f, 1f) < 0.5f;
            foreach (var sr in renderers)
                if (sr != null)
                    sr.enabled = on;
            t += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in renderers)
            if (sr != null)
                sr.enabled = true;
        SetActive(_phase1, false);
        SetActive(_phase2, true);
        _switching = null;
    }

    private void Finish()
    {
        _done = true;
        if (_switching != null)
            StopCoroutine(_switching);
        SetActive(_phase1, false);
        SetActive(_phase2, false);
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        foreach (var go in objects)
            if (go != null)
                go.SetActive(active);
    }
}
