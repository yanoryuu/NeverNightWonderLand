using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 敵の無限湧きスポナー。開始時に1体スポーンし、倒されると一定時間後に同じ場所へ再出現させる。
/// テストシーンで各敵タイプを何度でも試せるようにするための仕組み。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("スポーンする敵の Prefab")]
    [SerializeField] private GameObject _enemyPrefab;

    [Tooltip("敵タイプの定数アセット (null なら Prefab 設定のまま)")]
    [SerializeField] private EnemyConsts _consts;

    [Tooltip("敵の色 (タイプの見分け用)")]
    [SerializeField] private Color _tint = Color.white;

    [Tooltip("倒されてから再出現までの時間 (sec)")]
    [SerializeField] private float _respawnDelay = 2f;

    private EnemyController _current;

    private void Start()
    {
        Spawn();
    }

    private void Spawn()
    {
        if (_enemyPrefab == null || _current != null)
            return;

        var go = Instantiate(_enemyPrefab, transform.position, Quaternion.identity);
        _current = go.GetComponent<EnemyController>();
        if (_current == null)
        {
            Debug.LogError($"[{nameof(EnemySpawner)}] Prefab に EnemyController がありません。", this);
            Destroy(go);
            return;
        }

        _current.ApplyProfile(_consts, _tint);
        _current.OnDied += OnEnemyDied;
    }

    private void OnEnemyDied(EnemyController enemy)
    {
        enemy.OnDied -= OnEnemyDied;
        _current = null;
        RespawnAfterDelay().Forget();
    }

    private async UniTaskVoid RespawnAfterDelay()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_respawnDelay),
            cancellationToken: this.GetCancellationTokenOnDestroy());
        Spawn();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0f));
    }
}
