using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// プレイヤーが触れると指定シーンへ遷移するゾーン (エリアの出口)。
/// ※ 遷移先シーンは Build Settings に登録されている必要がある。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneTransitionZone : MonoBehaviour
{
    [Tooltip("遷移先のシーン名")]
    [SerializeField] private string _sceneName;

    private bool _fired;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_fired || string.IsNullOrEmpty(_sceneName))
            return;

        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        _fired = true;
        GamePause.Reset();
        SceneManager.LoadScene(_sceneName);
    }
}
