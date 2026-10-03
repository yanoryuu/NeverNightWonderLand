using UnityEngine;

/// <summary>
/// ショップの店主 NPC。インタラクトでショップ画面 (ShopView) を開く。
/// ShopView は UI シーン (ShopUI) に事前配置されている (未設定なら同じシーン → UI シーンの順に検索する。
/// GameField_old の旧拠点は自前の SceneUI に ShopView を持っており、そちらが優先される)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShopKeeper : MonoBehaviour, IInteractable
{
    [Tooltip("開くショップ画面。未設定なら同じシーン → UI シーン (ShopUI) の順に検索する")]
    [SerializeField] private ShopView _shop;

    [Tooltip("プロンプト表示位置のオフセット")]
    [SerializeField] private Vector3 _promptOffset = new Vector3(0f, 1.2f, 0f);

    public string PromptText => "買い物";
    public bool CanInteract => true;
    public Vector3 PromptAnchor => transform.position + _promptOffset;

    public void Interact(GameObject interactor)
    {
        var shop = _shop != null ? _shop : UIViewLookup.Find<ShopView>(this);
        if (shop == null)
        {
            Debug.LogWarning($"[{nameof(ShopKeeper)}] ShopView が見つかりません。", this);
            return;
        }

        shop.Open(interactor);
    }
}
