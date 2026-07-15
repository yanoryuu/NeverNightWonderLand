using UnityEngine;

/// <summary>
/// 拠点 (セーブポイント)。インタラクトで拠点メニュー
/// (HP全回復 / アイテム補充 / アイテム切替 / セーブ) を開く。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SavePoint : MonoBehaviour, IInteractable
{
    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    public string PromptText => "休む";
    public bool CanInteract => true;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        var menu = Object.FindFirstObjectByType<SavePointMenuView>();
        if (menu == null)
        {
            Debug.LogWarning("[SavePoint] SavePointMenuView がシーンにありません。", this);
            return;
        }

        menu.Open(interactor, this);
    }
}
