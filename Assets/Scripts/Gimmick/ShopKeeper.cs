using UnityEngine;

/// <summary>
/// ショップの店主 NPC。インタラクトでショップ画面 (ShopView) を開く。
/// ShopView は同シーンの SceneUI に事前配置しておく (未設定ならシーンから検索する)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShopKeeper : MonoBehaviour, IInteractable
{
    [Tooltip("開くショップ画面。未設定ならシーンから検索する")]
    [SerializeField] private ShopView _shop;

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    public string PromptText => "買い物";
    public bool CanInteract => true;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        var shop = _shop != null ? _shop : Object.FindFirstObjectByType<ShopView>();
        if (shop == null)
        {
            Debug.LogWarning($"[{nameof(ShopKeeper)}] ShopView が見つかりません。", this);
            return;
        }

        shop.Open(interactor);
    }
}
