using R3;
using UnityEngine;

/// <summary>
/// プレイヤー近傍のインタラクト対象を検出し、E 入力で実行する。
/// ステートマシンの外で完結する(移動やジャンプを阻害しない)。
/// 現在の対象は ReactiveProperty で公開し、InteractPromptView がプロンプト表示に使う。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInteractor : MonoBehaviour
{
    private PlayerController _controller;

    private readonly ReactiveProperty<IInteractable> _current = new(null);

    /// <summary>現在インタラクト可能な最寄りの対象 (無ければ null)。</summary>
    public ReadOnlyReactiveProperty<IInteractable> Current => _current;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void OnDestroy()
    {
        _current.Dispose();
    }

    private void Update()
    {
        if (_controller.IsDead)
        {
            _current.Value = null;
            return;
        }

        _current.Value = FindNearestInteractable();

        if (_current.Value != null && _controller.TryConsumeInteract())
            _current.Value.Interact(gameObject);
    }

    /// <summary>
    /// インタラクト範囲内にある T (例: SavePoint) を探す (最寄りでなくてもよい。無ければ null)。
    /// ポーズの「セーブ」の有効判定 (拠点の近くか) に使う。
    /// </summary>
    public T FindInRange<T>() where T : class
    {
        foreach (var hit in OverlapInteractRange())
        {
            var found = hit.GetComponentInParent<T>();
            if (found != null)
                return found;
        }

        return null;
    }

    private Collider2D[] OverlapInteractRange()
    {
        var consts = _controller.Consts;
        return Physics2D.OverlapCircleAll(transform.position, consts.InteractRadius, consts.InteractableLayer);
    }

    private IInteractable FindNearestInteractable()
    {
        var hits = OverlapInteractRange();

        IInteractable nearest = null;
        var nearestSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract)
                continue;

            var sqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = interactable;
            }
        }

        return nearest;
    }
}
