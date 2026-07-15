using UnityEngine;

/// <summary>
/// エリアのゴール。プレイヤーが触れるとリザルト画面 (撃破数・糸・タイム) を表示する。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GoalZone : MonoBehaviour
{
    private bool _fired;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_fired)
            return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        var result = Object.FindFirstObjectByType<ResultView>();
        if (result == null)
            return;

        _fired = true;
        result.Show(player.gameObject);
    }
}
