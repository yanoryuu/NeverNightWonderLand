using UnityEngine;

/// <summary>
/// 拠点 (セーブポイント)。インタラクトでホーム画面
/// (HP全回復 / アイテム補充 / 攻撃方法の入れ替え / セーブ) を開く。
/// ホーム画面は HomeUI シーン (Additive) 側にあり、FindFirstObjectByType で見つける。
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
        var home = Object.FindFirstObjectByType<HomeUIView>();
        if (home == null)
        {
            Debug.LogWarning("[SavePoint] HomeUIView が見つかりません。HomeUI シーンがロードされているか確認してください。", this);
            return;
        }

        home.Open(interactor, this);
    }
}
