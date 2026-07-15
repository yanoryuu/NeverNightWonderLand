using UnityEngine;

/// <summary>
/// チュートリアルの手順達成を判定するゾーン。プレイヤーが通過すると Passed になる。
/// トリガーの Collider2D を付けて配置し、TutorialManager が待機条件に使う。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TutorialStepTrigger : MonoBehaviour
{
    public bool Passed { get; private set; }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Passed)
            return;

        if (other.GetComponentInParent<PlayerController>() != null)
            Passed = true;
    }
}
