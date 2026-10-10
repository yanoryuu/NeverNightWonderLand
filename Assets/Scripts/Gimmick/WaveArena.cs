using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 敵の波を順に出す闘技場 (BossArena の複数ウェーブ版)。プレイヤーがトリガーに入るとゲートを閉じ、
/// ウェーブを1つずつ出して、そのウェーブの敵が全滅したら次のウェーブへ進む。
/// 全ウェーブを倒すと進行フラグを立ててゲートを開く (ご褒美は FlagActivator で同じフラグに連動させる)。
/// フラグが立っていれば (クリア済み) 何も起きず、ゲートも開いたまま。
/// 各ウェーブの敵は「プレハブを生成 (_spawns)」と「シーンに非アクティブで置いた敵を有効化 (_preplaced)」の
/// どちらでも (併用も) よい。闘技場の敵は撃破記録 (DefeatedEnemyRegistry) の対象外にするので、
/// 途中で死んでやり直しても全ウェーブが出直す。
/// トリガーの Collider2D はゲートより内側のエリアを覆うように配置し、ゲートは非アクティブで置いておく。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaveArena : MonoBehaviour
{
    [Serializable]
    public class SpawnEntry
    {
        [Tooltip("生成する敵のプレハブ (EnemyController 付き。Enemy.prefab / FlyingEnemy.prefab など)")]
        public GameObject Prefab;

        [Tooltip("敵タイプの定数アセット (null ならプレハブの設定のまま)")]
        public EnemyConsts Consts;

        [Tooltip("敵の色")]
        public Color Tint = Color.white;

        [Tooltip("出現位置。未設定なら闘技場の位置 + _offset")]
        public Transform Point;

        [Tooltip("Point 未設定時の出現位置 (闘技場からのオフセット)")]
        public Vector2 Offset;

        [Tooltip("大きさの倍率 (1 で等倍。仮ボスは 2)")]
        public float Scale = 1f;
    }

    [Serializable]
    public class Wave
    {
        [Tooltip("このウェーブで生成する敵")]
        public SpawnEntry[] Spawns = new SpawnEntry[0];

        [Tooltip("このウェーブで有効化する、シーンに非アクティブで置いた敵")]
        public GameObject[] Preplaced = new GameObject[0];

        [Tooltip("このウェーブを出すまでの待ち時間 (sec, 前のウェーブ全滅から)")]
        public float Delay = 1f;
    }

    [Tooltip("クリアの進行フラグ (GameProgress)。例: A1_ArenaCups")]
    [SerializeField] private string _flagId = "";

    [Tooltip("ウェーブの並び")]
    [SerializeField] private Wave[] _waves = new Wave[0];

    [Tooltip("入場時に閉じるゲート (非アクティブで配置しておく)")]
    [SerializeField] private GameObject[] _gates = new GameObject[0];

    [Tooltip("開始時の通知 (任意)")]
    [SerializeField] private string _startMessage = "";

    [Tooltip("全ウェーブ撃破時の通知")]
    [SerializeField] private string _clearMessage = "扉が開いた";

    private readonly List<EnemyController> _alive = new();
    private bool _started;
    private bool _waitingNextWave;
    private int _waveIndex = -1;

    /// <summary>全ウェーブを倒した時に発火する。</summary>
    public event Action OnCleared;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        // クリア済みなら何もしない (ゲートは開いたまま)
        if (GameProgress.Has(_flagId))
        {
            SetGatesClosed(false);
            enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_started || !enabled)
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        _started = true;
        SetGatesClosed(true);
        Notifier.Notify(_startMessage);
        StartNextWaveAfterDelay().Forget();
    }

    private void Update()
    {
        if (!_started || _waitingNextWave)
            return;

        // 撃破 (OnDied) 以外で消えた敵 (奈落に落ちた等) も全滅判定に数える
        _alive.RemoveAll(e => e == null || e.IsDead);
        if (_alive.Count > 0)
            return;

        if (_waveIndex + 1 < _waves.Length)
            StartNextWaveAfterDelay().Forget();
        else
            Clear();
    }

    private async UniTaskVoid StartNextWaveAfterDelay()
    {
        _waitingNextWave = true;
        _waveIndex++;

        var wave = _waveIndex < _waves.Length ? _waves[_waveIndex] : null;
        if (wave != null && wave.Delay > 0f)
        {
            var cancelled = await UniTask.Delay(TimeSpan.FromSeconds(wave.Delay),
                    cancellationToken: this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();
            if (cancelled)
                return;
        }

        if (wave != null)
            SpawnWave(wave);

        _waitingNextWave = false;
    }

    private void SpawnWave(Wave wave)
    {
        if (wave.Spawns != null)
        {
            foreach (var entry in wave.Spawns)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                var pos = entry.Point != null
                    ? entry.Point.position
                    : transform.position + (Vector3)entry.Offset;
                var go = Instantiate(entry.Prefab, pos, Quaternion.identity);
                if (!Mathf.Approximately(entry.Scale, 1f))
                    go.transform.localScale *= entry.Scale;

                var enemy = go.GetComponent<EnemyController>();
                if (enemy == null)
                {
                    Debug.LogError($"[{nameof(WaveArena)}] Prefab に EnemyController がありません。", this);
                    Destroy(go);
                    continue;
                }

                enemy.ApplyProfile(entry.Consts, entry.Tint);
                Track(enemy);
            }
        }

        if (wave.Preplaced != null)
        {
            foreach (var go in wave.Preplaced)
            {
                if (go == null)
                    continue;

                go.SetActive(true);

                // 撃破記録済みの敵は Awake で自分を消すので、出なかったものとして扱う
                if (!go.activeSelf || !go.TryGetComponent<EnemyController>(out var enemy))
                    continue;

                Track(enemy);
            }
        }
    }

    private void Track(EnemyController enemy)
    {
        enemy.SetDefeatTracking(false); // やり直しで再出現させるため、撃破記録の対象外にする
        _alive.Add(enemy);
    }

    private void Clear()
    {
        _started = false;
        enabled = false;

        GameProgress.Set(_flagId);
        SetGatesClosed(false);
        Notifier.Notify(_clearMessage);
        OnCleared?.Invoke();
    }

    private void SetGatesClosed(bool closed)
    {
        if (_gates == null)
            return;

        foreach (var gate in _gates)
        {
            if (gate != null)
                gate.SetActive(closed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        if (_waves == null)
            return;

        foreach (var wave in _waves)
        {
            if (wave?.Spawns == null)
                continue;

            foreach (var entry in wave.Spawns)
            {
                if (entry == null)
                    continue;

                var pos = entry.Point != null ? entry.Point.position : transform.position + (Vector3)entry.Offset;
                Gizmos.DrawWireCube(pos, Vector3.one);
            }
        }
    }
}
