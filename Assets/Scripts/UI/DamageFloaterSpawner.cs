using UnityEngine;

/// <summary>
/// DamageEvents を購読してダメージ数値フローターを生成する。シーンに1つ置く。
/// 対象(敵・箱)ごとの配線は不要で、ダメージを与えた位置に自動で数値が出る。
/// </summary>
public class DamageFloaterSpawner : MonoBehaviour
{
    [Tooltip("生成するフローターの Prefab")]
    [SerializeField] private DamageFloater _floaterPrefab;

    [Tooltip("同時ヒット時に数字が重ならないようにするランダムオフセットの半径")]
    [SerializeField] private float _scatterRadius = 0.3f;

    private void OnEnable()
    {
        DamageEvents.OnDamageApplied += Spawn;
    }

    private void OnDisable()
    {
        DamageEvents.OnDamageApplied -= Spawn;
    }

    private void Spawn(Vector2 position, int amount, DamageEvents.Kind kind)
    {
        if (_floaterPrefab == null)
            return;

        var offset = (Vector2)(Random.insideUnitCircle * _scatterRadius);
        var floater = Instantiate(_floaterPrefab, position + offset + Vector2.up * 0.5f, Quaternion.identity);
        floater.Play(amount, kind);
    }
}
