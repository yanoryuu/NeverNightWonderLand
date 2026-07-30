using UnityEngine;

/// <summary>
/// マップに配置して拾えるアイテムの抽象基底。レバーや拠点と同じインタラクト (上入力 / [E]) で取得する。
/// 中身は派生クラスの <see cref="OnPickedUp"/> が決める (糸 / 消耗品 / 装備品 / キーアイテム など)。
/// 新しい種類はこのクラスを継承して OnPickedUp を書くだけで追加できる。
/// 配置: SpriteRenderer + トリガー Collider2D を Interactable レイヤーに置く。
/// <see cref="_pickupId"/> を設定すると GameProgress で永続化され、一度拾うと再訪しても出現しない
/// (空ならシーンに入り直すたびに復活する)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public abstract class ItemPickup : MonoBehaviour, IInteractable
{
    [Tooltip("取得の永続化 Id (GameProgress)。空ならシーン再訪で復活する。例: Pickup_Maze_Thread1")]
    [SerializeField] private string _pickupId = "";

    [Tooltip("プロンプトに表示するテキスト")]
    [SerializeField] private string _promptText = "拾う";

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1f, 0f);

    private bool _pickedUp;

    public string PromptText => _promptText;
    public bool CanInteract => !_pickedUp;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    protected virtual void Awake()
    {
        // 取得済みなら出現させない
        if (!string.IsNullOrEmpty(_pickupId) && GameProgress.Has(_pickupId))
            Destroy(gameObject);
    }

    public void Interact(GameObject interactor)
    {
        if (_pickedUp)
            return;

        // 中身の付与は派生クラスに委譲する。失敗 (持ちきれない等) なら拾わず残す
        if (!OnPickedUp(interactor))
            return;

        _pickedUp = true;
        if (!string.IsNullOrEmpty(_pickupId))
            GameProgress.Set(_pickupId);

        Destroy(gameObject);
    }

    /// <summary>
    /// 取得時の効果を実装する (通知も派生側で出す)。
    /// 拾えた場合は true、拾えない場合 (持ちきれない等) は false を返す。
    /// </summary>
    protected abstract bool OnPickedUp(GameObject interactor);
}
