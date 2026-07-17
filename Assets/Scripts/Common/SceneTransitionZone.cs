using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// プレイヤーが触れると指定ステージへ遷移するゾーン (エリアの出口)。
/// 遷移先の入り口は <see cref="_entranceId"/> で指定し、遷移先ステージ内の
/// 同じ Id を持つ <see cref="PlayerSpawnPoint"/> から開始する (メトロイドヴァニアの双方向ドア)。
/// 入り口スポーンがこのゾーンと重なっていても、一度離れるまでは発火しない (アーミング)。
/// PlayerScene 方式ではステージだけを Additive で入れ替える (ステータス維持)。
/// ※ 遷移先シーンは Build Settings に登録されている必要がある。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneTransitionZone : MonoBehaviour
{
    [Tooltip("遷移先のシーン名")]
    [SerializeField] private string _sceneName;

    [Tooltip("遷移先ステージの入り口 (PlayerSpawnPoint) の Id。空ならデフォルト入口")]
    [SerializeField] private string _entranceId = "";

    private Collider2D _collider;
    private bool _armed;
    private bool _fired;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    private void Start()
    {
        // スポーン位置がこのゾーン内にある場合は、一度離れるまで発火させない
        // (双方向ドアで即逆戻りするのを防ぐ)。プレイヤーの配置は StageLoader が
        // シーンアクティベーション時に済ませているため、この判定は最終位置で行われる
        _armed = !IsPlayerInside();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_fired || !_armed || string.IsNullOrEmpty(_sceneName))
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        _fired = true;
        GamePause.Reset();

        if (StageLoader.Instance != null)
            StageLoader.Instance.TransitionTo(_sceneName, _entranceId); // ステージのみ入替 (ステータス維持)
        else
            SceneManager.LoadScene(_sceneName);                        // 旧方式 (サンプルシーン)
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_armed)
            return;

        if (other.GetComponentInParent<PlayerController>() != null)
            _armed = true;
    }

    /// <summary>プレイヤーがこのゾーンに重なっているか (アーミングの初期判定用)。</summary>
    private bool IsPlayerInside()
    {
        var results = new Collider2D[8];
        var count = _collider.OverlapCollider(new ContactFilter2D().NoFilter(), results);
        for (var i = 0; i < count; i++)
        {
            if (results[i] != null && results[i].GetComponentInParent<PlayerController>() != null)
                return true;
        }

        return false;
    }
}
